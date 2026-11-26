# architecture.md — SoundBridge Technical Architecture

---

## Overview

SoundBridge uses a three-layer architecture. Each layer has a clear
responsibility and communicates with the adjacent layer through a
defined interface. No layer reaches across the boundary of another.

```
┌─────────────────────────────────────────────────────┐
│  Layer 3 — UI (C# + WinUI 3)                        │
│  SoundBridge.exe                                    │
│  Views, ViewModels, Settings UI                     │
└────────────────────┬────────────────────────────────┘
                     │  Managed API calls
                     │  (.NET method calls, events)
┌────────────────────▼────────────────────────────────┐
│  Layer 2 — Bridge (C++/CLI)                         │
│  AudioBridge.dll                                    │
│  Wraps C++ classes as .NET objects                  │
│  Marshals data between managed and native memory    │
└────────────────────┬────────────────────────────────┘
                     │  Direct C++ calls
                     │  (no marshaling overhead)
┌────────────────────▼────────────────────────────────┐
│  Layer 1 — Audio Engine (C++17)                     │
│  MultiOutputAudio.dll                               │
│  WASAPI, device enumeration, capture, routing       │
└─────────────────────────────────────────────────────┘
                     │
                     │  WASAPI (Windows Core Audio APIs)
                     ▼
         Windows Audio Session API
         Virtual device driver (WDM)
         Physical output devices
```

---

## Layer 1 — Audio Engine (C++17 DLL)

**File:** `MultiOutputAudio.dll`
**Build:** CMake → MSVC x64
**Language:** C++17
**Dependencies:** Windows SDK (mmdeviceapi, audioclient, avrt), WRL (ComPtr)

### Components

#### DeviceEnumerator
Wraps `IMMDeviceEnumerator`. Lists all active WASAPI render endpoints.
Returns `std::vector<AudioDevice>` (id, friendlyName, isDefault).
Also resolves a device `ComPtr<IMMDevice>` by endpoint ID string.

#### LoopbackCapture
Opens a WASAPI loopback stream on a given render endpoint.
Uses `AUDCLNT_STREAMFLAGS_LOOPBACK` — captures whatever Windows
is sending to that device without intercepting or stopping it.
Polling-based (not event-driven) to avoid the known WASAPI loopback
silent-period event stall bug. Returns raw PCM frames via `ReadAvailable()`.

#### RenderStream
Opens a WASAPI render stream to one physical output device.
Uses `AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM` — lets Windows resample
the source format to match whatever the physical device natively uses.
Owns an internal thread (elevated to `AvSetMmThreadCharacteristics`)
and a bounded lock-free queue. Audio is pushed in from the capture
side and pulled out on the render thread.

Planned additions:
- `SetVolume(float gain)` — multiplies PCM samples before queuing
- `SetDelayMs(int ms)` — pre-fills queue with silence on start

#### MultiRenderer
Owns a collection of `RenderStream` instances.
`PushAudio()` distributes one captured buffer to every active stream.
Isolates failures — one bad device doesn't affect others.

#### AudioCalibrator (planned — M6)
Generates 3-beep test signal (800Hz sine wave).
Sends it through MultiRenderer to all active devices simultaneously.
Accepts user-reported delay offsets, applies them via `RenderStream::SetDelayMs()`.

#### SettingsManager (planned — M7)
Reads/writes `%APPDATA%\SoundBridge\config.json`.
Serializes/deserializes device list, volume, delay, UI preferences.

---

## Layer 2 — Bridge (C++/CLI DLL)

**File:** `AudioBridge.dll`
**Build:** MSVC C++/CLI (/clr)
**Language:** C++/CLI (mixed managed + native)
**Purpose:** Expose C++ engine to C# without COM or P/Invoke complexity

### Why C++/CLI (not P/Invoke)

P/Invoke works for simple flat C APIs but our engine uses C++ classes,
`std::vector`, `std::wstring`, and COM smart pointers — none of which
cross P/Invoke cleanly. C++/CLI wraps the C++ types directly in managed
ref classes that C# sees as normal .NET objects.

### Managed Classes Exposed to C#

#### ManagedDeviceInfo
```
ref class ManagedDeviceInfo {
    property String^ Id
    property String^ FriendlyName
    property bool    IsDefault
}
```

#### ManagedAudioRouter
```
ref class ManagedAudioRouter {
    List<ManagedDeviceInfo^>^ GetRenderDevices()
    void SetSource(String^ deviceId)
    void AddOutput(String^ deviceId)
    void RemoveOutput(String^ deviceId)
    void SetOutputVolume(String^ deviceId, float volume)
    void SetOutputDelayMs(String^ deviceId, int ms)
    void Start()
    void Stop()
    float GetPeakLevel()          // for VU meter
    double GetOutputQueuedMs(String^ deviceId)  // for sync status
    event EventHandler^ DeviceListChanged   // hot-plug
}
```

---

## Layer 3 — UI (C# + WinUI 3)

**File:** `SoundBridge.exe`
**Build:** MSBuild / Windows App SDK
**Language:** C#, XAML
**Framework:** WinUI 3 (Windows App SDK 1.5+)
**Minimum OS:** Windows 10 version 1809 (build 17763)

### Design Language
WinUI 3 Fluent Design:
- Mica background material (acrylic on older hardware)
- NavigationView for collapsible sidebar
- System light/dark theme following OS setting automatically
- Accent color follows Windows accent color setting

### Views

#### MainWindow
Hosts `NavigationView`. Contains the top-level shell.
Sidebar items: Dashboard, Devices, Calibration, Settings, About.

#### DashboardPage
Live routing status. VU meter (ProgressBar updated every 100ms).
Active device count. Quick stop/start toggle.

#### DevicesPage
Master source device selector (ComboBox populated from `GetRenderDevices()`).
Output device list (ListView of DeviceItemControl).
Each row: device name, enable toggle, volume slider, delay input, status dot.
"Scan" button, "Apply" button.

#### CalibrationPage
Step-by-step latency calibration wizard.
"Play Test Beeps" button.
Per-device delay input or guided binary-question mode.
"Apply Sync" button.

#### SettingsPage
Start with Windows toggle.
Minimize to tray toggle.
Theme selector (System / Light / Dark).
Config file location (open in Explorer button).
"Reset to defaults" button.

### ViewModels (MVVM pattern)

#### RouterViewModel
Owns `ManagedAudioRouter` instance.
Exposes `ObservableCollection<DeviceViewModel>` for the device list.
Commands: ScanDevices, ApplyConfig, StartRouting, StopRouting.
Fires `PeakLevelChanged` event on a 100ms timer for VU meter.

#### DeviceViewModel
Represents one output device row in the UI.
Properties: Name, IsEnabled, Volume (0–100), DelayMs, StatusColor.
All properties implement `INotifyPropertyChanged`.

#### SettingsViewModel
Reads/writes `SettingsManager` (via bridge).
Handles startup registration via Windows Task Scheduler or registry.

---

## Threading Model

```
UI thread (STA)
    WinUI 3 dispatcher
    All XAML updates must marshal here

Audio capture thread
    Calls LoopbackCapture::ReadAvailable() every 10ms
    Calls MultiRenderer::PushAudio()
    No UI calls allowed from this thread

Render threads (one per active RenderStream)
    Elevated to real-time priority via AvSetMmThreadCharacteristics
    Pulls from queue, writes to WASAPI render buffer
    No UI calls allowed from this thread

Timer thread (C# DispatcherTimer, 100ms)
    Calls GetPeakLevel() via bridge
    Updates VU meter on UI thread
```

---

## Virtual Driver Dependency

SoundBridge requires one of:
1. **MikeTheTech Virtual Audio Driver** (preferred — MIT licensed, bundled)
2. **VB-Cable** (fallback — freeware, user installs separately)
3. **FxSound** or any other signed virtual audio device already on system

The engine does not care which driver is used.
`DeviceEnumerator` lists all render endpoints; the user picks the source.
The driver only needs to appear as a standard WASAPI render endpoint.

---

## Settings File Location

```
%APPDATA%\SoundBridge\config.json
```

Created on first Apply. Auto-migrated on version upgrades.
