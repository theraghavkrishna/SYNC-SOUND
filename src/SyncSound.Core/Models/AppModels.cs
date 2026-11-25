namespace SyncSound.Core.Models;

public sealed record AudioTrackInfo(
    string FilePath,
    string Title,
    string Artist,
    string Album,
    TimeSpan Duration,
    byte[]? AlbumArtBytes = null);

public enum TrackPlaybackStatus
{
    Stopped = 0,
    Loading,
    Playing,
    Paused,
    Error
}

public sealed record PlaybackState(
    TrackPlaybackStatus Status,
    AudioTrackInfo? CurrentTrack,
    TimeSpan Position,
    TimeSpan Duration,
    float Volume,
    IReadOnlyList<AudioTrackInfo> Queue,
    IReadOnlyList<AudioDevice> SelectedOutputs,
    string? ErrorMessage = null);

public sealed record SharingState(
    AudioEngineState State,
    bool IsSharing,
    AudioDevice? SourceDevice,
    IReadOnlyList<AudioDevice> SelectedDevices,
    IReadOnlyList<AudioOutputSnapshot> ActiveOutputs,
    IReadOnlyList<AudioOutputSnapshot> FailedOutputs,
    DateTimeOffset? StartTime,
    string UserMessage,
    string? ErrorMessage = null);

public sealed class AppSettings
{
    public string Hotkey { get; set; } = "Ctrl+Shift+S";
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; }
    public bool CloseToTray { get; set; } = true;
    public bool SidebarAlwaysOnTop { get; set; } = true;
    public bool SidebarClickOutsideToClose { get; set; } = true;
    public bool AutoSelectPreferredDevices { get; set; } = true;
    public float PlayerVolume { get; set; } = 0.85f;
    public List<string> SelectedDeviceIds { get; set; } = [];
    public List<string> PreferredDeviceIds { get; set; } = [];
    public Dictionary<string, int> PerDeviceDelayMs { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
