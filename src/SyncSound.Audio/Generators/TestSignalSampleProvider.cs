using NAudio.Wave;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Generators;

/// <summary>
/// Generates a clean 440 Hz tone or a 120 BPM rhythmic chime pulse in 32-bit IEEE float stereo.
/// </summary>
public sealed class TestSignalSampleProvider : ISampleProvider
{
    private const double TwoPi = Math.PI * 2.0;
    private readonly AudioTestMode _mode;
    private readonly float _gain;
    private long _frameIndex;
    private double _phasePrimary;
    private double _phaseAccent;

    public TestSignalSampleProvider(AudioTestMode mode, int sampleRate = 48000, int channels = 2, float gain = 0.20f)
    {
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (channels is < 1 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(channels));
        }

        _mode = mode;
        _gain = Math.Clamp(gain, 0.0f, 0.9f);
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public int Read(Span<float> buffer)
    {
        var channels = WaveFormat.Channels;
        var frames = buffer.Length / channels;
        var sampleRate = WaveFormat.SampleRate;
        var fadeInFrames = Math.Max(1, sampleRate / 50); // 20 ms smooth fade-in

        for (var i = 0; i < frames; i++)
        {
            var fadeIn = _frameIndex < fadeInFrames
                ? (float)_frameIndex / fadeInFrames
                : 1.0f;

            var sample = _mode == AudioTestMode.ContinuousTone440Hz
                ? NextContinuousSample(sampleRate)
                : NextRhythmicPulseSample(sampleRate);

            sample *= _gain * fadeIn;

            var writeIdx = i * channels;
            buffer[writeIdx] = sample;
            if (channels == 2)
            {
                buffer[writeIdx + 1] = sample;
            }

            _frameIndex++;
        }

        return frames * channels;
    }

    private float NextContinuousSample(int sampleRate)
    {
        var value = (float)Math.Sin(_phasePrimary);
        _phasePrimary += TwoPi * 440.0 / sampleRate;
        if (_phasePrimary >= TwoPi)
        {
            _phasePrimary -= TwoPi;
        }

        return value;
    }

    private float NextRhythmicPulseSample(int sampleRate)
    {
        // 120 BPM => 1 beat every 0.5 seconds; 4-beat bar = 2.0 seconds
        var framesPerBeat = Math.Max(1, sampleRate / 2);
        var beatNumber = (int)((_frameIndex / framesPerBeat) % 4);
        var frameInBeat = _frameIndex % framesPerBeat;
        var timeInBeatSec = (double)frameInBeat / sampleRate;

        // Warm 280 ms sustained chord chime with clean 180 ms silent tail (total 500 ms beat)
        // Ensures wired USB-C DACs and 3.5 mm headphones deliver full, unmistakable RMS loudness.
        var freq = beatNumber == 0 ? 587.33 : 440.0;
        var harmonic = beatNumber == 0 ? 0.38 : 0.24;
        var fadeOut = timeInBeatSec >= 0.25 && timeInBeatSec < 0.30
            ? (0.30 - timeInBeatSec) / 0.05
            : 1.0;
        var env = timeInBeatSec < 0.30
            ? Math.Exp(-timeInBeatSec * 5.2) * Math.Min(1.0, timeInBeatSec / 0.008) * fadeOut
            : 0.0;

        var primary = Math.Sin(_phasePrimary);
        var overtone = Math.Sin(_phaseAccent) * harmonic;

        _phasePrimary += TwoPi * freq / sampleRate;
        if (_phasePrimary >= TwoPi)
        {
            _phasePrimary -= TwoPi;
        }

        _phaseAccent += TwoPi * (freq * 2.0) / sampleRate;
        if (_phaseAccent >= TwoPi)
        {
            _phaseAccent -= TwoPi;
        }

        return (float)((primary + overtone) * env);
    }
}
