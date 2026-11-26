using System.Windows;
using System.Windows.Media;
using SyncSound.Core.Models;

namespace SyncSound.App.ViewModels;

public sealed class DeviceItemViewModel : ViewModelBase
{
    private readonly Action<string, bool> _onSelectionToggled;
    private readonly Action<string, float>? _onVolumeChanged;
    private readonly Action<string, bool>? _onMuteChanged;
    private readonly Action<string, int>? _onDelayChanged;

    private bool _isSelected;
    private bool _isDeviceMuted;
    private bool _isExpanded;
    private bool _isActivelyPlaying;
    private double _volume;
    private int _delayMs;
    private string _statusBadge = "Connected";
    private string? _errorMessage;

    public DeviceItemViewModel(
        AudioDevice model,
        Action<string, bool> onSelectionToggled,
        double initialVolume = 1.0,
        Action<string, float>? onVolumeChanged = null,
        bool initialMuted = false,
        Action<string, bool>? onMuteChanged = null,
        int initialDelayMs = 0,
        Action<string, int>? onDelayChanged = null)
    {
        Model = model;
        _onSelectionToggled = onSelectionToggled;
        _onVolumeChanged = onVolumeChanged;
        _onMuteChanged = onMuteChanged;
        _onDelayChanged = onDelayChanged;
        _isSelected = model.IsSelected;
        _volume = Math.Clamp(initialVolume, 0.0, 1.0);
        _isDeviceMuted = initialMuted;
        _delayMs = Math.Clamp(initialDelayMs, 0, 300);

        ToggleCommand = new RelayCommand(() => IsSelected = !IsSelected);
        ToggleMuteCommand = new RelayCommand(() => IsDeviceMuted = !IsDeviceMuted);
        ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
    }

    public AudioDevice Model { get; }
    public string Id => Model.Id;
    public string Name => Model.DisplayName;
    public string FullSystemName => Model.Name;
    public bool IsDefault => Model.IsDefault;
    public bool IsHandsFreeProfile => Model.IsHandsFreeProfile;
    public bool IsAvailable => Model.IsAvailable;
    public AudioDeviceType DeviceType => Model.DeviceType;
    public AudioTransportKind TransportKind => Model.TransportKind;

    public RelayCommand ToggleCommand { get; }
    public RelayCommand ToggleMuteCommand { get; }
    public RelayCommand ToggleExpandCommand { get; }

    public bool HasBattery => Model.BatteryPercentage.HasValue;
    public int? BatteryPercentage => Model.BatteryPercentage;
    public bool IsLowBattery => Model.BatteryPercentage is <= 20;
    public string BatteryText => Model.BatteryPercentage.HasValue ? $"{Model.BatteryPercentage.Value}%" : string.Empty;

    public Geometry? DeviceIconGeometry
    {
        get
        {
            var key = Model.TransportKind switch
            {
                AudioTransportKind.UsbAudio => "Icon.UsbAudio",
                AudioTransportKind.DigitalDisplay => "Icon.Display",
                AudioTransportKind.BuiltInOrWiredJack when FullSystemName.Contains("Headphone", StringComparison.OrdinalIgnoreCase)
                    || FullSystemName.Contains("Realtek", StringComparison.OrdinalIgnoreCase) => "Icon.WiredJack",
                AudioTransportKind.BuiltInOrWiredJack => "Icon.Speaker",
                AudioTransportKind.BluetoothA2dp when FullSystemName.Contains("Bud", StringComparison.OrdinalIgnoreCase)
                    || FullSystemName.Contains("Pod", StringComparison.OrdinalIgnoreCase)
                    || FullSystemName.Contains("Ear", StringComparison.OrdinalIgnoreCase) => "Icon.Earbuds",
                AudioTransportKind.BluetoothA2dp => "Icon.Headphones",
                _ => Model.DeviceType switch
                {
                    AudioDeviceType.Speakers => "Icon.Speaker",
                    AudioDeviceType.DigitalDisplay => "Icon.Display",
                    AudioDeviceType.Headphones => "Icon.Headphones",
                    _ => "Icon.WiredJack"
                }
            };
            return Application.Current?.TryFindResource(key) as Geometry;
        }
    }

    public string CategoryLabel => Model.TransportKind switch
    {
        AudioTransportKind.BluetoothA2dp => "Bluetooth",
        AudioTransportKind.UsbAudio => "USB Audio",
        AudioTransportKind.BuiltInOrWiredJack => "Wired / System Audio",
        AudioTransportKind.DigitalDisplay => "Display Audio",
        _ => Model.DeviceType switch
        {
            AudioDeviceType.Headphones => "Headphones",
            AudioDeviceType.Speakers => "Speakers",
            AudioDeviceType.DigitalDisplay => "Display Audio",
            AudioDeviceType.LineOut => "Line Out",
            _ => "Audio Output"
        }
    };

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(StateLabel));
                _onSelectionToggled(Id, value);
            }
        }
    }

    public void SyncSelectedSilently(bool isSelected)
    {
        if (SetProperty(ref _isSelected, isSelected, nameof(IsSelected)))
        {
            OnPropertyChanged(nameof(StateLabel));
        }
    }

    public string StateLabel => IsActivelyPlaying ? "Listening" : (IsSelected ? "In group" : "Connected");

    public bool IsActivelyPlaying
    {
        get => _isActivelyPlaying;
        set
        {
            if (SetProperty(ref _isActivelyPlaying, value))
            {
                OnPropertyChanged(nameof(StateLabel));
            }
        }
    }

    public bool IsDeviceMuted
    {
        get => _isDeviceMuted;
        set
        {
            if (SetProperty(ref _isDeviceMuted, value))
            {
                OnPropertyChanged(nameof(VolumePercentText));
                _onMuteChanged?.Invoke(Id, value);
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public double Volume
    {
        get => _volume;
        set
        {
            var clamped = Math.Clamp(value, 0.0, 1.0);
            if (Math.Abs(_volume - clamped) > 0.001)
            {
                _volume = clamped;
                if (clamped > 0.01 && _isDeviceMuted)
                {
                    _isDeviceMuted = false;
                    OnPropertyChanged(nameof(IsDeviceMuted));
                    _onMuteChanged?.Invoke(Id, false);
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumePercentText));
                _onVolumeChanged?.Invoke(Id, (float)clamped);
            }
        }
    }

    public string VolumePercentText => IsDeviceMuted ? "MUTED" : $"{(int)Math.Round(_volume * 100)}%";

    public void SyncVolumeSilently(double volume, bool? isMuted = null)
    {
        var clamped = Math.Clamp(volume, 0.0, 1.0);
        if (Math.Abs(_volume - clamped) > 0.001)
        {
            _volume = clamped;
            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(VolumePercentText));
        }

        if (isMuted.HasValue && _isDeviceMuted != isMuted.Value)
        {
            _isDeviceMuted = isMuted.Value;
            OnPropertyChanged(nameof(IsDeviceMuted));
            OnPropertyChanged(nameof(VolumePercentText));
        }
    }

    public int DelayMs
    {
        get => _delayMs;
        set
        {
            var clamped = Math.Clamp(value, 0, 300);
            if (SetProperty(ref _delayMs, clamped))
            {
                OnPropertyChanged(nameof(DelayLabel));
                _onDelayChanged?.Invoke(Id, clamped);
            }
        }
    }

    public string DelayLabel => $"{_delayMs} ms";

    public string StatusBadge
    {
        get => _statusBadge;
        set => SetProperty(ref _statusBadge, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(_errorMessage);
}
