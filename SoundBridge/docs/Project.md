# SoundBridge Project Overview

Last updated: 2026-06-21

## Product

SoundBridge is a Windows desktop audio-routing application. The goal is to let a
user route one master/source audio device into multiple selected output devices,
then calibrate delay and volume from a native GUI.

## Current Stage

SoundBridge is now a WPF/.NET 8 GUI integrated with a native C++ WASAPI backend
through `SoundBridgeBridge.dll`.

Current verified outputs:

- WPF executable: `SoundBridge.exe`
- Native bridge DLL: `SoundBridgeBridge.dll`
- MSI installer: `dist\installer\SoundBridgeSetup-v<version>.msi`

## Repository Layout

```text
SoundBridge/
  app/        WPF GUI, C# view-models, services, assets
  backend/    Native C++ audio backend and bridge DLL
  docs/       Project notes and implementation handoff docs
  installer/  WiX MSI package and build scripts
  dist/       Generated publish and installer output
```

Generated build folders can be recreated and should not be treated as source.

## GUI State

The GUI is currently focused on these pages:

- `Devices`: default page, live routing overview, preserved muted default
  flowchart, active routing status console, selected output table.
- Nested `multi-output device` pages: template editing, master device selection,
  output selection, delay/volume metadata.
- `Calibration`: starts/stops routing and applies routing updates.
- `Settings`: startup with Windows and minimize-to-tray options.
- `About`: version/reference information.

The old standalone Dashboard route has been removed. Dashboard-style live status
now belongs inside the default Devices page.

## Important UX Rules

- The default Devices page must always show the muted fallback flow:
  `Master -> SoundBridge -> Default Audio`.
- The flowchart should only become accent/animated when routing is active.
- `Select` on a template activates/saves the template; it does not start routing.
- Calibration `OK` starts routing.
- Calibration `Disable` stops routing.
- Calibration `Update Routing` applies pending routing metadata.
- Only one template can be active at a time.
- Aggregate device creation is currently disabled in the UI until backend and
  visual behavior are ready.

## Runtime Behavior

Implemented runtime guards:

- Single-instance guard using a named mutex.
- Second app launch restores/focuses the existing window and exits.
- Minimize-to-tray on close when enabled in Settings.
- Tray menu supports open, stop routing, and exit.
- Startup-with-Windows setting writes to the current user's Run registry key.

## Persistence

Per-user app data lives under:

```text
%LocalAppData%\SoundBridge\
```

Current persisted files include:

- `templates.json`
- `templates.backup.json`
- `settings.json`
- `logs\app.log`
- `corrupted\*.bad.json`

Templates are JSON metadata only. Audio routing active state should be queried
from the backend at runtime, not trusted from saved JSON.

## Installer

Installer format: MSI through WiX.

Current MSI behavior:

- Product metadata loaded from `app.meta.json`.
- Start Menu shortcut always created.
- Desktop shortcut is optional through installer checkbox.
- Install scope is selected by the user.
- Privacy agreement shown first.
- Virtual audio device prerequisite notice shown during setup.
- Add/Remove Programs icon comes from `app\assets\icon.ico`.
- User data under `%LocalAppData%\SoundBridge` is preserved on uninstall.

Before final MSI build for a new release, update:

```text
app.meta.json -> product.version
```

## Build Commands

Run from repository root:

```powershell
cd "D:\Lucifer-Drive\Programs\Virtual Audio Drive Program\SoundBridge"
```

Build app only:

```powershell
dotnet build app\SoundBridge.Gui.csproj -c Release
```

Build full MSI:

```powershell
.\installer\build-msi.bat
```

Run published app after MSI build:

```powershell
dist\SoundBridge\SoundBridge.exe
```

## Current Risks

- Tray behavior is implemented at GUI level; backend routing authority still
  needs careful synchronization for advanced active-routing cases.
- MSI same-version upgrades are enabled for testing, but public release builds
  should bump `app.meta.json` version.
- Virtual audio device installation is not bundled in MSI v1.
- Aggregate devices remain disabled until backend and UX are ready.
