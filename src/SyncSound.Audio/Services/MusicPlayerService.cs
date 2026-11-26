using Serilog;
using SyncSound.Audio.Playback;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Services;

/// <summary>
/// Orchestrates local music playback, queue management, metadata, and multi-output routing via <see cref="IAudioEngine"/>.
/// </summary>
public sealed class MusicPlayerService : IMusicPlayerService
{
    private readonly IAudioEngine _engine;
    private readonly IDeviceManagerService _deviceManager;
    private readonly ISettingsService _settingsService;
    private readonly ILogger _logger;
    private readonly Lock _gate = new();

    private readonly List<AudioTrackInfo> _queue = [];
    private int _currentIndex = -1;
    private LocalAudioFileSource? _currentSource;
    private TrackPlaybackStatus _status = TrackPlaybackStatus.Stopped;
    private float _volume;
    private string? _errorMessage;
    private bool _isInternalTransition;
    private bool _disposed;

    public MusicPlayerService(
        IAudioEngine engine,
        IDeviceManagerService deviceManager,
        ISettingsService settingsService,
        ILogger? logger = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? Log.Logger;

        _volume = Math.Clamp(_settingsService.Current.PlayerVolume, 0.0f, 1.0f);
        _engine.SourceCompleted += OnSourceCompleted;
        _engine.StatusChanged += OnEngineStatusChanged;
        _deviceManager.DevicesUpdated += OnDevicesUpdated;
    }

    public PlaybackState CurrentState
    {
        get
        {
            lock (_gate)
            {
                return BuildStateLocked();
            }
        }
    }

    public event EventHandler<PlaybackState>? StateChanged;

    public async Task LoadFilesAsync(IEnumerable<string> filePaths, bool autoPlay = true)
    {
        var addedStartIndex = -1;
        var validPaths = filePaths
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (validPaths.Count == 0)
        {
            return;
        }

        var tracks = await Task.Run(() => validPaths.Select(LocalAudioFileSource.ReadTrackMetadata).ToList()).ConfigureAwait(false);

        lock (_gate)
        {
            addedStartIndex = _queue.Count;
            _queue.AddRange(tracks);
            if (_currentIndex < 0)
            {
                _currentIndex = 0;
            }
        }

        RaiseStateChanged();

        if (autoPlay && addedStartIndex >= 0)
        {
            await PlayTrackAtAsync(addedStartIndex).ConfigureAwait(false);
        }
    }

    public async Task PlayTrackAtAsync(int index)
    {
        AudioTrackInfo? track;
        lock (_gate)
        {
            if (index < 0 || index >= _queue.Count)
            {
                return;
            }

            _currentIndex = index;
            track = _queue[index];
            _status = TrackPlaybackStatus.Loading;
            _errorMessage = null;
        }

        RaiseStateChanged();

        var targets = _deviceManager.SelectedDevices;
        if (targets.Count == 0)
        {
            lock (_gate)
            {
                _status = TrackPlaybackStatus.Error;
                _errorMessage = "Choose a device to begin.";
            }

            RaiseStateChanged();
            return;
        }

        _isInternalTransition = true;
        try
        {
            await _engine.StopAsync().ConfigureAwait(false);
            DisposeCurrentSource();

            var source = new LocalAudioFileSource(track);
            lock (_gate)
            {
                _currentSource = source;
            }

            var effectiveVolumes = new Dictionary<string, float>(_settingsService.Current.PerDeviceVolume, StringComparer.OrdinalIgnoreCase);
            foreach (var mutedId in _settingsService.Current.MutedDeviceIds)
            {
                effectiveVolumes[mutedId] = 0.0f;
            }

            var config = new AudioEngineConfiguration
            {
                OutputLatencyMs = 40,
                PrefillChunks = 10,
                MasterVolume = _volume,
                PerOutputDelayMs = _settingsService.Current.PerDeviceDelayMs,
                PerOutputVolume = effectiveVolumes
            };

            var engineStatus = await _engine.StartAsync(source, targets, config).ConfigureAwait(false);

            lock (_gate)
            {
                if (engineStatus.State is AudioEngineState.Playing or AudioEngineState.Partial)
                {
                    _status = TrackPlaybackStatus.Playing;
                    _errorMessage = null;
                }
                else
                {
                    _status = TrackPlaybackStatus.Error;
                    _errorMessage = engineStatus.UserMessage;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed playing local file {FilePath}", track.FilePath);
            DisposeCurrentSource();
            lock (_gate)
            {
                _status = TrackPlaybackStatus.Error;
                _errorMessage = "This audio format isn't supported.";
            }
        }
        finally
        {
            _isInternalTransition = false;
            RaiseStateChanged();
        }
    }

    public async Task TogglePlayPauseAsync()
    {
        TrackPlaybackStatus currentStatus;
        int indexToPlay;
        lock (_gate)
        {
            currentStatus = _status;
            indexToPlay = _currentIndex;
        }

        if (currentStatus == TrackPlaybackStatus.Playing)
        {
            await _engine.PauseAsync().ConfigureAwait(false);
            lock (_gate)
            {
                _status = TrackPlaybackStatus.Paused;
            }

            RaiseStateChanged();
        }
        else if (currentStatus == TrackPlaybackStatus.Paused)
        {
            await _engine.ResumeAsync().ConfigureAwait(false);
            lock (_gate)
            {
                _status = TrackPlaybackStatus.Playing;
            }

            RaiseStateChanged();
        }
        else if (indexToPlay >= 0)
        {
            await PlayTrackAtAsync(indexToPlay).ConfigureAwait(false);
        }
    }

    public async Task StopAsync()
    {
        _isInternalTransition = true;
        try
        {
            await _engine.StopAsync().ConfigureAwait(false);
            DisposeCurrentSource();
            lock (_gate)
            {
                _status = TrackPlaybackStatus.Stopped;
                _errorMessage = null;
            }
        }
        finally
        {
            _isInternalTransition = false;
            RaiseStateChanged();
        }
    }

    public async Task NextAsync()
    {
        int nextIndex;
        lock (_gate)
        {
            if (_queue.Count == 0)
            {
                return;
            }

            nextIndex = (_currentIndex + 1) % _queue.Count;
        }

        await PlayTrackAtAsync(nextIndex).ConfigureAwait(false);
    }

    public async Task PreviousAsync()
    {
        int prevIndex;
        TimeSpan currentPos;
        lock (_gate)
        {
            if (_queue.Count == 0)
            {
                return;
            }

            currentPos = _currentSource?.Position ?? TimeSpan.Zero;
            if (currentPos.TotalSeconds > 3.0)
            {
                prevIndex = _currentIndex;
            }
            else
            {
                prevIndex = _currentIndex <= 0 ? _queue.Count - 1 : _currentIndex - 1;
            }
        }

        if (prevIndex == _currentIndex && _currentSource is not null)
        {
            Seek(TimeSpan.Zero);
            return;
        }

        await PlayTrackAtAsync(prevIndex).ConfigureAwait(false);
    }

    public void Seek(TimeSpan position)
    {
        lock (_gate)
        {
            _currentSource?.Seek(position);
        }

        RaiseStateChanged();
    }

    public void SetVolume(float volume)
    {
        var clamped = Math.Clamp(volume, 0.0f, 1.0f);
        lock (_gate)
        {
            _volume = clamped;
            _settingsService.Current.PlayerVolume = clamped;
        }

        _engine.SetMasterVolume(clamped);
        RaiseStateChanged();
        _ = _settingsService.SaveAsync();
    }

    public void RemoveFromQueue(int index)
    {
        lock (_gate)
        {
            if (index < 0 || index >= _queue.Count)
            {
                return;
            }

            _queue.RemoveAt(index);
            if (_queue.Count == 0)
            {
                _currentIndex = -1;
            }
            else if (index < _currentIndex)
            {
                _currentIndex--;
            }
            else if (_currentIndex >= _queue.Count)
            {
                _currentIndex = _queue.Count - 1;
            }
        }

        RaiseStateChanged();
    }

    public void ClearQueue()
    {
        _ = StopAsync();
        lock (_gate)
        {
            _queue.Clear();
            _currentIndex = -1;
        }

        RaiseStateChanged();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.SourceCompleted -= OnSourceCompleted;
        _engine.StatusChanged -= OnEngineStatusChanged;
        _deviceManager.DevicesUpdated -= OnDevicesUpdated;
        await StopAsync().ConfigureAwait(false);
    }

    private void OnSourceCompleted(object? sender, EventArgs e)
    {
        bool hasNext;
        int nextIndex;
        lock (_gate)
        {
            if (_currentSource is null || _status == TrackPlaybackStatus.Stopped)
            {
                return;
            }

            hasNext = _currentIndex + 1 < _queue.Count;
            nextIndex = _currentIndex + 1;
        }

        if (hasNext)
        {
            _ = PlayTrackAtAsync(nextIndex);
        }
        else
        {
            _ = StopAsync();
        }
    }

    private void OnEngineStatusChanged(object? sender, AudioEngineStatusSnapshot snapshot)
    {
        if (_isInternalTransition)
        {
            return;
        }

        lock (_gate)
        {
            if (_currentSource is null)
            {
                return;
            }

            if (snapshot.State == AudioEngineState.Idle && _status is TrackPlaybackStatus.Playing or TrackPlaybackStatus.Paused)
            {
                _status = TrackPlaybackStatus.Stopped;
            }
            else if (snapshot.State == AudioEngineState.Error)
            {
                _status = TrackPlaybackStatus.Error;
                _errorMessage = snapshot.UserMessage;
            }
        }

        RaiseStateChanged();
    }

    private void OnDevicesUpdated(object? sender, EventArgs e)
    {
        RaiseStateChanged();
    }

    private void DisposeCurrentSource()
    {
        LocalAudioFileSource? old;
        lock (_gate)
        {
            old = _currentSource;
            _currentSource = null;
        }

        old?.Dispose();
    }

    private PlaybackState BuildStateLocked()
    {
        var track = _currentIndex >= 0 && _currentIndex < _queue.Count ? _queue[_currentIndex] : null;
        var position = _currentSource?.Position ?? TimeSpan.Zero;
        var duration = _currentSource?.Duration ?? track?.Duration ?? TimeSpan.Zero;

        return new PlaybackState(
            _status,
            track,
            position,
            duration,
            _volume,
            _queue.ToList(),
            _deviceManager.SelectedDevices,
            _errorMessage);
    }

    private void RaiseStateChanged() => StateChanged?.Invoke(this, CurrentState);
}
