using System.Collections.ObjectModel;
using System.Windows;
using SyncSound.App.Services;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Bluetooth;

namespace SyncSound.App.ViewModels;

public sealed class DevicesViewModel : ViewModelBase, IDisposable
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly IAudioEngine _audioEngine;
    private readonly ISystemSharingService _sharingService;
    private readonly ISettingsService _settingsService;
    private readonly ISoundFeedbackService _soundFeedback;
    private readonly IWindowsSettingsLauncher _settingsLauncher;
    private string _selectionSummary = "Nothing connected yet.";
    private bool _hasDevices;
    private bool _hasSelectedGroupDevices;
    private bool _hasAvailableDevices;
    private string? _bannerMessage;

    public DevicesViewModel(
        IDeviceManagerService deviceManager,
        IAudioEngine audioEngine,
        ISystemSharingService sharingService,
        ISettingsService settingsService,
        ISoundFeedbackService soundFeedback,
        IWindowsSettingsLauncher settingsLauncher)
    {
        _deviceManager = deviceManager;
        _audioEngine = audioEngine;
        _sharingService = sharingService;
        _settingsService = settingsService;
        _soundFeedback = soundFeedback;
        _settingsLauncher = settingsLauncher;

        RefreshCommand = new RelayCommand(Refresh);
        SelectAllCommand = new RelayCommand(SelectAll, () => HasDevices);
        ClearSelectionCommand = new RelayCommand(ClearSelection, () => SelectedCount > 0);
        AddHeadphonesCommand = new RelayCommand(OpenBluetoothSettings);

        _deviceManager.DevicesUpdated += OnDevicesOrEngineUpdated;
        _sharingService.StateChanged += OnSharingStateChanged;
        SyncDevices();
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];
    public ObservableCollection<DeviceItemViewModel> SelectedGroupDevices { get; } = [];
    public ObservableCollection<DeviceItemViewModel> AvailableDevices { get; } = [];

    public RelayCommand RefreshCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand ClearSelectionCommand { get; }
    public RelayCommand AddHeadphonesCommand { get; }

    public int ConnectedCount => Devices.Count;
    public int SelectedCount => Devices.Count(d => d.IsSelected);

    public bool HasDevices { get => _hasDevices; private set => SetProperty(ref _hasDevices, value); }
    public bool HasSelectedGroupDevices { get => _hasSelectedGroupDevices; private set => SetProperty(ref _hasSelectedGroupDevices, value); }
    public bool HasAvailableDevices { get => _hasAvailableDevices; private set => SetProperty(ref _hasAvailableDevices, value); }
    public string SelectionSummary { get => _selectionSummary; private set => SetProperty(ref _selectionSummary, value); }

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

    public void Refresh() => _deviceManager.Refresh();

    public void SelectAll()
    {
        _soundFeedback.Play(UiSoundCue.Select);
        _deviceManager.SelectAllEligible();
    }

    public void ClearSelection()
    {
        _soundFeedback.Play(UiSoundCue.Deselect);
        _deviceManager.ClearSelection();
    }

    public void OpenBluetoothSettings()
    {
        BannerMessage = "Pair or connect headphones in Windows — they will appear here automatically.";
        if (!_settingsLauncher.OpenBluetoothSettings())
        {
            BannerMessage = "Open Windows Settings -> Bluetooth & devices to pair your headphones.";
        }
    }

    public void SyncVolumesFromSettings()
    {
        var mutedSet = _settingsService.Current.MutedDeviceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Devices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(item.Id, out var saved)
                ? saved
                : 1.0f;
            item.SyncVolumeSilently(vol, mutedSet.Contains(item.Id));
        }
    }

    public void Dispose()
    {
        _deviceManager.DevicesUpdated -= OnDevicesOrEngineUpdated;
        _sharingService.StateChanged -= OnSharingStateChanged;
    }

    private void OnDevicesOrEngineUpdated(object? sender, EventArgs e) => DispatchSync();
    private void OnSharingStateChanged(object? sender, SharingState e) => DispatchSync();

    private void DispatchSync()
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
        var sharing = _sharingService.CurrentState;
        var activeIds = sharing.ActiveOutputs
            .Where(o => o.State == AudioOutputState.Playing)
            .Select(o => o.DeviceId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (sharing.IsSharing && sharing.SourceDevice is not null)
        {
            activeIds.Add(sharing.SourceDevice.Id);
        }

        var failedMap = sharing.FailedOutputs.ToDictionary(f => f.DeviceId, f => f.ErrorMessage, StringComparer.OrdinalIgnoreCase);
        var mutedSet = _settingsService.Current.MutedDeviceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        Devices.Clear();
        SelectedGroupDevices.Clear();
        AvailableDevices.Clear();

        foreach (var device in _deviceManager.Devices)
        {
            var vol = _settingsService.Current.PerDeviceVolume.TryGetValue(device.Id, out var saved)
                ? saved
                : 1.0f;
            _settingsService.Current.PerDeviceDelayMs.TryGetValue(device.Id, out var delayMs);
            var isMuted = mutedSet.Contains(device.Id);

            var vm = new DeviceItemViewModel(
                device,
                OnDeviceSelectionToggled,
                vol,
                OnDeviceVolumeChanged,
                isMuted,
                OnDeviceMuteChanged,
                delayMs,
                OnDeviceDelayChanged)
            {
                IsActivelyPlaying = activeIds.Contains(device.Id),
                ErrorMessage = failedMap.TryGetValue(device.Id, out var err) ? err : null
            };

            Devices.Add(vm);
            if (vm.IsSelected)
            {
                SelectedGroupDevices.Add(vm);
            }
            else
            {
                AvailableDevices.Add(vm);
            }
        }

        HasDevices = Devices.Count > 0;
        HasSelectedGroupDevices = SelectedGroupDevices.Count > 0;
        HasAvailableDevices = AvailableDevices.Count > 0;

        OnPropertyChanged(nameof(ConnectedCount));
        OnPropertyChanged(nameof(SelectedCount));

        SelectionSummary = Devices.Count == 0
            ? "Your listening group starts here."
            : $"{SelectedCount} of {ConnectedCount} in listening group · Wired, USB & Bluetooth supported";

        SelectAllCommand.RaiseCanExecuteChanged();
        ClearSelectionCommand.RaiseCanExecuteChanged();
    }

    private void OnDeviceSelectionToggled(string deviceId, bool isSelected)
    {
        _soundFeedback.Play(isSelected ? UiSoundCue.Select : UiSoundCue.Deselect);
        _deviceManager.SetDeviceSelected(deviceId, isSelected);
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

    private void OnDeviceDelayChanged(string deviceId, int delayMs)
    {
        _settingsService.Current.PerDeviceDelayMs[deviceId] = delayMs;
        _audioEngine.SetOutputDelayMs(deviceId, delayMs);
        _ = _settingsService.SaveAsync();
    }
}
