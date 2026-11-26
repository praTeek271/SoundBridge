# flow.md — SoundBridge Flows

---

## 1. Installation Flow

```
User downloads SoundBridge installer (.msix)
        │
        ▼
MSIX installs the app to Program Files
        │
        ▼
Installer detects if virtual audio driver is present
        │
        ├── Not found → silently installs bundled VirtualAudioDriver.inf
        │              (no bcdedit needed — driver is signed)
        │
        └── Already present → skip driver install
        │
        ▼
App registers startup entry (optional, user choice during first launch)
        │
        ▼
First launch → shows First Run Setup wizard
```

---

## 2. First Run Setup Flow

```
App opens
    │
    ▼
Detects if "SoundBridge Virtual Device" is set as Windows default output
    │
    ├── NOT set → shows banner:
    │             "Set SoundBridge as your default output to begin routing."
    │             [Set as Default] button → calls Windows API to set it
    │
    └── Already set → proceeds normally
    │
    ▼
Scans and lists all available output devices
    │
    ▼
User selects which devices to route audio to
    │
    ▼
User clicks Apply → routing starts
    │
    ▼
Settings saved to %APPDATA%\SoundBridge\config.json
(reloaded automatically on next launch)
```

---

## 3. Daily Use Flow (after first-run setup)

```
Windows starts → SoundBridge auto-launches (if enabled)
    │
    ▼
Loads config.json
    │
    ▼
Re-opens saved source device (virtual device)
Re-opens saved output devices
Re-applies saved volume levels and delay offsets
    │
    ▼
Routing starts automatically
    │
    ▼
App minimizes to system tray (if minimize-to-tray enabled)
    │
    ▼
User hears audio on all selected devices
    │
    └── User can open tray icon → change devices, adjust volume,
        run calibration, or exit
```

---

## 4. Device Change Flow (hot-plug)

```
User plugs in / unplugs a device mid-session
    │
    ▼
Windows fires IMMNotificationClient::OnDeviceStateChanged event
    │
    ▼
SoundBridge detects the event
    │
    ├── New device connected:
    │       Add to available devices list in UI
    │       If device was previously saved in config → auto-route to it
    │
    └── Device disconnected:
            Remove from active output list
            Show notification: "USB Headset disconnected — routing stopped for that device"
            Other devices continue unaffected
```

---

## 5. Audio Data Flow

```
Any Windows application
(Spotify, YouTube, Game, Video call)
            │
            │  Windows audio session (WASAPI shared mode)
            ▼
  SoundBridge Virtual Device
  (WDM driver — no physical speaker)
            │
            │  WASAPI loopback capture
            │  (AUDCLNT_STREAMFLAGS_LOOPBACK)
            ▼
      LoopbackCapture
      (reads PCM frames every ~10ms)
            │
            │  raw PCM buffer
            ▼
       MultiRenderer
       (fans buffer to all active RenderStreams)
            │
    ┌───────┼───────┐
    │       │       │
    ▼       ▼       ▼
RenderStream RenderStream RenderStream
(Bluetooth)  (USB)    (3.5mm)
    │         │         │
    │  WASAPI render client per device
    │  (AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM)
    │  Windows resamples per device internally
    ▼         ▼         ▼
BT headset  USB DAC  Realtek jack
(all playing simultaneously)
```

---

## 6. Volume Control Flow

```
User moves volume slider for Device X in the UI
    │
    ▼
C# ViewModel sends command to C++/CLI bridge
    │
    ▼
Bridge calls RenderStream::SetVolume(float gain) on Device X's stream
    │
    ▼
RenderStream applies gain multiplier to each PCM sample
before writing to that device's render buffer
    │
    ▼
Device X volume changes in real time, no interruption to other devices
```

---

## 7. Latency Calibration Flow

```
User opens Calibration panel
    │
    ▼
SoundBridge plays 3 short beeps (800Hz sine, 200ms each)
through ALL active output devices simultaneously
    │
    ▼
User listens and identifies which device sounds LAST
(this becomes the sync reference — always the slowest device,
usually Bluetooth)
    │
    ▼
User selects the lagging devices and enters estimated delay in ms
OR uses binary mode:
    "Does Device A sound before or after Device B?" (Yes/No)
    Repeat until offset is found automatically
    │
    ▼
SoundBridge pre-fills each non-reference device's queue
with silence equal to the offset amount
(adds delay to faster devices to match the slowest one)
    │
    ▼
Verification: 3 more beeps play — user confirms sync
    │
    ▼
Delay offsets saved to config.json per device
```

---

## 8. Settings Flow

```
On first Apply:
    config.json created at %APPDATA%\SoundBridge\config.json

On every change (device toggle, volume, delay, theme):
    config.json updated in place (debounced 500ms)

On app launch:
    config.json loaded
    All settings restored silently

Schema:
{
  "sourceDeviceId": "string",
  "theme": "system" | "dark" | "light",
  "startWithWindows": bool,
  "minimizeToTray": bool,
  "outputDevices": [
    {
      "id": "string",
      "friendlyName": "string",
      "enabled": bool,
      "volume": 0.0–1.0,
      "delayMs": 0–500
    }
  ]
}
```
