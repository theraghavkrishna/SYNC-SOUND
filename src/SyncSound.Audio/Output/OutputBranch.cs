using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Output;

/// <summary>
/// Abstraction over a single initialized audio output branch for testability and fault isolation.
/// </summary>
public interface IOutputBranch : IDisposable
{
    string DeviceId { get; }
    string DeviceName { get; }
    string FormatSummary { get; }
    bool IsFaulted { get; }

    event EventHandler<string?>? Faulted;

    void Start();
    void EnqueueFloatSamples(byte[] buffer, int byteCount);
    void Stop();
}

public interface IOutputBranchFactory
{
    IOutputBranch CreateBranch(AudioDevice device, WaveFormat sourceFormat);
}

public sealed class WasapiOutputBranchFactory : IOutputBranchFactory
{
    public IOutputBranch CreateBranch(AudioDevice device, WaveFormat sourceFormat)
    {
        return new WasapiOutputBranch(device, sourceFormat);
    }
}

/// <summary>
/// Independent WASAPI shared-mode output branch with its own ring buffer, resampler, and fault boundary.
/// </summary>
public sealed class WasapiOutputBranch : IOutputBranch
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly MMDevice? _mmDevice;
    private readonly WasapiPlayer? _player;
    private readonly BufferedWaveProvider? _buffer;
    private readonly Lock _disposeGate = new();
    private volatile bool _disposed;
    private volatile bool _faulted;

    public WasapiOutputBranch(AudioDevice device, WaveFormat sourceFormat, int latencyMs = 80)
    {
        DeviceId = device.Id;
        DeviceName = device.Name;

        _enumerator = new MMDeviceEnumerator();
        try
        {
            _mmDevice = _enumerator.GetDevice(device.Id);
            if (_mmDevice.State != DeviceState.Active)
            {
                throw new InvalidOperationException($"Endpoint '{device.Name}' is not active (state: {_mmDevice.State}).");
            }

            using var audioClient = _mmDevice.CreateAudioClient();
            var mixFormat = audioClient.MixFormat;
            FormatSummary = $"{mixFormat.SampleRate} Hz, {mixFormat.Channels}ch, {mixFormat.BitsPerSample}-bit";

            _buffer = new BufferedWaveProvider(sourceFormat, TimeSpan.FromMilliseconds(500))
            {
                DiscardOnBufferOverflow = true,
                ReadFully = true
            };

            ISampleProvider sampleChain = _buffer.ToSampleProvider();

            if (mixFormat.Channels == 1 && sourceFormat.Channels == 2)
            {
                sampleChain = new StereoToMonoSampleProvider(sampleChain);
            }
            else if (mixFormat.Channels == 2 && sourceFormat.Channels == 1)
            {
                sampleChain = new MonoToStereoSampleProvider(sampleChain);
            }

            if (sampleChain.WaveFormat.SampleRate != mixFormat.SampleRate)
            {
                sampleChain = new WdlResamplingSampleProvider(sampleChain, mixFormat.SampleRate);
            }

            _player = new WasapiPlayerBuilder()
                .WithDevice(_mmDevice)
                .WithSharedMode()
                .WithPollingSync()
                .WithLatency(latencyMs)
                .WithMmcssThreadPriority("Audio")
                .Build();
            _player.PlaybackStopped += OnPlaybackStopped;
            _player.Init(new SampleToWaveProvider(sampleChain));
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public string DeviceId { get; }
    public string DeviceName { get; }
    public string FormatSummary { get; }
    public bool IsFaulted => _faulted;

    public event EventHandler<string?>? Faulted;

    public void Start()
    {
        if (_disposed || _faulted || _player is null)
        {
            return;
        }

        _player.Play();
    }

    public void EnqueueFloatSamples(byte[] buffer, int byteCount)
    {
        if (_disposed || _faulted || _buffer is null)
        {
            return;
        }

        try
        {
            _buffer.AddSamples(buffer, 0, byteCount);
        }
        catch (Exception ex)
        {
            MarkFaulted(ex.Message);
        }
    }

    public void Stop()
    {
        if (_disposed || _player is null)
        {
            return;
        }

        try
        {
            _player.Stop();
        }
        catch
        {
            // Ignore stop failures during teardown or device removal.
        }
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

            try
            {
                if (_player is not null)
                {
                    _player.PlaybackStopped -= OnPlaybackStopped;
                    _player.Stop();
                    _player.Dispose();
                }
            }
            catch
            {
                // Best-effort cleanup of audio client.
            }

            try
            {
                _mmDevice?.Dispose();
            }
            catch
            {
                // Ignore COM cleanup failures on invalidated devices.
            }

            _enumerator.Dispose();
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_disposed || _player is null)
        {
            return;
        }

        if (e.Exception is not null)
        {
            MarkFaulted(e.Exception.Message);
        }
    }

    private void MarkFaulted(string? reason)
    {
        if (_faulted)
        {
            return;
        }

        _faulted = true;
        Faulted?.Invoke(this, reason);
    }
}
