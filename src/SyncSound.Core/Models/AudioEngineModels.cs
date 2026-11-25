namespace SyncSound.Core.Models;

/// <summary>
/// High-level state machine for the SYNC SOUND audio engine and sharing session.
/// </summary>
public enum AudioEngineState
{
    Idle = 0,
    Preparing,
    Playing,
    Paused,
    Recovering,
    Partial,
    Stopping,
    Error
}

/// <summary>
/// Lifecycle state of an individual audio output endpoint inside a session.
/// </summary>
public enum AudioOutputState
{
    Stopped = 0,
    Initializing,
    Playing,
    Paused,
    Recovering,
    Disconnected,
    Failed
}

/// <summary>
/// Configuration parameters for an audio engine session.
/// </summary>
public sealed record AudioEngineConfiguration
{
    public int SampleRate { get; init; } = 48000;
    public int Channels { get; init; } = 2;
    public int ChunkMs { get; init; } = 10;
    public int OutputLatencyMs { get; init; } = 60;
    public int BufferDurationMs { get; init; } = 600;
    public int PrefillChunks { get; init; } = 8;
    public bool AutoRecoverOutputs { get; init; } = true;
    public float MasterVolume { get; init; } = 1.0f;
    public IReadOnlyDictionary<string, int>? PerOutputDelayMs { get; init; }
    public IReadOnlyDictionary<string, float>? PerOutputVolume { get; init; }
}

/// <summary>
/// Runtime status and diagnostics snapshot for a single active output branch.
/// </summary>
public sealed record AudioOutputSnapshot(
    string DeviceId,
    string DeviceName,
    AudioOutputState State,
    string? FormatSummary = null,
    int ConfiguredDelayMs = 0,
    int EstimatedLatencyMs = 0,
    double BufferedMs = 0,
    long UnderrunCount = 0,
    string? ErrorMessage = null);

/// <summary>
/// Immutable snapshot of the audio engine session.
/// </summary>
public sealed record AudioEngineStatusSnapshot(
    AudioEngineState State,
    string SourceName,
    IReadOnlyList<AudioOutputSnapshot> Outputs,
    string UserMessage,
    DateTimeOffset? StartedAtUtc = null);
