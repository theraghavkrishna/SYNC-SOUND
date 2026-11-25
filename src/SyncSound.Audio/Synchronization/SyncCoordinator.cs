using SyncSound.Core.Models;

namespace SyncSound.Audio.Synchronization;

public interface ISyncCoordinator
{
    int EstimateEndpointLatencyMs(AudioDevice device, int wasapiBufferLatencyMs);
    IReadOnlyDictionary<string, int> ComputeSuggestedDelays(
        IReadOnlyList<AudioDevice> devices,
        int wasapiBufferLatencyMs,
        IReadOnlyDictionary<string, int>? userOverrides = null);
}

/// <summary>
/// Estimates relative output latency and resolves per-device delay compensation.
/// Bluetooth A2DP devices typically carry 120-250 ms more pipeline delay than wired speakers;
/// user overrides take precedence when specified.
/// </summary>
public sealed class BestEffortSyncCoordinator : ISyncCoordinator
{
    public int EstimateEndpointLatencyMs(AudioDevice device, int wasapiBufferLatencyMs)
    {
        var baseTransportLatency = device.DeviceType switch
        {
            AudioDeviceType.Headphones when IsLikelyBluetooth(device) => 180,
            AudioDeviceType.HeadsetHandsFree => 160,
            AudioDeviceType.DigitalDisplay => 35,
            _ => 12
        };

        return baseTransportLatency + Math.Max(0, wasapiBufferLatencyMs);
    }

    public IReadOnlyDictionary<string, int> ComputeSuggestedDelays(
        IReadOnlyList<AudioDevice> devices,
        int wasapiBufferLatencyMs,
        IReadOnlyDictionary<string, int>? userOverrides = null)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (devices.Count == 0)
        {
            return result;
        }

        foreach (var device in devices)
        {
            if (userOverrides is not null && userOverrides.TryGetValue(device.Id, out var manualDelay))
            {
                result[device.Id] = Math.Clamp(manualDelay, 0, 500);
            }
            else
            {
                result[device.Id] = 0;
            }
        }

        return result;
    }

    private static bool IsLikelyBluetooth(AudioDevice device)
    {
        var name = device.Name;
        return name.Contains("Buds", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("AirPods", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("TWS", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("Headphones", StringComparison.OrdinalIgnoreCase) &&
               !name.Contains("Realtek", StringComparison.OrdinalIgnoreCase);
    }
}
