# SoundBridge GUI Development Status Report

Last updated: 2026-06-16

## Purpose Of This Report

This file is a handoff note for another AI or developer continuing the SoundBridge GUI work. It summarizes what the project is, what was read, what has been implemented, what design direction the user wants, where the code currently stands, and what still needs attention.

## Project Context

SoundBridge is a virtual audio routing / multi-output audio application. The intended user experience is a desktop GUI that can create and manage virtual multi-output and aggregate audio devices, select source/input devices, choose output devices, configure delay and volume, and connect to a backend audio engine.

The repository contains two relevant development tracks:

- `docs/`: product, architecture, flow, and GUI notes.
- `MultiOutputAudio/src/`: existing C++ terminal-oriented / backend-oriented soundbridge implementation.
- `app/`: current WPF GUI prototype in C#/.NET 8.

The GUI work has focused on visual layer and interaction behavior. The next major phase is integration: replacing or adapting the current mock service layer so the GUI can communicate with the real audio/backend implementation.

## Reference Material Read Earlier

The documentation folder was read first to understand intended direction:

- `docs/Project.md`
- `docs/architecture.md`
- `docs/blueprint.md`
- `docs/flow.md`
- `docs/gui-fix-notes.md`

The terminal/backend code was then inspected as implementation context:

- `MultiOutputAudio/src/main.cpp`
- `MultiOutputAudio/src/audio/*`
- `MultiOutputAudio/src/util/Win32Util.h`

The user also provided an HTML visual prototype:

- `application_visual_layer_prototype_v3_2.html`

Important design lesson from that prototype: do not copy it exactly. Understand its visual mood and interaction intent, then implement natively in WPF. Earlier work copied too closely and the user explicitly corrected that.

## User Design Direction

The user wants a desktop app GUI, not a canvas-like wrapper. The window itself should feel like the application, with a native Windows title bar and side navigation.

Key preferences established through conversation:

- Dark, glassy, restrained interface.
- Rounded controls where appropriate, but no outer decorative wrapper around the whole app pretending to be a canvas.
- App title belongs in the native Windows title bar, not duplicated in the sidebar.
- Sidebar should use icons.
- Sidebar can collapse/minimize.
- Search feature is not needed.
- Dashboard can be a placeholder for now: plain text is acceptable.
- Devices should be the default category on launch.
- Navigation must be stable and must not freeze when switching categories.
- Device creation should happen from the Devices section via a plus menu.
- Multi-output and aggregate devices should appear as nested sidebar items under Devices.
- Generated device pages should have actual content, not blank placeholders.
- Minimized sidebar should show icon-only category rows.

## Current Build Command

Use:

```powershell
dotnet build app\SoundBridge.Gui.csproj
```

The build has repeatedly succeeded with `0 Warning(s), 0 Error(s)`.

Common issue: if the GUI app is running, build fails because `app/bin/Debug/net8.0-windows/SoundBridge.Gui.exe` is locked. Stop the process and rebuild:

```powershell
Stop-Process -Name SoundBridge.Gui -Force
dotnet build app\SoundBridge.Gui.csproj
```

Do not treat the file-lock copy failure as a compile failure.

## Current GUI Architecture

The WPF GUI lives in `app/`.

Important files:

- `app/App.xaml`: global colors, brushes, button styles, text box style, combo box style, scroll bar style, and data templates mapping viewmodels to views.
- `app/MainWindow.xaml`: window shell, native title bar settings, sidebar, navigation content host.
- `app/MainWindow.xaml.cs`: sidebar collapse logic and remaining window-state helpers.
- `app/ViewModels/MainViewModel.cs`: main shell state, navigation items, device creation commands, active device management.
- `app/Models/NavigationItem.cs`: sidebar item model.
- `app/ViewModels/DeviceWorkspaceViewModel.cs`: generated multi-output / aggregate device page state.
- `app/Views/DeviceWorkspaceView.xaml`: generated device page UI.
- `app/ViewModels/CalibrationViewModel.cs`: calibration state, active multi-output details, volume targets.
- `app/Views/CalibrationView.xaml`: calibration UI.
- `app/Views/DevicesView.xaml`: default Devices page.
- `app/Services/MockAudioRouterService.cs`: mock device data and behavior.

## Major Work Completed

### Navigation Stability

Original problem: switching between `Dashboard`, `Devices`, `Calibration`, and `Settings` was freezing or crashing. There were overlap/focus issues around the old `ListBox` navigation model.

Current solution:

- Sidebar category switching is now command-driven.
- `MainViewModel.SelectNavigationCommand` sets `SelectedNavigationItem`.
- `SelectedPage` is shown by a `ContentControl` in `MainWindow.xaml`.
- WPF `DataTemplate`s in `App.xaml` map viewmodels to views.

This removed the fragile `ListBox.SelectedItem` pattern.

### Sidebar

Current sidebar features:

- Custom category rows with left active rail.
- Main categories: Dashboard, Devices, Calibration, Settings.
- About lives at bottom.
- Sidebar collapse toggled by hamburger icon.
- Collapsed width currently set in `MainWindow.xaml.cs` to `56`.
- Expanded width is `232`.
- Category labels hide when minimized.
- Created multi-output / aggregate devices remain visible as icon-only nested items when minimized.
- The minimized Devices `+` create button appears as a separate icon row under the Devices icon.
- The minimized Devices `+` currently uses a padded button surface: `Width="36"`, `Height="34"`, `Padding="6"`, `Margin="6,3,0,5"`.

Recent sidebar status:

The Devices category has been iterated many times and is still the most sensitive layout area. The latest completed changes:

- `DeviceActionHost` left margin: changed `Margin="8,2"` to `Margin="0,2"`.
- Devices button left padding: changed `Padding="6,8"` to `Padding="0,8"`.
- Minimized plus button was given a larger padded surface and left inset: `Margin="6,3,0,5"`.

This should still be visually checked in the running app before further integration work, because the Devices block is custom-built.

### Sidebar Devices Section

The Devices category now uses one combined icon+text button, matching other categories more closely. The plus button is separate.

Expanded sidebar behavior:

- Devices icon+text acts as a button to expand/collapse nested devices.
- Plus icon sits beside Devices and opens a popup menu.
- Popup menu contains:
  - `create multi-output device`
  - `create aggregate device`

Minimized sidebar behavior:

- Devices label is hidden.
- Devices icon remains.
- Plus appears as a separate padded icon row below Devices.
- Generated nested devices show as icon-only rows.

Known current risk:

The Devices block is custom-built and may still not visually align perfectly with Dashboard/Calibration/Settings. Inspect `MainWindow.xaml` around `DeviceActionHost` first.

### Native Title Bar

The app originally used a custom designed title bar with app icon/name and custom minimize/maximize/close buttons. The user later requested the original/native Windows title bar instead.

Current state:

- `MainWindow.xaml` uses `WindowStyle="SingleBorderWindow"`.
- The previous custom title bar row is collapsed with `RowDefinition Height="0"`.
- The native title bar now provides app title and system window buttons.
- The rest of the app body/sidebar/content layout was intentionally left unchanged.

Relevant file:

- `app/MainWindow.xaml`

### Device Creation

Implemented from the Devices plus menu.

Commands in `MainViewModel`:

- `CreateMultiOutputDeviceCommand`
- `CreateAggregateDeviceCommand`

Creates nested sidebar items named:

- `multi-output device 1`
- `multi-output device 2`
- `aggregate device 1`
- etc.

Each generated item has:

- `Title`
- `Description`
- `IconGlyph`
- `SubInfo`
- `Page`
- `IsSelected`
- `IsActiveDevice`
- `IsEnabled`

### Generated Device Workspace Pages

Created files:

- `app/ViewModels/DeviceWorkspaceViewModel.cs`
- `app/Views/DeviceWorkspaceView.xaml`
- `app/Views/DeviceWorkspaceView.xaml.cs`

Generated pages show:

- Device name.
- Device type.
- Select/Deselect button.
- Master device dropdown.
- Sample rate compact input.
- Output device list with checkboxes.
- Drift audio steppers.
- Scan and Apply buttons.

Sample rate input was fixed after earlier overflow problems:

- Compact width.
- No unnecessary horizontal scrolling.
- `kHz` suffix separated.

Master device dropdown now uses dark themed ComboBox styling from `App.xaml`.

### Active / Inactive Device State

Generated devices support select/deselect.

Behavior:

- Both multi-output and aggregate devices can be activated.
- Only one generated device can be active at a time.
- Clicking the active generated device's Select/Deselect button deactivates it.
- Active generated device sidebar icon/text/sub-info turns green.

Important distinction:

- Active multi-output device enables Calibration.
- Active aggregate device does not enable Calibration.
- If no active multi-output exists, Calibration is disabled.

Implementation points:

- `MainViewModel.ActivateDevicePage(...)`
- `DeviceWorkspaceViewModel.IsActive`
- `NavigationItem.IsActiveDevice`
- `CalibrationNavigationItem.IsEnabled`

### Sidebar Sub-info

Nested multi-output and aggregate sidebar items show:

```text
1 ins / 2 outs
```

Meaning:

- `ins`: 1 if a master/source device is selected, otherwise 0.
- `outs`: number of checked/enabled output devices.

Implemented in:

- `DeviceWorkspaceViewModel.RoutingSummary`
- `DeviceWorkspaceViewModel.InputCount`
- `DeviceWorkspaceViewModel.OutputCount`
- `NavigationItem.SubInfo`

The summary updates when:

- Master/source changes.
- Output checkbox state changes.
- Device is scanned.

### Calibration

Calibration page now depends on active multi-output state.

If active multi-output exists:

- Calibration category is enabled.
- Calibration page shows active multi-output name.
- Shows sample rate.
- Shows master device.
- Shows selected/enabled output devices.
- Output devices use volume sliders instead of checkboxes.

If no active multi-output exists:

- Calibration category is disabled.
- Viewmodel status says to select a multi-output device.

Relevant files:

- `app/ViewModels/CalibrationViewModel.cs`
- `app/Views/CalibrationView.xaml`

### Dashboard

Dashboard is intentionally minimal for now.

The user asked that if dashboard is not developed, it should simply show:

```text
this is dashboard main panel
```

Do not overbuild dashboard yet unless asked.

## Important UX Corrections From User

These should guide future work:

- Do not copy the HTML prototype exactly.
- Do not show theme-switch text from the prototype.
- Sidebar interactions must feel native and intentional.
- Avoid unnecessary search.
- Avoid square boxes; use rounded but not exaggerated shapes.
- Do not create a wrapper outside the app dialog/window.
- Do not duplicate the app name in sidebar when it already appears in title bar.
- If backend is not wired yet, that is acceptable for now.
- Focus is GUI polish and structure first.

## Current Known Issues / Areas To Check Next

1. Devices sidebar alignment is still being actively tuned.

   Latest code removed the extra Devices left margin/padding and adjusted the minimized plus button padding. This should be visually tested by launching the GUI.

2. Sidebar minimized layout needs visual inspection.

   Check that:
   - Devices icon is not overlapped.
   - Collapsed plus icon appears as its own row under Devices.
   - Nested generated devices show as icon-only rows.
   - Active rails do not visually crowd icons.

3. Build may fail if running GUI locks the executable.

   This is normal; stop `SoundBridge.Gui` and rebuild.

4. Git status/diff may be blocked by Windows safe-directory ownership.

   Earlier, `git status` failed with dubious ownership. Do not globally change git config without user permission.

5. Backend integration is not done.

   `MockAudioRouterService` currently provides fake devices and actions.

   Integration should likely start at `IAudioRouterService`. Keep the viewmodels depending on the interface if possible, and replace or adapt the mock implementation behind it.

6. No automated UI tests exist.

   Verification has been through `dotnet build` only.

## Suggested Next Steps

1. Run the app and visually inspect the sidebar in expanded and minimized states.
2. Fix any remaining Devices icon alignment with the active rail.
3. Consider extracting a reusable sidebar row style/template to avoid one-off XAML drift.
4. Begin backend integration through `IAudioRouterService`.
5. Map current mock operations to real backend/audio operations.
6. Continue polishing generated device pages as integration needs become clearer.
7. Add basic UI smoke tests or screenshot-based checks if the project allows it.

## File Iteration Status

### Heavily Modified

- `app/MainWindow.xaml`
  - Native window chrome setting, sidebar, category rows, device accordion/menu, nested device rendering.
  - Current hotspot for future work.

- `app/MainWindow.xaml.cs`
  - Sidebar collapse width and state.
  - Hamburger icon remains constant.
  - Closes device create popup on collapse.
  - Still contains some window-state helper methods from the custom title-bar era; these are harmless but may be removable later if no longer referenced.

- `app/App.xaml`
  - Theme resources.
  - Button styles.
  - TextBox style.
  - ComboBox dark template.
  - Data templates for viewmodels.

- `app/ViewModels/MainViewModel.cs`
  - Navigation command model.
  - Created device items.
  - Device creation commands.
  - Active generated device logic.
  - Calibration enable/disable logic.

- `app/Models/NavigationItem.cs`
  - Added sidebar-specific state: sub-info, active device, enabled.

- `app/ViewModels/DeviceWorkspaceViewModel.cs`
  - Generated page state.
  - Routing summary.
  - Select/deselect behavior.
  - Output enable tracking.

- `app/Views/DeviceWorkspaceView.xaml`
  - Generated multi-output/aggregate page UI.

- `app/ViewModels/CalibrationViewModel.cs`
  - Active multi-output state.
  - Active outputs.
  - Clear active state.

- `app/Views/CalibrationView.xaml`
  - Active device summary.
  - Volume sliders for active outputs.

### Existing / Supporting

- `app/Views/DevicesView.xaml`
  - Default Devices page.
  - Master dropdown.
  - Sample rate compact input.
  - Output checkboxes and delay steppers.

- `app/ViewModels/DevicesViewModel.cs`
  - Mock scan/apply behavior for default Devices page.

- `app/ViewModels/DeviceViewModel.cs`
  - Output device row state.

- `app/Services/MockAudioRouterService.cs`
  - Mock backend service.

- `app/Services/IAudioRouterService.cs`
  - Current integration seam between GUI viewmodels and audio/backend behavior.
  - Future real implementation should satisfy this interface or the interface should be expanded deliberately.

## Current State Summary

The GUI is no longer just a rough shell. It now has a working WPF navigation model, native Windows title bar, custom sidebar, device creation workflow, generated pages for multi-output and aggregate devices, active/inactive state, calibration dependency on active multi-output, themed controls, and compact/minimized sidebar behavior.

The main unresolved work is visual verification/polish of the Devices sidebar row in minimized/expanded states, followed by deeper backend integration through the native service layer.

## Integration Progress Update

Integration has now started.

The user shifted the terminal/backend files into:

```text
native/SoundBridge.Native/
```

That folder originally still behaved like the old terminal executable project. It has now been reshaped into a native audio core plus bridge boundary while preserving the legacy console harness.

### Native Project Structure Added

Current native integration files:

- `native/SoundBridge.Native/CMakeLists.txt`
  - Project renamed to `SoundBridgeNative`.
  - Adds static library target: `SoundBridgeNative`.
  - Adds native DLL bridge target: `SoundBridgeBridge`.
  - Keeps legacy console executable target: `MultiOutputAudio`.

- `native/SoundBridge.Native/src/engine/AudioRouterEngine.h`
- `native/SoundBridge.Native/src/engine/AudioRouterEngine.cpp`
  - New reusable native routing engine.
  - Wraps existing `DeviceEnumerator`, `LoopbackCapture`, and `MultiRenderer`.
  - Supports:
    - Enumerating render devices.
    - Setting source device.
    - Setting output devices.
    - Starting/stopping routing.
    - Reading peak level.
    - Reading active output count.
  - Runs the routing loop on a worker thread.
  - Initializes COM internally where needed.

- `native/SoundBridge.Native/src/bridge/SoundBridgeBridge.cpp`
  - New C ABI DLL layer for managed GUI integration.
  - Exports handle-based functions:
    - `SoundBridge_CreateEngine`
    - `SoundBridge_DestroyEngine`
    - `SoundBridge_EnumerateRenderDevices`
    - `SoundBridge_SetSourceDevice`
    - `SoundBridge_ClearOutputDevices`
    - `SoundBridge_AddOutputDevice`
    - `SoundBridge_StartRouting`
    - `SoundBridge_StopRouting`
    - `SoundBridge_IsRoutingActive`
    - `SoundBridge_GetPeakLevel`
    - `SoundBridge_GetActiveOutputCount`

### GUI Integration Files Added

- `app/Services/NativeAudioRouterInterop.cs`
  - P/Invoke declarations for `SoundBridgeBridge.dll`.
  - Uses classic `DllImport` to avoid requiring unsafe blocks in the WPF project.

- `app/Services/NativeAudioRouterService.cs`
  - Implements `IAudioRouterService`.
  - Calls the native bridge for device enumeration, source selection, output selection, start/stop, and peak level.
  - Volume, delay, and calibration tone calls are currently placeholders because the native engine does not yet implement per-output gain/delay or tone generation.

### GUI Startup Change

`MainViewModel` now accepts an injected `IAudioRouterService`.

`App.xaml.cs` now chooses the backend:

- Default: `MockAudioRouterService`.
- Native opt-in: set environment variable `SOUNDBRIDGE_USE_NATIVE=1`.

If native initialization fails, startup falls back to mock mode so GUI development does not get blocked by missing DLLs or device/backend issues.

### Native DLL Copy Rule

`app/SoundBridge.Gui.csproj` now conditionally copies:

```text
..\native\SoundBridge.Native\build-integration\Release\SoundBridgeBridge.dll
```

into the GUI output folder when that DLL exists.

This means:

- GUI-only builds still work if native has not been built.
- Integration builds can load the native DLL from beside `SoundBridge.Gui.exe`.

### Current Verified Build Commands

Native integration build:

```powershell
cmake -S native\SoundBridge.Native -B native\SoundBridge.Native\build-integration -A x64
cmake --build native\SoundBridge.Native\build-integration --config Release
```

GUI build:

```powershell
dotnet build app\SoundBridge.Gui.csproj
```

Both completed successfully after the integration changes.

### Important Native Build Note

The old moved CMake cache in:

```text
native/SoundBridge.Native/build/
```

still points back to the previous `MultiOutputAudio` source path. Reusing it causes a CMake source-directory mismatch error.

Use:

```text
native/SoundBridge.Native/build-integration/
```

for the current integration build unless the old cache is intentionally cleaned later.

### Current Integration Limits

The app is not fully backend-driven yet.

What works structurally:

- Native code builds as a reusable library.
- Native bridge DLL builds.
- GUI has a native service implementation.
- GUI can opt into native mode with an environment variable.
- GUI still safely defaults to mock mode.

What remains:

- Validate native mode at runtime with real audio devices.
- Add per-output volume support in native `RenderStream` / `MultiRenderer`.
- Add per-output delay support in native renderer queues.
- Add calibration beep generation.
- Decide whether generated multi-output / aggregate pages should create real persistent backend device profiles or just runtime routing profiles first.
- Eventually create a top-level solution/build script that builds native first, then GUI, then packages/copies runtime artifacts.

## Backend Handoff: Integration Workflow And Current Design

This section is the recommended starting point for another AI/developer taking over backend integration.

The current project has a WPF visual layer, a C# service abstraction, a P/Invoke interop layer, and a native C++ WASAPI engine. The GUI can already enumerate real Windows audio devices through the native backend when launched with native mode enabled. The remaining work is to make the native backend honor all metadata from the GUI: live routing updates, per-output volume, per-output delay, and eventually real template/profile persistence.

### High-Level Runtime Stack

Current stack:

```text
WPF Views
  -> ViewModels
    -> IAudioRouterService
      -> NativeAudioRouterService
        -> NativeAudioRouterInterop
          -> SoundBridgeBridge.dll C ABI exports
            -> AudioRouterEngine
              -> DeviceEnumerator / LoopbackCapture / MultiRenderer / RenderStream
                -> Windows WASAPI
```

Mock mode stack:

```text
WPF Views
  -> ViewModels
    -> IAudioRouterService
      -> MockAudioRouterService
```

Native mode is opt-in at app startup:

```powershell
$env:SOUNDBRIDGE_USE_NATIVE="1"
dotnet run --project app\SoundBridge.Gui.csproj
```

If native initialization fails, `App.xaml.cs` falls back to `MockAudioRouterService` so GUI work remains unblocked.

### Current File Structure For Integration

GUI:

```text
app/
  App.xaml
  App.xaml.cs
  SoundBridge.Gui.csproj
  MainWindow.xaml
  MainWindow.xaml.cs
  Models/
    AudioDeviceDescriptor.cs
    NavigationItem.cs
  Services/
    IAudioRouterService.cs
    MockAudioRouterService.cs
    NativeAudioRouterInterop.cs
    NativeAudioRouterService.cs
  ViewModels/
    MainViewModel.cs
    DeviceWorkspaceViewModel.cs
    CalibrationViewModel.cs
    DevicesViewModel.cs
    DeviceViewModel.cs
    ...
  Views/
    DeviceWorkspaceView.xaml
    CalibrationView.xaml
    DevicesView.xaml
    ...
  asset/
    beep.wav                 <- expected by Play Test, may not exist yet
```

Native:

```text
native/SoundBridge.Native/
  CMakeLists.txt
  src/
    main.cpp                 <- legacy console harness
    audio/
      DeviceEnumerator.*
      LoopbackCapture.*
      MultiRenderer.*
      RenderStream.*
    bridge/
      SoundBridgeBridge.cpp  <- C ABI bridge used by C# P/Invoke
    engine/
      AudioRouterEngine.*
    util/
      Win32Util.h
```

Build output currently used by GUI:

```text
native/SoundBridge.Native/build-integration/Release/SoundBridgeBridge.dll
```

The GUI project conditionally copies that DLL beside `SoundBridge.Gui.exe`.

### Current Build Commands

Use this build folder, not the old `build/` cache:

```powershell
cmake -S native\SoundBridge.Native -B native\SoundBridge.Native\build-integration -A x64
cmake --build native\SoundBridge.Native\build-integration --config Release
dotnet build app\SoundBridge.Gui.csproj
```

If build fails because `SoundBridge.Gui.exe` is locked, stop the running app:

```powershell
Stop-Process -Name SoundBridge.Gui -Force
```

Then rebuild.

### GUI Workflow Implemented

The current intended user flow is now represented in GUI state:

1. Open app.
2. Click sidebar `+`.
3. Select `create multi-output device`.
4. Navigate to `multi-output device 1`.
5. Select master source.
6. Select output devices.
7. Click `Apply Configuration`.
8. GUI validates/stages the template metadata.
9. `Select` becomes enabled.
10. Click `Select`.
11. Template becomes active/saved.
12. Calibration category becomes enabled.
13. Navigate to Calibration.
14. Click `OK`.
15. Audio routing starts.
16. `OK` changes to `Disable`.
17. `Disable` stops routing.

Important current semantics:

- `Apply Configuration` on a template validates/stages metadata.
- `Select` activates/saves the template.
- `OK` on Calibration starts routing.
- `Disable` on Calibration stops routing.
- `Update Routing` on Calibration is the only GUI action that applies pending metadata changes to an already active routing session.

### Active Template Versus Active Routing

The GUI now separates two concepts:

- Active template: the selected/saved multi-output device profile.
- Active routing: the native routing engine is currently running.

Relevant state in `DeviceWorkspaceViewModel`:

- `IsActive`
- `IsRoutingActive`
- `HasPendingRoutingUpdate`
- `IsConfigurationApplied`
- `SelectButtonText`

Button behavior:

- Before apply: `Select` disabled.
- After apply: `Select` enabled.
- After selected/active: button shows `Deselect`.
- If routing is active and this is the active template: button shows `Update`.

When routing is active and the user edits the active template:

- Routing is not immediately stopped.
- Live routing is not immediately updated.
- Template metadata is marked pending.
- `Apply Configuration` validates/stages only the template metadata.
- Clicking `Update` on the template page auto-navigates to Calibration.
- User then clicks `Update Routing` on Calibration.
- Backend implementation should eventually apply those pending metadata changes live.

Current GUI-stage caveat:

- The GUI flow is in place, but the native backend does not yet implement real live re-routing, gain, or delay updates.

### Calibration Page Current Design State

Current Calibration page sections:

- Header: `Calibration`.
- Instruction text.
- Active Template row:
  - Label: `Active Template:`
  - Value: active multi-output device name.
  - `Play Test` button.
- Active device summary card:
  - Active multi-output device name.
  - Sample rate.
  - Master device.
- Output device list:
  - Selected outputs from active template.
  - Each output has a volume slider.
- Bottom actions:
  - `OK` starts routing.
  - After routing starts, `OK` changes to `Disable`.
  - `Update Routing` appears when routing is active.
- Status prompt card:
  - Rounded/translucent panel.
  - Shows readiness, missing beep file, routing started, disabled, or update messages.

`Play Test` behavior:

- Uses `System.Media.SoundPlayer`.
- Looks for:

```text
app/asset/beep.wav
```

- At runtime the file is expected beside the executable as:

```text
asset/beep.wav
```

- `SoundBridge.Gui.csproj` conditionally copies this file if it exists.
- At the time of this report, `beep.wav` was not found in the workspace.

### Native Bridge API Currently Exposed

`native/SoundBridge.Native/src/bridge/SoundBridgeBridge.cpp` exports:

```text
SoundBridge_CreateEngine
SoundBridge_DestroyEngine
SoundBridge_EnumerateRenderDevices
SoundBridge_SetSourceDevice
SoundBridge_ClearOutputDevices
SoundBridge_AddOutputDevice
SoundBridge_StartRouting
SoundBridge_StopRouting
SoundBridge_IsRoutingActive
SoundBridge_GetPeakLevel
SoundBridge_GetActiveOutputCount
```

C# declarations live in:

```text
app/Services/NativeAudioRouterInterop.cs
```

Managed service wrapper:

```text
app/Services/NativeAudioRouterService.cs
```

Interface used by the GUI:

```text
app/Services/IAudioRouterService.cs
```

The backend developer should preserve `IAudioRouterService` as the GUI-facing seam unless there is a strong reason to expand it.

### What The Backend Currently Does

Native engine:

- Enumerates active Windows render endpoints.
- Opens source endpoint through WASAPI loopback.
- Opens output endpoints through WASAPI render streams.
- Starts a worker thread for capture/fan-out.
- Calculates rough peak level.
- Stops routing.

Known native behavior:

- `AudioRouterEngine.Start()` requires a source device and at least one output device.
- `AudioRouterEngine.SetSourceDevice()` and `SetOutputDevices()` currently throw if routing is already active.
- `SoundBridgeBridge.cpp` swallows native exceptions and returns `0` for failure.
- `AudioRouterEngine.RoutingLoop(...)` catches all exceptions and stops routing silently by setting state to false.

This means backend debugging will require better error reporting soon.

### Backend Gaps To Implement Next

Recommended backend tasks in order:

1. Add structured error reporting from native bridge to C#.

   Current C ABI returns only `0` or `1`, hiding the reason for failure. Add a last-error buffer export, e.g.:

   ```text
   SoundBridge_GetLastError(...)
   ```

2. Add per-output volume.

   Current GUI sends volume through:

   ```csharp
   IAudioRouterService.SetOutputVolume(string deviceId, int volume)
   ```

   But `NativeAudioRouterService.SetOutputVolume(...)` only stores/clamps placeholder values. Native `RenderStream` does not apply gain yet.

   Likely native implementation point:

   - `RenderStream::Push(...)` or `RenderStream::RenderLoop(...)`
   - `MultiRenderer` should own per-output state by endpoint ID.

3. Add per-output delay.

   Current GUI sends delay through:

   ```csharp
   IAudioRouterService.SetOutputDelayMs(string deviceId, int delayMs)
   ```

   But native delay queues are not implemented. The existing `RenderStream` queue can be extended, but delay should be applied deliberately to avoid drift and unbounded buffering.

4. Implement live routing update.

   Current GUI expects:

   - Active routing continues while user edits active template.
   - Calibration `Update Routing` applies new metadata.

   Current native engine does not support changing source/output devices while active.

   Possible approaches:

   - Stop/rebuild/restart routing internally on update.
   - Or implement dynamic add/remove/update in `MultiRenderer`.
   - For first milestone, stop/rebuild/restart may be acceptable if UX remains stable.

5. Decide source switching behavior during active routing.

   If user changes master source while routing active, backend must decide whether `Update Routing` restarts capture from the new source.

6. Add calibration/test audio behavior.

   Current `Play Test` plays `beep.wav` through normal app/system audio using `SoundPlayer`, not per selected output. A backend-level calibration tone could later route through selected outputs for real calibration.

7. Add runtime state synchronization.

   GUI currently tracks `IsRoutingStarted` in `CalibrationViewModel` and `IsRoutingActive` in `DeviceWorkspaceViewModel`. Backend should expose authoritative routing status and possibly route change events.

8. Add profile/template persistence.

   Current templates are in-memory only. Need decide persistence format and ownership.

### Important GUI Expectations For Backend

The backend should eventually support this contract:

- Enumerate devices quickly and safely.
- Select one source/master endpoint.
- Select multiple output endpoints.
- Start routing from source to all selected outputs.
- Stop routing.
- Update routing metadata while routing is active.
- Apply per-output volume changes.
- Apply per-output delay changes.
- Ideally expose current peak level and active output count.
- Fail with clear user-facing error messages.

Current GUI intentionally does not update live routing from template page controls. It only updates template metadata there. Live routing changes are committed from Calibration via `Update Routing`.

### Current Design Requirements To Preserve

- Native Windows title bar.
- No duplicate app name in sidebar.
- Devices category default on launch.
- Calibration disabled until an active multi-output template exists.
- Active template turns green in sidebar.
- Only one template active at a time.
- Aggregate devices are separate concept and should not enable Calibration yet.
- Dropdown lists show real device names.
- The closed master-device dropdown head initially shows `System-Default` only when the default/speaker source is selected by default.
- If user manually chooses another master device, the head shows the real device name.
- `Select` means save/activate template, not start routing.
- Calibration `OK` means start routing.
- Calibration `Disable` means stop routing.
- Calibration `Update Routing` means apply pending changes to active routing.

### Backend Development Starting Points

Start reading these files:

```text
app/Services/IAudioRouterService.cs
app/Services/NativeAudioRouterService.cs
app/Services/NativeAudioRouterInterop.cs
app/ViewModels/DeviceWorkspaceViewModel.cs
app/ViewModels/CalibrationViewModel.cs
native/SoundBridge.Native/src/bridge/SoundBridgeBridge.cpp
native/SoundBridge.Native/src/engine/AudioRouterEngine.h
native/SoundBridge.Native/src/engine/AudioRouterEngine.cpp
native/SoundBridge.Native/src/audio/MultiRenderer.h
native/SoundBridge.Native/src/audio/MultiRenderer.cpp
native/SoundBridge.Native/src/audio/RenderStream.h
native/SoundBridge.Native/src/audio/RenderStream.cpp
```

Do not start by rewriting the GUI. The GUI state machine now reflects the user-approved flow. Backend work should make native behavior match that flow.

### Current Verification Status

Latest verified commands:

```powershell
cmake --build native\SoundBridge.Native\build-integration --config Release
dotnet build app\SoundBridge.Gui.csproj
```

Both passed after the current GUI-stage routing workflow changes.

### Build Commands

Build `.exe` guide:

1. Build native backend DLL first:

```powershell
cmake --build native\SoundBridge.Native\build-integration --config Release
```

1. Build GUI Release:

```powershell
dotnet build app\SoundBridge.Gui.csproj -c Release
```

1. Run the generated exe:

```powershell
app\bin\Release\net8.0-windows\SoundBridge.Gui.exe
```

The Release folder already contains:

- `SoundBridge.Gui.exe`
- `SoundBridgeBridge.dll`

For a distributable folder later, use:

```powershell
dotnet publish app\SoundBridge.Gui.csproj -c Release -r win-x64 --self-contained true -o dist\SoundBridge
```

Then run:

```powershell
dist\SoundBridge\SoundBridge.Gui.exe
```

## 2026-06-21 Implementation Status Update

This section records the post-revert implementation pass where the app was
rebuilt up to the agreed tenth-point scope.

### Devices / Dashboard Merge

- The standalone Dashboard route was removed from navigation.
- `DashboardView.xaml`, `DashboardView.xaml.cs`, and `DashboardViewModel.cs`
  were deleted.
- Dashboard-style live routing information is now shown inside the default
  Devices page.
- The old Devices flowchart behavior was preserved:
  `Master -> SoundBridge -> Default Audio` still appears in muted state when no
  routing is active.
- The flowchart only switches to active/accented rendering when routing is
  active.
- `docs/devices-visual.md` now documents this rule explicitly so the old
  approved visual is not accidentally replaced by an empty state again.

### Runtime Guards

- Added single-instance guard in `App.xaml.cs`.
- A second app launch signals the existing instance to restore/focus and then
  exits.
- Added system tray support in `MainWindow.xaml.cs`.
- When `Minimize to system tray on close` is enabled, closing hides the window
  instead of exiting.
- Tray menu supports:
  - Open SoundBridge
  - Stop Routing
  - Exit
- Exit prompts when routing is active and stops routing before closing.

### Settings

- Removed the non-working theme selection UI for now.
- Added persisted settings under `%LocalAppData%\SoundBridge\settings.json`.
- `Start SoundBridge with Windows` now maps to the current user's Windows Run
  registry key.
- `Minimize to system tray on close` now persists across app restarts.

### MSI / Installer

- MSI build was verified with:

```powershell
.\installer\build-msi.bat
```

- Verified MSI output:

```text
dist\installer\SoundBridgeSetup.msi
```

- Installer now uses `app\assets\icon.ico` for Add/Remove Programs through
  `ARPPRODUCTICON`.
- `CREATE_DESKTOP_SHORTCUT` is marked secure so the checkbox value reaches the
  execute phase.
- `MajorUpgrade` allows same-version upgrades for local testing.
- A virtual audio device prerequisite notice dialog was added.
- The existing install-scope and desktop-shortcut dialogs remain in the MSI UI.
- Final release builds should still bump `app.meta.json -> product.version`
  before building a new MSI.

### Verified Commands

From:

```powershell
D:\Lucifer-Drive\Programs\Virtual Audio Drive Program\SoundBridge
```

Verified app build:

```powershell
dotnet build app\SoundBridge.Gui.csproj -c Release
```

Result:

```text
Build succeeded. 0 Warning(s), 0 Error(s).
```

Verified full installer build:

```powershell
.\installer\build-msi.bat
```

Result:

```text
MSI created: D:\Lucifer-Drive\Programs\Virtual Audio Drive Program\SoundBridge\dist\installer\SoundBridgeSetup.msi
```
