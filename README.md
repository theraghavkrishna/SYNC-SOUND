# SYNC SOUND

**One sound. Everyone listening.**

Connect multiple Bluetooth, wired, and other audio devices to your Windows 11 PC and listen together—with a calm native desktop app, a built-in local music player, and an instant **`Ctrl + Shift + S`** Quick Sidebar.

---

## What's Inside This Repository

- **`src/`** — Windows 11 desktop application (`C# / .NET 10 / WPF / MVVM / NAudio 3.3.0`)
  - `SyncSound.Core` — Domain models, state machine, interfaces
  - `SyncSound.Audio` — Reusable multi-output `AudioEngine`, `WasapiLoopbackAudioSource`, `LocalAudioFileSource`, `DelayLineSampleProvider`
  - `SyncSound.Windows` — Core Audio endpoint discovery, JSON settings, Win32 global hotkeys, tray icon, startup registration, single-instance guard
  - `SyncSound.App` — Main WPF app (`Home`, `Devices`, `Player`, `Settings`) + floating Quick Sidebar
  - `SyncSound.Tests` — Automated xUnit tests
- **`website/`** — Standalone Vercel-ready Next.js landing page (`Root Directory: website`)

---

## Features

- **System Audio Sharing:** Captures your Windows default output via WASAPI loopback and fans it out to selected additional headphones while letting the default output continue playing normally (no feedback loops).
- **Quick Sidebar (`Ctrl + Shift + S`):** Floating compact companion accessible from any app, with Escape/click-outside dismissal and Windows system tray integration.
- **Built-in Music Player:** Drag-and-drop MP3, WAV, FLAC, M4A, AAC, and WMA tracks with embedded album artwork, metadata, seek bar, queue, and simultaneous multi-headphone output.
- **Fault Isolation & Recovery:** If one Bluetooth pair disconnects mid-session, healthy headphones keep listening and reconnect automatically when back online.
- **Local & Private:** No account, no cloud, no telemetry. Settings and logs stay on your device under `%LocalAppData%\SyncSound`.

---

## Build & Run Locally

### Desktop App (Windows 11, .NET 10 SDK)

```powershell
dotnet restore SyncSound.sln
dotnet build SyncSound.sln -c Release
dotnet test SyncSound.sln
dotnet run --project src/SyncSound.App/SyncSound.App.csproj
```

Publish self-contained portable x64 build:

```powershell
dotnet publish src/SyncSound.App/SyncSound.App.csproj -c Release -r win-x64 --self-contained true -o publish/app
```

### Landing Page (`website/`)

```powershell
cd website
npm install
npm run build
```

Deploy to **Vercel** by importing the GitHub repository (`theraghavkrishna/SYNC-SOUND`) and setting **Root Directory** to `website`.
