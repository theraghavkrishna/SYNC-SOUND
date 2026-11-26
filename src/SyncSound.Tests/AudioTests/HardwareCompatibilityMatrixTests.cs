using SyncSound.Audio.Engine;
using SyncSound.Audio.Services;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;
using SyncSound.Windows.Settings;

namespace SyncSound.Tests.AudioTests;

/// <summary>
/// Covers the full wired / USB / Bluetooth compatibility matrix (Configs A-F).
/// </summary>
public sealed class HardwareCompatibilityMatrixTests
{
    [Fact]
    public async Task Matrix_WiredRealtekPlusBluetoothTws_AndUsbPlusBluetooth_BothSelectAndPlayTogether()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"syncsound-matrix-{Guid.NewGuid():N}.json");
        try
        {
            var realtek = new AudioDevice
            {
                Id = "realtek-jack",
                Name = "Speakers (2- Realtek(R) Audio)",
                DeviceType = AudioDeviceType.Other,
                TransportKind = AudioTransportKind.BuiltInOrWiredJack,
                EndpointState = AudioEndpointState.Active,
                IsDefault = true
            };
            var usbHeadset = new AudioDevice
            {
                Id = "usb-headset",
                Name = "Headset (HyperX USB Audio)",
                DeviceType = AudioDeviceType.Headphones,
                TransportKind = AudioTransportKind.UsbAudio,
                EndpointState = AudioEndpointState.Active
            };
            var tws1 = new AudioDevice
            {
                Id = "bt-tws-1",
                Name = "Headphones (OnePlus Nord Buds 3 Pro)",
                DeviceType = AudioDeviceType.Headphones,
                TransportKind = AudioTransportKind.BluetoothA2dp,
                EndpointState = AudioEndpointState.Active,
                BatteryPercentage = 85
            };
            var tws2 = new AudioDevice
            {
                Id = "bt-tws-2",
                Name = "Headphones (Noise Air Buds Pro 6)",
                DeviceType = AudioDeviceType.Headphones,
                TransportKind = AudioTransportKind.BluetoothA2dp,
                EndpointState = AudioEndpointState.Active
            };
            var btHfp = new AudioDevice
            {
                Id = "bt-hfp",
                Name = "Headset (OnePlus Nord Buds 3 Pro Hands-Free)",
                DeviceType = AudioDeviceType.HeadsetHandsFree,
                TransportKind = AudioTransportKind.BluetoothHandsFree,
                EndpointState = AudioEndpointState.Active
            };

            // Verify friendly DisplayName never falsely labels Realtek as "Laptop Speakers"
            Assert.Equal("Realtek Audio (Wired / Speakers)", realtek.DisplayName);
            Assert.Equal("OnePlus Nord Buds 3 Pro", tws1.DisplayName);
            Assert.Equal("HyperX USB Audio", usbHeadset.DisplayName);

            var enumerator = new MatrixEnumerator(realtek, usbHeadset, tws1, tws2, btHfp);
            var settings = new JsonSettingsService(tempPath);
            using var devMgr = new DeviceManagerService(enumerator, settings);
            await devMgr.InitializeAsync();

            // USB Headset MUST NOT be hidden, while Bluetooth HFP MUST be excluded
            Assert.Contains(devMgr.Devices, d => d.Id == usbHeadset.Id);
            Assert.Contains(devMgr.Devices, d => d.Id == realtek.Id);
            Assert.Contains(devMgr.Devices, d => d.Id == tws1.Id);
            Assert.Contains(devMgr.Devices, d => d.Id == tws2.Id);
            Assert.DoesNotContain(devMgr.Devices, d => d.Id == btHfp.Id);

            // Config C: Wired Realtek + Bluetooth TWS selected simultaneously
            devMgr.SetDeviceSelected(realtek.Id, true);
            devMgr.SetDeviceSelected(tws1.Id, true);
            Assert.Equal(2, devMgr.SelectedDevices.Count);

            // Config D & F: Add USB headset + second Bluetooth TWS -> all 4 active outputs together
            devMgr.SetDeviceSelected(usbHeadset.Id, true);
            devMgr.SetDeviceSelected(tws2.Id, true);
            Assert.Equal(4, devMgr.SelectedDevices.Count);

            // Verify AudioEngine drives all non-faulted outputs on shared timeline with independent volume
            var factory = new MatrixOutputFactory();
            await using var engine = new AudioEngine(enumerator, factory);
            using var source = new SineWaveSource();

            var status = await engine.StartAsync(
                source,
                [realtek, tws1, usbHeadset, tws2],
                new AudioEngineConfiguration { ChunkMs = 10, PrefillChunks = 4 });

            Assert.Equal(AudioEngineState.Playing, status.State);
            Assert.Equal(4, status.Outputs.Count);
            Assert.All(status.Outputs, o => Assert.Equal(AudioOutputState.Playing, o.State));

            // Independent per-device volume control
            engine.SetOutputVolume(tws1.Id, 0.35f);
            engine.SetOutputVolume(realtek.Id, 0.90f);
            await Task.Delay(120);

            Assert.All(factory.Outputs, o => Assert.True(o.SamplesWritten > 0));

            // Simulate Wired or Bluetooth disconnect while others keep playing
            enumerator.RaiseChanged(tws1.Id, DeviceChangeKind.Removed);
            await Task.Delay(100);
            Assert.Equal(AudioEngineState.Partial, engine.State);
            Assert.Equal(3, engine.Status.Outputs.Count(o => o.State == AudioOutputState.Playing));

            // Reconnect recovery
            enumerator.RaiseChanged(tws1.Id, DeviceChangeKind.Added, AudioEndpointState.Active);
            await Task.Delay(100);
            Assert.Equal(AudioEngineState.Playing, engine.State);
            Assert.Equal(4, engine.Status.Outputs.Count(o => o.State == AudioOutputState.Playing));

            await engine.StopAsync();
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private sealed class MatrixEnumerator(params AudioDevice[] devices) : IAudioDeviceEnumerator
    {
        public event EventHandler<AudioDeviceChangedEventArgs>? DeviceChanged;

        public IReadOnlyList<AudioDevice> GetRenderEndpoints(bool includeInactive = false) =>
            devices.Select(d => new AudioDevice
            {
                Id = d.Id,
                Name = d.Name,
                DeviceType = d.DeviceType,
                TransportKind = d.TransportKind,
                EndpointState = d.EndpointState,
                IsDefault = d.IsDefault,
                BatteryPercentage = d.BatteryPercentage
            }).ToList();

        public AudioDevice? GetDefaultRenderEndpoint() => devices.FirstOrDefault(d => d.IsDefault);

        public void RaiseChanged(string id, DeviceChangeKind kind, AudioEndpointState? state = null) =>
            DeviceChanged?.Invoke(this, new AudioDeviceChangedEventArgs(id, kind, state));

        public void Dispose() { _ = DeviceChanged; }
    }

    private sealed class MatrixOutputFactory : IAudioOutputFactory
    {
        public List<MatrixOutput> Outputs { get; } = [];

        public IAudioOutput CreateOutput(AudioDevice device, AudioPcmFormat sourceFormat, AudioEngineConfiguration configuration)
        {
            var output = new MatrixOutput(device.Id, device.Name);
            Outputs.Add(output);
            return output;
        }
    }

    private sealed class MatrixOutput(string deviceId, string deviceName) : IAudioOutput
    {
        public string DeviceId { get; } = deviceId;
        public string DeviceName { get; } = deviceName;
        public AudioOutputState State { get; private set; } = AudioOutputState.Stopped;
        public string? FormatSummary => "48000 Hz, 2ch, 32-bit";
        public int ConfiguredDelayMs { get; private set; }
        public int EstimatedLatencyMs => 40;
        public float Volume { get; private set; } = 1.0f;
        public bool IsFaulted => false;
        public long SamplesWritten { get; private set; }

        public event EventHandler<string?>? Faulted;

        public void Start() => State = AudioOutputState.Playing;
        public void Pause() => State = AudioOutputState.Paused;
        public void Resume() => State = AudioOutputState.Playing;
        public void Stop() => State = AudioOutputState.Stopped;
        public void SetDelayCompensationMs(int delayMs) => ConfiguredDelayMs = delayMs;
        public void SetVolume(float volume) => Volume = volume;

        public void WriteFloatSamples(ReadOnlySpan<float> samples, AudioPcmFormat sourceFormat)
        {
            SamplesWritten += samples.Length;
            _ = Faulted;
        }

        public AudioOutputSnapshot GetSnapshot() =>
            new(DeviceId, DeviceName, State, FormatSummary, ConfiguredDelayMs, EstimatedLatencyMs, 40, 0, null);

        public void Dispose() { }
    }

    private sealed class SineWaveSource : IAudioSource
    {
        public string DisplayName => "Sine Wave";
        public AudioPcmFormat Format => new(48000, 2);
        public bool CanPause => false;
        public bool CanSeek => false;
        public TimeSpan? Duration => null;
        public TimeSpan Position => TimeSpan.Zero;
        public bool IsEnded => false;
        public event EventHandler? EndOfStream;
        public event EventHandler<string>? SourceFaulted;

        public int ReadSamples(Span<float> destination)
        {
            destination.Fill(0.1f);
            _ = EndOfStream;
            _ = SourceFaulted;
            return destination.Length;
        }

        public void Seek(TimeSpan position) { }
        public void Dispose() { }
    }
}
