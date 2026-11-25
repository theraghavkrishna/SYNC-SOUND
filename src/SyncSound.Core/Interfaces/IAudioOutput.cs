using SyncSound.Core.Models;

namespace SyncSound.Core.Interfaces;

/// <summary>
/// Represents an independent audio output renderer inside an <see cref="IAudioEngine"/> session.
/// </summary>
public interface IAudioOutput : IDisposable
{
    string DeviceId { get; }
    string DeviceName { get; }
    AudioOutputState State { get; }
    string? FormatSummary { get; }
    int ConfiguredDelayMs { get; }
    int EstimatedLatencyMs { get; }
    bool IsFaulted { get; }

    event EventHandler<string?>? Faulted;

    void Start();
    void Pause();
    void Resume();
    void Stop();
    void SetDelayCompensationMs(int delayMs);
    void WriteFloatSamples(ReadOnlySpan<float> samples, AudioPcmFormat sourceFormat);
    AudioOutputSnapshot GetSnapshot();
}

public interface IAudioOutputFactory
{
    IAudioOutput CreateOutput(AudioDevice device, AudioPcmFormat sourceFormat, AudioEngineConfiguration configuration);
}
