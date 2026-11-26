using SyncSound.Core.Models;

namespace SyncSound.App.Services;

public enum SyncVisualState
{
    NoAdditionalHeadphones,
    OneDeviceConnected,
    Ready,
    Starting,
    Listening,
    Muted,
    Disconnected,
    Recovering
}

/// <summary>
/// Centralized, truthful, state-aware copywriting system for SYNC SOUND.
/// Supports any combination of wired, USB, and Bluetooth endpoints without blanket exclusions.
/// </summary>
public static class SyncCopy
{
    public static (SyncVisualState State, string Headline, string AccentLine, string Subtitle, string PrimaryAction) GetListenCopy(
        IReadOnlyList<AudioDevice> allDevices,
        IReadOnlyList<AudioDevice> selectedDevices,
        SharingState sharing,
        AudioDevice? defaultSource,
        bool isMuted)
    {
        if (allDevices.Count == 0)
        {
            return (
                SyncVisualState.NoAdditionalHeadphones,
                "Let's get everyone connected.",
                "Nothing connected yet.",
                "Connect your wired or Bluetooth headphones to Windows, then bring them here.",
                "Add headphones"
            );
        }

        if (sharing.IsSharing)
        {
            if (sharing.FailedOutputs.Count > 0 || sharing.State == AudioEngineState.Partial)
            {
                return (
                    SyncVisualState.Recovering,
                    "One went quiet.",
                    "Bringing everyone back.",
                    $"{sharing.ActiveOutputs.Count} still listening while we reconnect.",
                    "Stop listening"
                );
            }

            if (isMuted)
            {
                return (
                    SyncVisualState.Muted,
                    "Session muted.",
                    "Everyone's on pause.",
                    "Raise the dial or tap Unmute whenever you're ready.",
                    "Stop listening"
                );
            }

            var totalListeners = sharing.ActiveOutputs.Count + (sharing.SourceDevice is not null ? 1 : 0);
            return (
                SyncVisualState.Listening,
                "You're all listening.",
                "Everyone's in.",
                $"{totalListeners} {(totalListeners == 1 ? "device" : "devices")} sharing your Windows sound.",
                "Stop listening"
            );
        }

        var hasSecondaryFanout = defaultSource is not null
            ? selectedDevices.Any(d => !string.Equals(d.Id, defaultSource.Id, StringComparison.OrdinalIgnoreCase))
            : selectedDevices.Count >= 2;

        if (allDevices.Count == 1)
        {
            return (
                SyncVisualState.OneDeviceConnected,
                "One's ready.",
                "Add another device.",
                "Plug in wired headphones or pair a Bluetooth headset to listen together.",
                "Add headphones"
            );
        }

        if (selectedDevices.Count == 0)
        {
            return (
                SyncVisualState.OneDeviceConnected,
                "Choose who's listening.",
                "Listen together.",
                $"{allDevices.Count} devices available. Pick who joins your session.",
                "Choose headphones"
            );
        }

        if (!hasSecondaryFanout)
        {
            return (
                SyncVisualState.OneDeviceConnected,
                "One's ready.",
                "Add another device.",
                $"{selectedDevices[0].DisplayName} is your default audio. Select one more device to listen together.",
                "Choose headphones"
            );
        }

        return (
            SyncVisualState.Ready,
            "Everyone's here.",
            "Listen together.",
            $"{selectedDevices.Count} {(selectedDevices.Count == 1 ? "device" : "devices")} in your group. Press Start listening to play.",
            "Start listening"
        );
    }
}
