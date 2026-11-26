using System.Diagnostics;
using NAudio.Wave;
using Serilog;
using SyncSound.Audio.Generators;
using SyncSound.Audio.Output;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Playback;

/// <summary>
/// Multi-output test engine: generates an audible test signal and fans it out to
/// multiple independent WASAPI endpoints while isolating per-device failures.
/// </summary>
public sealed class MultiOutputTestEngine : IMultiOutputTestPlayer
{
    private const int SampleRate = 48000;
    private const int Channels = 2;
    private const int ChunkMs = 20;

    private readonly IAudioDeviceEnumerator _deviceEnumerator;
    private readonly IOutputBranchFactory _branchFactory;
    private readonly ILogger _logger;
    private readonly Lock _stateGate = new();
    private readonly List<IOutputBranch> _branches = [];
    private readonly List<OutputBranchStatus> _statuses = [];

    private CancellationTokenSource? _cts;
    private Task? _feederTask;
    private bool _isPlaying;
    private bool _disposed;

    public MultiOutputTestEngine(
        IAudioDeviceEnumerator deviceEnumerator,
        IOutputBranchFactory? branchFactory = null,
        ILogger? logger = null)
    {
        _deviceEnumerator = deviceEnumerator ?? throw new ArgumentNullException(nameof(deviceEnumerator));
        _logger = logger ?? Log.Logger;
        _branchFactory = branchFactory ?? new WasapiOutputBranchFactory(_logger);
        _deviceEnumerator.DeviceChanged += OnSystemDeviceChanged;
    }

    public bool IsPlaying
    {
        get
        {
            lock (_stateGate)
            {
                return _isPlaying;
            }
        }
    }

    public IReadOnlyList<OutputBranchStatus> ActiveBranches
    {
        get
        {
            lock (_stateGate)
            {
                return _statuses.ToList();
            }
        }
    }

    public event EventHandler<OutputBranchStatusChangedEventArgs>? OutputStatusChanged;
    public event EventHandler<string>? LogMessage;
    public event EventHandler<bool>? PlaybackStateChanged;

    public async Task<MultiOutputStartResult> StartAsync(
        IReadOnlyList<AudioDevice> targets,
        AudioTestMode mode,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await StopAsync().ConfigureAwait(false);

        var uniqueTargets = targets
            .Where(t => !string.IsNullOrWhiteSpace(t.Id))
            .GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (uniqueTargets.Count == 0)
        {
            return new MultiOutputStartResult(
                AnyStarted: false,
                BranchStatuses: [],
                SummaryMessage: "Choose a device to begin.");
        }

        var sourceFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);
        var createdBranches = new List<IOutputBranch>();
        var resultStatuses = new List<OutputBranchStatus>();

        foreach (var target in uniqueTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var branch = _branchFactory.CreateBranch(target, sourceFormat);
                branch.Faulted += OnBranchFaulted;
                createdBranches.Add(branch);

                var status = new OutputBranchStatus(
                    target.Id,
                    target.Name,
                    OutputBranchState.Playing,
                    branch.FormatSummary);
                resultStatuses.Add(status);
                RaiseStatusChanged(status);
                RaiseLog($"Initialized '{target.Name}' ({branch.FormatSummary}).");
            }
            catch (Exception ex)
            {
                var failedStatus = new OutputBranchStatus(
                    target.Id,
                    target.Name,
                    OutputBranchState.Failed,
                    ErrorMessage: ex.Message);
                resultStatuses.Add(failedStatus);
                RaiseStatusChanged(failedStatus);
                RaiseLog($"Failed to initialize '{target.Name}': {ex.Message}");
            }
        }

        if (createdBranches.Count == 0)
        {
            return new MultiOutputStartResult(
                AnyStarted: false,
                BranchStatuses: resultStatuses,
                SummaryMessage: "We couldn't start audio on the selected devices.");
        }

        var generator = new TestSignalSampleProvider(mode, SampleRate, Channels, gain: 0.42f);
        var framesPerChunk = SampleRate * ChunkMs / 1000;
        var floatCount = framesPerChunk * Channels;
        var floatBuffer = new float[floatCount];
        var byteBuffer = new byte[floatCount * sizeof(float)];
        var warmupZeroBytes = new byte[floatCount * sizeof(float)];

        // Pre-fill 120 ms warm-up silence (plus 40 ms signal) so wired USB-C DACs & Realtek amps wake
        // cleanly before the first chime attack hits, avoiding 80ms WASAPI initial pull starvation.
        for (var prefill = 0; prefill < 6; prefill++)
        {
            foreach (var branch in createdBranches)
            {
                branch.EnqueueFloatSamples(warmupZeroBytes, warmupZeroBytes.Length);
            }
        }

        for (var prefill = 0; prefill < 2; prefill++)
        {
            FillChunk(generator, floatBuffer, byteBuffer, floatCount);
            foreach (var branch in createdBranches)
            {
                branch.EnqueueFloatSamples(byteBuffer, byteBuffer.Length);
            }
        }

        _logger.Information(
            "Starting TestAudio ({Mode}) across {Count} branch(es): {Targets}",
            mode,
            createdBranches.Count,
            string.Join(", ", createdBranches.Select(b => $"{b.DeviceName} [{b.FormatSummary}]")));

        foreach (var branch in createdBranches)
        {
            try
            {
                branch.Start();
            }
            catch (Exception ex)
            {
                RaiseLog($"Failed starting '{branch.DeviceName}': {ex.Message}");
                HandleBranchFailure(branch.DeviceId, OutputBranchState.Failed, ex.Message);
            }
        }

        var cts = new CancellationTokenSource();
        lock (_stateGate)
        {
            _branches.Clear();
            _branches.AddRange(createdBranches.Where(b => !b.IsFaulted));
            _statuses.Clear();
            _statuses.AddRange(resultStatuses);
            _cts = cts;
            _isPlaying = _branches.Count > 0;
        }

        if (!_isPlaying)
        {
            cts.Dispose();
            DisposeBranches(createdBranches);
            return new MultiOutputStartResult(
                AnyStarted: false,
                BranchStatuses: resultStatuses,
                SummaryMessage: "We couldn't start audio on the selected devices.");
        }

        PlaybackStateChanged?.Invoke(this, true);
        _feederTask = Task.Run(() => FeederLoopAsync(generator, floatBuffer, byteBuffer, floatCount, cts.Token), CancellationToken.None);

        var activeCount = createdBranches.Count(b => !b.IsFaulted);
        var failedCount = resultStatuses.Count(s => s.State == OutputBranchState.Failed);
        var summary = failedCount == 0
            ? $"Playing {mode} on {activeCount} output(s)."
            : $"Playing on {activeCount} output(s); {failedCount} failed to start.";

        return new MultiOutputStartResult(true, resultStatuses, summary);
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        Task? feederTask;
        List<IOutputBranch> branchesToDispose;

        lock (_stateGate)
        {
            cts = _cts;
            feederTask = _feederTask;
            _cts = null;
            _feederTask = null;
            branchesToDispose = _branches.ToList();
            _branches.Clear();

            for (var i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].State == OutputBranchState.Playing)
                {
                    _statuses[i] = _statuses[i] with { State = OutputBranchState.Stopped };
                    RaiseStatusChanged(_statuses[i]);
                }
            }

            _isPlaying = false;
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
                // Expected on stop.
            }
        }

        cts?.Dispose();
        DisposeBranches(branchesToDispose);
        PlaybackStateChanged?.Invoke(this, false);
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

    private async Task FeederLoopAsync(
        TestSignalSampleProvider generator,
        float[] floatBuffer,
        byte[] byteBuffer,
        int floatCount,
        CancellationToken token)
    {
        var startTick = Stopwatch.GetTimestamp();
        var chunkTicks = Stopwatch.Frequency * ChunkMs / 1000;
        long chunkIndex = 0;

        try
        {
            while (!token.IsCancellationRequested)
            {
                List<IOutputBranch> snapshot;
                lock (_stateGate)
                {
                    snapshot = _branches.ToList();
                }

                if (snapshot.Count == 0)
                {
                    RaiseLog("No healthy outputs remain. Stopping playback.");
                    break;
                }

                FillChunk(generator, floatBuffer, byteBuffer, floatCount);

                foreach (var branch in snapshot)
                {
                    if (!branch.IsFaulted)
                    {
                        branch.EnqueueFloatSamples(byteBuffer, byteBuffer.Length);
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
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            RaiseLog($"Feeder loop error: {ex.Message}");
        }
        finally
        {
            if (!token.IsCancellationRequested)
            {
                await StopAsync().ConfigureAwait(false);
            }
        }
    }

    private static void FillChunk(
        TestSignalSampleProvider generator,
        float[] floatBuffer,
        byte[] byteBuffer,
        int floatCount)
    {
        var read = generator.Read(floatBuffer, 0, floatCount);
        Buffer.BlockCopy(floatBuffer, 0, byteBuffer, 0, read * sizeof(float));
    }

    private void OnSystemDeviceChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.DeviceId))
        {
            return;
        }

        var isRemoved = e.ChangeKind == DeviceChangeKind.Removed ||
                        (e.ChangeKind == DeviceChangeKind.StateChanged &&
                         e.NewState is not null &&
                         e.NewState != AudioEndpointState.Active);

        if (!isRemoved)
        {
            return;
        }

        HandleBranchFailure(
            e.DeviceId,
            OutputBranchState.Disconnected,
            $"Endpoint disconnected ({e.ChangeKind}: {e.NewState?.ToString() ?? "Removed"}).");
    }

    private void OnBranchFaulted(object? sender, string? reason)
    {
        if (sender is IOutputBranch branch)
        {
            HandleBranchFailure(
                branch.DeviceId,
                OutputBranchState.Failed,
                string.IsNullOrWhiteSpace(reason) ? "Audio stream stopped unexpectedly." : reason);
        }
    }

    private void HandleBranchFailure(string deviceId, OutputBranchState newState, string reason)
    {
        IOutputBranch? removedBranch = null;
        OutputBranchStatus? updatedStatus = null;
        var remainingCount = 0;

        lock (_stateGate)
        {
            var idx = _branches.FindIndex(b => string.Equals(b.DeviceId, deviceId, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                removedBranch = _branches[idx];
                _branches.RemoveAt(idx);
            }

            remainingCount = _branches.Count;

            for (var i = 0; i < _statuses.Count; i++)
            {
                if (string.Equals(_statuses[i].DeviceId, deviceId, StringComparison.OrdinalIgnoreCase) &&
                    _statuses[i].State == OutputBranchState.Playing)
                {
                    updatedStatus = _statuses[i] with { State = newState, ErrorMessage = reason };
                    _statuses[i] = updatedStatus;
                    break;
                }
            }
        }

        if (removedBranch is not null)
        {
            removedBranch.Faulted -= OnBranchFaulted;
            removedBranch.Dispose();
            RaiseLog($"'{removedBranch.DeviceName}' {newState}: {reason} ({remainingCount} healthy output(s) remaining).");
        }

        if (updatedStatus is not null)
        {
            RaiseStatusChanged(updatedStatus);
        }

        if (removedBranch is not null && remainingCount == 0)
        {
            _ = StopAsync();
        }
    }

    private void DisposeBranches(IEnumerable<IOutputBranch> branches)
    {
        foreach (var branch in branches)
        {
            branch.Faulted -= OnBranchFaulted;
            try
            {
                branch.Dispose();
            }
            catch
            {
                // Ignore branch disposal errors.
            }
        }
    }

    private void RaiseStatusChanged(OutputBranchStatus status) =>
        OutputStatusChanged?.Invoke(this, new OutputBranchStatusChangedEventArgs(status));

    private void RaiseLog(string message) =>
        LogMessage?.Invoke(this, $"[{DateTime.Now:HH:mm:ss}] {message}");
}
