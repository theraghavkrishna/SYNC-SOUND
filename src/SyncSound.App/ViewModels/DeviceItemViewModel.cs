using SyncSound.Core.Models;

namespace SyncSound.App.ViewModels;

public sealed class DeviceItemViewModel : ViewModelBase
{
    private readonly Action<string, bool> _onSelectionToggled;
    private readonly Action<string, float>? _onVolumeChanged;
    private bool _isSelected;
    private double _volume;
    private string _statusBadge = "Connected";
    private string? _errorMessage;

    public DeviceItemViewModel(
        AudioDevice model,
        Action<string, bool> onSelectionToggled,
        double initialVolume = 1.0,
        Action<string, float>? onVolumeChanged = null)
    {
        Model = model;
        _onSelectionToggled = onSelectionToggled;
        _onVolumeChanged = onVolumeChanged;
        _isSelected = model.IsSelected;
        _volume = Math.Clamp(initialVolume, 0.0, 1.0);
    }

    public AudioDevice Model { get; }
    public string Id => Model.Id;
    public string Name => Model.Name;
    public bool IsDefault => Model.IsDefault;
    public bool IsHandsFreeProfile => Model.IsHandsFreeProfile;
    public bool IsAvailable => Model.IsAvailable;
    public AudioDeviceType DeviceType => Model.DeviceType;

    public string CategoryLabel => Model.DeviceType switch
    {
        AudioDeviceType.Headphones => "Headphones",
        AudioDeviceType.HeadsetHandsFree => "Hands-Free · Voice quality",
        AudioDeviceType.Speakers => "Speakers",
        AudioDeviceType.DigitalDisplay => "Display Audio",
        AudioDeviceType.LineOut => "Line Out",
        _ => "Audio Device"
    };

    public string RoleNote => Model.IsDefault
        ? "Playing original sound"
        : Model.IsHandsFreeProfile
            ? "Use stereo Headphones for best music quality"
            : "Ready to join";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                _onSelectionToggled(Id, value);
            }
        }
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
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumePercentText));
                _onVolumeChanged?.Invoke(Id, (float)clamped);
            }
        }
    }

    public string VolumePercentText => $"{(int)Math.Round(_volume * 100)}%";

    public void SyncVolumeSilently(double volume)
    {
        var clamped = Math.Clamp(volume, 0.0, 1.0);
        if (Math.Abs(_volume - clamped) > 0.001)
        {
            _volume = clamped;
            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(VolumePercentText));
        }
    }

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
