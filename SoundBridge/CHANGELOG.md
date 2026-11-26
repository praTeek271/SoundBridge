# Changelog

All notable changes to **SoundBridge** are documented here.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versions follow [Semantic Versioning](https://semver.org/).

---

## [1.5.0] — 2026-10-24

### Added
- **Template delete button** — each multi-output device template now has a
  dedicated `Delete Template` button in both the workspace header and the
  bottom action bar. A right-click context menu on the sidebar item also
  exposes this action. A confirmation dialog prevents accidental deletion.
- **Auto-navigate to Calibration on template select** — clicking `Select` /
  `Apply Configuration` on a template now automatically switches to the
  Calibration page, matching the behaviour of the `Update Routing` action
  while routing is already active.
- **Windows active output indicator on Devices page** — a new compact info
  bar below the Master device card shows the name of the currently active
  Windows playback device. The indicator dot turns green when routing is
  active. The name refreshes on template select, routing start/stop, and
  every ~5 seconds during the live-status tick.

### Changed
- `AudioDeviceDescriptor.IsDefault` is now a read-write property (was
  `init`-only) so the running service can update the default-device flag
  dynamically without recreating the descriptor.
- `SystemDefaultAudioEndpointService` gains a new static method
  `GetDefaultRenderEndpointName()` using raw COM interop
  (`IMMDeviceEnumerator` -> `IMMDevice` -> `IPropertyStore`) — no extra
  NuGet packages required.

---

## [1.4.3] — 2026-09-15

### Fixed
- MSI product code registered for version 1.4.3.
- Startup-with-Windows registry key now written to `HKCU\...\Run` using the
  correct executable path after a per-machine install.

---

## [1.4.2] — 2026-08-20

### Added
- Minimize-to-tray on window close (configurable in Settings).
- System tray menu: Open, Stop Routing, Exit.
- Single-instance guard via named mutex; second launch restores the existing
  window.

### Fixed
- Sidebar collapse state was not preserved across navigation.

---

## [1.4.1] — 2026-07-30

### Added
- Privacy Agreement RTF shown during MSI setup.
- Virtual Audio Device prerequisite notice added to installer wizard.

### Fixed
- `SoundBridgeBridge.dll` not found error on first launch after MSI install.

---

## [1.4.0] — 2026-07-01

### Added
- WiX v4 MSI installer with per-machine install scope.
- Start Menu shortcut (always) and optional Desktop shortcut.
- `app.meta.json` as the single source of truth for product metadata fed
  into the MSI build pipeline.
- `build-msi.ps1` / `build-msi.bat` build scripts.
- Per-version ProductCode auto-generation with persistence back to
  `app.meta.json`.

---

[1.5.0]: https://github.com/prateek271/soundbridge/compare/v1.4.3...v1.5.0
[1.4.3]: https://github.com/prateek271/soundbridge/compare/v1.4.2...v1.4.3
[1.4.2]: https://github.com/prateek271/soundbridge/compare/v1.4.1...v1.4.2
[1.4.1]: https://github.com/prateek271/soundbridge/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/prateek271/soundbridge/releases/tag/v1.4.0
