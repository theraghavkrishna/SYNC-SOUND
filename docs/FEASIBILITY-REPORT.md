# SYNC SOUND — Phase 0 Audio Feasibility Report

**Date:** 2026-11-25  
**Phase:** Phase 0 — Project Foundation & Technical Feasibility  
**Status:** Solution built (Debug & Release, 0 warnings/0 errors) and 7/7 automated tests passed; awaiting physical multi-headphone listening verification.

---

## 1. Environment Audited

- **OS:** Windows 11 26H2 (Build `26300.9818`)
- **Runtime / SDK:** .NET 10 SDK `10.0.404` (installed to `C:\Users\chira\.dotnet`)
- **Audio Library:** `NAudio 3.3.0` (`WasapiPlayerBuilder` + `MMDeviceNotificationClient`)
- **Git:** `2.55.0.windows.5`
- **Audio Hardware / Endpoints Observed on Machine:**
  - `Speakers (Realtek(R) Audio)` — Active
  - `Headphones (OnePlus Nord Buds 3 Pro)` — Bluetooth A2DP Stereo (Active during audit)
  - `Headset (OnePlus Nord Buds 3 Pro Hands-Free)` — Bluetooth HFP (Active during audit)
  - `Headphones (HP Tone)` & `Headset (HP Tone Hands-Free)` — Paired Bluetooth
  - `Headphones (ZEB-ZEST 10)` — Paired Bluetooth

---

## 2. Automated Build & Test Verification (Completed)

- `dotnet build SyncSound.sln -c Debug --no-restore`: **Succeeded (0 warnings, 0 errors)**
- `dotnet build SyncSound.sln -c Release --no-restore`: **Succeeded (0 warnings, 0 errors)**
- `dotnet test SyncSound.sln -c Debug --no-build`: **7 passed, 0 failed**
  - `TestSignalSampleProviderTests` (continuous 440 Hz tone + 120 BPM rhythmic chime pulse)
  - `MultiOutputTestEngineTests` (empty selection validation, deduplication + multi-branch start/stop, partial init failure isolation, mid-playback disconnect isolation keeping healthy outputs active)
  - `WindowsAudioDeviceEnumeratorTests` (real Windows Core Audio endpoint enumeration without duplicates or exceptions)
- **Live 2-Output WASAPI Hardware Probe (2026-11-25 18:35 IST):**
  - Enumerated 2 active endpoints: `Headphones (OnePlus Nord Buds 3 Pro)` (Default, `48000 Hz, 2ch, 32-bit`) and `Speakers (2- Realtek(R) Audio)` (`48000 Hz, 2ch, 32-bit`).
  - Both `WasapiPlayer` branches initialized cleanly, streamed `RhythmicChimePulse` simultaneously for 2 seconds with `HealthyBranches = 2`, and stopped cleanly (`IsPlaying = False`).
  - *Note:* Only 1 Bluetooth headphone pair (`OnePlus Nord Buds 3 Pro`) was connected at the time of this probe; 2+ simultaneous Bluetooth pairs (`HP Tone`, `ZEB-ZEST 10`) still require turning on those earbuds and physically verifying audio + sync.

---

## 3. Recommended Engine Architecture (Phase 0 Findings)

1. **Per-Output Independent Buffers & Renderers:**  
   Each selected WASAPI endpoint can run at a different mix format (e.g., 48 kHz 32-bit float stereo on Realtek vs 44.1 kHz / 48 kHz on Bluetooth A2DP vs 16 kHz mono on HFP). Driving each output with its own `WasapiOut` instance fed by an isolated ring/buffered provider with per-output resampling avoids blocking healthy devices when one Bluetooth device stalls or disconnects.
2. **A2DP vs Hands-Free (HFP) Awareness:**  
   Bluetooth devices appear as two Windows endpoints (`Headphones` for A2DP stereo and `Headset Hands-Free` for HFP). Phase 0 surfaces endpoint role/type clearly so users test stereo A2DP endpoints rather than degrading Bluetooth bandwidth with HFP.
3. **Fault Isolation on Disconnect:**  
   By wrapping each output branch independently and subscribing to Core Audio `IMMNotificationClient` events, disconnecting one device stops and disposes only that output branch while remaining outputs continue playing.

---

## 4. Physical Hardware Testing Log (To Be Verified by User)

Run the Phase 0 utility (`dotnet run --project src/SyncSound.App/SyncSound.App.csproj`) and record your observations below:

| Test Case | Devices Tested | Audio Mode | Result (Pass/Fail) | Audible Sync / Quality Notes |
| :--- | :--- | :--- | :--- | :--- |
| 1. Single device | e.g. Realtek Speakers or OnePlus Nord Buds 3 Pro | Tone / Pulse | *Pending manual test* | |
| 2. Two outputs (BT + Speakers) | | Tone / Pulse | *Pending manual test* | |
| 3. Two Bluetooth outputs | | Tone / Pulse | *Pending manual test* | |
| 4. Three outputs (if available) | | Tone / Pulse | *Pending manual test* | |
| 5. Four outputs (if available) | | Tone / Pulse | *Pending manual test* | |
| 6. Disconnect during playback | | Multi-output | *Pending manual test* | |
| 7. Reconnect + Refresh + Replay | | Multi-output | *Pending manual test* | |

---

## 5. Technical Limitations & Decision Gate

- **Pending:** Physical verification of simultaneous A2DP streaming stability and perceived latency skew across your specific Bluetooth earbuds before committing to Phase 1.
