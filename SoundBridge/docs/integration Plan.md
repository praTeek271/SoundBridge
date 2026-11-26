**Integration Direction**
The clean path is: keep the WPF GUI as-is, keep `IAudioRouterService` as the GUI boundary, and replace `MockAudioRouterService` with a real implementation backed by the C++ audio engine.

The current backend is not ready to be called directly by the GUI yet because `MultiOutputAudio/src/main.cpp` is an interactive console app. But the useful engine pieces already exist:

- `DeviceEnumerator`: lists WASAPI render devices.
- `LoopbackCapture`: captures audio from selected source via WASAPI loopback.
- `MultiRenderer`: sends captured audio to multiple render streams.
- `RenderStream`: plays audio to one output device.

So the work is not “invent backend.” The work is “wrap the existing backend into a callable service.”

**Recommended Architecture**
Keep this shape:

```text
WPF GUI
  -> IAudioRouterService
    -> RealAudioRouterService.cs
      -> AudioBridge.dll C++/CLI
        -> Native C++ engine classes
```

Why this is best:

- The docs already planned a C++/CLI bridge.
- C# can call C++/CLI like normal .NET code.
- It avoids fragile console process control.
- It avoids painful P/Invoke around `std::vector`, `std::wstring`, COM pointers, and C++ classes.
- The GUI viewmodels barely need to change because they already talk to `IAudioRouterService`.

**Do Not Integrate By Launching The Console EXE**
We could technically make the GUI spawn `MultiOutputAudio.exe`, pass input, and parse output, but I strongly do not recommend it.

It would be brittle because:

- Current backend waits for console input.
- Device selection is index-based, not stable ID-based.
- Start/stop/status would require process control.
- Peak levels and hot-plug events would be hard to expose cleanly.
- Error recovery would be awkward.

That approach is okay only as a very temporary proof-of-life test, not as real integration.

**Phased Plan**

1. **Extract Native Engine Controller**
   Create a reusable native C++ class, something like `AudioRouterEngine`, that owns:
   - `DeviceEnumerator`
   - `LoopbackCapture`
   - `MultiRenderer`
   - capture loop thread
   - active source ID
   - selected output IDs
   - peak level
   - start/stop lifecycle

2. **Convert Console Logic Into Engine API**
   Move the logic from `main.cpp` into methods:
   - `GetRenderDevices()`
   - `SetSourceDevice(id)`
   - `SetOutputs(ids)`
   - `SetOutputVolume(id, volume)`
   - `SetOutputDelayMs(id, ms)`
   - `Start()`
   - `Stop()`
   - `GetPeakLevel()`

3. **Add C++/CLI Bridge**
   Create `AudioBridge.dll` exposing a managed class like:
   - `ManagedAudioRouter`
   - `ManagedDeviceInfo`

   This bridge translates C# strings/lists into native C++ types.

4. **Implement `RealAudioRouterService.cs`**
   This class implements the existing GUI interface:

   ```csharp
   public sealed class RealAudioRouterService : IAudioRouterService
   ```

   Internally it calls `ManagedAudioRouter`.

5. **Switch Dependency In `MainViewModel`**
   Right now:

   ```csharp
   RouterService = new MockAudioRouterService();
   ```

   Later:

   ```csharp
   RouterService = new RealAudioRouterService();
   ```

   Or better, add a small factory so we can toggle mock/real during development.

6. **Keep Mock Service**
   Do not delete `MockAudioRouterService`. It is useful for GUI work when audio backend is broken or unavailable.

**Important Integration Gap**
The current native engine does not yet support everything the GUI exposes.

Already mostly available:

- Device enumeration.
- Source selection.
- Output selection.
- Start/stop routing.
- Peak level calculation in `main.cpp`.

Still missing or only planned:

- Per-output volume.
- Per-output delay.
- Calibration beep generation.
- Device hot-plug event handling.
- Settings persistence.
- Virtual driver installation/default-device setup.

So the first integration milestone should be modest:

```text
M1: Real device list appears in GUI.
M2: User selects source and outputs.
M3: Start routing audio through selected devices.
M4: Stop routing.
M5: Peak meter/status updates.
```

Only after that should we wire:

```text
volume -> delay -> calibration -> settings persistence -> hot-plug
```

**My Suggested Next Step**
Start by creating the native `AudioRouterEngine` class from the existing `main.cpp` logic. That gives us a reusable backend core without touching the GUI much yet.

Once that compiles, we add the C++/CLI bridge and then plug it into `IAudioRouterService`.
