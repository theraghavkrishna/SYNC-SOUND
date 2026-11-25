using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;
using SyncSound.Audio.Synchronization;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Output;

public sealed class WasapiAudioOutputFactory : IAudioOutputFactory
{
    private readonly ISyncCoordinator _syncCoordinator;
    private readonly ILogger _logger;

    public WasapiAudioOutputFactory(ISyncCoordinator? syncCoordinator = null, ILogger? logger = null)
    {
        _syncCoordinator = syncCoordinator ?? new BestEffortSyncCoordinator();
        _logger = logger ?? Log.Logger;
    }

    public IAudioOutput CreateOutput(AudioDevice device, AudioPcmFormat sourceFormat, AudioEngineConfiguration configuration)
    {
        var delayMs = 0;
        if (configuration.PerOutputDelayMs is not null &&
            configuration.PerOutputDelayMs.TryGetValue(device.Id, out var configured))
        {
            delayMs = configured;
        }

        var estimatedLatency = _syncCoordinator.EstimateEndpointLatencyMs(device, configuration.OutputLatencyMs);
        var initialVolume = 1.0f;
        if (configuration.PerOutputVolume is not null &&
            configuration.PerOutputVolume.TryGetValue(device.Id, out var configuredVolume))
        {
            initialVolume = configuredVolume;
        }

        return new WasapiAudioOutput(device, sourceFormat, configuration, delayMs, estimatedLatency, initialVolume, _logger);
    }
}

/// <summary>
/// Independent WASAPI shared-mode output renderer with managed resampling, channel adaptation,
/// delay compensation, and zero-allocation steady-state writes.
/// </summary>
public sealed class WasapiAudioOutput : IAudioOutput
{
    private readonly MMDeviceEnumerator _enumerator;
    private readonly MMDevice? _mmDevice;
    private readonly WasapiPlayer? _player;
    private readonly BufferedWaveProvider? _buffer;
    private readonly DelayLineSampleProvider? _delayLine;
    private readonly ILogger _logger;
    private readonly Lock _disposeGate = new();
    private readonly byte[] _scratchBytes;

    private volatile AudioOutputState _state = AudioOutputState.Initializing;
    private volatile bool _disposed;
    private volatile bool _faulted;
    private volatile float _volume = 1.0f;
    private long _underrunCount;
    private string? _errorMessage;

    public WasapiAudioOutput(
        AudioDevice device,
        AudioPcmFormat sourceFormat,
        AudioEngineConfiguration configuration,
        int initialDelayMs,
        int estimatedLatencyMs,
        float initialVolume = 1.0f,
        ILogger? logger = null)
    {
        DeviceId = device.Id;
        DeviceName = device.Name;
        EstimatedLatencyMs = estimatedLatencyMs;
        _volume = Math.Clamp(initialVolume, 0.0f, 1.0f);
        _logger = logger ?? Log.Logger;

        // Preallocate enough scratch space for typical 20-100 ms chunks without heap churn.
        var initialCapacityBytes = sourceFormat.SampleRate * sourceFormat.Channels * sizeof(float) / 5;
        _scratchBytes = new byte[Math.Max(16384, initialCapacityBytes)];

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

            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sourceFormat.SampleRate, sourceFormat.Channels);
            _buffer = new BufferedWaveProvider(waveFormat, TimeSpan.FromMilliseconds(Math.Clamp(configuration.BufferDurationMs, 150, 2000)))
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

            _delayLine = new DelayLineSampleProvider(sampleChain, initialDelayMs);

            _player = new WasapiPlayerBuilder()
                .WithDevice(_mmDevice)
                .WithSharedMode()
                .WithPollingSync()
                .WithLatency(Math.Clamp(configuration.OutputLatencyMs, 20, 200))
                .WithMmcssThreadPriority("Audio")
                .Build();

            _player.PlaybackStopped += OnPlaybackStopped;
            _player.Init(new SampleToWaveProvider(_delayLine));
            _state = AudioOutputState.Stopped;
        }
        catch (Exception ex)
        {
            _state = AudioOutputState.Failed;
            _errorMessage = ex.Message;
            Dispose();
            throw;
        }
    }

    public string DeviceId { get; }
    public string DeviceName { get; }
    public AudioOutputState State => _state;
    public string? FormatSummary { get; }
    public int ConfiguredDelayMs => _delayLine?.DelayMs ?? 0;
    public int EstimatedLatencyMs { get; }
    public float Volume => _volume;
    public bool IsFaulted => _faulted;

    public event EventHandler<string?>? Faulted;

    public void Start()
    {
        if (_disposed || _faulted || _player is null)
        {
            return;
        }

        _player.Play();
        _state = AudioOutputState.Playing;
    }

    public void Pause()
    {
        if (_disposed || _faulted || _player is null)
        {
            return;
        }

        _player.Pause();
        _state = AudioOutputState.Paused;
    }

    public void Resume()
    {
        if (_disposed || _faulted || _player is null)
        {
            return;
        }

        _player.Play();
        _state = AudioOutputState.Playing;
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
            _buffer?.ClearBuffer();
            if (!_faulted)
            {
                _state = AudioOutputState.Stopped;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error stopping WASAPI output {DeviceName}", DeviceName);
        }
    }

    public void SetDelayCompensationMs(int delayMs)
    {
        _delayLine?.SetDelayMs(delayMs);
    }

    public void SetVolume(float volume)
    {
        _volume = Math.Clamp(volume, 0.0f, 1.0f);
    }

    public void WriteFloatSamples(ReadOnlySpan<float> samples, AudioPcmFormat sourceFormat)
    {
        if (_disposed || _faulted || _buffer is null || samples.IsEmpty)
        {
            return;
        }

        var byteCount = samples.Length * sizeof(float);
        var targetBytes = byteCount <= _scratchBytes.Length
            ? _scratchBytes
            : new byte[byteCount];

        var vol = _volume;
        if (vol >= 0.999f)
        {
            MemoryMarshal.AsBytes(samples).CopyTo(targetBytes);
        }
        else if (vol <= 0.001f)
        {
            Array.Clear(targetBytes, 0, byteCount);
        }
        else
        {
            var destFloats = MemoryMarshal.Cast<byte, float>(targetBytes.AsSpan(0, byteCount));
            for (var i = 0; i < samples.Length; i++)
            {
                destFloats[i] = samples[i] * vol;
            }
        }

        try
        {
            if (_buffer.BufferedBytes == 0 && _state == AudioOutputState.Playing)
            {
                Interlocked.Increment(ref _underrunCount);
            }

            _buffer.AddSamples(targetBytes, 0, byteCount);
        }
        catch (Exception ex)
        {
            MarkFaulted(ex.Message);
        }
    }

    public AudioOutputSnapshot GetSnapshot()
    {
        var bufferedMs = _buffer?.BufferedDuration.TotalMilliseconds ?? 0;
        return new AudioOutputSnapshot(
            DeviceId,
            DeviceName,
            _state,
            FormatSummary,
            ConfiguredDelayMs,
            EstimatedLatencyMs,
            Math.Round(bufferedMs, 1),
            Interlocked.Read(ref _underrunCount),
            _errorMessage);
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
            catch (Exception ex)
            {
                _logger.Debug(ex, "Ignored WASAPI player dispose error for {DeviceName}", DeviceName);
            }

            try
            {
                _mmDevice?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Ignored MMDevice dispose error for {DeviceName}", DeviceName);
            }

            _enumerator.Dispose();
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_disposed)
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
        _state = AudioOutputState.Failed;
        _errorMessage = reason;
        _logger.Warning("Output branch {DeviceName} faulted: {Reason}", DeviceName, reason);
        Faulted?.Invoke(this, reason);
    }
}
