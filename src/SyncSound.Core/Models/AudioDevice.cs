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

    public bool IsAvailable => EndpointState == AudioEndpointState.Active;
    public bool IsHandsFreeProfile => DeviceType == AudioDeviceType.HeadsetHandsFree;
}
