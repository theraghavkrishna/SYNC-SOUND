using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using SyncSound.App.Services;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.App.ViewModels;

public enum AppSection
{
    Home = 0,
    Devices,
    Settings
}

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly ISystemSharingService _sharingService;
    private readonly IAudioEngine _audioEngine;
    private readonly IMultiOutputTestPlayer _testPlayer;
    private readonly IMusicPlayerService _musicPlayer;
    private readonly ISettingsService _settingsService;
    private readonly ISoundFeedbackService _soundFeedback;
    private readonly ThemeService _themeService;
    private readonly DispatcherTimer _meterTimer;
    private readonly DispatcherTimer _toastTimer;
    private readonly DispatcherTimer _testAutoStopTimer;

    private HashSet<string> _knownConnectedIds = new(StringComparer.OrdinalIgnoreCase);
    private bool _hasInitializedKnownDevices;

    private AppSection _currentSection = AppSection.Home;
    private SyncVisualState _visualState = SyncVisualState.Ready;
    private string _heroTitle = "Everyone's here.";
    private string _heroAccent = "Listen together.";
    private string _heroSubtitle = "Choose who's listening, then press Start listening.";
    private string _shareButtonText = "Start listening";
    private string _footerConnectionText = "0 connected";
    private string _connectedCountText = "0 connected";
    private string _selectedCountText = "0 in group";
    private string _shortcutHint = "Ctrl+Shift+S";
    private bool _isSharing;
    private bool _hasErrorBanner;
    private string? _statusBannerMessage;
    private bool _hasConnectedDevices;
    private int _selectedCount;
    private double _masterVolume = 1.0;
    private double _preMuteVolume = 1.0;
    private bool _isMuted;
    private double _signalLevel;
    private bool _isTestingSound;
    private string? _testSoundStatusText;
    private string? _notificationText;
    private bool _isMusicDockExpanded;
    private string _themeBadgeText = "System";

    public MainViewModel(
        IDeviceManagerService deviceManager,
        ISystemSharingService sharingService,
        IAudioEngine audioEngine,
        IMultiOutputTestPlayer testPlayer,
        IMusicPlayerService musicPlayer,
        ISettingsService settingsService,
        ISoundFeedbackService soundFeedback,
        ThemeService themeService,
        DevicesViewModel devicesViewModel,
        SettingsViewModel settingsViewModel,
        QuickSidebarViewModel quickSidebarViewModel)
    {
        _deviceManager = deviceManager;
        _sharingService = sharingService;
        _audioEngine = audioEngine;
        _testPlayer = testPlayer;
        _musicPlayer = musicPlayer;
        _settingsService = settingsService;
        _soundFeedback = soundFeedback;
        _themeService = themeService;

        DevicesViewModel = devicesViewModel;
        SettingsViewModel = settingsViewModel;
        QuickSidebarViewModel = quickSidebarViewModel;

        var initialVol = Math.Clamp(_settingsService.Current.MasterVolume, 0.0f, 1.0f);
        if (initialVol <= 0.01f)
        {
            initialVol = 1.0f;
            _settingsService.Current.MasterVolume = 1.0f;
        }

        _masterVolume = initialVol;
        _preMuteVolume = initialVol;
        _isMuted = false;
        _audioEngine.SetMasterVolume(initialVol);
        _themeBadgeText = FormatThemeLabel(_settingsService.Current.ThemeMode);

        NavigateCommand = new RelayCommand(p =>
        {
            if (p is AppSection section)
            {
                CurrentSection = section;
            }
            else if (p is string name && Enum.TryParse<AppSection>(name, out var parsed))
            {
                CurrentSection = parsed;
            }
        });

        ToggleSharingCommand = new AsyncRelayCommand(PrimaryActionAsync, () => true);
        ToggleMuteCommand = new RelayCommand(ToggleMute);
        TestSoundCommand = new AsyncRelayCommand(ToggleTestSoundAsync, () => true);
        OpenSidebarCommand = new RelayCommand(() => OpenSidebarRequested?.Invoke(this, EventArgs.Empty));
        ManageDevicesCommand = new RelayCommand(() => CurrentSection = AppSection.Devices);
        CycleThemeCommand = new RelayCommand(CycleTheme);
        ToggleMusicDockCommand = new RelayCommand(() => IsMusicDockExpanded = !IsMusicDockExpanded);
        ChooseMusicFileCommand = new RelayCommand(() => ChooseMusicFileRequested?.Invoke(this, EventArgs.Empty));
        PlayPauseMusicCommand = new AsyncRelayCommand(() => _musicPlayer.TogglePlayPauseAsync(), () => HasTrackLoaded);
        NextTrackCommand = new AsyncRelayCommand(() => _musicPlayer.NextAsync(), () => HasTrackLoaded);
        PreviousTrackCommand = new AsyncRelayCommand(() => _musicPlayer.PreviousAsync(), () => HasTrackLoaded);

        _meterTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(45) };
        _meterTimer.Tick += (_, _) =>
        {
            var level = (IsSharing && !IsMuted) ? Math.Clamp(_audioEngine.CurrentSignalLevel, 0f, 1f) : 0.0;
            SignalLevel = level;
            QuickSidebarViewModel.UpdateSignalLevel(level);
        };

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.8) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            NotificationText = null;
        };

        _testAutoStopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.6) };
        _testAutoStopTimer.Tick += async (_, _) =>
        {
            _testAutoStopTimer.Stop();
            if (_testPlayer.IsPlaying)
            {
                await _testPlayer.StopAsync();
                IsTestingSound = false;
                TestSoundStatusText = "Sound test complete.";
            }
        };

        _deviceManager.DevicesUpdated += OnDevicesUpdated;
        _sharingService.StateChanged += OnSharingStateChanged;
        _musicPlayer.StateChanged += OnMusicStateChanged;
        SettingsViewModel.HotkeyUpdated += (_, shortcut) => ShortcutHint = shortcut;
        SettingsViewModel.AppearancePreferencesChanged += (_, _) =>
        {
            _themeBadgeText = FormatThemeLabel(_settingsService.Current.ThemeMode);
            OnPropertyChanged(nameof(ThemeBadgeText));
            OnPropertyChanged(nameof(ReducedMotion));
            QuickSidebarViewModel.NotifyPreferencesChanged();
        };

        ShortcutHint = _settingsService.Current.Hotkey;
        RefreshHomeState();
    }

    public DevicesViewModel DevicesViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public QuickSidebarViewModel QuickSidebarViewModel { get; }

    public ObservableCollection<DeviceItemViewModel> ListeningGroupDevices { get; } = [];

    public RelayCommand NavigateCommand { get; }
    public AsyncRelayCommand ToggleSharingCommand { get; }
    public RelayCommand ToggleMuteCommand { get; }
    public AsyncRelayCommand TestSoundCommand { get; }
    public RelayCommand OpenSidebarCommand { get; }
    public RelayCommand ManageDevicesCommand { get; }
    public RelayCommand CycleThemeCommand { get; }
    public RelayCommand ToggleMusicDockCommand { get; }
    public RelayCommand ChooseMusicFileCommand { get; }
    public AsyncRelayCommand PlayPauseMusicCommand { get; }
    public AsyncRelayCommand NextTrackCommand { get; }
    public AsyncRelayCommand PreviousTrackCommand { get; }

    public event EventHandler? OpenSidebarRequested;
    public event EventHandler? ChooseMusicFileRequested;

    public AppSection CurrentSection
    {
        get => _currentSection;
        set
        {
            if (SetProperty(ref _currentSection, value))
            {
                if (value == AppSection.Devices)
                {
                    DevicesViewModel.SyncVolumesFromSettings();
                }

                OnPropertyChanged(nameof(IsHomeSelected));
                OnPropertyChanged(nameof(IsDevicesSelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
            }
        }
    }

    public bool IsHomeSelected
    {
        get => _currentSection == AppSection.Home;
        set { if (value) CurrentSection = AppSection.Home; }
    }

    public bool IsDevicesSelected
    {
        get => _currentSection == AppSection.Devices;
        set { if (value) CurrentSection = AppSection.Devices; }
    }

    public bool IsSettingsSelected
    {
        get => _currentSection == AppSection.Settings;
        set { if (value) CurrentSection = AppSection.Settings; }
    }

    public SyncVisualState VisualState
    {
        get => _visualState;
        private set => SetProperty(ref _visualState, value);
    }

    public string HeroTitle { get => _heroTitle; private set => SetProperty(ref _heroTitle, value); }
    public string HeroAccent { get => _heroAccent; private set => SetProperty(ref _heroAccent, value); }
    public string HeroSubtitle { get => _heroSubtitle; private set => SetProperty(ref _heroSubtitle, value); }
    public string ShareButtonText { get => _shareButtonText; private set => SetProperty(ref _shareButtonText, value); }
    public string FooterConnectionText { get => _footerConnectionText; private set => SetProperty(ref _footerConnectionText, value); }
    public string ConnectedCountText { get => _connectedCountText; private set => SetProperty(ref _connectedCountText, value); }
    public string SelectedCountText { get => _selectedCountText; private set => SetProperty(ref _selectedCountText, value); }
    public string ShortcutHint { get => _shortcutHint; private set => SetProperty(ref _shortcutHint, value); }
    public bool IsSharing { get => _isSharing; private set => SetProperty(ref _isSharing, value); }
    public bool HasConnectedDevices { get => _hasConnectedDevices; private set => SetProperty(ref _hasConnectedDevices, value); }
    public int SelectedCount { get => _selectedCount; private set => SetProperty(ref _selectedCount, value); }
    public bool ReducedMotion => _settingsService.Current.ReducedMotion;
    public string ThemeBadgeText => _themeBadgeText;

    public bool HasErrorBanner { get => _hasErrorBanner; private set => SetProperty(ref _hasErrorBanner, value); }
    public string? StatusBannerMessage { get => _statusBannerMessage; private set => SetProperty(ref _statusBannerMessage, value); }

    public double SignalLevel { get => _signalLevel; private set => SetProperty(ref _signalLevel, value); }

    public bool IsTestingSound
    {
        get => _isTestingSound;
        private set
        {
            if (SetProperty(ref _isTestingSound, value))
            {
                OnPropertyChanged(nameof(TestSoundButtonLabel));
            }
        }
    }

    public string TestSoundButtonLabel => IsTestingSound ? "Stop test" : "Test sound";

    public string? TestSoundStatusText
    {
        get => _testSoundStatusText;
        private set
        {
            if (SetProperty(ref _testSoundStatusText, value))
            {
                OnPropertyChanged(nameof(HasTestSoundStatus));
            }
        }
    }

    public bool HasTestSoundStatus => !string.IsNullOrWhiteSpace(_testSoundStatusText);

    public string? NotificationText
    {
        get => _notificationText;
        private set
        {
            if (SetProperty(ref _notificationText, value))
            {
                OnPropertyChanged(nameof(HasNotification));
            }
        }
    }

    public bool HasNotification => !string.IsNullOrWhiteSpace(_notificationText);

    public bool IsMusicDockExpanded
    {
        get => _isMusicDockExpanded;
        set => SetProperty(ref _isMusicDockExpanded, value);
    }

    public bool HasTrackLoaded => _musicPlayer.CurrentState.CurrentTrack is not null;
    public bool IsMusicPlaying => _musicPlayer.CurrentState.Status == TrackPlaybackStatus.Playing;
    public string TrackTitle => _musicPlayer.CurrentState.CurrentTrack?.Title ?? "Want to test a song?";
    public string TrackArtist => _musicPlayer.CurrentState.CurrentTrack?.Artist ?? "Choose a local audio file";
    public byte[]? TrackAlbumArtBytes => _musicPlayer.CurrentState.CurrentTrack?.AlbumArtBytes;
    public double TrackDurationSeconds => Math.Max(1.0, _musicPlayer.CurrentState.Duration.TotalSeconds);
    public double TrackPositionSeconds
    {
        get => _musicPlayer.CurrentState.Position.TotalSeconds;
        set => _musicPlayer.Seek(TimeSpan.FromSeconds(Math.Clamp(value, 0, TrackDurationSeconds)));
    }
    public string TrackTimeText => HasTrackLoaded
        ? $"{_musicPlayer.CurrentState.Position:mm\\:ss} / {_musicPlayer.CurrentState.Duration:mm\\:ss}"
        : "MP3 · WAV · FLAC · M4A";

    public async Task LoadMusicFilesAsync(IEnumerable<string> paths)
    {
        var valid = paths.Where(File.Exists).ToList();
        if (valid.Count == 0)
        {
            return;
        }

        IsMusicDockExpanded = true;
        await _musicPlayer.LoadFilesAsync(valid, autoPlay: true);
    }

    public double MasterVolume
    {
        get => _masterVolume;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_masterVolume - clamped) > 0.001)
            {
                _masterVolume = clamped;
                if (clamped > 0.01)
                {
                    _preMuteVolume = clamped;
                    IsMuted = false;
                }
                else
                {
                    IsMuted = true;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(MasterVolumePercentText));

                _audioEngine.SetMasterVolume((float)clamped);
                _settingsService.Current.MasterVolume = (float)clamped;
                _ = _settingsService.SaveAsync();

                QuickSidebarViewModel.SyncMasterVolumeFromSettings();
                RefreshCopyOnly();
            }
        }
    }

    public string MasterVolumePercentText => $"{(int)Math.Round(_masterVolume * 100)}%";

    public bool IsMuted
    {
        get => _isMuted;
        private set => SetProperty(ref _isMuted, value);
    }

    public void PlayVolumeDetentCue() => _soundFeedback.Play(UiSoundCue.VolumeDetent);

    public void SyncMasterVolumeFromSettings()
    {
        var saved = Math.Clamp(_settingsService.Current.MasterVolume, 0.0f, 1.0f);
        if (Math.Abs(_masterVolume - saved) > 0.001)
        {
            _masterVolume = saved;
            IsMuted = saved <= 0.005;
            OnPropertyChanged(nameof(MasterVolume));
            OnPropertyChanged(nameof(MasterVolumePercentText));
            RefreshCopyOnly();
        }
    }

    public void ShowToast(string message)
    {
        NotificationText = message;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    public void Dispose()
    {
        _meterTimer.Stop();
        _toastTimer.Stop();
        _testAutoStopTimer.Stop();
        _deviceManager.DevicesUpdated -= OnDevicesUpdated;
        _sharingService.StateChanged -= OnSharingStateChanged;
        _musicPlayer.StateChanged -= OnMusicStateChanged;
        SettingsViewModel.Dispose();
        QuickSidebarViewModel.Dispose();
    }

    private void CycleTheme()
    {
        var next = _settingsService.Current.ThemeMode switch
        {
            AppThemeMode.System => AppThemeMode.Light,
            AppThemeMode.Light => AppThemeMode.Dark,
            _ => AppThemeMode.System
        };
        SettingsViewModel.ThemeMode = next;
        _themeBadgeText = FormatThemeLabel(next);
        OnPropertyChanged(nameof(ThemeBadgeText));
    }

    private void ToggleMute()
    {
        _soundFeedback.Play(UiSoundCue.Mute);
        if (IsMuted || _masterVolume <= 0.005)
        {
            MasterVolume = _preMuteVolume > 0.05 ? _preMuteVolume : 0.85;
        }
        else
        {
            _preMuteVolume = _masterVolume;
            MasterVolume = 0.0;
        }
    }

    private async Task ToggleTestSoundAsync()
    {
        if (_testPlayer.IsPlaying)
        {
            _testAutoStopTimer.Stop();
            await _testPlayer.StopAsync();
            IsTestingSound = false;
            TestSoundStatusText = "Sound test stopped.";
            return;
        }

        var targets = _deviceManager.SelectedDevices;
        if (targets.Count == 0)
        {
            targets = _deviceManager.Devices;
        }

        if (targets.Count == 0)
        {
            TestSoundStatusText = "Connect headphones to test sound.";
            return;
        }

        IsTestingSound = true;
        TestSoundStatusText = "Playing chime on selected outputs…";

        var result = await _testPlayer.StartAsync(targets, AudioTestMode.RhythmicChimePulse);
        if (!result.AnyStarted)
        {
            IsTestingSound = false;
            TestSoundStatusText = "Couldn't play test chime on selected devices.";
            return;
        }

        var playingCount = result.BranchStatuses.Count(b => b.State == OutputBranchState.Playing);
        var failed = result.BranchStatuses.Where(b => b.State == OutputBranchState.Failed).ToList();
        TestSoundStatusText = failed.Count > 0
            ? $"Chime on {playingCount} active · Failed: {string.Join(", ", failed.Select(f => f.DeviceName))}"
            : $"Testing chime on {playingCount} {(playingCount == 1 ? "output" : "outputs")}…";

        _testAutoStopTimer.Stop();
        _testAutoStopTimer.Start();
    }

    private async Task PrimaryActionAsync()
    {
        if (IsSharing)
        {
            _soundFeedback.Play(UiSoundCue.Stop);
            await _sharingService.StopSharingAsync();
            if (_settingsService.Current.NotifySessionStarted)
            {
                ShowToast("Listening session ended.");
            }
            return;
        }

        if (_deviceManager.Devices.Count <= 1)
        {
            DevicesViewModel.OpenBluetoothSettings();
            return;
        }

        var selected = _deviceManager.SelectedDevices;
        var defaultSource = _deviceManager.DefaultDevice;
        var hasSecondaryTarget = defaultSource is not null
            ? selected.Any(d => !string.Equals(d.Id, defaultSource.Id, StringComparison.OrdinalIgnoreCase))
            : selected.Count >= 2;

        if (!hasSecondaryTarget)
        {
            CurrentSection = AppSection.Devices;
            return;
        }

        var state = await _sharingService.StartSharingAsync();
        if (state.IsSharing)
        {
            _soundFeedback.Play(UiSoundCue.Start);
            if (_settingsService.Current.NotifySessionStarted)
            {
                ShowToast("You're all listening.");
            }
        }
    }

    private void OnDevicesUpdated(object? sender, EventArgs e) => DispatchRefresh(checkDeviceDelta: true);
    private void OnSharingStateChanged(object? sender, SharingState e) => DispatchRefresh(checkDeviceDelta: false);
    private void OnMusicStateChanged(object? sender, PlaybackState e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(RefreshMusicBindings);
        }
        else
        {
            RefreshMusicBindings();
        }
    }

    private void RefreshMusicBindings()
    {
        OnPropertyChanged(nameof(HasTrackLoaded));
        OnPropertyChanged(nameof(IsMusicPlaying));
        OnPropertyChanged(nameof(TrackTitle));
        OnPropertyChanged(nameof(TrackArtist));
        OnPropertyChanged(nameof(TrackAlbumArtBytes));
        OnPropertyChanged(nameof(TrackDurationSeconds));
        OnPropertyChanged(nameof(TrackPositionSeconds));
        OnPropertyChanged(nameof(TrackTimeText));
        PlayPauseMusicCommand.RaiseCanExecuteChanged();
        NextTrackCommand.RaiseCanExecuteChanged();
        PreviousTrackCommand.RaiseCanExecuteChanged();
    }

    private void DispatchRefresh(bool checkDeviceDelta)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => RefreshHomeState(checkDeviceDelta));
        }
        else
        {
            RefreshHomeState(checkDeviceDelta);
        }
    }

    private void RefreshCopyOnly()
    {
        var copy = SyncCopy.GetListenCopy(
            _deviceManager.Devices,
            _deviceManager.SelectedDevices,
            _sharingService.CurrentState,
            _deviceManager.DefaultDevice,
            IsMuted);
        VisualState = copy.State;
        HeroTitle = copy.Headline;
        HeroAccent = copy.AccentLine;
        HeroSubtitle = copy.Subtitle;
        ShareButtonText = copy.PrimaryAction;
    }

    private void RefreshHomeState(bool checkDeviceDelta = false)
    {
        var allDevices = _deviceManager.Devices;
        var selected = _deviceManager.SelectedDevices;
        var sharing = _sharingService.CurrentState;

        if (checkDeviceDelta)
        {
            var currentIds = allDevices.Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (_hasInitializedKnownDevices)
            {
                var addedCount = currentIds.Count(id => !_knownConnectedIds.Contains(id));
                var removedCount = _knownConnectedIds.Count(id => !currentIds.Contains(id));

                if (addedCount > 0 && _settingsService.Current.NotifyDeviceConnected)
                {
                    _soundFeedback.Play(UiSoundCue.Reconnect);
                    ShowToast(addedCount == 1 ? "Headphones connected." : $"{addedCount} devices connected.");
                }
                else if (removedCount > 0 && _settingsService.Current.NotifyDeviceDisconnected)
                {
                    _soundFeedback.Play(UiSoundCue.Disconnect);
                    ShowToast("A device disconnected.");
                }
            }
            _knownConnectedIds = currentIds;
            _hasInitializedKnownDevices = true;
        }

        HasConnectedDevices = allDevices.Count > 0;
        IsSharing = sharing.IsSharing;
        SelectedCount = selected.Count;

        if (IsSharing)
        {
            _meterTimer.Start();
        }
        else
        {
            _meterTimer.Stop();
            SignalLevel = 0.0;
        }

        ConnectedCountText = allDevices.Count == 1 ? "1 connected" : $"{allDevices.Count} connected";
        SelectedCountText = selected.Count == 1 ? "1 in group" : $"{selected.Count} in group";
        FooterConnectionText = ConnectedCountText;

        var activeOutputIds = sharing.ActiveOutputs
            .Where(o => o.State == AudioOutputState.Playing)
            .Select(o => o.DeviceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (IsSharing && sharing.SourceDevice is not null)
        {
            activeOutputIds.Add(sharing.SourceDevice.Id);
        }

        var mutedSet = _settingsService.Current.MutedDeviceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        ListeningGroupDevices.Clear();
        foreach (var d in allDevices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(d.Id, out var saved)
                ? saved
                : 1.0f;
            _settingsService.Current.PerDeviceDelayMs.TryGetValue(d.Id, out var delayMs);
            var isDevMuted = mutedSet.Contains(d.Id);

            var item = new DeviceItemViewModel(
                d,
                OnListeningGroupDeviceToggled,
                vol,
                (id, v) =>
                {
                    _settingsService.Current.PerDeviceVolume[id] = v;
                    if (!_settingsService.Current.MutedDeviceIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                    {
                        _audioEngine.SetOutputVolume(id, v);
                    }
                    _ = _settingsService.SaveAsync();
                },
                isDevMuted,
                OnDeviceMuteChanged,
                delayMs,
                (id, ms) =>
                {
                    _settingsService.Current.PerDeviceDelayMs[id] = ms;
                    _audioEngine.SetOutputDelayMs(id, ms);
                    _ = _settingsService.SaveAsync();
                })
            {
                IsActivelyPlaying = activeOutputIds.Contains(d.Id)
            };
            ListeningGroupDevices.Add(item);
        }

        RefreshCopyOnly();

        var firstFailed = sharing.FailedOutputs.FirstOrDefault();
        HasErrorBanner = IsSharing && (sharing.State == AudioEngineState.Partial || firstFailed is not null)
                         || (!IsSharing && !string.IsNullOrWhiteSpace(sharing.ErrorMessage));
        StatusBannerMessage = firstFailed is not null
            ? $"{firstFailed.DeviceName}: {firstFailed.ErrorMessage ?? "Disconnected"}"
            : HasErrorBanner ? (sharing.ErrorMessage ?? sharing.UserMessage) : null;

        ToggleSharingCommand.RaiseCanExecuteChanged();
    }

    private void OnListeningGroupDeviceToggled(string deviceId, bool isSelected)
    {
        _soundFeedback.Play(isSelected ? UiSoundCue.Select : UiSoundCue.Deselect);
        _deviceManager.SetDeviceSelected(deviceId, isSelected);
    }

    private void OnDeviceMuteChanged(string deviceId, bool isMuted)
    {
        _soundFeedback.Play(UiSoundCue.Mute);
        _settingsService.Current.MutedDeviceIds.RemoveAll(id => string.Equals(id, deviceId, StringComparison.OrdinalIgnoreCase));
        if (isMuted)
        {
            _settingsService.Current.MutedDeviceIds.Add(deviceId);
            _audioEngine.SetOutputVolume(deviceId, 0.0f);
        }
        else
        {
            var restored = _settingsService.Current.PerDeviceVolume.TryGetValue(deviceId, out var v) ? v : 1.0f;
            _audioEngine.SetOutputVolume(deviceId, restored);
        }

        _ = _settingsService.SaveAsync();
    }

    private static string FormatThemeLabel(AppThemeMode mode) => mode switch
    {
        AppThemeMode.Light => "Light",
        AppThemeMode.Dark => "Dark",
        _ => "System"
    };
}
