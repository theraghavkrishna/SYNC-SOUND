using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using SyncSound.App.Services;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Bluetooth;

namespace SyncSound.App.ViewModels;

public sealed class QuickSidebarViewModel : ViewModelBase, IDisposable
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly ISystemSharingService _sharingService;
    private readonly IAudioEngine _audioEngine;
    private readonly IMultiOutputTestPlayer _testPlayer;
    private readonly ISettingsService _settingsService;
    private readonly ISoundFeedbackService _soundFeedback;
    private readonly IWindowsSettingsLauncher _windowsLauncher;
    private readonly DispatcherTimer _testAutoStopTimer;

    private string _statusHeadline = "Everyone's here.";
    private string _statusDetail = "Listen together.";
    private string _shareButtonText = "Start listening";
    private string _countBadgeText = "0 in group";
    private SyncVisualState _visualState = SyncVisualState.Ready;
    private bool _isSharing;
    private bool _canToggleSharing;
    private int _selectedCount;
    private double _masterVolume = 1.0;
    private double _preMuteVolume = 1.0;
    private bool _isMuted;
    private double _signalLevel;
    private bool _isTestingSound;
    private string? _testStatusText;

    public QuickSidebarViewModel(
        IDeviceManagerService deviceManager,
        ISystemSharingService sharingService,
        IAudioEngine audioEngine,
        IMultiOutputTestPlayer testPlayer,
        ISettingsService settingsService,
        ISoundFeedbackService soundFeedback,
        IWindowsSettingsLauncher windowsLauncher)
    {
        _deviceManager = deviceManager;
        _sharingService = sharingService;
        _audioEngine = audioEngine;
        _testPlayer = testPlayer;
        _settingsService = settingsService;
        _soundFeedback = soundFeedback;
        _windowsLauncher = windowsLauncher;

        var initialVol = Math.Clamp(_settingsService.Current.MasterVolume, 0.0f, 1.0f);
        if (initialVol <= 0.01f)
        {
            initialVol = 1.0f;
        }

        _masterVolume = initialVol;
        _preMuteVolume = initialVol;
        _isMuted = false;

        ToggleSharingCommand = new AsyncRelayCommand(ToggleSharingAsync, () => CanToggleSharing || IsSharing);
        ToggleMuteCommand = new RelayCommand(ToggleMute);
        TestSoundCommand = new AsyncRelayCommand(ToggleTestSoundAsync, () => _deviceManager.Devices.Count > 0);
        RefreshCommand = new RelayCommand(() => _deviceManager.Refresh());
        AddHeadphonesCommand = new RelayCommand(() => _windowsLauncher.OpenBluetoothSettings());
        ManageInWindowsCommand = new RelayCommand(() => _windowsLauncher.OpenSoundSettings());
        OpenMainAppCommand = new RelayCommand(() => OpenMainAppRequested?.Invoke(this, EventArgs.Empty));
        OpenPreferencesCommand = new RelayCommand(() => OpenPreferencesRequested?.Invoke(this, EventArgs.Empty));
        CloseSidebarCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        _testAutoStopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.6) };
        _testAutoStopTimer.Tick += async (_, _) =>
        {
            _testAutoStopTimer.Stop();
            if (_testPlayer.IsPlaying)
            {
                await _testPlayer.StopAsync();
                IsTestingSound = false;
                TestStatusText = "Sound test complete.";
            }
        };

        _deviceManager.DevicesUpdated += OnDevicesOrSharingChanged;
        _sharingService.StateChanged += OnSharingStateChanged;
        Refresh();
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];

    public AsyncRelayCommand ToggleSharingCommand { get; }
    public RelayCommand ToggleMuteCommand { get; }
    public AsyncRelayCommand TestSoundCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand AddHeadphonesCommand { get; }
    public RelayCommand ManageInWindowsCommand { get; }
    public RelayCommand OpenMainAppCommand { get; }
    public RelayCommand OpenPreferencesCommand { get; }
    public RelayCommand CloseSidebarCommand { get; }

    public event EventHandler? OpenMainAppRequested;
    public event EventHandler? OpenPreferencesRequested;
    public event EventHandler? CloseRequested;
    public event EventHandler? MasterVolumeSynced;

    public SyncVisualState VisualState { get => _visualState; private set => SetProperty(ref _visualState, value); }
    public bool AlwaysOnTop => _settingsService.Current.SidebarAlwaysOnTop;
    public bool ClickOutsideToClose => _settingsService.Current.SidebarClickOutsideToClose;
    public bool RememberSidebarPosition => _settingsService.Current.RememberSidebarPosition;
    public bool ReducedMotion => _settingsService.Current.ReducedMotion;
    public ISettingsService SettingsService => _settingsService;

    public string StatusHeadline { get => _statusHeadline; private set => SetProperty(ref _statusHeadline, value); }
    public string StatusDetail { get => _statusDetail; private set => SetProperty(ref _statusDetail, value); }
    public string ShareButtonText { get => _shareButtonText; private set => SetProperty(ref _shareButtonText, value); }
    public string CountBadgeText { get => _countBadgeText; private set => SetProperty(ref _countBadgeText, value); }
    public bool IsSharing { get => _isSharing; private set => SetProperty(ref _isSharing, value); }
    public bool CanToggleSharing { get => _canToggleSharing; private set => SetProperty(ref _canToggleSharing, value); }
    public int SelectedCount { get => _selectedCount; private set => SetProperty(ref _selectedCount, value); }
    public double SignalLevel { get => _signalLevel; private set => SetProperty(ref _signalLevel, value); }
    public bool IsTestingSound { get => _isTestingSound; private set => SetProperty(ref _isTestingSound, value); }
    public string? TestStatusText { get => _testStatusText; private set { if (SetProperty(ref _testStatusText, value)) OnPropertyChanged(nameof(HasTestStatus)); } }
    public bool HasTestStatus => !string.IsNullOrWhiteSpace(_testStatusText);

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
                MasterVolumeSynced?.Invoke(this, EventArgs.Empty);
                UpdateCopyOnly();
            }
        }
    }

    public string MasterVolumePercentText => $"{(int)Math.Round(_masterVolume * 100)}%";
    public bool IsMuted { get => _isMuted; private set => SetProperty(ref _isMuted, value); }

    public void UpdateSignalLevel(double level) => SignalLevel = level;

    public void SyncMasterVolumeFromSettings()
    {
        var saved = Math.Clamp(_settingsService.Current.MasterVolume, 0.0f, 1.0f);
        if (Math.Abs(_masterVolume - saved) > 0.001)
        {
            _masterVolume = saved;
            IsMuted = saved <= 0.005;
            OnPropertyChanged(nameof(MasterVolume));
            OnPropertyChanged(nameof(MasterVolumePercentText));
            UpdateCopyOnly();
        }
    }

    public void NotifyPreferencesChanged()
    {
        OnPropertyChanged(nameof(AlwaysOnTop));
        OnPropertyChanged(nameof(ClickOutsideToClose));
        OnPropertyChanged(nameof(RememberSidebarPosition));
        OnPropertyChanged(nameof(ReducedMotion));
    }

    public void SyncVolumesFromSettings()
    {
        SyncMasterVolumeFromSettings();
        var mutedSet = _settingsService.Current.MutedDeviceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Devices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(item.Id, out var saved) ? saved : 1.0f;
            item.SyncVolumeSilently(vol, mutedSet.Contains(item.Id));
        }
    }

    public void Dispose()
    {
        _testAutoStopTimer.Stop();
        _deviceManager.DevicesUpdated -= OnDevicesOrSharingChanged;
        _sharingService.StateChanged -= OnSharingStateChanged;
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
            TestStatusText = "Sound test stopped.";
            return;
        }

        var targets = _deviceManager.SelectedDevices;
        if (targets.Count == 0)
        {
            targets = _deviceManager.Devices;
        }

        if (targets.Count == 0)
        {
            return;
        }

        IsTestingSound = true;
        TestStatusText = "Playing chime on group…";
        var res = await _testPlayer.StartAsync(targets, AudioTestMode.RhythmicChimePulse);
        if (!res.AnyStarted)
        {
            IsTestingSound = false;
            TestStatusText = "Couldn't play test chime.";
            return;
        }

        _testAutoStopTimer.Stop();
        _testAutoStopTimer.Start();
    }

    private async Task ToggleSharingAsync()
    {
        if (IsSharing)
        {
            _soundFeedback.Play(UiSoundCue.Stop);
            await _sharingService.StopSharingAsync();
        }
        else
        {
            var state = await _sharingService.StartSharingAsync();
            if (state.IsSharing)
            {
                _soundFeedback.Play(UiSoundCue.Start);
            }
        }
    }

    private void OnDevicesOrSharingChanged(object? sender, EventArgs e) => DispatchRefresh();
    private void OnSharingStateChanged(object? sender, SharingState e) => DispatchRefresh();

    private void DispatchRefresh()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(Refresh);
        }
        else
        {
            Refresh();
        }
    }

    private void UpdateCopyOnly()
    {
        var copy = SyncCopy.GetListenCopy(
            _deviceManager.Devices,
            _deviceManager.SelectedDevices,
            _sharingService.CurrentState,
            _deviceManager.DefaultDevice,
            IsMuted);
        VisualState = copy.State;
        StatusHeadline = copy.Headline;
        StatusDetail = copy.AccentLine;
        ShareButtonText = _sharingService.CurrentState.IsSharing ? "Stop listening" : "Start listening";
    }

    private void Refresh()
    {
        var sharing = _sharingService.CurrentState;
        var allDevices = _deviceManager.Devices;
        var selected = _deviceManager.SelectedDevices;
        var defaultSource = _deviceManager.DefaultDevice;

        var activeIds = sharing.ActiveOutputs
            .Where(o => o.State == AudioOutputState.Playing)
            .Select(o => o.DeviceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (sharing.IsSharing && sharing.SourceDevice is not null)
        {
            activeIds.Add(sharing.SourceDevice.Id);
        }

        var mutedSet = _settingsService.Current.MutedDeviceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expandedIds = Devices.Where(x => x.IsExpanded).Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Devices.Clear();
        foreach (var d in allDevices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(d.Id, out var saved) ? saved : 1.0f;
            Devices.Add(new DeviceItemViewModel(
                d,
                (id, sel) =>
                {
                    _soundFeedback.Play(sel ? UiSoundCue.Select : UiSoundCue.Deselect);
                    _deviceManager.SetDeviceSelected(id, sel);
                },
                vol,
                OnDeviceVolumeChanged,
                mutedSet.Contains(d.Id),
                OnDeviceMuteChanged)
            {
                IsActivelyPlaying = activeIds.Contains(d.Id),
                IsExpanded = expandedIds.Contains(d.Id)
            });
        }

        IsSharing = sharing.IsSharing;
        SelectedCount = selected.Count;
        var hasSecondary = defaultSource is not null
            ? selected.Any(d => !string.Equals(d.Id, defaultSource.Id, StringComparison.OrdinalIgnoreCase))
            : selected.Count >= 2;
        CanToggleSharing = hasSecondary;

        CountBadgeText = $"{selected.Count} of {allDevices.Count} in group";
        UpdateCopyOnly();
        ToggleSharingCommand.RaiseCanExecuteChanged();
        TestSoundCommand.RaiseCanExecuteChanged();
    }

    private void OnDeviceVolumeChanged(string deviceId, float volume)
    {
        _settingsService.Current.PerDeviceVolume[deviceId] = volume;
        if (!_settingsService.Current.MutedDeviceIds.Contains(deviceId, StringComparer.OrdinalIgnoreCase))
        {
            _audioEngine.SetOutputVolume(deviceId, volume);
        }
        _ = _settingsService.SaveAsync();
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
}
