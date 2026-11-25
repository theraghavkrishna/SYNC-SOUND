using NAudio.Wave;

namespace SyncSound.Audio.Synchronization;

/// <summary>
/// Fixed-capacity ring delay line operating in the output sample pipeline for best-effort
/// per-device synchronization compensation (0 to 500 ms) without steady-state heap allocations.
/// </summary>
public sealed class DelayLineSampleProvider : ISampleProvider
{
    private const int MaxDelayMs = 500;

    private readonly ISampleProvider _source;
    private readonly float[] _ringBuffer;
    private readonly int _channels;
    private readonly int _sampleRate;
    private readonly Lock _gate = new();

    private int _writeIndex;
    private int _delayFrames;
    private int _primedFrames;

    public DelayLineSampleProvider(ISampleProvider source, int initialDelayMs = 0)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _sampleRate = source.WaveFormat.SampleRate;
        _channels = source.WaveFormat.Channels;

        var maxFrames = (_sampleRate * MaxDelayMs / 1000) + 1;
        _ringBuffer = new float[maxFrames * _channels];
        SetDelayMs(initialDelayMs);
    }

    public WaveFormat WaveFormat => _source.WaveFormat;

    public int DelayMs
    {
        get
        {
            lock (_gate)
            {
                return _delayFrames * 1000 / _sampleRate;
            }
        }
    }

    public void SetDelayMs(int delayMs)
    {
        var clampedMs = Math.Clamp(delayMs, 0, MaxDelayMs);
        var frames = clampedMs * _sampleRate / 1000;

        lock (_gate)
        {
            if (_delayFrames == frames)
            {
                return;
            }

            _delayFrames = frames;
            _primedFrames = 0;
            Array.Clear(_ringBuffer);
            _writeIndex = 0;
        }
    }

    public int Read(float[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public int Read(Span<float> buffer)
    {
        var read = _source.Read(buffer);
        if (read <= 0)
        {
            return read;
        }

        lock (_gate)
        {
            if (_delayFrames == 0)
            {
                return read;
            }

            var capacitySamples = _ringBuffer.Length;
            var delaySamples = _delayFrames * _channels;

            for (var i = 0; i < read; i++)
            {
                var inputSample = buffer[i];
                _ringBuffer[_writeIndex] = inputSample;

                if (_primedFrames * _channels < delaySamples)
                {
                    buffer[i] = 0.0f;
                    if ((i + 1) % _channels == 0)
                    {
                        _primedFrames++;
                    }
                }
                else
                {
                    var readIndex = _writeIndex - delaySamples;
                    if (readIndex < 0)
                    {
                        readIndex += capacitySamples;
                    }

                    buffer[i] = _ringBuffer[readIndex];
                }

                _writeIndex++;
                if (_writeIndex >= capacitySamples)
                {
                    _writeIndex = 0;
                }
            }
        }

        return read;
    }
}
