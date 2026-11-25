using SyncSound.Core.Models;

namespace SyncSound.Core.Interfaces;

public enum DeviceChangeKind
{
    Added = 0,
    Removed,
    StateChanged,
    DefaultChanged,
    PropertyChanged
}

public sealed class AudioDeviceChangedEventArgs : EventArgs
{
    public AudioDeviceChangedEventArgs(string deviceId, DeviceChangeKind changeKind, AudioEndpointState? newState = null)
    {
        DeviceId = deviceId;
        ChangeKind = changeKind;
        NewState = newState;
    }

    public string DeviceId { get; }
    public DeviceChangeKind ChangeKind { get; }
    public AudioEndpointState? NewState { get; }
}

/// <summary>
/// Enumerates real Windows audio render endpoints and notifies listeners of live changes.
/// </summary>
public interface IAudioDeviceEnumerator : IDisposable
{
    IReadOnlyList<AudioDevice> GetRenderEndpoints(bool includeInactive = false);
    AudioDevice? GetDefaultRenderEndpoint();
    event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;
}
