# SoundBridge Backend

Companion app for [Virtual Audio Driver by MikeTheTech](https://github.com/VirtualDrivers/Virtual-Audio-Driver).

Captures audio from the Virtual Audio Device via WASAPI loopback and fans it out
simultaneously to any combination of Bluetooth, USB, and 3.5mm output devices.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| Visual Studio 2022 | Community (free) | Workload: **Desktop development with C++** |
| Windows SDK | 10.0.22000+ | Included with the VS workload above |
| CMake | 3.20+ | Bundled with VS 2022, or [cmake.org](https://cmake.org) |
| Git | any | Needed so CMake can fetch FTXUI |
| Virtual Audio Driver | 25.7.14+ (signed) | From [releases](https://github.com/VirtualDrivers/Virtual-Audio-Driver/releases) |

> The driver's latest release is **already signed** — no `bcdedit /set testsigning on` required.

---

## Build

```powershell
# From the SoundBridge repository root:
cmake -S backend -B backend\build-integration -A x64
cmake --build backend\build-integration --config Release
```

Native outputs land at `backend\build-integration\Release\`.

**Or from Visual Studio 2022:** `File > Open > CMake...` → select this folder → it configures automatically.

---

## Milestones

- [x] **M1** — Project scaffold + device enumeration (list all audio outputs)
- [x] **M2+M3** — WASAPI loopback capture + multi-device fan-out renderer
- [ ] **M4** — FTXUI control panel (device select, per-device volume)
- [ ] **M5** — Settings persistence + installer

---

## Try it (M2+M3)

1. Run `MultiOutputAudio.exe`.
2. It lists your output devices and asks you to pick a **source** (the
   device currently set as your Windows default output - this is what gets
   captured) and one or more **outputs** (where that audio gets sent too,
   in addition to playing normally).
3. Play some audio (YouTube, Spotify, etc.) on the source device.
4. You should see a live VU meter (`[#####-----] -> N device(s)`) and hear
   audio from every selected output device simultaneously.
5. Press Enter to stop.

> Note: capturing FROM a device and rendering back TO the *same* device can
> cause an echo/feedback loop - pick distinct devices for the cleanest test
> (e.g. capture from your default speakers, route to a USB or Bluetooth
> headset).

---

## Architecture

```
Windows audio system (any app)
         │
         ▼
Virtual Audio Device          ← MikeTheTech's WDM driver (installed separately)
    [WDM driver]
         │  WASAPI loopback capture
         ▼
    Audio Router              ← this app
         │
    ┌────┼────┐
    ▼    ▼    ▼
   BT   USB  3.5mm            ← WASAPI render clients (one thread each)
```
