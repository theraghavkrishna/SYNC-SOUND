# SYNC SOUND — Product Requirements Document (v1.0)

**Tagline:** One sound. Everyone listening.  
**Platform:** Windows 11 (initial target)  
**Product Type:** Desktop utility  
**Distribution:** GitHub + Windows installer  
**Core Technology:** C# / .NET 10 / WPF / MVVM / WASAPI (NAudio)  
**Primary Experience:** Full app + Quick Sidebar

---

## 1. Executive Summary

SYNC SOUND enables a user to connect multiple audio devices to a Windows PC and play audio simultaneously through all selected devices.

Users should be able to:
1. Connect/pair headphones using Windows.
2. Open SYNC SOUND.
3. See their available audio devices.
4. Select the devices they want.
5. Start sharing.
6. Play anything on Windows.
7. Have everyone hear the audio.

The application also includes a built-in local music player, allowing users to test and enjoy direct multi-output playback. Technical complexity (audio routing, buffer management, synchronization, sample-rate conversion, device clocks, WASAPI) is intentionally hidden from the user.

---

## 2. Problem Statement

Windows users cannot easily play the same audio through several independently connected Bluetooth headphones simultaneously.

**The problem we solve:**  
> "I have several headphones connected. I want everyone to hear the same thing from my laptop. Why can't I just press one button?"

**The product answer:**  
> **Connect. Select everyone. Play.**

---

## 3. Product Vision & Design Philosophy

SYNC SOUND should feel like consumer software designed with Apple-caliber craft without copying Apple products:
- Complexity hidden, clarity visible
- Beautiful by default, no unnecessary controls, no audio-engine terminology
- One obvious action, delightful microinteractions
- Stable audio over visual gimmicks
- Fast access from anywhere

---

## 4. Core User Stories

- **Device management:** See connected headphones, select which headphones receive audio, remember preferred devices, handle disconnections automatically.
- **Audio sharing:** Play anything on Windows through several headphones simultaneously, stop sharing without disconnecting headphones, keep working if one headphone disconnects.
- **Music player:** Import a favorite song and test the multi-device system with a clean music player.
- **Sidebar:** Open SYNC SOUND via a keyboard shortcut without interrupting the current app, inspect connected devices, and start/stop sharing.

---

## 5. Functional Requirements

### FR-01: Device Discovery
- Enumerate real Windows audio output endpoints with friendly names, type/category when available, and connection/availability state.
- Refresh device list, detect device additions/removals live, avoid duplicates, and show a clear empty state.
- MVP device pairing uses Windows Settings; SYNC SOUND must never pretend to pair devices or display fake devices.

### FR-02: Device Selection
- Select/deselect single or multiple devices, show selection count, select all eligible devices, remove a device from an active session.

### FR-03: System Audio Sharing
- Capture Windows system audio via WASAPI loopback from the default render endpoint.
- Route captured audio to selected additional endpoints without causing duplicate playback or feedback on the source endpoint.
- Handle format conversion, endpoint changes, initialization failures, and recovery.

### FR-04: Multi-Output Playback
- Independent output streams sharing one source, with format negotiation, resampling where necessary, buffer management, per-output error handling, and branch recovery.

### FR-05: Synchronization
- Best-effort alignment via output latency estimation, buffer alignment, per-output delay compensation, drift management, and endpoint-change recovery.
- Must not falsely promise perfect synchronization across all Bluetooth hardware/codec combinations.

### FR-06: Local Music Player
- Open file, drag-and-drop, play, pause, stop, previous/next, seek, current time, duration, volume, track metadata, album art, queue, and multi-output playback.

### FR-07: Quick Sidebar
- Global hotkey (default `Ctrl + Shift + S`, configurable), toggle open/close, Escape to close, optional click-outside close, optional always-on-top, smooth slide animation, no unnecessary focus stealing.

### FR-08: System Tray
- Tray icon with open main window, toggle sidebar, start/stop sharing, quit, and background operation.

### FR-09: Settings
- Global shortcut, start with Windows, start minimized, close-to-tray, sidebar preferences, preferred/default devices, Diagnostics (hidden from main UI), About, Version, Open logs folder.

### FR-10: Device Recovery
- Detect vanished devices, update UI, continue healthy devices, avoid crashing, attempt reconnection when appropriate, restore when available, show simple user-facing messages.

---

## 6. UI, UX & Design System

- **Navigation:** `Home`, `Devices`, `Player`, `Settings` + connected count footer.
- **Color Tokens:** Warm Canvas (primary background), Pure Surface (cards/panels), Deep Ink (primary typography), Signal Coral (signature accent).
- **Typography & Shape:** Editorial display hero headings, clean sans-serif body, soft rounded corners, generous whitespace, purposeful subtle motion respecting reduced-motion preferences.

---

## 7. Non-Functional Requirements

- **NFR-01 Reliability:** Never crash on Bluetooth disconnect, endpoint changes, default output changes, capture failures, repeated sidebar toggles, or sleep/wake.
- **NFR-02 Performance:** Fast launch, no UI-thread audio processing, low idle CPU, bounded memory.
- **NFR-03 Privacy:** Offline-first, no account, no cloud, no telemetry, local settings/logs only (no personal audio content in logs).
- **NFR-04 Accessibility:** Keyboard navigation, screen-reader labels, contrast, status never conveyed by color alone.
- **NFR-05 Maintainability:** Modular MVVM, DI, interfaces around audio engine, structured logging (Serilog), automated tests (xUnit), no God classes.
- **NFR-06 Distribution:** Self-contained Windows x64 build, Inno Setup installer, portable ZIP, GitHub Releases.

---

## 8. Error-Handling Copy Matrix

| Internal Condition | User-Facing Message |
| :--- | :--- |
| No devices | Let's connect some headphones. |
| Output initialization failed | We couldn't start audio on one device. |
| Device disconnects | One headphone disconnected. |
| Reconnection attempt | Reconnecting… |
| Capture unavailable | We couldn't access your computer's audio. |
| No source | Choose a device to begin. |
| Audio engine crash/failure | Audio stopped unexpectedly. Try again. |
| Unsupported format | This audio format isn't supported. |
| Shortcut unavailable | This shortcut is already being used. |
