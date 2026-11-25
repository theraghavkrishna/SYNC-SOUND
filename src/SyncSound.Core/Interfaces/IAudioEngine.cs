using SyncSound.Core.Models;

namespace SyncSound.Core.Interfaces;

/// <summary>
/// Reusable multi-output audio fan-out engine supporting live device changes, recovery, and delay compensation.
/// </summary>
public interface IAudioEngine : IAsyncDisposable
{
    AudioEngineState State { get; }
    AudioEngineStatusSnapshot Status { get; }
    float MasterVolume { get; }

    event EventHandler<AudioEngineStatusSnapshot>? StatusChanged;
    event EventHandler? SourceCompleted;

    Task<AudioEngineStatusSnapshot> StartAsync(
        IAudioSource source,
        IReadOnlyList<AudioDevice> targetDevices,
        AudioEngineConfiguration? configuration = null,
        CancellationToken cancellationToken = default);

    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
    void SetMasterVolume(float volume);
    void SetOutputDelayMs(string deviceId, int delayMs);
    void SetOutputVolume(string deviceId, float volume);
}

public interface ISettingsService
{
    AppSettings Current { get; }
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
}
