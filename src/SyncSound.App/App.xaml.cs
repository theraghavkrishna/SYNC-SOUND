using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SyncSound.App.ViewModels;
using SyncSound.App.Views;
using SyncSound.Audio.Engine;
using SyncSound.Audio.Output;
using SyncSound.Audio.Services;
using SyncSound.Audio.Synchronization;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Audio;
using SyncSound.Windows.Bluetooth;
using SyncSound.Windows.Hotkeys;
using SyncSound.Windows.Settings;
using SyncSound.Windows.Startup;
using SyncSound.Windows.Tray;
using SyncSound.Windows.WindowManagement;

namespace SyncSound.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private SingleInstanceManager? _singleInstance;
    private MainWindow? _mainWindow;
    private QuickSidebarWindow? _sidebarWindow;
    private ITrayIconService? _trayService;
    private IGlobalHotkeyService? _hotkeyService;
    private bool _isExiting;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = SingleInstanceManager.Acquire(out var isPrimary);
        if (!isPrimary)
        {
            _singleInstance.SignalPrimaryInstance();
            _singleInstance.Dispose();
            Shutdown();
            return;
        }

        ConfigureLogging();

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI dispatcher exception");
            args.Handled = true;
        };

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
        await settingsService.LoadAsync();

        var deviceManager = _serviceProvider.GetRequiredService<IDeviceManagerService>();
        await deviceManager.InitializeAsync();

        var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
        _mainWindow = new MainWindow(mainVm, settingsService);
        MainWindow = _mainWindow;

        _sidebarWindow = new QuickSidebarWindow(_serviceProvider.GetRequiredService<QuickSidebarViewModel>());
        _sidebarWindow.DataContext = mainVm.QuickSidebarViewModel;

        mainVm.OpenSidebarRequested += (_, _) => ToggleSidebar();
        mainVm.QuickSidebarViewModel.OpenMainAppRequested += (_, _) =>
        {
            _sidebarWindow.HideAnimated();
            ShowMainWindow();
        };
        mainVm.QuickSidebarViewModel.CloseRequested += (_, _) => _sidebarWindow.HideAnimated();
        mainVm.SettingsViewModel.SidebarPreferencesChanged += (_, _) =>
        {
            mainVm.QuickSidebarViewModel.NotifyPreferencesChanged();
            if (_sidebarWindow is not null)
            {
                _sidebarWindow.Topmost = settingsService.Current.SidebarAlwaysOnTop;
            }
        };

        // Global Hotkey (Default Ctrl+Shift+S)
        _hotkeyService = _serviceProvider.GetRequiredService<IGlobalHotkeyService>();
        _hotkeyService.HotkeyPressed += (_, _) => Dispatcher.BeginInvoke(ToggleSidebar);
        var registered = _hotkeyService.TryRegister(settingsService.Current.Hotkey, out var hotkeyError);
        mainVm.SettingsViewModel.RefreshHotkeyState(registered, hotkeyError);

        // System Tray
        _trayService = _serviceProvider.GetRequiredService<ITrayIconService>();
        _trayService.Initialize();
        _trayService.OpenRequested += (_, _) => Dispatcher.BeginInvoke(ShowMainWindow);
        _trayService.ToggleSidebarRequested += (_, _) => Dispatcher.BeginInvoke(ToggleSidebar);
        _trayService.ToggleSharingRequested += (_, _) =>
        {
            var sharing = _serviceProvider.GetRequiredService<ISystemSharingService>();
            _ = sharing.CurrentState.IsSharing ? sharing.StopSharingAsync() : sharing.StartSharingAsync();
        };
        _trayService.QuitRequested += (_, _) => Dispatcher.BeginInvoke(QuitApplication);

        var sharingService = _serviceProvider.GetRequiredService<ISystemSharingService>();
        sharingService.StateChanged += (_, state) =>
            Dispatcher.BeginInvoke(() => _trayService?.UpdateSharingState(state.IsSharing, state.ActiveOutputs.Count));

        // Second-instance activation
        _singleInstance.SecondInstanceLaunched += (_, _) => Dispatcher.BeginInvoke(ShowMainWindow);
        _singleInstance.StartListening();

        var startMinimized = e.Args.Any(a => a.Equals("--minimized", StringComparison.OrdinalIgnoreCase)) ||
                             settingsService.Current.StartMinimized;

        if (!startMinimized)
        {
            ShowMainWindow();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        CleanupAsync().GetAwaiter().GetResult();
        base.OnExit(e);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
        {
            _mainWindow.WindowState = WindowState.Normal;
        }

        _mainWindow.Activate();
        _mainWindow.Focus();
    }

    private void ToggleSidebar()
    {
        if (_sidebarWindow is null)
        {
            return;
        }

        if (_sidebarWindow.IsVisible)
        {
            _sidebarWindow.HideAnimated();
        }
        else
        {
            (_sidebarWindow.DataContext as QuickSidebarViewModel)?.SyncVolumesFromSettings();
            _sidebarWindow.ShowAnimated();
        }
    }

    private void QuitApplication()
    {
        _isExiting = true;
        if (_mainWindow is not null)
        {
            _mainWindow.AllowClose = true;
            _mainWindow.Close();
        }

        Shutdown();
    }

    private async Task CleanupAsync()
    {
        if (_isExiting && _serviceProvider is null)
        {
            return;
        }

        _isExiting = true;

        try
        {
            _hotkeyService?.Unregister();
            _hotkeyService?.Dispose();
            _trayService?.Dispose();
            _sidebarWindow?.Close();

            if (_serviceProvider is not null)
            {
                await _serviceProvider.DisposeAsync();
                _serviceProvider = null;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error during application shutdown cleanup");
        }
        finally
        {
            _singleInstance?.Dispose();
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureLogging()
    {
        var logsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SyncSound",
            "logs");
        Directory.CreateDirectory(logsDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logsDir, "syncsound-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(Log.Logger);
        services.AddSingleton<ISettingsService, JsonSettingsService>();
        services.AddSingleton<IAudioDeviceEnumerator, WindowsAudioDeviceEnumerator>();
        services.AddSingleton<ISyncCoordinator, BestEffortSyncCoordinator>();
        services.AddSingleton<IAudioOutputFactory, WasapiAudioOutputFactory>();
        services.AddSingleton<IAudioEngine, AudioEngine>();
        services.AddSingleton<IDeviceManagerService, DeviceManagerService>();
        services.AddSingleton<ISystemSharingService, SystemSharingService>();
        services.AddSingleton<IWindowsSettingsLauncher, WindowsSettingsLauncher>();
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        services.AddSingleton<IStartupRegistrationService, StartupRegistrationService>();
        services.AddSingleton<ITrayIconService, TrayIconService>();

        services.AddSingleton<DevicesViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<QuickSidebarViewModel>();
        services.AddSingleton<MainViewModel>();
    }
}
