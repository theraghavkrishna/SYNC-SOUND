using System.Collections.ObjectModel;
using System.Windows;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.App.ViewModels;

public enum AppSection
{
    Home = 0,
    Devices,
    Player,
    Settings
}

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly ISystemSharingService _sharingService;
    private readonly ISettingsService _settingsService;

    private AppSection _currentSection = AppSection.Home;
    private string _heroTitle = "Ready to listen.";
    private string _heroSubtitle = "Connect your headphones, choose everyone, and press Start sharing.";
    private string _shareButtonText = "Start sharing";
    private string _footerConnectionText = "0 devices connected";
    private string _shortcutHint = "Ctrl+Shift+S";
    private bool _isSharing;
    private bool _hasErrorBanner;
    private string? _statusBannerMessage;
    private bool _hasConnectedDevices;

    public MainViewModel(
        IDeviceManagerService deviceManager,
        ISystemSharingService sharingService,
        ISettingsService settingsService,
        DevicesViewModel devicesViewModel,
        PlayerViewModel playerViewModel,
        SettingsViewModel settingsViewModel,
        QuickSidebarViewModel quickSidebarViewModel)
    {
        _deviceManager = deviceManager;
        _sharingService = sharingService;
        _settingsService = settingsService;

        DevicesViewModel = devicesViewModel;
        PlayerViewModel = playerViewModel;
        SettingsViewModel = settingsViewModel;
        QuickSidebarViewModel = quickSidebarViewModel;

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

        ToggleSharingCommand = new AsyncRelayCommand(ToggleSharingAsync, () => HasConnectedDevices || IsSharing);
        OpenSidebarCommand = new RelayCommand(() => OpenSidebarRequested?.Invoke(this, EventArgs.Empty));
        ManageDevicesCommand = new RelayCommand(() => CurrentSection = AppSection.Devices);

        _deviceManager.DevicesUpdated += OnDevicesUpdated;
        _sharingService.StateChanged += OnSharingStateChanged;
        _settingsService = settingsService;
        SettingsViewModel.HotkeyUpdated += (_, shortcut) => ShortcutHint = shortcut;
        ShortcutHint = _settingsService.Current.Hotkey;

        RefreshHomeState();
    }

    public DevicesViewModel DevicesViewModel { get; }
    public PlayerViewModel PlayerViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }
    public QuickSidebarViewModel QuickSidebarViewModel { get; }

    public ObservableCollection<string> HomeDeviceChips { get; } = [];

    public RelayCommand NavigateCommand { get; }
    public AsyncRelayCommand ToggleSharingCommand { get; }
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
                OnPropertyChanged(nameof(IsHomeSelected));
                OnPropertyChanged(nameof(IsDevicesSelected));
                OnPropertyChanged(nameof(IsPlayerSelected));
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

    public bool IsPlayerSelected
    {
        get => _currentSection == AppSection.Player;
        set { if (value) CurrentSection = AppSection.Player; }
    }

    public bool IsSettingsSelected
    {
        get => _currentSection == AppSection.Settings;
        set { if (value) CurrentSection = AppSection.Settings; }
    }

    public string HeroTitle
    {
        get => _heroTitle;
        private set => SetProperty(ref _heroTitle, value);
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

    public void Dispose()
    {
        _deviceManager.DevicesUpdated -= OnDevicesUpdated;
        _sharingService.StateChanged -= OnSharingStateChanged;
        PlayerViewModel.Dispose();
        SettingsViewModel.Dispose();
        QuickSidebarViewModel.Dispose();
    }

    private Task ToggleSharingAsync() =>
        IsSharing ? _sharingService.StopSharingAsync() : _sharingService.StartSharingAsync();

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

    private void RefreshHomeState()
    {
        var allDevices = _deviceManager.Devices;
        var selected = _deviceManager.SelectedDevices;
        var sharing = _sharingService.CurrentState;
        var defaultSource = _deviceManager.DefaultDevice;

        HasConnectedDevices = allDevices.Count > 0;
        IsSharing = sharing.IsSharing;
        ShareButtonText = IsSharing ? "Stop sharing" : "Start sharing";
        FooterConnectionText = allDevices.Count == 1
            ? "1 device connected"
            : $"{allDevices.Count} devices connected";

        HomeDeviceChips.Clear();
        var chipSource = selected.Count > 0 ? selected : allDevices;
        foreach (var d in chipSource.Take(4))
        {
            HomeDeviceChips.Add(d.Name);
        }

        if (allDevices.Count == 0)
        {
            HeroTitle = "Let's connect some headphones.";
            HeroSubtitle = "Pair your earbuds or plug in headphones through Windows, and they will appear here automatically.";
            StatusBannerMessage = null;
            HasErrorBanner = false;
        }
        else if (IsSharing)
        {
            HeroTitle = sharing.State == AudioEngineState.Partial
                ? sharing.UserMessage
                : "You're all listening.";

            var receiverCount = sharing.ActiveOutputs.Count;
            var sourceName = sharing.SourceDevice?.Name ?? defaultSource?.Name ?? "your PC";
            HeroSubtitle = $"{sourceName} continues playing normally while {receiverCount} additional {(receiverCount == 1 ? "device receives" : "devices receive")} shared audio.";

            HasErrorBanner = sharing.State == AudioEngineState.Partial || !string.IsNullOrWhiteSpace(sharing.ErrorMessage);
            StatusBannerMessage = HasErrorBanner ? (sharing.ErrorMessage ?? sharing.UserMessage) : null;
        }
        else
        {
            HeroTitle = "Ready to listen.";
            if (selected.Count == 0)
            {
                HeroSubtitle = "Choose the headphones you want to listen with, then press Start sharing.";
            }
            else if (defaultSource is not null && selected.Count == 1 &&
                     string.Equals(selected[0].Id, defaultSource.Id, StringComparison.OrdinalIgnoreCase))
            {
                HeroSubtitle = $"{defaultSource.Name} is already your Windows output. Select one more headphone to share together, or play local songs in Player.";
            }
            else
            {
                HeroSubtitle = $"{selected.Count} {(selected.Count == 1 ? "device" : "devices")} selected. Play anything on Windows and everyone hears it.";
            }

            HasErrorBanner = !string.IsNullOrWhiteSpace(sharing.ErrorMessage);
            StatusBannerMessage = sharing.ErrorMessage;
        }

        ToggleSharingCommand.RaiseCanExecuteChanged();
    }
}
