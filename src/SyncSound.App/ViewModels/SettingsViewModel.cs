using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using SyncSound.App.Services;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Bluetooth;
using SyncSound.Windows.Hotkeys;
using SyncSound.Windows.Startup;

namespace SyncSound.App.ViewModels;

public enum PreferencesCategory
{
    Appearance = 0,
    QuickAccess,
    Listening,
    Windows,
    Notifications,
    About
}

public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ThemeService _themeService;
    private readonly IGlobalHotkeyService _hotkeyService;
    private readonly IStartupRegistrationService _startupService;
    private readonly IWindowsSettingsLauncher _windowsLauncher;
    private readonly IAudioEngine _audioEngine;
    private readonly IDeviceManagerService _deviceManager;

    private string _shortcutInput;
    private string? _shortcutStatusMessage;
    private bool _hasShortcutError;
    private bool _startWithWindows;
    private bool _startMinimized;
    private bool _closeToTray;
    private bool _sidebarAlwaysOnTop;
    private bool _sidebarClickOutsideToClose;
    private bool _rememberSidebarPosition;
    private bool _autoSelectPreferredDevices;
    private bool _reducedMotion;
    private bool _enableUiSounds;
    private bool _notifyDeviceConnected;
    private bool _notifyDeviceDisconnected;
    private bool _notifyDeviceRecovered;
    private bool _notifySessionStarted;
    private bool _notifyLowBattery;
    private bool _diagnosticsExpanded;
    private PreferencesCategory _selectedCategory = PreferencesCategory.Appearance;

    public SettingsViewModel(
        ISettingsService settingsService,
        ThemeService themeService,
        IGlobalHotkeyService hotkeyService,
        IStartupRegistrationService startupService,
        IWindowsSettingsLauncher windowsLauncher,
        IAudioEngine audioEngine,
        IDeviceManagerService deviceManager)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        _hotkeyService = hotkeyService;
        _startupService = startupService;
        _windowsLauncher = windowsLauncher;
        _audioEngine = audioEngine;
        _deviceManager = deviceManager;

        var s = _settingsService.Current;
        _shortcutInput = s.Hotkey;
        _startWithWindows = s.StartWithWindows;
        _startMinimized = s.StartMinimized;
        _closeToTray = s.CloseToTray;
        _sidebarAlwaysOnTop = s.SidebarAlwaysOnTop;
        _sidebarClickOutsideToClose = s.SidebarClickOutsideToClose;
        _rememberSidebarPosition = s.RememberSidebarPosition;
        _autoSelectPreferredDevices = s.AutoSelectPreferredDevices;
        _reducedMotion = s.ReducedMotion;
        _enableUiSounds = s.EnableUiSounds;
        _notifyDeviceConnected = s.NotifyDeviceConnected;
        _notifyDeviceDisconnected = s.NotifyDeviceDisconnected;
        _notifyDeviceRecovered = s.NotifyDeviceRecovered;
        _notifySessionStarted = s.NotifySessionStarted;
        _notifyLowBattery = s.NotifyLowBattery;

        ApplyShortcutCommand = new RelayCommand(ApplyShortcut);
        OpenLogsFolderCommand = new RelayCommand(() => _windowsLauncher.OpenLogsFolder());
        OpenSoundSettingsCommand = new RelayCommand(() => _windowsLauncher.OpenSoundSettings());
        OpenSidebarPreviewCommand = new RelayCommand(() => OpenSidebarPreviewRequested?.Invoke(this, EventArgs.Empty));
        OpenGitHubCommand = new RelayCommand(() => OpenUrl("https://github.com/theraghavkrishna/SYNC-SOUND"));
        OpenReleasesCommand = new RelayCommand(() => OpenUrl("https://github.com/theraghavkrishna/SYNC-SOUND/releases"));
        SelectThemeCommand = new RelayCommand(p =>
        {
            if (p is string str && Enum.TryParse<AppThemeMode>(str, true, out var mode))
            {
                ThemeMode = mode;
            }
            else if (p is AppThemeMode m)
            {
                ThemeMode = m;
            }
        });

        _audioEngine.StatusChanged += OnEngineStatusChanged;
        _deviceManager.DevicesUpdated += OnDevicesUpdated;
        RefreshDiagnostics();
    }

    public ObservableCollection<DiagnosticOutputItem> DiagnosticOutputs { get; } = [];
    public ObservableCollection<DeviceDelayItem> DelaySettings { get; } = [];

    public RelayCommand ApplyShortcutCommand { get; }
    public RelayCommand OpenLogsFolderCommand { get; }
    public RelayCommand OpenSoundSettingsCommand { get; }
    public RelayCommand OpenSidebarPreviewCommand { get; }
    public RelayCommand OpenGitHubCommand { get; }
    public RelayCommand OpenReleasesCommand { get; }
    public RelayCommand SelectThemeCommand { get; }

    public event EventHandler? OpenSidebarPreviewRequested;
    public event EventHandler? SidebarPreferencesChanged;
    public event EventHandler? AppearancePreferencesChanged;
    public event EventHandler<string>? HotkeyUpdated;

    public PreferencesCategory SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                OnPropertyChanged(nameof(IsAppearanceCategory));
                OnPropertyChanged(nameof(IsQuickAccessCategory));
                OnPropertyChanged(nameof(IsListeningCategory));
                OnPropertyChanged(nameof(IsWindowsCategory));
                OnPropertyChanged(nameof(IsNotificationsCategory));
                OnPropertyChanged(nameof(IsAboutCategory));
            }
        }
    }

    public bool IsAppearanceCategory { get => _selectedCategory == PreferencesCategory.Appearance; set { if (value) SelectedCategory = PreferencesCategory.Appearance; } }
    public bool IsQuickAccessCategory { get => _selectedCategory == PreferencesCategory.QuickAccess; set { if (value) SelectedCategory = PreferencesCategory.QuickAccess; } }
    public bool IsListeningCategory { get => _selectedCategory == PreferencesCategory.Listening; set { if (value) SelectedCategory = PreferencesCategory.Listening; } }
    public bool IsWindowsCategory { get => _selectedCategory == PreferencesCategory.Windows; set { if (value) SelectedCategory = PreferencesCategory.Windows; } }
    public bool IsNotificationsCategory { get => _selectedCategory == PreferencesCategory.Notifications; set { if (value) SelectedCategory = PreferencesCategory.Notifications; } }
    public bool IsAboutCategory { get => _selectedCategory == PreferencesCategory.About; set { if (value) SelectedCategory = PreferencesCategory.About; } }

    public AppThemeMode ThemeMode
    {
        get => _settingsService.Current.ThemeMode;
        set
        {
            if (_settingsService.Current.ThemeMode != value)
            {
                _settingsService.Current.ThemeMode = value;
                _themeService.ApplyTheme(value);
                _ = _settingsService.SaveAsync();
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLightTheme));
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(IsSystemTheme));
                AppearancePreferencesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsLightTheme { get => ThemeMode == AppThemeMode.Light; set { if (value) ThemeMode = AppThemeMode.Light; } }
    public bool IsDarkTheme { get => ThemeMode == AppThemeMode.Dark; set { if (value) ThemeMode = AppThemeMode.Dark; } }
    public bool IsSystemTheme { get => ThemeMode == AppThemeMode.System; set { if (value) ThemeMode = AppThemeMode.System; } }

    public string AppVersion { get; } =
        $"SYNC SOUND v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "4.0.0"}";

    public bool ReducedMotion
    {
        get => _reducedMotion;
        set
        {
            if (SetProperty(ref _reducedMotion, value))
            {
                _settingsService.Current.ReducedMotion = value;
                _ = _settingsService.SaveAsync();
                AppearancePreferencesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool EnableUiSounds
    {
        get => _enableUiSounds;
        set
        {
            if (SetProperty(ref _enableUiSounds, value))
            {
                _settingsService.Current.EnableUiSounds = value;
                _ = _settingsService.SaveAsync();
            }
        }
    }

    public string ShortcutInput { get => _shortcutInput; set => SetProperty(ref _shortcutInput, value); }

    public string? ShortcutStatusMessage
    {
        get => _shortcutStatusMessage;
        private set
        {
            if (SetProperty(ref _shortcutStatusMessage, value))
            {
                OnPropertyChanged(nameof(HasShortcutStatus));
            }
        }
    }

    public bool HasShortcutStatus => !string.IsNullOrWhiteSpace(_shortcutStatusMessage);
    public bool HasShortcutError { get => _hasShortcutError; private set => SetProperty(ref _hasShortcutError, value); }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (SetProperty(ref _startWithWindows, value))
            {
                _settingsService.Current.StartWithWindows = value;
                _startupService.SetEnabled(value, _startMinimized);
                _ = _settingsService.SaveAsync();
            }
        }
    }

    public bool StartMinimized
    {
        get => _startMinimized;
        set
        {
            if (SetProperty(ref _startMinimized, value))
            {
                _settingsService.Current.StartMinimized = value;
                if (_startWithWindows)
                {
                    _startupService.SetEnabled(true, value);
                }

                _ = _settingsService.SaveAsync();
            }
        }
    }

    public bool CloseToTray
    {
        get => _closeToTray;
        set
        {
            if (SetProperty(ref _closeToTray, value))
            {
                _settingsService.Current.CloseToTray = value;
                _ = _settingsService.SaveAsync();
            }
        }
    }

    public bool SidebarAlwaysOnTop
    {
        get => _sidebarAlwaysOnTop;
        set
        {
            if (SetProperty(ref _sidebarAlwaysOnTop, value))
            {
                _settingsService.Current.SidebarAlwaysOnTop = value;
                _ = _settingsService.SaveAsync();
                SidebarPreferencesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool SidebarClickOutsideToClose
    {
        get => _sidebarClickOutsideToClose;
        set
        {
            if (SetProperty(ref _sidebarClickOutsideToClose, value))
            {
                _settingsService.Current.SidebarClickOutsideToClose = value;
                _ = _settingsService.SaveAsync();
                SidebarPreferencesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool RememberSidebarPosition
    {
        get => _rememberSidebarPosition;
        set
        {
            if (SetProperty(ref _rememberSidebarPosition, value))
            {
                _settingsService.Current.RememberSidebarPosition = value;
                if (!value)
                {
                    _settingsService.Current.SidebarLeft = null;
                    _settingsService.Current.SidebarTop = null;
                }
                _ = _settingsService.SaveAsync();
                SidebarPreferencesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool AutoSelectPreferredDevices
    {
        get => _autoSelectPreferredDevices;
        set
        {
            if (SetProperty(ref _autoSelectPreferredDevices, value))
            {
                _settingsService.Current.AutoSelectPreferredDevices = value;
                _ = _settingsService.SaveAsync();
            }
        }
    }

    public bool NotifyDeviceConnected
    {
        get => _notifyDeviceConnected;
        set { if (SetProperty(ref _notifyDeviceConnected, value)) { _settingsService.Current.NotifyDeviceConnected = value; _ = _settingsService.SaveAsync(); } }
    }

    public bool NotifyDeviceDisconnected
    {
        get => _notifyDeviceDisconnected;
        set { if (SetProperty(ref _notifyDeviceDisconnected, value)) { _settingsService.Current.NotifyDeviceDisconnected = value; _ = _settingsService.SaveAsync(); } }
    }

    public bool NotifyDeviceRecovered
    {
        get => _notifyDeviceRecovered;
        set { if (SetProperty(ref _notifyDeviceRecovered, value)) { _settingsService.Current.NotifyDeviceRecovered = value; _ = _settingsService.SaveAsync(); } }
    }

    public bool NotifySessionStarted
    {
        get => _notifySessionStarted;
        set { if (SetProperty(ref _notifySessionStarted, value)) { _settingsService.Current.NotifySessionStarted = value; _ = _settingsService.SaveAsync(); } }
    }

    public bool NotifyLowBattery
    {
        get => _notifyLowBattery;
        set { if (SetProperty(ref _notifyLowBattery, value)) { _settingsService.Current.NotifyLowBattery = value; _ = _settingsService.SaveAsync(); } }
    }

    public bool DiagnosticsExpanded { get => _diagnosticsExpanded; set => SetProperty(ref _diagnosticsExpanded, value); }

    public void ApplyShortcut()
    {
        if (_hotkeyService.TryRegister(ShortcutInput, out var error))
        {
            ShortcutInput = _hotkeyService.CurrentShortcut;
            _settingsService.Current.Hotkey = _hotkeyService.CurrentShortcut;
            _ = _settingsService.SaveAsync();
            HasShortcutError = false;
            ShortcutStatusMessage = $"Active shortcut: {_hotkeyService.CurrentShortcut}";
            HotkeyUpdated?.Invoke(this, _hotkeyService.CurrentShortcut);
        }
        else
        {
            HasShortcutError = true;
            ShortcutStatusMessage = error ?? "This shortcut is already being used.";
        }
    }

    public void RefreshHotkeyState(bool isRegistered, string? errorMessage)
    {
        ShortcutInput = _settingsService.Current.Hotkey;
        if (isRegistered)
        {
            HasShortcutError = false;
            ShortcutStatusMessage = $"Active shortcut: {_hotkeyService.CurrentShortcut}";
        }
        else
        {
            HasShortcutError = true;
            ShortcutStatusMessage = errorMessage ?? "This shortcut is already being used.";
        }
    }

    public void Dispose()
    {
        _audioEngine.StatusChanged -= OnEngineStatusChanged;
        _deviceManager.DevicesUpdated -= OnDevicesUpdated;
    }

    private void OnEngineStatusChanged(object? sender, AudioEngineStatusSnapshot e) => DispatchRefresh();
    private void OnDevicesUpdated(object? sender, EventArgs e) => DispatchRefresh();

    private void DispatchRefresh()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(RefreshDiagnostics);
        }
        else
        {
            RefreshDiagnostics();
        }
    }

    private void RefreshDiagnostics()
    {
        DiagnosticOutputs.Clear();
        foreach (var output in _audioEngine.Status.Outputs)
        {
            DiagnosticOutputs.Add(new DiagnosticOutputItem(
                output.DeviceName,
                output.State.ToString(),
                output.FormatSummary ?? "—",
                $"{output.EstimatedLatencyMs} ms est · {output.BufferedMs:0.#} ms buf · {output.UnderrunCount} underruns",
                output.ErrorMessage));
        }

        DelaySettings.Clear();
        foreach (var device in _deviceManager.Devices.Where(d => !d.IsHandsFreeProfile))
        {
            _settingsService.Current.PerDeviceDelayMs.TryGetValue(device.Id, out var delayMs);
            DelaySettings.Add(new DeviceDelayItem(device.Id, device.Name, delayMs, OnDelayChanged));
        }
    }

    private void OnDelayChanged(string deviceId, int delayMs)
    {
        _settingsService.Current.PerDeviceDelayMs[deviceId] = delayMs;
        _audioEngine.SetOutputDelayMs(deviceId, delayMs);
        _ = _settingsService.SaveAsync();
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Ignore shell launch errors.
        }
    }

    public sealed record DiagnosticOutputItem(string Name, string State, string Format, string Metrics, string? Error);

    public sealed class DeviceDelayItem : ViewModelBase
    {
        private readonly Action<string, int> _onChanged;
        private int _delayMs;

        public DeviceDelayItem(string deviceId, string deviceName, int delayMs, Action<string, int> onChanged)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            _delayMs = delayMs;
            _onChanged = onChanged;
        }

        public string DeviceId { get; }
        public string DeviceName { get; }

        public int DelayMs
        {
            get => _delayMs;
            set
            {
                if (SetProperty(ref _delayMs, Math.Clamp(value, 0, 300)))
                {
                    OnPropertyChanged(nameof(DelayLabel));
                    _onChanged(DeviceId, _delayMs);
                }
            }
        }

        public string DelayLabel => $"{_delayMs} ms";
    }
}
