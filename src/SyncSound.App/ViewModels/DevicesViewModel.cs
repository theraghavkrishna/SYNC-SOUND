using System.Collections.ObjectModel;
using System.Windows;
using SyncSound.Core.Interfaces;
using SyncSound.Windows.Bluetooth;

namespace SyncSound.App.ViewModels;

public sealed class DevicesViewModel : ViewModelBase
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly IAudioEngine _audioEngine;
    private readonly ISettingsService _settingsService;
    private readonly IWindowsSettingsLauncher _settingsLauncher;
    private string _selectionSummary = "No headphones connected";
    private bool _hasDevices;
    private string? _bannerMessage;

    public DevicesViewModel(
        IDeviceManagerService deviceManager,
        IAudioEngine audioEngine,
        ISettingsService settingsService,
        IWindowsSettingsLauncher settingsLauncher)
    {
        _deviceManager = deviceManager;
        _audioEngine = audioEngine;
        _settingsService = settingsService;
        _settingsLauncher = settingsLauncher;

        RefreshCommand = new RelayCommand(Refresh);
        SelectAllCommand = new RelayCommand(SelectAll, () => HasDevices);
        ClearSelectionCommand = new RelayCommand(ClearSelection, () => SelectedCount > 0);
        AddHeadphonesCommand = new RelayCommand(OpenBluetoothSettings);

        _deviceManager.DevicesUpdated += OnDevicesUpdated;
        SyncDevices();
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];

    public RelayCommand RefreshCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand AddHeadphonesCommand { get; }

    public int ConnectedCount => Devices.Count;
    public int SelectedCount => Devices.Count(d => d.IsSelected);

    public bool HasDevices
    {
        get => _hasDevices;
        private set => SetProperty(ref _hasDevices, value);
    }

    public string SelectionSummary
    {
        get => _selectionSummary;
        private set => SetProperty(ref _selectionSummary, value);
    }

    public string? BannerMessage
    {
        get => _bannerMessage;
        set
        {
            if (SetProperty(ref _bannerMessage, value))
            {
                OnPropertyChanged(nameof(HasBanner));
            }
        }
    }

    public bool HasBanner => !string.IsNullOrWhiteSpace(_bannerMessage);

    public void Refresh()
    {
        _deviceManager.Refresh();
    }

    public void SelectAll()
    {
        _deviceManager.SelectAllEligible();
    }

    public void ClearSelection()
    {
        _deviceManager.ClearSelection();
    }

    public void OpenBluetoothSettings()
    {
        if (!_settingsLauncher.OpenBluetoothSettings())
        {
            BannerMessage = "Let's add another headphone. Pair your headphones in Windows Settings -> Bluetooth & devices.";
        }
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

    private void OnDevicesUpdated(object? sender, EventArgs e)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(SyncDevices);
        }
        else
        {
            SyncDevices();
        }
    }

    private void SyncDevices()
    {
        Devices.Clear();
        foreach (var device in _deviceManager.Devices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(device.Id, out var saved)
                ? saved
                : 1.0f;
            Devices.Add(new DeviceItemViewModel(device, OnDeviceSelectionToggled, vol, OnDeviceVolumeChanged));
        }

        HasDevices = Devices.Count > 0;
        OnPropertyChanged(nameof(ConnectedCount));
        OnPropertyChanged(nameof(SelectedCount));

        SelectionSummary = Devices.Count == 0
            ? "Your listening group starts here."
            : $"{SelectedCount} of {ConnectedCount} in group";

        SelectAllCommand.RaiseCanExecuteChanged();
        ClearSelectionCommand.RaiseCanExecuteChanged();
    }

    private void OnDeviceSelectionToggled(string deviceId, bool isSelected)
    {
        _deviceManager.SetDeviceSelected(deviceId, isSelected);
    }

    private void OnDeviceVolumeChanged(string deviceId, float volume)
    {
        _settingsService.Current.PerDeviceVolume[deviceId] = volume;
        _audioEngine.SetOutputVolume(deviceId, volume);
        _ = _settingsService.SaveAsync();
    }
}
