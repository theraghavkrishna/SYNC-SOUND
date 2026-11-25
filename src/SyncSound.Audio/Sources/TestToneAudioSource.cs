using SyncSound.Audio.Generators;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Sources;

/// <summary>
/// Adapts <see cref="TestSignalSampleProvider"/> into an <see cref="IAudioSource"/> for the reusable AudioEngine.
/// </summary>
public sealed class TestToneAudioSource : IAudioSource
{
    private readonly TestSignalSampleProvider _provider;
    private bool _disposed;

    public TestToneAudioSource(AudioTestMode mode, int sampleRate = 48000, int channels = 2, float gain = 0.20f)
    {
        _provider = new TestSignalSampleProvider(mode, sampleRate, channels, gain);
        DisplayName = mode == AudioTestMode.ContinuousTone440Hz
            ? "Test Tone (440 Hz)"
            : "Rhythmic Chime Pulse";
        Format = new AudioPcmFormat(sampleRate, channels);
    }

    public string DisplayName { get; }
    public AudioPcmFormat Format { get; }
    public bool CanPause => true;
    public bool CanSeek => false;
    public TimeSpan? Duration => null;
    public TimeSpan Position => TimeSpan.Zero;
    public bool IsEnded => false;

    public event EventHandler? EndOfStream;
    public event EventHandler<string>? SourceFaulted;

    public int ReadSamples(Span<float> destination)
    {
        if (_disposed)
        {
            return 0;
        }

        return _provider.Read(destination);
    }

    public void Seek(TimeSpan position)
    {
        // Continuous generator does not support seeking.
    }

    public void Dispose()
    {
        _disposed = true;
        _ = EndOfStream;
        _ = SourceFaulted;
    }
}
