using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;
using SyncSound.Core.Interfaces;

namespace SyncSound.Audio.Capture;

/// <summary>
/// Captures Windows system audio from a specified render endpoint via WASAPI loopback
/// using NAudio 3.3.0 <see cref="WasapiRecorder"/> and exposes a continuous 48 kHz IEEE float stereo source.
/// </summary>
public sealed class WasapiLoopbackAudioSource : IAudioSource
{
    private const int TargetSampleRate = 48000;
    private const int TargetChannels = 2;

    private readonly MMDeviceEnumerator _enumerator;
    private readonly MMDevice _sourceDevice;
    private readonly WasapiRecorder _recorder;
    private readonly BufferedWaveProvider _captureBuffer;
    private readonly ISampleProvider _sampleChain;
    private readonly ILogger _logger;
    private readonly Lock _disposeGate = new();
    private byte[] _copyScratch = new byte[16384];
    private volatile bool _disposed;
    private volatile bool _faulted;

    public WasapiLoopbackAudioSource(string sourceDeviceId, string sourceDeviceName, ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
        SourceDeviceId = sourceDeviceId;
        DisplayName = sourceDeviceName;

        _enumerator = new MMDeviceEnumerator();
        try
        {
            _sourceDevice = _enumerator.GetDevice(sourceDeviceId);
            if (_sourceDevice.State != DeviceState.Active)
            {
                throw new InvalidOperationException($"Source device '{sourceDeviceName}' is not active.");
            }

            _recorder = new WasapiRecorderBuilder()
                .WithDevice(_sourceDevice)
                .WithLoopbackCapture()
                .WithSharedMode()
                .WithEventSync()
                .WithMmcssThreadPriority("Audio")
                .Build();

            var captureFormat = _recorder.WaveFormat;

            _captureBuffer = new BufferedWaveProvider(captureFormat, TimeSpan.FromMilliseconds(800))
            {
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };

            ISampleProvider chain = _captureBuffer.ToSampleProvider();
            if (captureFormat.Channels == 1)
            {
                chain = new MonoToStereoSampleProvider(chain);
            }
            else if (captureFormat.Channels > 2)
            {
                var mux = new MultiplexingSampleProvider([chain], 2);
                mux.ConnectInputToOutput(0, 0);
                mux.ConnectInputToOutput(1, 1);
                chain = mux;
            }

            if (chain.WaveFormat.SampleRate != TargetSampleRate)
            {
                chain = new WdlResamplingSampleProvider(chain, TargetSampleRate);
            }

            _sampleChain = chain;
            Format = new AudioPcmFormat(TargetSampleRate, TargetChannels);

            _recorder.DataAvailable += OnDataAvailable;
            _recorder.RecordingStopped += OnRecordingStopped;
            _recorder.StartRecording();

            _logger.Information("Started WASAPI loopback capture on {SourceDevice} ({Format})", sourceDeviceName, captureFormat);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public string SourceDeviceId { get; }
    public string DisplayName { get; }
    public AudioPcmFormat Format { get; }
    public bool CanPause => false;
    public bool CanSeek => false;
    public TimeSpan? Duration => null;
    public TimeSpan Position => TimeSpan.Zero;
    public bool IsEnded => _faulted;

    public event EventHandler? EndOfStream;
    public event EventHandler<string>? SourceFaulted;

    public int ReadSamples(Span<float> destination)
    {
        if (_disposed || _faulted || destination.IsEmpty)
        {
            return 0;
        }

        if (_captureBuffer.BufferedDuration.TotalMilliseconds > 180)
        {
            _captureBuffer.ClearBuffer();
        }

        return _sampleChain.Read(destination);
    }

    public void Seek(TimeSpan position)
    {
        // Live system loopback cannot seek.
    }

    public void Dispose()
    {
        lock (_disposeGate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _ = EndOfStream;

            if (_recorder is not null)
            {
                _recorder.DataAvailable -= OnDataAvailable;
                _recorder.RecordingStopped -= OnRecordingStopped;
                try
                {
                    _recorder.StopRecording();
                }
                catch
                {
                    // Ignore stop failures on invalidated device.
                }

                _recorder.Dispose();
            }

            _sourceDevice?.Dispose();
            _enumerator.Dispose();
        }
    }

    private void OnDataAvailable(ReadOnlySpan<byte> buffer, AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        if (_disposed || buffer.IsEmpty)
        {
            return;
        }

        if (_copyScratch.Length < buffer.Length)
        {
            _copyScratch = new byte[buffer.Length];
        }

        if ((flags & AudioClientBufferFlags.Silent) != 0)
        {
            Array.Clear(_copyScratch, 0, buffer.Length);
        }
        else
        {
            buffer.CopyTo(_copyScratch);
        }

        _captureBuffer.AddSamples(_copyScratch, 0, buffer.Length);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        if (e.Exception is not null)
        {
            _faulted = true;
            _logger.Warning(e.Exception, "WASAPI loopback capture stopped unexpectedly");
            SourceFaulted?.Invoke(this, "We couldn't access your computer's audio.");
        }
    }
}
