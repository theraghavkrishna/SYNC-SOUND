# SYNC SOUND

**One sound. Everyone listening.**

Share your Windows PC's audio across multiple supported headphones. Choose who's listening. Press play.

---

## Product Architecture

- **`src/`** — Native Windows Desktop Application (`C# / .NET 10 / WPF / MVVM / NAudio 3.3.0`)
  - `SyncSound.Core` — Domain models, `AudioDevice`, `AppSettings` (`AppThemeMode`, per-device & session volume), state interfaces
  - `SyncSound.Audio` — Multi-output WASAPI loopback capture & fan-out (`AudioEngine`, `WasapiLoopbackAudioSource`, drift-protected `WasapiAudioOutput`, `DelayLineSampleProvider`)
  - `SyncSound.Windows` — Core Audio endpoint discovery, PnP Bluetooth battery queries (`BluetoothBatteryReader`), JSON persistence, Win32 global hotkeys, tray icon, single-instance guard
  - `SyncSound.App` — Per-Monitor DPI v2 aware UI (`Listen`, `Headphones`, `Preferences` + `QuickSidebarWindow`) in Warm Cream (`#F4EFE6`), Black (`#191817`), and Burnt Orange (`#C65D32`) with Light, Dark, and System themes
  - `SyncSound.Tests` — Automated xUnit tests
- **`website/`** — Next.js landing page sharing the exact SYNC SOUND visual design system (`Root Directory: website`)

---

## Key Features

- **Connect → Choose → Listen:** Discover Bluetooth and wired headphones paired with Windows, pick your listening group, and share system audio seamlessly.
- **Tactile Volume & Wired + Bluetooth Freedom:** Precision **Sync Dial** session volume + per-headphone rotary dials, real Bluetooth battery indicators where exposed by Windows, and seamless simultaneous routing across 3.5 mm wired Realtek headphones, USB headsets, and Bluetooth TWS earbuds.
- **Quick Sidebar (`Ctrl + Shift + S`):** Draggable floating control instrument with remembered screen position, always-on-top support, and instant Start/Stop listening.
- **Fault Isolation & Auto-Recovery:** If a headphone disconnects out of range, healthy headphones stay active and reconnect automatically when back online.
- **Light, Dark & System Modes:** Instant, persistent theme switching across the app and Quick Sidebar.

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
