using System.Diagnostics;
using Serilog;
using SyncSound.Audio.Output;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Engine;

/// <summary>
/// Reusable multi-output audio engine with non-blocking feeder loop, fault isolation,
/// per-output delay compensation, and automatic endpoint recovery.
/// </summary>
public sealed class AudioEngine : IAudioEngine
{
    private readonly IAudioDeviceEnumerator _deviceEnumerator;
    private readonly IAudioOutputFactory _outputFactory;
    private readonly ILogger _logger;
    private readonly Lock _gate = new();

    private readonly List<OutputEntry> _entries = [];
    private IAudioSource? _activeSource;
    private AudioEngineConfiguration _configuration = new();
    private CancellationTokenSource? _feederCts;
    private Task? _feederTask;
    private DateTimeOffset? _startedAtUtc;
    private AudioEngineState _state = AudioEngineState.Idle;
    private string _userMessage = "Ready to listen.";
    private float _masterVolume = 1.0f;
    private bool _isPaused;
    private bool _disposed;

    public AudioEngine(
        IAudioDeviceEnumerator deviceEnumerator,
        IAudioOutputFactory? outputFactory = null,
        ILogger? logger = null)
    {
        _deviceEnumerator = deviceEnumerator ?? throw new ArgumentNullException(nameof(deviceEnumerator));
        _logger = logger ?? Log.Logger;
        _outputFactory = outputFactory ?? new WasapiAudioOutputFactory(logger: _logger);
        _deviceEnumerator.DeviceChanged += OnSystemDeviceChanged;
    }

    public AudioEngineState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public AudioEngineStatusSnapshot Status
    {
        get
        {
            lock (_gate)
            {
                return BuildSnapshotLocked();
            }
        }
    }

    public float MasterVolume
    {
        get
        {
            lock (_gate)
            {
                return _masterVolume;
            }
        }
    }

    public event EventHandler<AudioEngineStatusSnapshot>? StatusChanged;
    public event EventHandler? SourceCompleted;

    public async Task<AudioEngineStatusSnapshot> StartAsync(
        IAudioSource source,
        IReadOnlyList<AudioDevice> targetDevices,
        AudioEngineConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(targetDevices);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await StopAsync().ConfigureAwait(false);

        var uniqueTargets = targetDevices
            .Where(d => !string.IsNullOrWhiteSpace(d.Id))
            .GroupBy(d => d.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (uniqueTargets.Count == 0)
        {
            var emptySnapshot = SetState(AudioEngineState.Idle, source.DisplayName, [], "Choose a device to begin.");
            return emptySnapshot;
        }

        _configuration = configuration ?? new AudioEngineConfiguration();
        _masterVolume = Math.Clamp(_configuration.MasterVolume, 0.0f, 1.0f);
        _isPaused = false;

        SetState(AudioEngineState.Preparing, source.DisplayName, [], "Starting audio...");

        var entries = new List<OutputEntry>(uniqueTargets.Count);
        foreach (var device in uniqueTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries.Add(TryInitializeEntry(device, source.Format, _configuration));
        }

        var healthyEntries = entries.Where(e => e.Output is not null).ToList();
        if (healthyEntries.Count == 0)
        {
            DisposeEntries(entries);
            var errSnapshot = SetState(
                AudioEngineState.Error,
                source.DisplayName,
                entries.Select(e => e.ToSnapshot()).ToList(),
                "We couldn't start audio on the selected devices.");
            return errSnapshot;
        }

        var framesPerChunk = Math.Max(64, source.Format.SampleRate * _configuration.ChunkMs / 1000);
        var floatCount = framesPerChunk * source.Format.Channels;
        var sampleBuffer = new float[floatCount];

        // Pre-fill aligned startup chunks before starting the WASAPI renderers.
        for (var i = 0; i < Math.Max(1, _configuration.PrefillChunks); i++)
        {
            var read = ReadScaledSamples(source, sampleBuffer, _masterVolume);
            if (read <= 0)
            {
                break;
            }

            foreach (var entry in healthyEntries)
            {
                entry.Output!.WriteFloatSamples(sampleBuffer.AsSpan(0, read), source.Format);
            }
        }

        foreach (var entry in healthyEntries)
        {
            try
            {
                entry.Output!.Start();
                _logger.Information(
                    "Audio output active: {DeviceName} ({FormatSummary})",
                    entry.Device.Name,
                    entry.Output.FormatSummary);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to start output {DeviceName}", entry.Device.Name);
                MarkEntryFailed(entry, AudioOutputState.Failed, ex.Message);
            }
        }

        var cts = new CancellationTokenSource();
        AudioEngineStatusSnapshot startedSnapshot;

        lock (_gate)
        {
            _entries.Clear();
            _entries.AddRange(entries);
            _activeSource = source;
            _activeSource.EndOfStream += OnSourceEndOfStream;
            _activeSource.SourceFaulted += OnSourceFaulted;
            _feederCts = cts;
            _startedAtUtc = DateTimeOffset.UtcNow;

            RecomputeEngineStateLocked();
            startedSnapshot = BuildSnapshotLocked();
        }

        StatusChanged?.Invoke(this, startedSnapshot);

        if (_entries.Any(e => e.Output is not null && !e.Output.IsFaulted))
        {
            _feederTask = Task.Run(
                () => RunFeederLoopAsync(source, sampleBuffer, _configuration.ChunkMs, cts.Token),
                CancellationToken.None);
        }
        else
        {
            await StopAsync().ConfigureAwait(false);
        }

        return Status;
    }

    public Task PauseAsync()
    {
        AudioEngineStatusSnapshot? snapshot = null;

        lock (_gate)
        {
            if (_state is not (AudioEngineState.Playing or AudioEngineState.Partial or AudioEngineState.Recovering))
            {
                return Task.CompletedTask;
            }

            _isPaused = true;
            foreach (var entry in _entries)
            {
                entry.Output?.Pause();
            }

            _state = AudioEngineState.Paused;
            _userMessage = "Paused.";
            snapshot = BuildSnapshotLocked();
        }

        StatusChanged?.Invoke(this, snapshot);
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        AudioEngineStatusSnapshot? snapshot = null;

        lock (_gate)
        {
            if (_state != AudioEngineState.Paused)
            {
                return Task.CompletedTask;
            }

            _isPaused = false;
            foreach (var entry in _entries)
            {
                entry.Output?.Resume();
            }

            RecomputeEngineStateLocked();
            snapshot = BuildSnapshotLocked();
        }

        StatusChanged?.Invoke(this, snapshot);
        return Task.CompletedTask;
    }

    public Task StopAsync() => StopInternalAsync(waitForFeeder: true);

    private async Task StopInternalAsync(bool waitForFeeder)
    {
        CancellationTokenSource? cts;
        Task? feederTask;
        IAudioSource? source;
        List<OutputEntry> entriesToDispose;
        AudioEngineStatusSnapshot? stoppingSnapshot = null;

        lock (_gate)
        {
            if (_state == AudioEngineState.Idle && _feederTask is null && _entries.Count == 0)
            {
                return;
            }

            _state = AudioEngineState.Stopping;
            _userMessage = "Stopping...";
            stoppingSnapshot = BuildSnapshotLocked();

            cts = _feederCts;
            feederTask = waitForFeeder ? _feederTask : null;
            source = _activeSource;
            entriesToDispose = _entries.ToList();

            _feederCts = null;
            _feederTask = null;
            _activeSource = null;
            _entries.Clear();
            _startedAtUtc = null;
            _isPaused = false;
        }

        if (stoppingSnapshot is not null)
        {
            StatusChanged?.Invoke(this, stoppingSnapshot);
        }

        if (source is not null)
        {
            source.EndOfStream -= OnSourceEndOfStream;
            source.SourceFaulted -= OnSourceFaulted;
        }

        if (cts is not null)
        {
            await cts.CancelAsync().ConfigureAwait(false);
        }

        if (feederTask is not null)
        {
            try
            {
                await feederTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected cancellation on stop.
            }
        }

        cts?.Dispose();
        DisposeEntries(entriesToDispose);

        AudioEngineStatusSnapshot idleSnapshot;
        lock (_gate)
        {
            _state = AudioEngineState.Idle;
            _userMessage = "Ready to listen.";
            idleSnapshot = BuildSnapshotLocked();
        }

        StatusChanged?.Invoke(this, idleSnapshot);
    }

    public void SetMasterVolume(float volume)
    {
        lock (_gate)
        {
            _masterVolume = Math.Clamp(volume, 0.0f, 1.0f);
        }
    }

    public void SetOutputDelayMs(string deviceId, int delayMs)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        var clamped = Math.Clamp(delayMs, 0, 500);
        AudioEngineStatusSnapshot? snapshot = null;

        lock (_gate)
        {
            var entry = _entries.FirstOrDefault(e => string.Equals(e.Device.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (entry is not null)
            {
                entry.ConfiguredDelayMs = clamped;
                entry.Output?.SetDelayCompensationMs(clamped);
                snapshot = BuildSnapshotLocked();
            }
        }

        if (snapshot is not null)
        {
            StatusChanged?.Invoke(this, snapshot);
        }
    }

    public void SetOutputVolume(string deviceId, float volume)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        var clamped = Math.Clamp(volume, 0.0f, 1.0f);
        lock (_gate)
        {
            var entry = _entries.FirstOrDefault(e => string.Equals(e.Device.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            entry?.Output?.SetVolume(clamped);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _deviceEnumerator.DeviceChanged -= OnSystemDeviceChanged;
        await StopAsync().ConfigureAwait(false);
    }

    private async Task RunFeederLoopAsync(
        IAudioSource source,
        float[] sampleBuffer,
        int chunkMs,
        CancellationToken token)
    {
        var startTick = Stopwatch.GetTimestamp();
        var chunkTicks = Math.Max(1L, Stopwatch.Frequency * chunkMs / 1000);
        long chunkIndex = 0;
        var sourceEnded = false;

        try
        {
            while (!token.IsCancellationRequested)
            {
                bool paused;
                float volume;
                List<OutputEntry> activeOutputs;

                lock (_gate)
                {
                    paused = _isPaused;
                    volume = _masterVolume;
                    activeOutputs = _entries.Where(e => e.Output is not null && !e.Output.IsFaulted).ToList();
                }

                if (activeOutputs.Count == 0)
                {
                    // Stay alive only if recovering disconnected devices; otherwise stop.
                    bool anyRecovering;
                    lock (_gate)
                    {
                        anyRecovering = _configuration.AutoRecoverOutputs &&
                                        _entries.Any(e => e.LastKnownState == AudioOutputState.Disconnected);
                    }

                    if (!anyRecovering)
                    {
                        break;
                    }
                }
                else if (!paused)
                {
                    var read = ReadScaledSamples(source, sampleBuffer, volume);
                    if (read > 0)
                    {
                        var span = sampleBuffer.AsSpan(0, read);
                        foreach (var entry in activeOutputs)
                        {
                            entry.Output?.WriteFloatSamples(span, source.Format);
                        }
                    }

                    if (source.IsEnded)
                    {
                        sourceEnded = true;
                        break;
                    }
                }

                chunkIndex++;
                var nextTick = startTick + chunkIndex * chunkTicks;
                var remainingTicks = nextTick - Stopwatch.GetTimestamp();
                if (remainingTicks > 0)
                {
                    var delayMs = (int)(remainingTicks * 1000 / Stopwatch.Frequency);
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs, token).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on stop.
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "AudioEngine feeder loop terminated unexpectedly");
            lock (_gate)
            {
                _state = AudioEngineState.Error;
                _userMessage = "Audio stopped unexpectedly. Try again.";
            }

            StatusChanged?.Invoke(this, Status);
        }
        finally
        {
            if (sourceEnded && !token.IsCancellationRequested)
            {
                // Allow prebuffered samples to drain briefly before firing SourceCompleted.
                try
                {
                    await Task.Delay(120, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Ignore.
                }

                if (!token.IsCancellationRequested)
                {
                    await StopInternalAsync(waitForFeeder: false).ConfigureAwait(false);
                    SourceCompleted?.Invoke(this, EventArgs.Empty);
                }
            }
            else if (!token.IsCancellationRequested)
            {
                await StopInternalAsync(waitForFeeder: false).ConfigureAwait(false);
            }
        }
    }

    private static int ReadScaledSamples(IAudioSource source, float[] buffer, float volume)
    {
        var read = source.ReadSamples(buffer);
        if (read <= 0 || Math.Abs(volume - 1.0f) < 0.0001f)
        {
            return read;
        }

        for (var i = 0; i < read; i++)
        {
            buffer[i] *= volume;
        }

        return read;
    }

    private OutputEntry TryInitializeEntry(
        AudioDevice device,
        AudioPcmFormat format,
        AudioEngineConfiguration config)
    {
        var delayMs = 0;
        if (config.PerOutputDelayMs is not null &&
            config.PerOutputDelayMs.TryGetValue(device.Id, out var configuredDelay))
        {
            delayMs = Math.Clamp(configuredDelay, 0, 500);
        }

        try
        {
            var output = _outputFactory.CreateOutput(device, format, config);
            output.SetDelayCompensationMs(delayMs);
            output.Faulted += OnOutputFaulted;

            return new OutputEntry(device, output, delayMs);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to initialize audio output {DeviceName}", device.Name);
            return new OutputEntry(device, null, delayMs)
            {
                LastKnownState = AudioOutputState.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    private void OnSystemDeviceChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.DeviceId) || _disposed)
        {
            return;
        }

        var isDisconnected = e.ChangeKind == DeviceChangeKind.Removed ||
                             (e.ChangeKind == DeviceChangeKind.StateChanged &&
                              e.NewState is not null &&
                              e.NewState != AudioEndpointState.Active);

        if (isDisconnected)
        {
            HandleDeviceLost(e.DeviceId);
            return;
        }

        var isReconnected = e.ChangeKind == DeviceChangeKind.Added ||
                            (e.ChangeKind == DeviceChangeKind.StateChanged &&
                             e.NewState == AudioEndpointState.Active);

        if (isReconnected)
        {
            TryRecoverDevice(e.DeviceId);
        }
    }

    private void HandleDeviceLost(string deviceId)
    {
        IAudioOutput? removedOutput = null;
        AudioEngineStatusSnapshot? snapshot = null;

        lock (_gate)
        {
            var entry = _entries.FirstOrDefault(e => string.Equals(e.Device.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (entry is null || entry.Output is null)
            {
                return;
            }

            removedOutput = entry.Output;
            entry.Output = null;
            entry.LastKnownState = AudioOutputState.Disconnected;
            entry.ErrorMessage = "One headphone disconnected.";

            RecomputeEngineStateLocked();
            snapshot = BuildSnapshotLocked();
        }

        if (removedOutput is not null)
        {
            removedOutput.Faulted -= OnOutputFaulted;
            removedOutput.Dispose();
        }

        if (snapshot is not null)
        {
            StatusChanged?.Invoke(this, snapshot);
        }
    }

    private void TryRecoverDevice(string deviceId)
    {
        AudioDevice? deviceSnapshot = null;
        IAudioSource? activeSource = null;
        AudioEngineConfiguration configSnapshot;
        int configuredDelay = 0;

        lock (_gate)
        {
            if (!_configuration.AutoRecoverOutputs ||
                _activeSource is null ||
                _state is AudioEngineState.Idle or AudioEngineState.Stopping)
            {
                return;
            }

            var entry = _entries.FirstOrDefault(e => string.Equals(e.Device.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (entry is null || entry.Output is not null)
            {
                return;
            }

            deviceSnapshot = entry.Device;
            activeSource = _activeSource;
            configSnapshot = _configuration;
            configuredDelay = entry.ConfiguredDelayMs;
        }

        if (deviceSnapshot is null || activeSource is null)
        {
            return;
        }

        try
        {
            var newOutput = _outputFactory.CreateOutput(deviceSnapshot, activeSource.Format, configSnapshot);
            newOutput.SetDelayCompensationMs(configuredDelay);
            var prefillFrames = Math.Max(64, activeSource.Format.SampleRate * configSnapshot.ChunkMs / 1000);
            var prefillSilence = new float[prefillFrames * activeSource.Format.Channels];
            for (var i = 0; i < Math.Max(1, configSnapshot.PrefillChunks); i++)
            {
                newOutput.WriteFloatSamples(prefillSilence, activeSource.Format);
            }
            newOutput.Faulted += OnOutputFaulted;

            AudioEngineStatusSnapshot? snapshot = null;
            lock (_gate)
            {
                var entry = _entries.FirstOrDefault(e => string.Equals(e.Device.Id, deviceId, StringComparison.OrdinalIgnoreCase));
                if (entry is null || _activeSource is null)
                {
                    newOutput.Faulted -= OnOutputFaulted;
                    newOutput.Dispose();
                    return;
                }

                entry.Output = newOutput;
                entry.LastKnownState = AudioOutputState.Playing;
                entry.ErrorMessage = null;

                if (_isPaused)
                {
                    newOutput.Start();
                    newOutput.Pause();
                }
                else
                {
                    newOutput.Start();
                }

                RecomputeEngineStateLocked();
                snapshot = BuildSnapshotLocked();
            }

            _logger.Information("Recovered audio output {DeviceName}", deviceSnapshot.Name);
            StatusChanged?.Invoke(this, snapshot);
        }
        catch (Exception ex)
        {
            _logger.Debug(ex, "Device {DeviceName} not ready yet for recovery", deviceSnapshot.Name);
        }
    }

    private void OnOutputFaulted(object? sender, string? reason)
    {
        if (sender is not IAudioOutput output)
        {
            return;
        }

        AudioEngineStatusSnapshot? snapshot = null;

        lock (_gate)
        {
            var entry = _entries.FirstOrDefault(e => ReferenceEquals(e.Output, output));
            if (entry is null)
            {
                return;
            }

            entry.Output = null;
            entry.LastKnownState = AudioOutputState.Failed;
            entry.ErrorMessage = string.IsNullOrWhiteSpace(reason)
                ? "We couldn't start audio on one device."
                : reason;

            RecomputeEngineStateLocked();
            snapshot = BuildSnapshotLocked();
        }

        output.Faulted -= OnOutputFaulted;
        output.Dispose();

        if (snapshot is not null)
        {
            StatusChanged?.Invoke(this, snapshot);
        }
    }

    private void OnSourceEndOfStream(object? sender, EventArgs e)
    {
        _ = Task.Run(async () =>
        {
            await StopAsync().ConfigureAwait(false);
            SourceCompleted?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnSourceFaulted(object? sender, string reason)
    {
        _logger.Warning("Active audio source faulted: {Reason}", reason);
        _ = Task.Run(async () =>
        {
            await StopAsync().ConfigureAwait(false);
        });
    }

    private void MarkEntryFailed(OutputEntry entry, AudioOutputState state, string error)
    {
        if (entry.Output is not null)
        {
            entry.Output.Faulted -= OnOutputFaulted;
            entry.Output.Dispose();
            entry.Output = null;
        }

        entry.LastKnownState = state;
        entry.ErrorMessage = error;
    }

    private void RecomputeEngineStateLocked()
    {
        var activeCount = _entries.Count(e => e.Output is not null && !e.Output.IsFaulted);
        var disconnectedCount = _entries.Count(e => e.Output is null && e.LastKnownState == AudioOutputState.Disconnected);
        var failedCount = _entries.Count(e => e.Output is null && e.LastKnownState == AudioOutputState.Failed);

        if (_isPaused)
        {
            _state = AudioEngineState.Paused;
            _userMessage = "Paused.";
            return;
        }

        if (activeCount == 0 && disconnectedCount > 0 && _configuration.AutoRecoverOutputs)
        {
            _state = AudioEngineState.Recovering;
            _userMessage = "Reconnecting…";
            return;
        }

        if (activeCount == 0)
        {
            _state = AudioEngineState.Error;
            _userMessage = "Audio stopped unexpectedly. Try again.";
            return;
        }

        if (disconnectedCount > 0)
        {
            _state = AudioEngineState.Partial;
            _userMessage = "One headphone disconnected.";
            return;
        }

        if (failedCount > 0)
        {
            _state = AudioEngineState.Partial;
            _userMessage = "We couldn't start audio on one device.";
            return;
        }

        _state = AudioEngineState.Playing;
        _userMessage = "You're all listening.";
    }

    private AudioEngineStatusSnapshot SetState(
        AudioEngineState state,
        string sourceName,
        IReadOnlyList<AudioOutputSnapshot> outputs,
        string userMessage)
    {
        AudioEngineStatusSnapshot snapshot;
        lock (_gate)
        {
            _state = state;
            _userMessage = userMessage;
            snapshot = new AudioEngineStatusSnapshot(state, sourceName, outputs, userMessage, _startedAtUtc);
        }

        StatusChanged?.Invoke(this, snapshot);
        return snapshot;
    }

    private AudioEngineStatusSnapshot BuildSnapshotLocked()
    {
        var outputs = _entries.Select(e => e.ToSnapshot()).ToList();
        return new AudioEngineStatusSnapshot(
            _state,
            _activeSource?.DisplayName ?? string.Empty,
            outputs,
            _userMessage,
            _startedAtUtc);
    }

    private void DisposeEntries(IEnumerable<OutputEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.Output is not null)
            {
                entry.Output.Faulted -= OnOutputFaulted;
                try
                {
                    entry.Output.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Debug(ex, "Ignored output dispose error for {DeviceName}", entry.Device.Name);
                }
            }
        }
    }

    private sealed class OutputEntry
    {
        public OutputEntry(AudioDevice device, IAudioOutput? output, int configuredDelayMs)
        {
            Device = device;
            Output = output;
            ConfiguredDelayMs = configuredDelayMs;
            LastKnownState = output?.State ?? AudioOutputState.Failed;
        }

        public AudioDevice Device { get; }
        public IAudioOutput? Output { get; set; }
        public int ConfiguredDelayMs { get; set; }
        public AudioOutputState LastKnownState { get; set; }
        public string? ErrorMessage { get; set; }

        public AudioOutputSnapshot ToSnapshot()
        {
            if (Output is not null)
            {
                return Output.GetSnapshot();
            }

            return new AudioOutputSnapshot(
                Device.Id,
                Device.Name,
                LastKnownState,
                ConfiguredDelayMs: ConfiguredDelayMs,
                ErrorMessage: ErrorMessage);
        }
    }
}
