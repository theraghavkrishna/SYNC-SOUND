# SYNC SOUND — Architecture

## 1. Solution Layout

```text
SyncSound.sln
├── src/SyncSound.Core       # Domain models, interfaces, state machine, settings contracts
├── src/SyncSound.Audio      # Audio engine, WASAPI capture/output, resampling, buffering, sync
├── src/SyncSound.Windows    # Windows Core Audio endpoint discovery, hotkeys, tray, startup
├── src/SyncSound.App        # WPF presentation layer (Views, ViewModels, Themes, Services)
└── src/SyncSound.Tests      # xUnit unit & integration tests
```

## 2. Dependency Boundaries

- `SyncSound.Core`: Zero UI or OS-specific dependencies. Defines `AudioDevice`, `SharingState`, `PlaybackState`, `AppSettings`, and core service interfaces.
- `SyncSound.Audio`: Depends on `SyncSound.Core` and `NAudio`. Encapsulates WASAPI loopback capture, multi-output fan-out, format negotiation, resampling, per-output buffering, and failure isolation.
- `SyncSound.Windows`: Depends on `SyncSound.Core` (and NAudio/Win32 APIs where required). Implements `MMDeviceEnumerator` / `IMMNotificationClient`, `RegisterHotKey`, system tray, and startup registration.
- `SyncSound.App`: Depends on `SyncSound.Core`, `SyncSound.Audio`, and `SyncSound.Windows` via dependency injection. Strictly MVVM; **never runs audio processing on the UI thread**.

## 3. Audio Data Paths

### Path A: System Audio Sharing (Phase 5)
```text
Windows Mix ─► Default Render Endpoint (continues playing normally)
                     │
             WASAPI Loopback Capture
                     │
             Shared Audio Pipeline
                     ├──► Output 2 (Independent WASAPI Renderer)
                     ├──► Output 3 (Independent WASAPI Renderer)
                     └──► Output N (Independent WASAPI Renderer)
```

### Path B: Local Music Playback (Phase 4)
```text
Local File ─► Decoder ─► Shared Audio Pipeline
                              ├──► Output 1
                              ├──► Output 2
                              └──► Output N
```

## 4. Phase 0 Architecture Notes

Phase 0 uses a minimal WPF diagnostic window inside `SyncSound.App` backed by real endpoint enumeration and real multi-output tone/audio playback in `SyncSound.Audio` / `SyncSound.Windows`. No final UI styling or fake state is introduced in Phase 0.
