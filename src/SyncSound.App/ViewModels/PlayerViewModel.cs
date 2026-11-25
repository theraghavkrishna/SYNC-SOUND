using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.App.ViewModels;

public sealed class PlayerViewModel : ViewModelBase, IDisposable
{
    private readonly IMusicPlayerService _playerService;
    private readonly DispatcherTimer _positionTimer;

    private AudioTrackInfo? _currentTrack;
    private string _trackTitle = "Drop a song to listen together";
    private string _trackArtist = "Supports MP3, WAV, FLAC, M4A, and AAC";
    private byte[]? _albumArtBytes;
    private string _outputBadge = "Choose a device to begin.";
    private string _elapsedText = "0:00";
    private string _durationText = "0:00";
    private double _durationSeconds = 1;
    private double _positionSeconds;
    private double _volume = 0.85;
    private bool _isPlaying;
    private bool _hasTrack;
    private bool _isSeeking;
    private string? _errorMessage;

    public PlayerViewModel(IMusicPlayerService playerService)
    {
        _playerService = playerService;

        ImportFilesCommand = new AsyncRelayCommand(BrowseAndImportAsync);
        PlayPauseCommand = new AsyncRelayCommand(() => _playerService.TogglePlayPauseAsync(), () => HasTrack);
        StopCommand = new AsyncRelayCommand(() => _playerService.StopAsync(), () => HasTrack);
        NextCommand = new AsyncRelayCommand(() => _playerService.NextAsync(), () => Queue.Count > 1);
        PreviousCommand = new AsyncRelayCommand(() => _playerService.PreviousAsync(), () => Queue.Count > 0);
        ClearQueueCommand = new RelayCommand(() => _playerService.ClearQueue(), () => Queue.Count > 0);
        PlayQueueItemCommand = new AsyncRelayCommand(PlayQueueItemAsync);
        RemoveQueueItemCommand = new RelayCommand(RemoveQueueItem);

        _positionTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _positionTimer.Tick += (_, _) => UpdateLivePosition();

        _playerService.StateChanged += OnPlayerStateChanged;
        ApplyState(_playerService.CurrentState);
    }

    public ObservableCollection<QueueTrackItem> Queue { get; } = [];

    public AsyncRelayCommand ImportFilesCommand { get; }
    public AsyncRelayCommand PlayPauseCommand { get; }
    public AsyncRelayCommand StopCommand { get; }
    public AsyncRelayCommand NextCommand { get; }
    public AsyncRelayCommand PreviousCommand { get; }
    public RelayCommand ClearQueueCommand { get; }
    public AsyncRelayCommand PlayQueueItemCommand { get; }
    public RelayCommand RemoveQueueItemCommand { get; }

    public string TrackTitle
    {
        get => _trackTitle;
        private set => SetProperty(ref _trackTitle, value);
    }

    public string TrackArtist
    {
        get => _trackArtist;
        private set => SetProperty(ref _trackArtist, value);
    }

    public byte[]? AlbumArtBytes
    {
        get => _albumArtBytes;
        private set
        {
            if (SetProperty(ref _albumArtBytes, value))
            {
                OnPropertyChanged(nameof(HasAlbumArt));
            }
        }
    }

    public bool HasAlbumArt => _albumArtBytes is { Length: > 0 };

    public string OutputBadge
    {
        get => _outputBadge;
        private set => SetProperty(ref _outputBadge, value);
    }

    public string ElapsedText
    {
        get => _elapsedText;
        private set => SetProperty(ref _elapsedText, value);
    }

    public string DurationText
    {
        get => _durationText;
        private set => SetProperty(ref _durationText, value);
    }

    public double DurationSeconds
    {
        get => _durationSeconds;
        private set => SetProperty(ref _durationSeconds, value);
    }

    public double PositionSeconds
    {
        get => _positionSeconds;
        set
        {
            if (SetProperty(ref _positionSeconds, value) && _isSeeking)
            {
                ElapsedText = FormatTime(TimeSpan.FromSeconds(value));
            }
        }
    }

    public double Volume
    {
        get => _volume;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (SetProperty(ref _volume, clamped))
            {
                _playerService.SetVolume((float)clamped);
            }
        }
    }

    public bool IsPlaying
    {
        get => _isPlaying;
        private set => SetProperty(ref _isPlaying, value);
    }

    public bool HasTrack
    {
        get => _hasTrack;
        private set => SetProperty(ref _hasTrack, value);
    }

    public bool HasQueue => Queue.Count > 0;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);

    public void BeginSeek() => _isSeeking = true;

    public void CommitSeek()
    {
        _isSeeking = false;
        _playerService.Seek(TimeSpan.FromSeconds(PositionSeconds));
    }

    public Task HandleDroppedFilesAsync(IEnumerable<string> paths)
    {
        var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".mp3", ".wav", ".flac", ".m4a", ".aac", ".wma", ".ogg" };

        var valid = paths.Where(p => supported.Contains(System.IO.Path.GetExtension(p))).ToList();
        return valid.Count > 0 ? _playerService.LoadFilesAsync(valid, autoPlay: true) : Task.CompletedTask;
    }

    public void Dispose()
    {
        _positionTimer.Stop();
        _playerService.StateChanged -= OnPlayerStateChanged;
    }

    private async Task BrowseAndImportAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose music for SYNC SOUND",
            Multiselect = true,
            Filter = "Audio Files|*.mp3;*.wav;*.flac;*.m4a;*.aac;*.wma;*.ogg|All Files|*.*"
        };

        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            await _playerService.LoadFilesAsync(dialog.FileNames, autoPlay: true);
        }
    }

    private Task PlayQueueItemAsync(object? parameter)
    {
        if (parameter is QueueTrackItem item)
        {
            return _playerService.PlayTrackAtAsync(item.Index);
        }

        return Task.CompletedTask;
    }

    private void RemoveQueueItem(object? parameter)
    {
        if (parameter is QueueTrackItem item)
        {
            _playerService.RemoveFromQueue(item.Index);
        }
    }

    private void OnPlayerStateChanged(object? sender, PlaybackState state)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => ApplyState(state));
        }
        else
        {
            ApplyState(state);
        }
    }

    private void ApplyState(PlaybackState state)
    {
        _currentTrack = state.CurrentTrack;
        HasTrack = state.CurrentTrack is not null;
        IsPlaying = state.Status == TrackPlaybackStatus.Playing;
        ErrorMessage = state.ErrorMessage;

        if (state.CurrentTrack is not null)
        {
            TrackTitle = state.CurrentTrack.Title;
            TrackArtist = string.IsNullOrWhiteSpace(state.CurrentTrack.Album)
                ? state.CurrentTrack.Artist
                : $"{state.CurrentTrack.Artist} · {state.CurrentTrack.Album}";
            AlbumArtBytes = state.CurrentTrack.AlbumArtBytes;
        }
        else
        {
            TrackTitle = "Drop a song to listen together";
            TrackArtist = "Supports MP3, WAV, FLAC, M4A, and AAC";
            AlbumArtBytes = null;
        }

        var totalSeconds = Math.Max(1.0, state.Duration.TotalSeconds);
        DurationSeconds = totalSeconds;
        DurationText = FormatTime(state.Duration);

        if (!_isSeeking)
        {
            PositionSeconds = Math.Min(totalSeconds, state.Position.TotalSeconds);
            ElapsedText = FormatTime(state.Position);
        }

        if (Math.Abs(_volume - state.Volume) > 0.01)
        {
            _volume = state.Volume;
            OnPropertyChanged(nameof(Volume));
        }

        var outputCount = state.SelectedOutputs.Count;
        OutputBadge = outputCount == 0
            ? "Choose a device to begin."
            : IsPlaying
                ? $"Playing through {outputCount} {(outputCount == 1 ? "device" : "devices")}"
                : $"Ready for {outputCount} {(outputCount == 1 ? "device" : "devices")}";

        Queue.Clear();
        for (var i = 0; i < state.Queue.Count; i++)
        {
            var t = state.Queue[i];
            var isCurrent = state.CurrentTrack is not null &&
                            string.Equals(t.FilePath, state.CurrentTrack.FilePath, StringComparison.OrdinalIgnoreCase);
            Queue.Add(new QueueTrackItem(i, t.Title, t.Artist, FormatTime(t.Duration), isCurrent));
        }

        if (IsPlaying && !_positionTimer.IsEnabled)
        {
            _positionTimer.Start();
        }
        else if (!IsPlaying && _positionTimer.IsEnabled)
        {
            _positionTimer.Stop();
        }

        PlayPauseCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        NextCommand.RaiseCanExecuteChanged();
        PreviousCommand.RaiseCanExecuteChanged();
        ClearQueueCommand.RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(HasQueue));
    }

    private void UpdateLivePosition()
    {
        if (_isSeeking || _currentTrack is null)
        {
            return;
        }

        var state = _playerService.CurrentState;
        PositionSeconds = Math.Min(DurationSeconds, state.Position.TotalSeconds);
        ElapsedText = FormatTime(state.Position);
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time < TimeSpan.Zero)
        {
            time = TimeSpan.Zero;
        }

        return time.TotalHours >= 1
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"m\:ss");
    }

    public sealed record QueueTrackItem(int Index, string Title, string Artist, string DurationText, bool IsCurrent);
}
