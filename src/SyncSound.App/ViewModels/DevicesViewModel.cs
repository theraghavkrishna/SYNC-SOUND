using System.Collections.ObjectModel;
using System.Windows;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Bluetooth;

namespace SyncSound.App.ViewModels;

public sealed class DevicesViewModel : ViewModelBase
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly IAudioEngine _audioEngine;
    private readonly ISettingsService _settingsService;
    private readonly IWindowsSettingsLauncher _settingsLauncher;
    private string _selectionSummary = "Nothing connected yet.";
    private bool _hasDevices;
    private string? _bannerMessage;
    private AudioDevice? _pendingSpeakerDevice;

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
        SelectAllCommand = new RelayCommand(SelectAllCompatible, () => HasDevices);
        ClearSelectionCommand = new RelayCommand(ClearSelection, () => SelectedCount > 0);
        AddHeadphonesCommand = new RelayCommand(OpenBluetoothSettings);
        ConfirmSpeakerSwitchCommand = new RelayCommand(ConfirmSpeakerSwitch);
        CancelSpeakerSwitchCommand = new RelayCommand(CancelSpeakerSwitch);

        _deviceManager.DevicesUpdated += OnDevicesUpdated;
        SyncDevices();
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];

    public RelayCommand RefreshCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand AddHeadphonesCommand { get; }
    public RelayCommand ConfirmSpeakerSwitchCommand { get; }
    public RelayCommand CancelSpeakerSwitchCommand { get; }

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

    public bool HasSpeakerSwitchPrompt => _pendingSpeakerDevice is not null;

    public string SpeakerSwitchTitle => _pendingSpeakerDevice is not null
        ? $"Switch to {_pendingSpeakerDevice.DisplayName}?"
        : "Switch to laptop speakers?";

    public void Refresh()
    {
        _deviceManager.Refresh();
    }

    public void SelectAllCompatible()
    {
        var headphones = _deviceManager.Devices.Where(d => d.IsAvailable && !d.IsHandsFreeProfile && !d.IsSpeakerDevice).ToList();
        if (headphones.Count > 0)
        {
            foreach (var spk in _deviceManager.SelectedDevices.Where(d => d.IsSpeakerDevice))
            {
                _deviceManager.SetDeviceSelected(spk.Id, false);
            }

            foreach (var hp in headphones)
            {
                _deviceManager.SetDeviceSelected(hp.Id, true);
            }
        }
        else
        {
            _deviceManager.SelectAllEligible();
        }
    }

    public void ClearSelection()
    {
        _deviceManager.ClearSelection();
    }

    public void OpenBluetoothSettings()
    {
        BannerMessage = "Let's add another headphone. Opening Windows Bluetooth settings…";
        if (!_settingsLauncher.OpenBluetoothSettings())
        {
            BannerMessage = "Open Windows Settings -> Bluetooth & devices to pair your headphones.";
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

    public void RequestSpeakerSwitch(AudioDevice speaker)
    {
        _pendingSpeakerDevice = speaker;
        NotifyPromptChanged();
        SyncDevices();
    }

    public void ConfirmSpeakerSwitch()
    {
        if (_pendingSpeakerDevice is null)
        {
            return;
        }

        var target = _pendingSpeakerDevice;
        _pendingSpeakerDevice = null;
        NotifyPromptChanged();

        foreach (var d in _deviceManager.SelectedDevices.Where(d => !d.IsSpeakerDevice))
        {
            _deviceManager.SetDeviceSelected(d.Id, false);
        }

        _deviceManager.SetDeviceSelected(target.Id, true);
        BannerMessage = $"Switched listening output to {target.DisplayName}.";
    }

    public void CancelSpeakerSwitch()
    {
        _pendingSpeakerDevice = null;
        NotifyPromptChanged();
        SyncDevices();
    }

    private void NotifyPromptChanged()
    {
        OnPropertyChanged(nameof(HasSpeakerSwitchPrompt));
        OnPropertyChanged(nameof(SpeakerSwitchTitle));
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
                    // Revert visual checkbox immediately and ask confirmation before removing headphones
                    var vmItem = Devices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
                    vmItem?.SyncSelectedSilently(false);
                    _pendingSpeakerDevice = clicked;
                    NotifyPromptChanged();
                    return;
                }
            }
            else
            {
                // Selecting headphones automatically removes mutually-exclusive Speakers
                foreach (var spk in _deviceManager.SelectedDevices.Where(d => d.IsSpeakerDevice))
                {
                    _deviceManager.SetDeviceSelected(spk.Id, false);
                }
            }
        }

        _deviceManager.SetDeviceSelected(deviceId, isSelected);
    }

    private void OnDeviceVolumeChanged(string deviceId, float volume)
    {
        _settingsService.Current.PerDeviceVolume[deviceId] = volume;
        _audioEngine.SetOutputVolume(deviceId, volume);
        _ = _settingsService.SaveAsync();
    }
}
