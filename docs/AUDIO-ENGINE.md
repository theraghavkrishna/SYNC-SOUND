# SYNC SOUND — Audio Engine Architecture (Phase 1)

## 1. Core Pipeline

```text
IAudioSource (48 kHz IEEE Float Stereo)
      │
      ▼  (20 ms non-blocking feeder loop, master volume applied in-place)
AudioEngine
      ├──► WasapiAudioOutput (Device 1)
      │        └── BufferedWaveProvider -> Channel Map -> WdlResampler -> DelayLine -> WasapiPlayer (MMCSS "Audio")
      ├──► WasapiAudioOutput (Device 2)
      │        └── BufferedWaveProvider -> Channel Map -> WdlResampler -> DelayLine -> WasapiPlayer (MMCSS "Audio")
      └──► WasapiAudioOutput (Device N)
               └── BufferedWaveProvider -> Channel Map -> WdlResampler -> DelayLine -> WasapiPlayer (MMCSS "Audio")
```

## 2. Design Principles

- **Off the UI Thread:** All PCM reads, scaling, resampling, delay buffering, and WASAPI rendering run on background audio threads (`MMCSS "Audio"` thread priority on `WasapiPlayer`).
- **Fault Isolation:** Each output branch owns an independent `BufferedWaveProvider` and `WasapiPlayer`. If one Bluetooth device drops, stalls, or fails to initialize, only that branch transitions to `Disconnected` / `Failed` (`AudioEngineState.Partial`) while healthy outputs keep playing.
- **Automatic Output Recovery:** When a previously active session device disappears (`Removed` / `Unplugged`) and later returns (`Added` / `Active`) while the session is still live, `AudioEngine` re-initializes that output branch and restores playback automatically.
- **Best-Effort Synchronization:** `DelayLineSampleProvider` provides a fixed-capacity, zero-allocation `0–500 ms` delay ring buffer per output, paired with `BestEffortSyncCoordinator` latency estimates. Because Bluetooth firmware and codecs vary across hardware, SYNC SOUND treats synchronization as best-effort rather than promising zero-skew hardware lock.
- **Clean Disposal:** Stopping or disposing cancels the feeder loop, unhooks `EndOfStream` / `SourceFaulted` / `Faulted` / `DeviceChanged`, stops `WasapiPlayer`, and disposes `MMDevice` and `MMDeviceEnumerator`.
