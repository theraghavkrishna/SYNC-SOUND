using SyncSound.Audio.Services;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Settings;

namespace SyncSound.Tests.DeviceTests;

public sealed class DeviceManagerServiceTests
{
    [Fact]
    public async Task SelectionPersistsAcrossReinitialization_AndSelectAllSkipsHandsFree()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"syncsound-settings-{Guid.NewGuid():N}.json");
        try
        {
            var enumerator = new FakeEnumerator(
                new AudioDevice { Id = "spk", Name = "Speakers", DeviceType = AudioDeviceType.Speakers, EndpointState = AudioEndpointState.Active, IsDefault = true },
                new AudioDevice { Id = "buds-a2dp", Name = "Nord Buds", DeviceType = AudioDeviceType.Headphones, EndpointState = AudioEndpointState.Active },
                new AudioDevice { Id = "buds-hfp", Name = "Nord Buds Hands-Free", DeviceType = AudioDeviceType.HeadsetHandsFree, EndpointState = AudioEndpointState.Active }
            );

            var settings1 = new JsonSettingsService(tempPath);
            using var manager1 = new DeviceManagerService(enumerator, settings1);
            await manager1.InitializeAsync();

            // Hands-Free (HFP) endpoints are filtered out so stereo A2DP is never disrupted
            Assert.Equal(2, manager1.Devices.Count);
            Assert.DoesNotContain(manager1.Devices, d => d.Id == "buds-hfp");

            // Test individual toggle (and verify shared-model mutation never skips persistence)
            var buds = manager1.Devices.Single(d => d.Id == "buds-a2dp");
            manager1.SetDeviceSelected(buds.Id, true);
            Assert.Single(manager1.SelectedDevices);

            manager1.SelectAllEligible();

            Assert.Equal(2, manager1.SelectedDevices.Count);
            Assert.DoesNotContain(manager1.SelectedDevices, d => d.Id == "buds-hfp");

            // Wait briefly for fire-and-forget SaveAsync
            await Task.Delay(150);

            var settings2 = new JsonSettingsService(tempPath);
            using var manager2 = new DeviceManagerService(enumerator, settings2);
            await manager2.InitializeAsync();

            Assert.Equal(2, manager2.SelectedDevices.Count);
            Assert.Contains(manager2.SelectedDevices, d => d.Id == "buds-a2dp");
            Assert.Contains(manager2.SelectedDevices, d => d.Id == "spk");
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private sealed class FakeEnumerator(params AudioDevice[] devices) : IAudioDeviceEnumerator
    {
        public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

        public IReadOnlyList<AudioDevice> GetRenderEndpoints(bool includeInactive = false) =>
            devices.Select(d => new AudioDevice
            {
                Id = d.Id,
                Name = d.Name,
                DeviceType = d.DeviceType,
                EndpointState = d.EndpointState,
                IsDefault = d.IsDefault
            }).ToList();

        public AudioDevice? GetDefaultRenderEndpoint() => devices.FirstOrDefault(d => d.IsDefault);

        public void Dispose() { _ = DeviceChanged; }
    }
}
