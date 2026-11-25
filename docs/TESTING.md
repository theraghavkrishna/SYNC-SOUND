# SYNC SOUND — Testing Guide

## Automated Testing

Run all unit and integration tests from the repository root:

```powershell
dotnet test
```

## Phase 0 Manual Hardware Verification Checklist

Because audio playback on physical Bluetooth and wired hardware cannot be verified by software compilation alone, Phase 0 requires physical listening tests on this Windows machine:

1. **Single-Device Tone Test**
   - Connect one output (e.g., `Speakers (Realtek(R) Audio)` or `OnePlus Nord Buds 3 Pro`).
   - Select it in the Phase 0 utility and click **Play Test Tone**.
   - Verify clear, glitch-free audio.
2. **Dual-Device Simultaneous Test**
   - Connect two outputs (e.g., `OnePlus Nord Buds 3 Pro` + `HP Tone`, or Laptop Speakers + 1 Bluetooth earbud).
   - Select both and click **Test Multi-Output**.
   - Confirm audio plays from **both** devices at the same time.
   - Also test the rhythmic/pulse audio option (if enabled) or speech/tone to observe synchronization offset.
3. **Triple / Quad Device Test (if hardware available)**
   - Add a 3rd and 4th output and verify whether Windows/Bluetooth sustains concurrent streams.
4. **Mid-Playback Disconnect & Recovery Test**
   - While multi-output playback is active, power off or disconnect one Bluetooth device.
   - Confirm the app logs/displays the disconnect event, does **not** crash, and keeps playing on the remaining healthy device(s).
   - Reconnect the device, click **Refresh**, and verify playback can start again cleanly.

Record actual results in `docs/FEASIBILITY-REPORT.md`.
