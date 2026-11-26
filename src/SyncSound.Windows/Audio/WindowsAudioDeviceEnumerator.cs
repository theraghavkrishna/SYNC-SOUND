using NAudio.CoreAudioApi;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Windows.Audio;

/// <summary>
/// Enumerates real Windows audio render endpoints via Core Audio (WASAPI)
/// using FormFactor, EnumeratorName, and device metadata rather than brittle name heuristics alone.
/// </summary>
public sealed class WindowsAudioDeviceEnumerator : IAudioDeviceEnumerator
{
    // Windows Core Audio PKEYs
    private static readonly PropertyKey PKEY_AudioEndpoint_FormFactor =
        new(new Guid("1da5d803-d492-4edd-8c23-e0c0ffee7f0e"), 0);

    private static readonly PropertyKey PKEY_Device_EnumeratorName =
        new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 24);

    private static readonly PropertyKey PKEY_Device_BusInfSection =
        new(new Guid("a8b865dd-2e3d-4094-ad97-e593a70c75d6"), 6);

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
        var (type, transport) = ClassifyEndpoint(friendlyName, mmDevice);
        var battery = transport == AudioTransportKind.BluetoothA2dp && batteryMap is not null
            ? BluetoothBatteryReader.MatchBatteryForEndpoint(friendlyName, batteryMap)
            : null;

        return new AudioDevice
        {
            Id = mmDevice.ID,
            Name = friendlyName,
            DeviceType = type,
            TransportKind = transport,
            EndpointState = state,
            IsDefault = string.Equals(mmDevice.ID, defaultId, StringComparison.OrdinalIgnoreCase),
            IsHealthy = state == AudioEndpointState.Active,
            BatteryPercentage = battery,
            LastSeen = DateTimeOffset.UtcNow
        };
    }

    private static (AudioDeviceType Type, AudioTransportKind Transport) ClassifyEndpoint(string name, MMDevice device)
    {
        var enumeratorName = TryReadStringProperty(device, PKEY_Device_EnumeratorName) ?? string.Empty;
        var infSection = TryReadStringProperty(device, PKEY_Device_BusInfSection) ?? string.Empty;
        var formFactor = TryReadIntProperty(device, PKEY_AudioEndpoint_FormFactor);

        var isBluetoothHfpBus =
            enumeratorName.Equals("BTHHFENUM", StringComparison.OrdinalIgnoreCase) ||
            infSection.Contains("BthHF", StringComparison.OrdinalIgnoreCase);

        // Identify Bluetooth Hands-Free telephony profile accurately without hiding USB/wired headsets
        if (isBluetoothHfpBus ||
            name.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("HF Audio", StringComparison.OrdinalIgnoreCase))
        {
            return (AudioDeviceType.HeadsetHandsFree, AudioTransportKind.BluetoothHandsFree);
        }

        var isBluetoothA2dp =
            enumeratorName.Equals("BTHENUM", StringComparison.OrdinalIgnoreCase) ||
            infSection.Contains("BthA2dp", StringComparison.OrdinalIgnoreCase);

        var isUsb =
            enumeratorName.Equals("USB", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("USB", StringComparison.OrdinalIgnoreCase);

        var isOnboardCodec =
            enumeratorName.Equals("INTELAUDIO", StringComparison.OrdinalIgnoreCase) ||
            enumeratorName.Equals("HDAUDIO", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Realtek", StringComparison.OrdinalIgnoreCase);

        var transport = isBluetoothA2dp ? AudioTransportKind.BluetoothA2dp
            : isUsb ? AudioTransportKind.UsbAudio
            : isOnboardCodec ? AudioTransportKind.BuiltInOrWiredJack
            : AudioTransportKind.Unknown;

        // FormFactor values from Windows EndpointFormFactor enum:
        // 1=Speakers, 2=LineLevel, 3=Headphones, 5=Headset, 8=SPDIF, 9=DigitalAudioDisplayDevice
        if (formFactor is 3 or 5 ||
            isBluetoothA2dp ||
            name.Contains("Headphones", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Earbuds", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Earphones", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Buds", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("AirPods", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Headset", StringComparison.OrdinalIgnoreCase))
        {
            return (AudioDeviceType.Headphones, transport);
        }

        if (formFactor is 8 or 9 ||
            name.Contains("HDMI", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("DisplayPort", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("S/PDIF", StringComparison.OrdinalIgnoreCase))
        {
            return (AudioDeviceType.DigitalDisplay, AudioTransportKind.DigitalDisplay);
        }

        // Onboard Realtek / HDAUDIO endpoints often share a "Speakers" FormFactor with the 3.5 mm wired headphone jack.
        // Do not misclassify them as speakers-only; represent ambiguously shared codecs neutrally as Other.
        if (isOnboardCodec)
        {
            return (AudioDeviceType.Other, AudioTransportKind.BuiltInOrWiredJack);
        }

        if (formFactor == 1)
        {
            return (AudioDeviceType.Speakers, transport);
        }

        if (formFactor == 2)
        {
            return (AudioDeviceType.LineOut, transport);
        }

        return (AudioDeviceType.Other, transport);
    }

    private static string? TryReadStringProperty(MMDevice device, PropertyKey key)
    {
        try
        {
            if (device.Properties.Contains(key))
            {
                return device.Properties[key].Value?.ToString();
            }
        }
        catch
        {
            // Ignore metadata read errors.
        }

        return null;
    }

    private static int? TryReadIntProperty(MMDevice device, PropertyKey key)
    {
        try
        {
            if (device.Properties.Contains(key) && device.Properties[key].Value is not null)
            {
                return Convert.ToInt32(device.Properties[key].Value);
            }
        }
        catch
        {
            // Ignore metadata read errors.
        }

        return null;
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
        try
        {
            DeviceChanged?.Invoke(this, new AudioDeviceChangedEventArgs(deviceId, kind, state));
        }
        catch
        {
            // Never allow listener exceptions to crash Core Audio callback threads.
        }
    }
}
