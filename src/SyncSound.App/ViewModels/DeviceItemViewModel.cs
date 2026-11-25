using SyncSound.Core.Models;

namespace SyncSound.App.ViewModels;

public sealed class DeviceItemViewModel : ViewModelBase
{
    private readonly Action<string, bool> _onSelectionToggled;
    private bool _isSelected;
    private string _statusBadge = "Connected";
    private string? _errorMessage;

    public DeviceItemViewModel(AudioDevice model, Action<string, bool> onSelectionToggled)
    {
        Model = model;
        _onSelectionToggled = onSelectionToggled;
        _isSelected = model.IsSelected;
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
