# blueprint.md — SoundBridge UI Blueprint

---

## Window

- Default size: 900 × 560px
- Minimum size: 640 × 420px
- Resizable: yes
- Mica background material (WinUI 3 native)
- Title bar: "SoundBridge" + app icon
- Window chrome: standard Win32 (minimize, maximize, close)

---

## Navigation Structure

```
NavigationView (collapsible sidebar, left-aligned)
│
├── [icon] Dashboard        ← live status overview
├── [icon] Devices          ← main routing setup panel (default landing)
├── [icon] Calibration      ← latency sync tool
├── [icon] Settings         ← app preferences
│
└── (footer)
    [icon] About
```

Sidebar collapses to icon-only mode on narrow widths or user toggle.
Tooltips shown on icon-only mode (NavigationViewItem.Content).

---

## Page: Devices (primary page)

```
┌─ Source ──────────────────────────────────────────────────┐
│  Route audio FROM:  [ SoundBridge Virtual Device    ▾ ]  [↺]│
└───────────────────────────────────────────────────────────┘

┌─ Output Devices ──────────────────────────────────────────┐
│                                                            │
│  ●  Device Name          [━━━━━━━━━━] Vol  [ 0 ms] [●]   │
│  ○  Device Name          [━━━━━━━━━━] Vol  [ 0 ms] [○]   │
│  ○  Device Name          [━━━━━━━━━━] Vol  [ 0 ms] [○]   │
│                                                            │
│  [Scan for devices]                      [Apply  ▶]       │
└───────────────────────────────────────────────────────────┘
```

### Device row columns
| Column | Control | Notes |
|---|---|---|
| Enable | ToggleSwitch | Adds/removes device from active routing |
| Name | TextBlock | Friendly name from Windows (truncated + tooltip) |
| Volume | Slider (0–100) | Disabled when device not enabled |
| Delay | NumberBox (0–500ms) | Disabled when device not enabled |
| Status dot | Ellipse (green/grey) | Green = audio flowing, grey = idle/off |

### Buttons
- **Scan** — refreshes device list from `DeviceEnumerator`
- **Apply** — calls `Start()` on router, shows loading overlay then success

---

## Page: Dashboard

```
┌─ Routing Status ──────────────────────────────────────────┐
│                                                            │
│  ● ROUTING ACTIVE          Source: SoundBridge Virtual    │
│                                                            │
│  Level  [████████░░░░░░░░░░░░]                            │
│                                                            │
│  Active outputs: 3 / 5 devices                            │
│                                                            │
│  [ Stop Routing ]                                         │
└───────────────────────────────────────────────────────────┘

┌─ Active Devices ──────────────────────────────────────────┐
│  [●] Sony WH-1000XM5         80%   +0ms                  │
│  [●] USB Headset Pro        100%  +80ms                  │
│  [●] Realtek HD Audio        70%  +80ms                  │
└───────────────────────────────────────────────────────────┘
```

- VU meter: `ProgressBar` updated every 100ms via DispatcherTimer
- Status dot: `FontIcon` or `Ellipse` with color binding
- "Stop Routing" becomes "Start Routing" when idle

---

## Page: Calibration

```
Step 1 of 3 — Play test signal

  SoundBridge will play 3 short beeps on ALL active
  output devices simultaneously.

  Put on all your headphones / listen to all speakers.
  Identify which device you hear LAST.

  [ ▶ Play Beeps ]

──────────────────────────────────────────────────────────

Step 2 of 3 — Set delays

  Select the device that sounded LAST (your reference):
  [ Sony WH-1000XM5              ▾ ]

  How far ahead (in ms) does each other device sound?

  USB Headset Pro      [ 80  ] ms
  Realtek HD Audio     [ 80  ] ms

──────────────────────────────────────────────────────────

Step 3 of 3 — Verify

  [ ▶ Play Verification Beeps ]

  Do all devices sound in sync now?
  [ ✓ Yes, save these settings ]   [ ✗ No, try again ]
```

- Step flow controlled by `CalibrationViewModel` state machine
- Beeps triggered via `ManagedAudioRouter.PlayCalibrationBeeps()`
- Delay values written back to `DeviceViewModel.DelayMs` on confirm

---

## Page: Settings

```
┌─ Startup ─────────────────────────────────────────────────┐
│  Start SoundBridge with Windows        [ ○ ]             │
│  Minimize to system tray on close      [ ● ]             │
└───────────────────────────────────────────────────────────┘

┌─ Appearance ──────────────────────────────────────────────┐
│  Theme     ( ) System default  (●) Dark  ( ) Light       │
└───────────────────────────────────────────────────────────┘

┌─ Configuration ───────────────────────────────────────────┐
│  Config file: %APPDATA%\SoundBridge\config.json          │
│  [ Open folder ]     [ Reset to defaults ]               │
└───────────────────────────────────────────────────────────┘
```

---

## Page: About

```
  [App icon]  SoundBridge
              Version 1.0.0

  Multi-output audio router for Windows.
  Play audio on multiple devices simultaneously.

  ─────────────────────────────────────────────

  Virtual Audio Driver by MikeTheTech (MIT License)
  Windows App SDK by Microsoft (MIT License)

  [ View on GitHub ]   [ Report an issue ]
```

---

## System Tray Behavior

When minimized to tray:
- Tray icon: SoundBridge logo
- Left-click: restore window
- Right-click context menu:
  - "SoundBridge — Routing Active" (status, greyed)
  - separator
  - "Open SoundBridge"
  - "Stop Routing" / "Start Routing"
  - separator
  - "Exit"

---

## Visual States

| State | Status dot | VU meter | Apply button |
|---|---|---|---|
| No source selected | grey | empty | disabled |
| Source selected, not applied | grey | empty | enabled |
| Routing active | green (pulsing) | live | shows "Stop" |
| Device error | red | — | "Retry" |
| Device disconnected | amber | — | auto-recovers |

---

## Accessibility

- All controls keyboard-navigable (Tab order defined)
- Screen reader: all controls have `AutomationProperties.Name`
- VU meter has text fallback: "Level: 74%"
- High contrast mode: WinUI 3 handles automatically
- Minimum touch target: 44×44px (WinUI 3 default)
