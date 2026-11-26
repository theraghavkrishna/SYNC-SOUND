using System.Collections.ObjectModel;
using System.Windows;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.App.ViewModels;

public sealed class QuickSidebarViewModel : ViewModelBase, IDisposable
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly ISystemSharingService _sharingService;
    private readonly IAudioEngine _audioEngine;
    private readonly ISettingsService _settingsService;

    private string _statusHeadline = "Ready to listen.";
    private string _statusDetail = "Select headphones to share your sound.";
    private string _shareButtonText = "Start sharing";
    private bool _isSharing;
    private bool _canToggleSharing;

    public QuickSidebarViewModel(
        IDeviceManagerService deviceManager,
        ISystemSharingService sharingService,
        IAudioEngine audioEngine,
        ISettingsService settingsService)
    {
        _deviceManager = deviceManager;
        _sharingService = sharingService;
        _audioEngine = audioEngine;
        _settingsService = settingsService;

        ToggleSharingCommand = new AsyncRelayCommand(ToggleSharingAsync, () => CanToggleSharing || IsSharing);
        OpenMainAppCommand = new RelayCommand(() => OpenMainAppRequested?.Invoke(this, EventArgs.Empty));
        CloseSidebarCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        _deviceManager.DevicesUpdated += OnDevicesOrSharingChanged;
        _sharingService.StateChanged += OnSharingStateChanged;
        Refresh();
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];

    public AsyncRelayCommand ToggleSharingCommand { get; }
    public RelayCommand OpenMainAppCommand { get; }
    public RelayCommand CloseSidebarCommand { get; }

    public event EventHandler? OpenMainAppRequested;
    public event EventHandler? CloseRequested;

    public bool AlwaysOnTop => _settingsService.Current.SidebarAlwaysOnTop;
    public bool ClickOutsideToClose => _settingsService.Current.SidebarClickOutsideToClose;

    public string StatusHeadline
    {
        get => _statusHeadline;
        private set => SetProperty(ref _statusHeadline, value);
    }

    public string StatusDetail
    {
        get => _statusDetail;
        private set => SetProperty(ref _statusDetail, value);
    }

    public string ShareButtonText
    {
        get => _shareButtonText;
        private set => SetProperty(ref _shareButtonText, value);
    }

    public bool IsSharing
    {
        get => _isSharing;
        private set => SetProperty(ref _isSharing, value);
    }

    public bool CanToggleSharing
    {
        get => _canToggleSharing;
        private set => SetProperty(ref _canToggleSharing, value);
    }

    public void NotifyPreferencesChanged()
    {
        OnPropertyChanged(nameof(AlwaysOnTop));
        OnPropertyChanged(nameof(ClickOutsideToClose));
    }

    public void SyncVolumesFromSettings()
    {
        foreach (var item in Devices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(item.Id, out var saved)
                ? saved
                : 1.0f;
            item.SyncVolumeSilently(vol);
        }
    }

    public void Dispose()
    {
        _deviceManager.DevicesUpdated -= OnDevicesOrSharingChanged;
        _sharingService.StateChanged -= OnSharingStateChanged;
    }

    private Task ToggleSharingAsync() =>
        IsSharing ? _sharingService.StopSharingAsync() : _sharingService.StartSharingAsync();

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

    private void Refresh()
    {
        Devices.Clear();
        foreach (var d in _deviceManager.Devices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(d.Id, out var saved)
                ? saved
                : 1.0f;
            Devices.Add(new DeviceItemViewModel(
                d,
                (id, sel) => _deviceManager.SetDeviceSelected(id, sel),
                vol,
                OnDeviceVolumeChanged));
        }

        var sharing = _sharingService.CurrentState;
        var connected = Devices.Count;
        var selected = Devices.Count(d => d.IsSelected);

        IsSharing = sharing.IsSharing;
        CanToggleSharing = connected > 0 && selected > 0;
        ShareButtonText = IsSharing ? "Stop sharing" : "Start sharing";

        if (connected == 0)
        {
            StatusHeadline = "No outputs found.";
            StatusDetail = "Pair headphones in Windows Settings.";
        }
        else if (IsSharing)
        {
            StatusHeadline = sharing.State == AudioEngineState.Partial ? sharing.UserMessage : "Playing to everyone.";
            StatusDetail = $"{sharing.ActiveOutputs.Count} receiving live synced audio";
        }
        else if (selected == 0)
        {
            StatusHeadline = "Pick who listens.";
            StatusDetail = $"{connected} connected";
        }
        else
        {
            StatusHeadline = "Ready to share.";
            StatusDetail = $"{selected} armed · {connected} connected";
        }

        ToggleSharingCommand.RaiseCanExecuteChanged();
    }

    private void OnDeviceVolumeChanged(string deviceId, float volume)
    {
        _settingsService.Current.PerDeviceVolume[deviceId] = volume;
        _audioEngine.SetOutputVolume(deviceId, volume);
        _ = _settingsService.SaveAsync();
    }
}
