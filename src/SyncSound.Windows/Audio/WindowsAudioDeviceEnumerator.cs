using NAudio.CoreAudioApi;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Windows.Audio;

/// <summary>
/// Enumerates real Windows audio render endpoints via Core Audio (WASAPI)
/// and forwards endpoint add/remove/state/default notifications.
/// </summary>
public sealed class WindowsAudioDeviceEnumerator : IAudioDeviceEnumerator
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly MMDeviceNotificationClient _notificationClient;
    private readonly Lock _gate = new();
    private bool _disposed;

    public WindowsAudioDeviceEnumerator()
    {
        _enumerator = new MMDeviceEnumerator();
        _notificationClient = _enumerator.CreateNotificationClient(useSynchronizationContext: false);
        _notificationClient.DeviceAdded += OnDeviceAdded;
        _notificationClient.DeviceRemoved += OnDeviceRemoved;
        _notificationClient.DeviceStateChanged += OnDeviceStateChanged;
        _notificationClient.DefaultDeviceChanged += OnDefaultDeviceChanged;
    }

    public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

    public IReadOnlyList<AudioDevice> GetRenderEndpoints(bool includeInactive = false)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var defaultId = TryGetDefaultEndpointId();
            var mask = includeInactive
                ? DeviceState.Active | DeviceState.Disabled | DeviceState.Unplugged | DeviceState.NotPresent
                : DeviceState.Active;

            var collection = _enumerator.EnumerateAudioEndPoints(DataFlow.Render, mask);
            var batteryMap = BluetoothBatteryReader.GetBatteryLevelsByDeviceName();
            var devices = new List<AudioDevice>(collection.Count);
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var mmDevice in collection)
            {
                using (mmDevice)
                {
                    if (!seenIds.Add(mmDevice.ID))
                    {
                        continue;
                    }

                    devices.Add(MapDevice(mmDevice, defaultId, batteryMap));
                }
            }

            return devices
                .OrderByDescending(d => d.IsDefault)
                .ThenBy(d => d.IsHandsFreeProfile)
                .ThenBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
    }

    public AudioDevice? GetDefaultRenderEndpoint()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
            {
                return null;
            }

            using var mmDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return MapDevice(mmDevice, mmDevice.ID);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _notificationClient.DeviceAdded -= OnDeviceAdded;
            _notificationClient.DeviceRemoved -= OnDeviceRemoved;
            _notificationClient.DeviceStateChanged -= OnDeviceStateChanged;
            _notificationClient.DefaultDeviceChanged -= OnDefaultDeviceChanged;

            try
            {
                _notificationClient.Dispose();
            }
            catch
            {
                // Best-effort unregister during shutdown.
            }

            _enumerator.Dispose();
        }
    }

    private string? TryGetDefaultEndpointId()
    {
        try
        {
            if (!_enumerator.HasDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
            {
                return null;
            }

            using var defaultDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return defaultDevice.ID;
        }
        catch
        {
            return null;
        }
    }

    private static AudioDevice MapDevice(
        MMDevice mmDevice,
        string? defaultId,
        IReadOnlyDictionary<string, int>? batteryMap = null)
    {
        var friendlyName = SafeGetFriendlyName(mmDevice);
        var state = MapState(mmDevice.State);
        var type = InferDeviceType(friendlyName, mmDevice);
        var battery = batteryMap is not null
            ? BluetoothBatteryReader.MatchBatteryForEndpoint(friendlyName, batteryMap)
            : null;

        return new AudioDevice
        {
            Id = mmDevice.ID,
            Name = friendlyName,
            DeviceType = type,
            EndpointState = state,
            IsDefault = string.Equals(mmDevice.ID, defaultId, StringComparison.OrdinalIgnoreCase),
            IsHealthy = state == AudioEndpointState.Active,
            BatteryPercentage = battery,
            LastSeen = DateTimeOffset.UtcNow
        };
    }

    private static string SafeGetFriendlyName(MMDevice mmDevice)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(mmDevice.FriendlyName)
                ? mmDevice.FriendlyName
                : mmDevice.DeviceFriendlyName;
        }
        catch
        {
            return mmDevice.ID;
        }
    }

    private static AudioEndpointState MapState(DeviceState state) => state switch
    {
        DeviceState.Active => AudioEndpointState.Active,
        DeviceState.Disabled => AudioEndpointState.Disabled,
        DeviceState.NotPresent => AudioEndpointState.NotPresent,
        DeviceState.Unplugged => AudioEndpointState.Unplugged,
        _ => AudioEndpointState.Unknown
    };

    private static AudioDeviceType InferDeviceType(string name, MMDevice device)
    {
        if (name.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("HF Audio", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Headset", StringComparison.OrdinalIgnoreCase))
        {
            return AudioDeviceType.HeadsetHandsFree;
        }

        if (name.Contains("Headphones", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Earbuds", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Buds", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("AirPods", StringComparison.OrdinalIgnoreCase))
        {
            return AudioDeviceType.Headphones;
        }

        if (name.Contains("Speakers", StringComparison.OrdinalIgnoreCase))
        {
            return AudioDeviceType.Speakers;
        }

        if (name.Contains("HDMI", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("DisplayPort", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Digital", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("S/PDIF", StringComparison.OrdinalIgnoreCase))
        {
            return AudioDeviceType.DigitalDisplay;
        }

        try
        {
            var iconPath = device.IconPath ?? string.Empty;
            if (iconPath.Contains(",-3011", StringComparison.OrdinalIgnoreCase))
            {
                return AudioDeviceType.Headphones;
            }
        }
        catch
        {
            // Ignore icon lookup errors.
        }

        return AudioDeviceType.Other;
    }

    private void OnDeviceAdded(object? sender, DeviceNotificationEventArgs e) =>
        RaiseDeviceChanged(e.DeviceId ?? string.Empty, DeviceChangeKind.Added);

    private void OnDeviceRemoved(object? sender, DeviceNotificationEventArgs e) =>
        RaiseDeviceChanged(e.DeviceId ?? string.Empty, DeviceChangeKind.Removed);

    private void OnDeviceStateChanged(object? sender, DeviceStateChangedEventArgs e) =>
        RaiseDeviceChanged(e.DeviceId ?? string.Empty, DeviceChangeKind.StateChanged, MapState(e.NewState));

    private void OnDefaultDeviceChanged(object? sender, DefaultDeviceChangedEventArgs e)
    {
        if (e.Flow == DataFlow.Render && e.Role == Role.Multimedia)
        {
            RaiseDeviceChanged(e.DeviceId ?? string.Empty, DeviceChangeKind.DefaultChanged);
        }
    }

    private void RaiseDeviceChanged(string deviceId, DeviceChangeKind kind, AudioEndpointState? state = null)
    {
        if (_disposed)
        {
            return;
        }

        DeviceChanged?.Invoke(this, new AudioDeviceChangedEventArgs(deviceId, kind, state));
    }
}
