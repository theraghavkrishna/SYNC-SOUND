using System.Collections.ObjectModel;
using System.Windows;
using SyncSound.App.Services;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.App.ViewModels;

public sealed class QuickSidebarViewModel : ViewModelBase, IDisposable
{
    private readonly IDeviceManagerService _deviceManager;
    private readonly ISystemSharingService _sharingService;
    private readonly IAudioEngine _audioEngine;
    private readonly ISettingsService _settingsService;

    private string _statusHeadline = "Everyone's here.";
    private string _statusDetail = "Listen together.";
    private string _shareButtonText = "Start listening";
    private string _countBadgeText = "0 in group";
    private SyncVisualState _visualState = SyncVisualState.Ready;
    private bool _isSharing;
    private bool _canToggleSharing;
    private double _masterVolume = 1.0;
    private double _preMuteVolume = 1.0;
    private bool _isMuted;

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
        OpenMainAppCommand = new RelayCommand(() => OpenMainAppRequested?.Invoke(this, EventArgs.Empty));
        OpenPreferencesCommand = new RelayCommand(() => OpenPreferencesRequested?.Invoke(this, EventArgs.Empty));
        CloseSidebarCommand = new RelayCommand(() => CloseRequested?.Invoke(this, EventArgs.Empty));

        _deviceManager.DevicesUpdated += OnDevicesOrSharingChanged;
        _sharingService.StateChanged += OnSharingStateChanged;
        Refresh();
    }

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];

    public AsyncRelayCommand ToggleSharingCommand { get; }
    public RelayCommand ToggleMuteCommand { get; }
    public RelayCommand OpenMainAppCommand { get; }
    public RelayCommand OpenPreferencesCommand { get; }
    public RelayCommand CloseSidebarCommand { get; }

    public event EventHandler? OpenMainAppRequested;
    public event EventHandler? OpenPreferencesRequested;
    public event EventHandler? CloseRequested;
    public event EventHandler? MasterVolumeSynced;

    public SyncVisualState VisualState
    {
        get => _visualState;
        private set => SetProperty(ref _visualState, value);
    }

    public bool AlwaysOnTop => _settingsService.Current.SidebarAlwaysOnTop;
    public bool ClickOutsideToClose => _settingsService.Current.SidebarClickOutsideToClose;
    public ISettingsService SettingsService => _settingsService;

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

    public string CountBadgeText
    {
        get => _countBadgeText;
        private set => SetProperty(ref _countBadgeText, value);
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
        }
    }

    public void NotifyPreferencesChanged()
    {
        OnPropertyChanged(nameof(AlwaysOnTop));
        OnPropertyChanged(nameof(ClickOutsideToClose));
    }

    public void SyncVolumesFromSettings()
    {
        SyncMasterVolumeFromSettings();
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
        var allDevices = _deviceManager.Devices;
        var selectedDevices = _deviceManager.SelectedDevices;
        var defaultDevice = _deviceManager.DefaultDevice;

        IsSharing = sharing.IsSharing;
        var hasSecondaryTarget = defaultDevice is not null
            ? selectedDevices.Any(d => !string.Equals(d.Id, defaultDevice.Id, StringComparison.OrdinalIgnoreCase) && !d.IsSpeakerDevice)
            : selectedDevices.Count(d => !d.IsSpeakerDevice) >= 2;

        CanToggleSharing = allDevices.Count > 1 && hasSecondaryTarget;
        CountBadgeText = $"{selectedDevices.Count} of {allDevices.Count} in group";

        var copy = SyncCopy.GetListenCopy(allDevices, selectedDevices, sharing, defaultDevice, IsMuted);
        VisualState = copy.State;
        StatusHeadline = copy.Headline;
        StatusDetail = copy.AccentLine;
        ShareButtonText = IsSharing ? "Stop listening" : "Start listening";

        ToggleSharingCommand.RaiseCanExecuteChanged();
    }

    private void OnDeviceVolumeChanged(string deviceId, float volume)
    {
        _settingsService.Current.PerDeviceVolume[deviceId] = volume;
        _audioEngine.SetOutputVolume(deviceId, volume);
        _ = _settingsService.SaveAsync();
    }
}
