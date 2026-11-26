namespace SyncSound.Core.Models;

/// <summary>
/// High-level category of a Windows audio output endpoint.
/// </summary>
public enum AudioDeviceType
{
    Unknown = 0,
    Speakers,
    Headphones,
    HeadsetHandsFree,
    LineOut,
    DigitalDisplay,
    Other
}

/// <summary>
/// Mirrors the Windows Core Audio endpoint state.
/// </summary>
public enum AudioEndpointState
{
    Unknown = 0,
    Active,
    Disabled,
    NotPresent,
    Unplugged
}

/// <summary>
/// Represents a real Windows audio output endpoint.
/// </summary>
public sealed class AudioDevice
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string FriendlyName => Name;
    public AudioDeviceType DeviceType { get; init; }
    public AudioEndpointState EndpointState { get; init; }
    public bool IsDefault { get; init; }
    public bool IsSelected { get; set; }
    public bool IsSource { get; set; }
    public bool IsHealthy { get; set; } = true;
    public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    public string? ErrorState { get; set; }
    public int? BatteryPercentage { get; init; }
    public bool? IsCharging { get; init; }

    public bool IsAvailable => EndpointState == AudioEndpointState.Active;
    public bool IsHandsFreeProfile => DeviceType == AudioDeviceType.HeadsetHandsFree;
    public bool IsSpeakerDevice => DeviceType == AudioDeviceType.Speakers;

    /// <summary>
    /// Extracts a clean consumer-friendly name (e.g. "Headphones (OnePlus Nord Buds 3 Pro)" -> "OnePlus Nord Buds 3 Pro").
    /// Preserves the raw Windows name on <see cref="Name"/> for tooltips and diagnostics.
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return "Audio Device";
            }

            if (DeviceType == AudioDeviceType.Speakers &&
                Name.Contains("Realtek", StringComparison.OrdinalIgnoreCase))
            {
                return "Laptop Speakers";
            }

            var openIdx = Name.IndexOf('(');
            var closeIdx = Name.LastIndexOf(')');
            if (openIdx > 0 && closeIdx > openIdx + 1)
            {
                var prefix = Name[..openIdx].Trim();
                var inner = Name[(openIdx + 1)..closeIdx].Trim();
                if (prefix.Contains("Headphones", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Contains("Headset", StringComparison.OrdinalIgnoreCase) ||
                    prefix.Contains("Speakers", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(inner) && !inner.StartsWith("2-", StringComparison.OrdinalIgnoreCase))
                    {
                        return inner;
                    }
                }
            }

            return Name;
        }
    }
}
