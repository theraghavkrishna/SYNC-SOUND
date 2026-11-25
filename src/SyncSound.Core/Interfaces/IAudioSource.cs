namespace SyncSound.Core.Interfaces;

/// <summary>
/// Describes the PCM format of an <see cref="IAudioSource"/> (32-bit IEEE float interleaved).
/// </summary>
public readonly record struct AudioPcmFormat(int SampleRate, int Channels);

/// <summary>
/// Pull- or push-capable audio source feeding the SYNC SOUND multi-output engine.
/// Produces 32-bit IEEE float interleaved samples in <see cref="Format"/>.
/// </summary>
public interface IAudioSource : IDisposable
{
    string DisplayName { get; }
    AudioPcmFormat Format { get; }
    bool CanPause { get; }
    bool CanSeek { get; }
    TimeSpan? Duration { get; }
    TimeSpan Position { get; }

    /// <summary>
    /// Raised when a finite source (such as a local track) finishes playing naturally.
    /// </summary>
    event EventHandler? EndOfStream;

    /// <summary>
    /// Raised when an unrecoverable source failure occurs (e.g., loopback capture endpoint lost).
    /// </summary>
    event EventHandler<string>? SourceFaulted;

    /// <summary>
    /// Reads interleaved float samples into <paramref name="destination"/>.
    /// Returns the number of float samples written (0 when temporarily starved or paused;
    /// check <see cref="IsEnded"/> for natural completion).
    /// </summary>
    int ReadSamples(Span<float> destination);

    bool IsEnded { get; }

    void Seek(TimeSpan position);
}
