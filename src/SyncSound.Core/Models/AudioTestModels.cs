namespace SyncSound.Core.Models;

/// <summary>
/// Signal modes supported by the Phase 0 feasibility test generator.
/// </summary>
public enum AudioTestMode
{
    /// <summary>
    /// Smooth 440 Hz sine wave for verifying clean playback on one or more outputs.
    /// </summary>
    ContinuousTone440Hz = 0,

    /// <summary>
    /// Rhythmic dual-tone chime pulse (120 BPM) so latency skew between devices is clearly audible.
    /// </summary>
    RhythmicChimePulse = 1
}

public enum OutputBranchState
{
    Stopped = 0,
    Starting,
    Playing,
    Disconnected,
    Failed
}

public sealed record OutputBranchStatus(
    string DeviceId,
    string DeviceName,
    OutputBranchState State,
    string? FormatSummary = null,
    string? ErrorMessage = null);

public sealed record MultiOutputStartResult(
    bool AnyStarted,
    IReadOnlyList<OutputBranchStatus> BranchStatuses,
    string SummaryMessage);
