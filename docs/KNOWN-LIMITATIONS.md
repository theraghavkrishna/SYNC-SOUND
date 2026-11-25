# SYNC SOUND — Known Limitations

1. **Bluetooth Synchronization Variance:** Bluetooth codecs (SBC, AAC, aptX), receiver jitter buffers, and firmware delays differ across headphones and cannot be locked to sample-accurate zero latency by Windows software alone. Use per-device delay compensation in **Settings → Diagnostics** if wired speakers play ahead of Bluetooth earbuds.
2. **Bluetooth Multipoint / Radio Bandwidth:** Some Windows laptop Bluetooth adapters degrade audio quality or drop a stream when 3+ concurrent A2DP stereo headsets are connected simultaneously. Always select the stereo `Headphones` endpoint rather than `Hands-Free (HFP)`.
3. **System Audio Source Exclusion:** During system audio sharing, the Windows default playback device continues playing original system audio directly and is excluded from loopback fan-out to prevent echo/feedback loops. At least one additional output device is required to share system audio.
4. **Unsigned Windows SmartScreen Prompt:** Unsigned builds downloaded from GitHub may display Windows Defender SmartScreen (`More info → Run anyway`) until signed with an Authenticode certificate.
