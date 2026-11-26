using SyncSound.Audio.Services;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Settings;

namespace SyncSound.Tests.CoreTests;

public sealed class SystemSharingServiceTests
{
    [Fact]
    public async Task StartSharing_WhenOnlyDefaultDeviceSelected_PreventsFeedbackLoopAndGuidesUser()
    {
        var defaultDevice = new AudioDevice
        {
            Id = "default-1",
            Name = "Laptop Speakers",
            DeviceType = AudioDeviceType.Speakers,
            EndpointState = AudioEndpointState.Active,
            IsDefault = true,
            IsSelected = true
        };

        var enumerator = new FakeEnumerator(defaultDevice);
        var settings = new JsonSettingsService(Path.Combine(Path.GetTempPath(), $"ss-test-{Guid.NewGuid():N}.json"));
        using var deviceManager = new DeviceManagerService(enumerator, settings);
        await deviceManager.InitializeAsync();
        deviceManager.SetDeviceSelected("default-1", true);

        await using var engine = new FakeAudioEngine();
        await using var sharing = new SystemSharingService(engine, deviceManager, enumerator, settings);

        var result = await sharing.StartSharingAsync();

        Assert.False(result.IsSharing);
        Assert.Contains("Select at least one other headphone", result.UserMessage);
        Assert.False(engine.WasStarted);
    }

    private sealed class FakeEnumerator(AudioDevice defaultDevice) : IAudioDeviceEnumerator
    {
        public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;
        public IReadOnlyList<AudioDevice> GetRenderEndpoints(bool includeInactive = false) => [defaultDevice];
        public AudioDevice? GetDefaultRenderEndpoint() => defaultDevice;
        public void Dispose() { _ = DeviceChanged; }
    }

    private sealed class FakeAudioEngine : IAudioEngine
    {
        public bool WasStarted { get; private set; }
        public AudioEngineState State => AudioEngineState.Idle;
        public AudioEngineStatusSnapshot Status => new(AudioEngineState.Idle, string.Empty, [], "Ready to listen.");
        public float MasterVolume => 1.0f;
        public float CurrentSignalLevel => 0f;

        public event EventHandler<AudioEngineStatusSnapshot>? StatusChanged;
        public event EventHandler? SourceCompleted;

        public Task<AudioEngineStatusSnapshot> StartAsync(IAudioSource source, IReadOnlyList<AudioDevice> targetDevices, AudioEngineConfiguration? configuration = null, CancellationToken cancellationToken = default)
        {
            WasStarted = true;
            return Task.FromResult(Status);
        }

        public Task PauseAsync() => Task.CompletedTask;
        public Task ResumeAsync() => Task.CompletedTask;
        public Task StopAsync() => Task.CompletedTask;
        public void SetMasterVolume(float volume) { }
        public void SetOutputDelayMs(string deviceId, int delayMs) { }
        public void SetOutputVolume(string deviceId, float volume) { }
        public ValueTask DisposeAsync()
        {
            _ = StatusChanged;
            _ = SourceCompleted;
            return ValueTask.CompletedTask;
        }
    }
}
