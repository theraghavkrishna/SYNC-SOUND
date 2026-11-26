using SyncSound.Core.Models;

namespace SyncSound.App.Services;

public enum SyncVisualState
{
    NoAdditionalHeadphones,
    OneDeviceConnected,
    Ready,
    Starting,
    Listening,
    Disconnected,
    Recovering
}

/// <summary>
/// Centralized, state-aware editorial copy system for SYNC SOUND.
/// Keeps technical diagnostics out of the emotional center of the product.
/// </summary>
public static class SyncCopy
{
    public static (SyncVisualState State, string Headline, string AccentLine, string Subtitle, string PrimaryAction) GetListenCopy(
        int totalCount,
        int selectedCount,
        SharingState sharing,
        AudioDevice? defaultSource)
    {
        if (totalCount == 0)
        {
            return (
                SyncVisualState.NoAdditionalHeadphones,
                "Let's get everyone connected.",
                "Your listening group starts here.",
                "Connect headphones or speakers to Windows and they'll appear automatically.",
                "Add headphones"
            );
        }

        if (sharing.IsSharing)
        {
            if (sharing.FailedOutputs.Count > 0 || sharing.State == AudioEngineState.Partial)
            {
                return (
                    SyncVisualState.Recovering,
                    "One headphone disconnected.",
                    "Getting everyone back together…",
                    $"{sharing.ActiveOutputs.Count} still listening while we reconnect.",
                    "Stop listening"
                );
            }

            var listeners = sharing.ActiveOutputs.Count + (sharing.SourceDevice is not null ? 1 : 0);
            return (
                SyncVisualState.Listening,
                "You're all listening.",
                "Everyone's in.",
                $"{listeners} {(listeners == 1 ? "device" : "devices")} sharing your Windows sound.",
                "Stop listening"
            );
        }

        if (totalCount == 1)
        {
            return (
                SyncVisualState.OneDeviceConnected,
                "One headphone connected.",
                "Add another to listen together.",
                "Pair a second pair in Windows, then choose who joins.",
                "Add headphones"
            );
        }

        if (selectedCount == 0)
        {
            return (
                SyncVisualState.OneDeviceConnected,
                "Choose who joins the sound.",
                "Pick your listening group.",
                $"{totalCount} audio devices connected. Select who hears.",
                "Choose headphones"
            );
        }

        if (defaultSource is not null && selectedCount == 1 &&
            sharing.SelectedDevices.Count == 1 &&
            string.Equals(sharing.SelectedDevices[0].Id, defaultSource.Id, StringComparison.OrdinalIgnoreCase))
        {
            return (
                SyncVisualState.OneDeviceConnected,
                "One headphone ready.",
                "Add another to listen together.",
                $"{defaultSource.Name} is already playing. Select one more to share.",
                "Choose headphones"
            );
        }

        return (
            SyncVisualState.Ready,
            "Everyone's here.",
            "Your headphones are ready.",
            $"{selectedCount} {(selectedCount == 1 ? "device" : "devices")} selected. Press Start listening whenever you're ready.",
            "Start listening"
        );
    }
}
