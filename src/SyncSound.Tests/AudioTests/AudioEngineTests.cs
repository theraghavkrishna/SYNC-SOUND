using NAudio.Wave;
using SyncSound.Audio.Engine;
using SyncSound.Audio.Sources;
using SyncSound.Audio.Synchronization;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Tests.AudioTests;

public sealed class AudioEngineTests
{
    [Fact]
    public async Task Lifecycle_PlayPauseResumeStop_TransitionsCleanly()
    {
        var enumerator = new FakeEnumerator();
        var factory = new FakeOutputFactory();
        await using var engine = new AudioEngine(enumerator, factory);
        var states = new List<AudioEngineState>();
        engine.StatusChanged += (_, s) => states.Add(s.State);

        using var source = new TestToneAudioSource(AudioTestMode.ContinuousTone440Hz);
        var d1 = CreateDevice("dev-1", "Buds One");
        var d2 = CreateDevice("dev-2", "Buds Two");

        var startStatus = await engine.StartAsync(source, [d1, d2]);
        Assert.Equal(AudioEngineState.Playing, startStatus.State);
        Assert.Equal(2, startStatus.Outputs.Count);
        Assert.All(factory.Outputs, o => Assert.Equal(AudioOutputState.Playing, o.State));

        await engine.PauseAsync();
        Assert.Equal(AudioEngineState.Paused, engine.State);
        Assert.All(factory.Outputs, o => Assert.Equal(AudioOutputState.Paused, o.State));

        await engine.ResumeAsync();
        Assert.Equal(AudioEngineState.Playing, engine.State);
        Assert.All(factory.Outputs, o => Assert.Equal(AudioOutputState.Playing, o.State));

        await engine.StopAsync();
        Assert.Equal(AudioEngineState.Idle, engine.State);
        Assert.All(factory.Outputs, o => Assert.True(o.Disposed));
        Assert.Contains(AudioEngineState.Preparing, states);
        Assert.Contains(AudioEngineState.Stopping, states);
    }

    [Fact]
    public async Task DisconnectAndReconnect_TransitionsPartialThenRecoversToPlaying()
    {
        var enumerator = new FakeEnumerator();
        var factory = new FakeOutputFactory();
        await using var engine = new AudioEngine(enumerator, factory);

        using var source = new TestToneAudioSource(AudioTestMode.RhythmicChimePulse);
        var d1 = CreateDevice("dev-1", "Buds One");
        var d2 = CreateDevice("dev-2", "Buds Two");

        await engine.StartAsync(source, [d1, d2]);
        Assert.Equal(AudioEngineState.Playing, engine.State);

        // Disconnect dev-1
        enumerator.Emit("dev-1", DeviceChangeKind.StateChanged, AudioEndpointState.Unplugged);
        Assert.Equal(AudioEngineState.Partial, engine.State);
        Assert.Equal("One headphone disconnected.", engine.Status.UserMessage);

        var firstOutput = factory.Outputs.First(o => o.DeviceId == "dev-1");
        var secondOutput = factory.Outputs.First(o => o.DeviceId == "dev-2");
        Assert.True(firstOutput.Disposed);
        Assert.False(secondOutput.Disposed);

        // Reconnect dev-1
        enumerator.Emit("dev-1", DeviceChangeKind.StateChanged, AudioEndpointState.Active);
        Assert.Equal(AudioEngineState.Playing, engine.State);
        Assert.Equal(3, factory.Outputs.Count); // new recovered branch created
    }

    [Fact]
    public async Task MasterVolume_ScalesWrittenSamples()
    {
        var enumerator = new FakeEnumerator();
        var factory = new FakeOutputFactory();
        await using var engine = new AudioEngine(enumerator, factory);

        using var source = new ConstantSource(0.8f);
        var d1 = CreateDevice("dev-1", "Speakers");

        await engine.StartAsync(source, [d1], new AudioEngineConfiguration { MasterVolume = 0.5f, PrefillChunks = 1 });

        var output = factory.Outputs.Single();
        Assert.NotEmpty(output.WrittenSamples);
        Assert.All(output.WrittenSamples.Take(100), s => Assert.InRange(s, 0.399f, 0.401f));
    }

    [Fact]
    public async Task FiniteSource_CompletesAndRaisesSourceCompleted()
    {
        var enumerator = new FakeEnumerator();
        var factory = new FakeOutputFactory();
        await using var engine = new AudioEngine(enumerator, factory);

        var completedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        engine.SourceCompleted += (_, _) => completedTcs.TrySetResult();

        using var source = new FiniteConstantSource(totalSamples: 960 * 2); // 1 chunk (20 ms)
        var d1 = CreateDevice("dev-1", "Speakers");

        await engine.StartAsync(source, [d1], new AudioEngineConfiguration { PrefillChunks = 1 });

        var completed = await Task.WhenAny(completedTcs.Task, Task.Delay(3000));
        Assert.Same(completedTcs.Task, completed);
        Assert.Equal(AudioEngineState.Idle, engine.State);
    }

    [Fact]
    public void DelayLineSampleProvider_DelaysSignalByConfiguredMilliseconds()
    {
        var source = new ImpulseSampleProvider(sampleRate: 1000, channels: 1);
        var delay = new DelayLineSampleProvider(source, initialDelayMs: 10); // 10 samples at 1000 Hz

        var buffer = new float[20];
        var read = delay.Read(buffer);

        Assert.Equal(20, read);
        // First 10 samples should be 0 (priming delay), impulse arrives at index 10
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(0.0f, buffer[i]);
        }

        Assert.Equal(1.0f, buffer[10]);
    }

    private static AudioDevice CreateDevice(string id, string name) => new()
    {
        Id = id,
        Name = name,
        DeviceType = AudioDeviceType.Headphones,
        EndpointState = AudioEndpointState.Active,
        IsSelected = true
    };

    private sealed class FakeEnumerator : IAudioDeviceEnumerator
    {
        public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

        public IReadOnlyList<AudioDevice> GetRenderEndpoints(bool includeInactive = false) => [];
        public AudioDevice? GetDefaultRenderEndpoint() => null;

        public void Emit(string id, DeviceChangeKind kind, AudioEndpointState? state = null) =>
            DeviceChanged?.Invoke(this, new AudioDeviceChangedEventArgs(id, kind, state));

        public void Dispose()
        {
        }
    }

    private sealed class FakeOutputFactory : IAudioOutputFactory
    {
        public List<FakeAudioOutput> Outputs { get; } = [];

        public IAudioOutput CreateOutput(AudioDevice device, AudioPcmFormat sourceFormat, AudioEngineConfiguration configuration)
        {
            var output = new FakeAudioOutput(device.Id, device.Name);
            Outputs.Add(output);
            return output;
        }
    }

    private sealed class FakeAudioOutput : IAudioOutput
    {
        public FakeAudioOutput(string id, string name)
        {
            DeviceId = id;
            DeviceName = name;
        }

        public string DeviceId { get; }
        public string DeviceName { get; }
        public AudioOutputState State { get; private set; } = AudioOutputState.Stopped;
        public string? FormatSummary => "48000 Hz, 2ch, 32-bit";
        public int ConfiguredDelayMs { get; private set; }
        public int EstimatedLatencyMs => 40;
        public float Volume { get; private set; } = 1.0f;
        public bool IsFaulted { get; private set; }
        public bool Disposed { get; private set; }
        public List<float> WrittenSamples { get; } = [];

        public event EventHandler<string?>? Faulted;

        public void Start() => State = AudioOutputState.Playing;
        public void Pause() => State = AudioOutputState.Paused;
        public void Resume() => State = AudioOutputState.Playing;
        public void Stop() => State = AudioOutputState.Stopped;
        public void SetDelayCompensationMs(int delayMs) => ConfiguredDelayMs = delayMs;
        public void SetVolume(float volume) => Volume = Math.Clamp(volume, 0.0f, 1.0f);

        public void WriteFloatSamples(ReadOnlySpan<float> samples, AudioPcmFormat sourceFormat)
        {
            if (WrittenSamples.Count < 4096)
            {
                WrittenSamples.AddRange(samples.ToArray());
            }
        }

        public AudioOutputSnapshot GetSnapshot() =>
            new(DeviceId, DeviceName, State, FormatSummary, ConfiguredDelayMs, EstimatedLatencyMs);

        public void Dispose()
        {
            Disposed = true;
            _ = Faulted;
            _ = IsFaulted;
        }
    }

    private sealed class ConstantSource(float value) : IAudioSource
    {
        public string DisplayName => "Constant";
        public AudioPcmFormat Format => new(48000, 2);
        public bool CanPause => true;
        public bool CanSeek => false;
        public TimeSpan? Duration => null;
        public TimeSpan Position => TimeSpan.Zero;
        public bool IsEnded => false;
        public event EventHandler? EndOfStream;
        public event EventHandler<string>? SourceFaulted;

        public int ReadSamples(Span<float> destination)
        {
            destination.Fill(value);
            return destination.Length;
        }

        public void Seek(TimeSpan position) { }
        public void Dispose() { _ = EndOfStream; _ = SourceFaulted; }
    }

    private sealed class FiniteConstantSource(int totalSamples) : IAudioSource
    {
        private int _remaining = totalSamples;
        public string DisplayName => "Finite";
        public AudioPcmFormat Format => new(48000, 2);
        public bool CanPause => true;
        public bool CanSeek => false;
        public TimeSpan? Duration => TimeSpan.FromMilliseconds(20);
        public TimeSpan Position => TimeSpan.Zero;
        public bool IsEnded => _remaining <= 0;
        public event EventHandler? EndOfStream;
        public event EventHandler<string>? SourceFaulted;

        public int ReadSamples(Span<float> destination)
        {
            if (_remaining <= 0)
            {
                return 0;
            }

            var toWrite = Math.Min(_remaining, destination.Length);
            destination[..toWrite].Fill(0.25f);
            _remaining -= toWrite;
            return toWrite;
        }

        public void Seek(TimeSpan position) { }
        public void Dispose() { _ = EndOfStream; _ = SourceFaulted; }
    }

    private sealed class ImpulseSampleProvider(int sampleRate, int channels) : ISampleProvider
    {
        private bool _sent;
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);

        public int Read(float[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public int Read(Span<float> buffer)
        {
            buffer.Clear();
            if (!_sent && !buffer.IsEmpty)
            {
                buffer[0] = 1.0f;
                _sent = true;
            }

            return buffer.Length;
        }
    }
}
