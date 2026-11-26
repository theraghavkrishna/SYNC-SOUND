# SYNC SOUND

**One sound. Everyone listening.**

Share your Windows PC's audio across multiple supported headphones simultaneously — Bluetooth TWS earbuds, 3.5 mm wired headsets, and USB audio together. Choose who's listening. Press play.

---

## Product Architecture (`v4.0`)

- **`src/`** — Native Windows Desktop Application (`C# / .NET 10 / WPF / MVVM / NAudio`)
  - `SyncSound.Core` — Domain models, `AudioDevice`, `AudioTransportKind`, `AppSettings`, notification & reduced-motion preferences
  - `SyncSound.Audio` — Multi-output WASAPI loopback capture & fan-out (`AudioEngine` with live RMS signal metering, `WasapiLoopbackAudioSource`, drift-protected `WasapiAudioOutput`, `MultiOutputTestEngine`, `MusicPlayerService`)
  - `SyncSound.Windows` — Hardware-metadata endpoint discovery (`PKEY_AudioEndpoint_FormFactor`, `EnumeratorName`, `BusInfSection`), PnP Bluetooth battery queries (`BluetoothBatteryReader`), JSON persistence, Win32 global hotkeys, tray icon, single-instance guard
  - `SyncSound.App` — Razor-sharp Per-Monitor DPI v2 vector UI (`Listen` theatre with 10-layer reactive `SyncOrb`, `Devices` Connection Atelier, 6-category `Preferences` + floating glass `QuickSidebarWindow` + tactile `SoundFeedbackService`) in Warm Cream (`#F4EFE6`), Carbon Black (`#191817`), and Burnt Orange (`#C65D32`)
  - `SyncSound.Tests` — Automated xUnit tests (21/21 passing)
- **`website/`** — Next.js landing page sharing the exact SYNC SOUND v4 visual design system (`Root Directory: website`)

---

## Key Features

- **Connect · Choose · Listen:** Discover Bluetooth A2DP, 3.5 mm Realtek jack headphones, and USB headsets in Windows, pick your listening group, and share audio simultaneously with zero buffer underruns.
- **Stateful Sync Orb & Listening Theatre:** 10-layer crystal Sync Orb with connected device constellation nodes and real-time audio reactivity, paired with multi-output **Test Sound** verification and an optional collapsible Music Dock.
- **Tactile Master & Per-Device Control:** Precision **Sync Dial** with keyboard & haptic detent feedback, individual per-headphone volume trim & mute, and real Bluetooth battery indicators where exposed by Windows.
- **Quick Sidebar (`Ctrl + Shift + S`):** Floating glass control instrument with live mini `SyncOrb`, expandable inline device controls, interruptible motion, and multi-monitor work-area positioning.
- **Fault Isolation & Auto-Recovery:** If a headphone disconnects out of range, remaining headphones play uninterrupted and auto-recover when back in range.

---

## Build, Test & Publish

### Desktop App (Windows 10/11, .NET 10 SDK)

```powershell
dotnet restore SyncSound.sln
dotnet build SyncSound.sln -c Release
dotnet test SyncSound.sln -c Release
dotnet run --project src/SyncSound.App/SyncSound.App.csproj
```

Publish self-contained single-file x64 release:

```powershell
dotnet publish src/SyncSound.App/SyncSound.App.csproj -c Release -r win-x64 --self-contained true -o publish/app
```

### Landing Page (`website/`)

```powershell
cd website
npm install
npm run build
```

Deploy to **Vercel** from `theraghavkrishna/SYNC-SOUND` with **Root Directory** set to `website`.
