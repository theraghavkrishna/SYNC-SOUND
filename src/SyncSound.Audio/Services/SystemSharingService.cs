using Serilog;
using SyncSound.Audio.Capture;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Services;

/// <summary>
/// Manages system audio sharing via WASAPI loopback from the Windows default render endpoint
/// to all selected non-source endpoints, preventing feedback loops and recovering from default changes.
/// </summary>
public sealed class SystemSharingService : ISystemSharingService
{
    private readonly IAudioEngine _engine;
    private readonly IDeviceManagerService _deviceManager;
    private readonly IAudioDeviceEnumerator _deviceEnumerator;
    private readonly ISettingsService _settingsService;
    private readonly ILogger _logger;
    private readonly Lock _gate = new();

    private WasapiLoopbackAudioSource? _loopbackSource;
    private AudioDevice? _sourceDevice;
    private DateTimeOffset? _startTime;
    private bool _isSharing;
    private bool _isTransitioning;
    private string _userMessage = "Ready to listen.";
    private string? _errorMessage;
    private bool _disposed;

    public SystemSharingService(
        IAudioEngine engine,
        IDeviceManagerService deviceManager,
        IAudioDeviceEnumerator deviceEnumerator,
        ISettingsService settingsService,
        ILogger? logger = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _deviceEnumerator = deviceEnumerator ?? throw new ArgumentNullException(nameof(deviceEnumerator));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? Log.Logger;

        _engine.StatusChanged += OnEngineStatusChanged;
        _deviceEnumerator.DeviceChanged += OnSystemDeviceChanged;
    }

    public SharingState CurrentState
    {
        get
        {
            lock (_gate)
            {
                return BuildStateLocked();
            }
        }
    }

    public event EventHandler<SharingState>? StateChanged;

    public async Task<SharingState> StartSharingAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var defaultSource = _deviceEnumerator.GetDefaultRenderEndpoint();
        if (defaultSource is null)
        {
            return SetFailureState("We couldn't access your computer's audio.", "No active Windows default playback device was found.");
        }

        var selected = _deviceManager.SelectedDevices;
        if (selected.Count == 0)
        {
            return SetFailureState("Choose a device to begin.");
        }

        // Exclude the source endpoint so captured system audio is never routed back into itself.
        var fanoutTargets = selected
            .Where(d => !string.Equals(d.Id, defaultSource.Id, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (fanoutTargets.Count == 0)
        {
            return SetFailureState(
                "Select at least one other headphone to share with.",
                $"{defaultSource.Name} is already playing your computer's audio directly.");
        }

        _logger.Information(
            "Starting system sharing from source '{Source}' to {Count} target(s): {Targets}",
            defaultSource.Name,
            fanoutTargets.Count,
            string.Join(", ", fanoutTargets.Select(t => t.Name)));

        _isTransitioning = true;
        try
        {
            await _engine.StopAsync().ConfigureAwait(false);
            DisposeLoopbackSource();

            var source = new WasapiLoopbackAudioSource(defaultSource.Id, defaultSource.Name, _logger);
            lock (_gate)
            {
                _loopbackSource = source;
                _sourceDevice = defaultSource;
                _startTime = DateTimeOffset.UtcNow;
                _isSharing = true;
                _errorMessage = null;
            }

            var config = new AudioEngineConfiguration
            {
                ChunkMs = 10,
                OutputLatencyMs = 35,
                BufferDurationMs = 600,
                PrefillChunks = 3,
                AutoRecoverOutputs = true,
                MasterVolume = Math.Clamp(_settingsService.Current.MasterVolume, 0.0f, 1.0f),
                PerOutputDelayMs = _settingsService.Current.PerDeviceDelayMs,
                PerOutputVolume = _settingsService.Current.PerDeviceVolume
            };

            var engineStatus = await _engine.StartAsync(source, fanoutTargets, config, cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                if (engineStatus.State is AudioEngineState.Playing or AudioEngineState.Partial or AudioEngineState.Recovering)
                {
                    _isSharing = true;
                    _userMessage = engineStatus.UserMessage;
                    _errorMessage = null;
                }
                else
                {
                    _isSharing = false;
                    _userMessage = engineStatus.UserMessage;
                    _errorMessage = engineStatus.UserMessage;
                    DisposeLoopbackSource();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start system audio sharing");
            DisposeLoopbackSource();
            SetFailureState("We couldn't access your computer's audio.", ex.Message);
        }
        finally
        {
            _isTransitioning = false;
        }

        var state = CurrentState;
        StateChanged?.Invoke(this, state);
        return state;
    }

    public async Task StopSharingAsync()
    {
        _isTransitioning = true;
        try
        {
            await _engine.StopAsync().ConfigureAwait(false);
            DisposeLoopbackSource();
            lock (_gate)
            {
                _isSharing = false;
                _sourceDevice = null;
                _startTime = null;
                _userMessage = "Ready to listen.";
                _errorMessage = null;
            }
        }
        finally
        {
            _isTransitioning = false;
        }

        StateChanged?.Invoke(this, CurrentState);
    }

    public async Task RefreshTargetsAsync()
    {
        if (!_isSharing)
        {
            return;
        }

        await StartSharingAsync().ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.StatusChanged -= OnEngineStatusChanged;
        _deviceEnumerator.DeviceChanged -= OnSystemDeviceChanged;
        await StopSharingAsync().ConfigureAwait(false);
    }

    private void OnSystemDeviceChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        if (!_isSharing || _isTransitioning || _disposed)
        {
            return;
        }

        if (e.ChangeKind == DeviceChangeKind.DefaultChanged &&
            !string.IsNullOrWhiteSpace(e.DeviceId) &&
            !string.Equals(e.DeviceId, _sourceDevice?.Id, StringComparison.OrdinalIgnoreCase))
        {
            _logger.Information("Default render endpoint changed during sharing; reinitializing loopback capture.");
            _ = Task.Run(() => StartSharingAsync());
        }
    }

    private void OnEngineStatusChanged(object? sender, AudioEngineStatusSnapshot snapshot)
    {
        if (_isTransitioning)
        {
            return;
        }

        bool shouldEmit;
        lock (_gate)
        {
            if (_loopbackSource is null)
            {
                return;
            }

            shouldEmit = true;
            _userMessage = snapshot.UserMessage;

            if (snapshot.State == AudioEngineState.Idle && _isSharing)
            {
                _isSharing = false;
                _startTime = null;
                DisposeLoopbackSource();
            }
            else if (snapshot.State == AudioEngineState.Error)
            {
                _isSharing = false;
                _errorMessage = snapshot.UserMessage;
                DisposeLoopbackSource();
            }
        }

        if (shouldEmit)
        {
            StateChanged?.Invoke(this, CurrentState);
        }
    }

    private SharingState SetFailureState(string userMessage, string? errorDetail = null)
    {
        lock (_gate)
        {
            _isSharing = false;
            _userMessage = userMessage;
            _errorMessage = errorDetail;
        }

        var state = CurrentState;
        StateChanged?.Invoke(this, state);
        return state;
    }

    private void DisposeLoopbackSource()
    {
        WasapiLoopbackAudioSource? old;
        lock (_gate)
        {
            old = _loopbackSource;
            _loopbackSource = null;
        }

        old?.Dispose();
    }

    private SharingState BuildStateLocked()
    {
        var engineSnap = _engine.Status;
        var active = engineSnap.Outputs.Where(o => o.State is AudioOutputState.Playing or AudioOutputState.Initializing).ToList();
        var failed = engineSnap.Outputs.Where(o => o.State is AudioOutputState.Failed or AudioOutputState.Disconnected).ToList();

        return new SharingState(
            _isSharing ? engineSnap.State : AudioEngineState.Idle,
            _isSharing,
            _sourceDevice,
            _deviceManager.SelectedDevices,
            active,
            failed,
            _startTime,
            _userMessage,
            _errorMessage);
    }
}
