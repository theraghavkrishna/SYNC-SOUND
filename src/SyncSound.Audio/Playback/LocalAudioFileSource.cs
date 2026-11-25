using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Playback;

/// <summary>
/// Decodes local audio files (MP3, WAV, FLAC, M4A, WMA, OGG) into 48 kHz IEEE float stereo
/// for multi-output playback through <see cref="IAudioEngine"/>.
/// </summary>
public sealed class LocalAudioFileSource : IAudioSource
{
    private const int TargetSampleRate = 48000;
    private const int TargetChannels = 2;

    private readonly AudioFileReader _reader;
    private readonly ISampleProvider _sampleChain;
    private readonly Lock _ioGate = new();
    private bool _isEnded;
    private bool _disposed;

    public LocalAudioFileSource(AudioTrackInfo track)
    {
        ArgumentNullException.ThrowIfNull(track);
        Track = track;
        DisplayName = track.Title;

        _reader = new AudioFileReader(track.FilePath);
        ISampleProvider chain = _reader;

        if (chain.WaveFormat.Channels == 1)
        {
            chain = new MonoToStereoSampleProvider(chain);
        }

        if (chain.WaveFormat.SampleRate != TargetSampleRate)
        {
            chain = new WdlResamplingSampleProvider(chain, TargetSampleRate);
        }

        _sampleChain = chain;
        Format = new AudioPcmFormat(TargetSampleRate, TargetChannels);
    }

    public AudioTrackInfo Track { get; }
    public string DisplayName { get; }
    public AudioPcmFormat Format { get; }
    public bool CanPause => true;
    public bool CanSeek => true;
    public TimeSpan? Duration => _reader.TotalTime;

    public TimeSpan Position
    {
        get
        {
            lock (_ioGate)
            {
                return _disposed ? TimeSpan.Zero : _reader.CurrentTime;
            }
        }
    }

    public bool IsEnded
    {
        get
        {
            lock (_ioGate)
            {
                return _isEnded;
            }
        }
    }

    public event EventHandler? EndOfStream;
    public event EventHandler<string>? SourceFaulted;

    public static AudioTrackInfo ReadTrackMetadata(string filePath)
    {
        var fallbackTitle = Path.GetFileNameWithoutExtension(filePath);
        try
        {
            using var tagFile = TagLib.File.Create(filePath);
            var title = !string.IsNullOrWhiteSpace(tagFile.Tag.Title) ? tagFile.Tag.Title : fallbackTitle;
            var artist = tagFile.Tag.FirstPerformer
                         ?? tagFile.Tag.FirstAlbumArtist
                         ?? "Unknown Artist";
            var album = tagFile.Tag.Album ?? string.Empty;
            var duration = tagFile.Properties.Duration;
            byte[]? artBytes = tagFile.Tag.Pictures.Length > 0 ? tagFile.Tag.Pictures[0].Data.Data : null;

            return new AudioTrackInfo(filePath, title, artist, album, duration, artBytes);
        }
        catch
        {
            try
            {
                using var reader = new AudioFileReader(filePath);
                return new AudioTrackInfo(filePath, fallbackTitle, "Unknown Artist", string.Empty, reader.TotalTime);
            }
            catch
            {
                return new AudioTrackInfo(filePath, fallbackTitle, "Unknown Artist", string.Empty, TimeSpan.Zero);
            }
        }
    }

    public int ReadSamples(Span<float> destination)
    {
        lock (_ioGate)
        {
            if (_disposed || _isEnded || destination.IsEmpty)
            {
                return 0;
            }

            try
            {
                var read = _sampleChain.Read(destination);
                if (read == 0)
                {
                    _isEnded = true;
                    EndOfStream?.Invoke(this, EventArgs.Empty);
                    return 0;
                }

                if (read < destination.Length)
                {
                    destination[read..].Clear();
                    _isEnded = true;
                }

                return read;
            }
            catch (Exception ex)
            {
                _isEnded = true;
                SourceFaulted?.Invoke(this, ex.Message);
                return 0;
            }
        }
    }

    public void Seek(TimeSpan position)
    {
        lock (_ioGate)
        {
            if (_disposed)
            {
                return;
            }

            var clamped = position < TimeSpan.Zero
                ? TimeSpan.Zero
                : position > _reader.TotalTime
                    ? _reader.TotalTime
                    : position;

            _reader.CurrentTime = clamped;
            _isEnded = false;
        }
    }

    public void Dispose()
    {
        lock (_ioGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _reader.Dispose();
        }
    }
}
