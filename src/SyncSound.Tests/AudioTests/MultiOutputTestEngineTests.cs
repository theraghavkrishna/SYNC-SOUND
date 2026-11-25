using NAudio.Wave;
using SyncSound.Audio.Output;
using SyncSound.Audio.Playback;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Tests.AudioTests;

public sealed class MultiOutputTestEngineTests
{
    [Fact]
    public async Task StartAsync_WithNoDevices_ReturnsFriendlyPromptAndDoesNotPlay()
    {
        var enumerator = new FakeDeviceEnumerator();
        var factory = new FakeOutputBranchFactory();
        await using var engine = new MultiOutputTestEngine(enumerator, factory);

        var result = await engine.StartAsync([], AudioTestMode.ContinuousTone440Hz);

        Assert.False(result.AnyStarted);
        Assert.False(engine.IsPlaying);
        Assert.Equal("Choose a device to begin.", result.SummaryMessage);
    }

    [Fact]
    public async Task StartAsync_DeduplicatesDevicesAndStartsMultipleBranches()
    {
        var enumerator = new FakeDeviceEnumerator();
        var factory = new FakeOutputBranchFactory();
        await using var engine = new MultiOutputTestEngine(enumerator, factory);

        var d1 = CreateDevice("id-1", "Buds A");
        var d2 = CreateDevice("id-2", "Buds B");

        var result = await engine.StartAsync([d1, d1, d2], AudioTestMode.RhythmicChimePulse);

        Assert.True(result.AnyStarted);
        Assert.True(engine.IsPlaying);
        Assert.Equal(2, result.BranchStatuses.Count);
        Assert.Equal(2, factory.CreatedBranches.Count);
        Assert.All(factory.CreatedBranches, b => Assert.True(b.Started));

        await engine.StopAsync();
        Assert.False(engine.IsPlaying);
        Assert.All(factory.CreatedBranches, b => Assert.True(b.Disposed));
    }

    [Fact]
    public async Task DisconnectEvent_RemovesDisconnectedBranchAndKeepsHealthyBranchPlaying()
    {
        var enumerator = new FakeDeviceEnumerator();
        var factory = new FakeOutputBranchFactory();
        await using var engine = new MultiOutputTestEngine(enumerator, factory);

        var d1 = CreateDevice("id-1", "Buds A");
        var d2 = CreateDevice("id-2", "Buds B");

        await engine.StartAsync([d1, d2], AudioTestMode.ContinuousTone440Hz);
        Assert.True(engine.IsPlaying);

        // Simulate Buds A disconnecting while playing
        enumerator.EmitChange("id-1", DeviceChangeKind.StateChanged, AudioEndpointState.Unplugged);

        // Give async branch teardown a moment if needed, though HandleBranchFailure is synchronous
        var branchA = factory.CreatedBranches.Single(b => b.DeviceId == "id-1");
        var branchB = factory.CreatedBranches.Single(b => b.DeviceId == "id-2");

        Assert.True(branchA.Disposed);
        Assert.False(branchB.Disposed);
        Assert.True(engine.IsPlaying);

        var statusA = engine.ActiveBranches.Single(s => s.DeviceId == "id-1");
        var statusB = engine.ActiveBranches.Single(s => s.DeviceId == "id-2");
        Assert.Equal(OutputBranchState.Disconnected, statusA.State);
        Assert.Equal(OutputBranchState.Playing, statusB.State);
    }

    [Fact]
    public async Task PartialInitializationFailure_ContinuesWithHealthyDevices()
    {
        var enumerator = new FakeDeviceEnumerator();
        var factory = new FakeOutputBranchFactory { FailDeviceId = "bad-id" };
        await using var engine = new MultiOutputTestEngine(enumerator, factory);

        var bad = CreateDevice("bad-id", "Broken Endpoint");
        var good = CreateDevice("good-id", "Healthy Endpoint");

        var result = await engine.StartAsync([bad, good], AudioTestMode.ContinuousTone440Hz);

        Assert.True(result.AnyStarted);
        Assert.True(engine.IsPlaying);
        Assert.Equal(OutputBranchState.Failed, result.BranchStatuses.Single(s => s.DeviceId == "bad-id").State);
        Assert.Equal(OutputBranchState.Playing, result.BranchStatuses.Single(s => s.DeviceId == "good-id").State);
    }

    private static AudioDevice CreateDevice(string id, string name) => new()
    {
        Id = id,
        Name = name,
        DeviceType = AudioDeviceType.Headphones,
        EndpointState = AudioEndpointState.Active,
        IsSelected = true
    };

    private sealed class FakeDeviceEnumerator : IAudioDeviceEnumerator
    {
        public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

        public IReadOnlyList<AudioDevice> GetRenderEndpoints(bool includeInactive = false) => [];
        public AudioDevice? GetDefaultRenderEndpoint() => null;

        public void EmitChange(string deviceId, DeviceChangeKind kind, AudioEndpointState? state = null) =>
            DeviceChanged?.Invoke(this, new AudioDeviceChangedEventArgs(deviceId, kind, state));

        public void Dispose()
        {
        }
    }

    private sealed class FakeOutputBranchFactory : IOutputBranchFactory
    {
        public string? FailDeviceId { get; init; }
        public List<FakeOutputBranch> CreatedBranches { get; } = [];

        public IOutputBranch CreateBranch(AudioDevice device, WaveFormat sourceFormat)
        {
            if (string.Equals(device.Id, FailDeviceId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Simulated endpoint init failure.");
            }

            var branch = new FakeOutputBranch(device.Id, device.Name);
            CreatedBranches.Add(branch);
            return branch;
        }
    }

    private sealed class FakeOutputBranch : IOutputBranch
    {
        public FakeOutputBranch(string deviceId, string deviceName)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
        }

        public string DeviceId { get; }
        public string DeviceName { get; }
        public string FormatSummary => "48000 Hz, 2ch, 32-bit";
        public bool IsFaulted { get; private set; }
        public bool Started { get; private set; }
        public bool Disposed { get; private set; }

        public event EventHandler<string?>? Faulted;

        public void Start() => Started = true;
        public void EnqueueFloatSamples(byte[] buffer, int byteCount) { }
        public void Stop() => Started = false;

        public void TriggerFault(string? reason)
        {
            IsFaulted = true;
            Faulted?.Invoke(this, reason);
        }

        public void Dispose() => Disposed = true;
    }
}
