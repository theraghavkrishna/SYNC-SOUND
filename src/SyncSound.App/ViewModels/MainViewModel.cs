using System.Collections.ObjectModel;
using System.Windows;
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
    private readonly ISettingsService _settingsService;

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
    private double _masterVolume = 1.0;
    private double _preMuteVolume = 1.0;
    private bool _isMuted;

    public MainViewModel(
        IDeviceManagerService deviceManager,
        ISystemSharingService sharingService,
        IAudioEngine audioEngine,
        ISettingsService settingsService,
        DevicesViewModel devicesViewModel,
        SettingsViewModel settingsViewModel,
        QuickSidebarViewModel quickSidebarViewModel)
    {
        _deviceManager = deviceManager;
        _sharingService = sharingService;
        _audioEngine = audioEngine;
        _settingsService = settingsService;

        DevicesViewModel = devicesViewModel;
        SettingsViewModel = settingsViewModel;
        QuickSidebarViewModel = quickSidebarViewModel;

        // Guarantee SYNC SOUND never opens silently muted from a stale 0% save
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
        OpenSidebarCommand = new RelayCommand(() => OpenSidebarRequested?.Invoke(this, EventArgs.Empty));
        ManageDevicesCommand = new RelayCommand(() => CurrentSection = AppSection.Devices);

        _deviceManager.DevicesUpdated += OnDevicesUpdated;
        _sharingService.StateChanged += OnSharingStateChanged;
        SettingsViewModel.HotkeyUpdated += (_, shortcut) => ShortcutHint = shortcut;
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
    public RelayCommand OpenSidebarCommand { get; }
    public RelayCommand ManageDevicesCommand { get; }

    public event EventHandler? OpenSidebarRequested;

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

    public string HeroTitle
    {
        get => _heroTitle;
        private set => SetProperty(ref _heroTitle, value);
    }

    public string HeroAccent
    {
        get => _heroAccent;
        private set => SetProperty(ref _heroAccent, value);
    }

    public string HeroSubtitle
    {
        get => _heroSubtitle;
        private set => SetProperty(ref _heroSubtitle, value);
    }

    public string ShareButtonText
    {
        get => _shareButtonText;
        private set => SetProperty(ref _shareButtonText, value);
    }

    public string FooterConnectionText
    {
        get => _footerConnectionText;
        private set => SetProperty(ref _footerConnectionText, value);
    }

    public string ConnectedCountText
    {
        get => _connectedCountText;
        private set => SetProperty(ref _connectedCountText, value);
    }

    public string SelectedCountText
    {
        get => _selectedCountText;
        private set => SetProperty(ref _selectedCountText, value);
    }

    public string ShortcutHint
    {
        get => _shortcutHint;
        private set => SetProperty(ref _shortcutHint, value);
    }

    public bool IsSharing
    {
        get => _isSharing;
        private set => SetProperty(ref _isSharing, value);
    }

    public bool HasConnectedDevices
    {
        get => _hasConnectedDevices;
        private set => SetProperty(ref _hasConnectedDevices, value);
    }

    public bool HasErrorBanner
    {
        get => _hasErrorBanner;
        private set => SetProperty(ref _hasErrorBanner, value);
    }

    public string? StatusBannerMessage
    {
        get => _statusBannerMessage;
        private set => SetProperty(ref _statusBannerMessage, value);
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

    public void Dispose()
    {
        _deviceManager.DevicesUpdated -= OnDevicesUpdated;
        _sharingService.StateChanged -= OnSharingStateChanged;
        SettingsViewModel.Dispose();
        QuickSidebarViewModel.Dispose();
    }

    private void ToggleMute()
    {
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

    private Task PrimaryActionAsync()
    {
        if (IsSharing)
        {
            return _sharingService.StopSharingAsync();
        }

        if (_deviceManager.Devices.Count <= 1)
        {
            DevicesViewModel.OpenBluetoothSettings();
            return Task.CompletedTask;
        }

        var selected = _deviceManager.SelectedDevices;
        var defaultSource = _deviceManager.DefaultDevice;
        var hasSecondaryTarget = defaultSource is not null
            ? selected.Any(d => !string.Equals(d.Id, defaultSource.Id, StringComparison.OrdinalIgnoreCase) && !d.IsSpeakerDevice)
            : selected.Count(d => !d.IsSpeakerDevice) >= 2;

        if (!hasSecondaryTarget)
        {
            CurrentSection = AppSection.Devices;
            return Task.CompletedTask;
        }

        return _sharingService.StartSharingAsync();
    }

    private void OnDevicesUpdated(object? sender, EventArgs e) => DispatchRefresh();
    private void OnSharingStateChanged(object? sender, SharingState e) => DispatchRefresh();

    private void DispatchRefresh()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(RefreshHomeState);
        }
        else
        {
            RefreshHomeState();
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

    private void RefreshHomeState()
    {
        var allDevices = _deviceManager.Devices;
        var selected = _deviceManager.SelectedDevices;
        var sharing = _sharingService.CurrentState;
        var defaultSource = _deviceManager.DefaultDevice;

        HasConnectedDevices = allDevices.Count > 0;
        IsSharing = sharing.IsSharing;

        ConnectedCountText = allDevices.Count == 1 ? "1 connected" : $"{allDevices.Count} connected";
        SelectedCountText = selected.Count == 1 ? "1 in group" : $"{selected.Count} in group";
        FooterConnectionText = ConnectedCountText;

        ListeningGroupDevices.Clear();
        foreach (var d in allDevices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(d.Id, out var saved)
                ? saved
                : 1.0f;
            ListeningGroupDevices.Add(new DeviceItemViewModel(
                d,
                OnListeningGroupDeviceToggled,
                vol,
                (id, v) =>
                {
                    _settingsService.Current.PerDeviceVolume[id] = v;
                    _audioEngine.SetOutputVolume(id, v);
                    _ = _settingsService.SaveAsync();
                }));
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
        var clicked = _deviceManager.Devices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
        if (clicked is null)
        {
            return;
        }

        if (isSelected)
        {
            if (clicked.IsSpeakerDevice)
            {
                var otherHeadphones = _deviceManager.SelectedDevices.Where(d => !d.IsSpeakerDevice).ToList();
                if (otherHeadphones.Count > 0)
                {
                    DevicesViewModel.RequestSpeakerSwitch(clicked);
                    CurrentSection = AppSection.Devices;
                    return;
                }
            }
            else
            {
                foreach (var spk in _deviceManager.SelectedDevices.Where(d => d.IsSpeakerDevice))
                {
                    _deviceManager.SetDeviceSelected(spk.Id, false);
                }
            }
        }

        _deviceManager.SetDeviceSelected(deviceId, isSelected);
    }
}
