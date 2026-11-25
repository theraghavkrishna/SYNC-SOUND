using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Bluetooth;
using SyncSound.Windows.Hotkeys;
using SyncSound.Windows.Startup;

namespace SyncSound.App.ViewModels;

public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly ISettingsService _settingsService;
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
    private bool _autoSelectPreferredDevices;
    private bool _diagnosticsExpanded;

    public SettingsViewModel(
        ISettingsService settingsService,
        IGlobalHotkeyService hotkeyService,
        IStartupRegistrationService startupService,
        IWindowsSettingsLauncher windowsLauncher,
        IAudioEngine audioEngine,
        IDeviceManagerService deviceManager)
    {
        _settingsService = settingsService;
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
        _autoSelectPreferredDevices = s.AutoSelectPreferredDevices;

        ApplyShortcutCommand = new RelayCommand(ApplyShortcut);
        OpenLogsFolderCommand = new RelayCommand(() => _windowsLauncher.OpenLogsFolder());
        OpenSoundSettingsCommand = new RelayCommand(() => _windowsLauncher.OpenSoundSettings());

        _audioEngine.StatusChanged += OnEngineStatusChanged;
        _deviceManager.DevicesUpdated += OnDevicesUpdated;
        RefreshDiagnostics();
    }

    public ObservableCollection<DiagnosticOutputItem> DiagnosticOutputs { get; } = [];
    public ObservableCollection<DeviceDelayItem> DelaySettings { get; } = [];

    public RelayCommand ApplyShortcutCommand { get; }
    public RelayCommand OpenLogsFolderCommand { get; }
    public RelayCommand OpenSoundSettingsCommand { get; }

    public string AppVersion { get; } =
        $"SYNC SOUND v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";

    public string ShortcutInput
    {
        get => _shortcutInput;
        set => SetProperty(ref _shortcutInput, value);
    }

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

    public bool HasShortcutError
    {
        get => _hasShortcutError;
        private set => SetProperty(ref _hasShortcutError, value);
    }

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

    public bool DiagnosticsExpanded
    {
        get => _diagnosticsExpanded;
        set => SetProperty(ref _diagnosticsExpanded, value);
    }

    public event EventHandler? SidebarPreferencesChanged;
    public event EventHandler<string>? HotkeyUpdated;

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

    public sealed record DiagnosticOutputItem(
        string Name,
        string State,
        string Format,
        string Metrics,
        string? Error);

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
