# SoundBridge Installation, Persistence, and Runtime Process Notes

## Current Context

SoundBridge is currently a WPF GUI app backed by a native WASAPI routing DLL.
The GUI can create multi-output or aggregate template pages at runtime, but those
created templates currently live only in memory. If the app closes, the created
templates are lost.

The next production step is to add persistence so a user can install the app,
create routing templates, close the app, reopen it, and continue from the saved
template state.

## Template Persistence Goal

Saved templates should survive:(correction not saved instead created templates should survive)

- Normal app close and reopen.
- Windows restart.
- App update, as long as the configuration schema is compatible.
- Routing being disabled and re-enabled.

Saved templates should not require:

- The native backend to be running before the GUI opens.
- Admin permission for ordinary edits.
- Writing into the install directory.

## Recommended Storage Location

Use the current Windows user profile, not the application install folder.

Recommended path:

```text
%LocalAppData%\SoundBridge\
  templates.json
  templates.backup.json
  logs\
    app.log
  corrupted\
```

Expanded example:

```text
C:\Users\<User>\AppData\Local\SoundBridge\templates.json
```

Why Local AppData:

- Normal desktop apps can write there without admin.
- It survives app reinstall/update.
- It is per-user, which matches Windows audio device defaults.
- Audio endpoint IDs are machine-local and should not roam to another PC.
- It avoids writing into `Program Files`, which is usually protected.

If machine-local configuration is needed later, use:

```text
%ProgramData%\SoundBridge\
```

But template drafts should stay per-user unless there is a strong reason to make
them machine-wide.

## Storage Format Decision

Use JSON for v1 template persistence because the amount of metadata is small and
the app does not store secrets in the template file.

The chosen v1 layout is:

```text
%LocalAppData%\SoundBridge\
  templates.json          <- source of truth for created templates
  templates.backup.json   <- last known-good backup
  logs\
    app.log               <- template change and recovery log
  corrupted\
    *.bad.json            <- quarantined corrupted files
```

Important rule:

- `templates.json` is the source of truth.
- `templates.backup.json` is recovery fallback.
- `logs/app.log` is not the source of truth; it is only for diagnostics,
  inspection, and recovery notes.

JSON is acceptable here only if we are disciplined:

- Store it in `%LocalAppData%`, not the install folder.
- Write updates atomically using a temporary file and replace operation.
- Keep a backup before replacing the source file.
- Include a `schemaVersion`.
- Validate loaded values before applying them to routing.
- Treat imported or edited JSON as untrusted.
- Never store secrets in the template JSON.

Recommended folder ACL rule:

- Create `%LocalAppData%\SoundBridge`.
- Let Windows inherit normal per-user profile ACLs.
- Do not grant broad `Everyone` or machine-wide write permissions.
- Do not store config in the install directory.

If we later store secrets, tokens, license keys, or privileged service material,
do not store them directly in JSON. Use Windows DPAPI or Windows Credential
Manager.

## Template Data Model

The persisted template should be represented by storage DTOs, separate from WPF
view-models.

Recommended conceptual shape:

```json
{
  "schemaVersion": 1,
  "templates": [
    {
      "id": "multi-output-1",
      "name": "multi-output device 1",
      "type": "Multi-output",
      "isSelected": true,
      "isRoutingActiveOnLastExit": false,
      "sourceDeviceId": "{Windows endpoint id}",
      "sourceDeviceName": "SoundBridge Virtual Device",
      "sampleRateHz": 48000,
      "outputs": [
        {
          "deviceId": "{Windows endpoint id}",
          "deviceName": "Speakers (Realtek Audio)",
          "isEnabled": true,
          "volume": 80,
          "delayMs": 0
        }
      ],
      "createdAtUtc": "2026-06-18T00:00:00Z",
      "updatedAtUtc": "2026-06-18T00:00:00Z"
    }
  ]
}
```

Keep both device ID and friendly name:

- Device ID is needed to reconnect to the real endpoint.
- Friendly name is useful if the device is missing after reboot, unplug, or
  Bluetooth disconnect.

## Loading Behavior

On app startup:

1. Load `%LocalAppData%\SoundBridge\templates.json`.
2. Scan current Windows render devices.
3. Recreate sidebar nested template items.
4. Match saved `sourceDeviceId` and output `deviceId` values against current
   devices.
5. If a saved device is missing, keep the template but mark that device as
   unavailable in the UI.
6. Do not auto-start routing by default in v1.

Recovery behavior:

1. If `templates.json` loads, use it.
2. If `templates.json` fails, try `templates.backup.json`.
3. If backup loads, restore backup into `templates.json` and continue.
4. If both fail, read whatever raw text can be read from both files.
5. Write failure details and raw snippets to `logs/app.log`.
6. Move bad files to `corrupted\` with a timestamped `.bad.json` suffix.
7. Start with an empty template list.

Do not permanently delete corrupted files immediately. Quarantine them first so
we can inspect what went wrong.

Recommended v1 rule:

- Restore templates and selected template state.
- Do not automatically start audio routing on app launch.
- Require the user to go to Calibration and press `OK`.

Reason:

- Auto-switching the Windows default output device on startup can surprise the
  user.
- Bluetooth/USB devices may not be connected yet.
- Native routing failure on startup would make the app feel unstable.

Later, a setting can be added:

```text
Start last active routing automatically on app launch
```

This should be off by default.

## Saving Behavior

Save template state when:

- User creates a multi-output or aggregate device.
- User renames a template, if renaming is added.
- User changes master/source device.
- User enables/disables output devices.
- User changes output volume.
- User changes output delay.
- User clicks Apply Configuration.
- User selects/deselects a template.
- Routing update succeeds.

Implementation recommendation:

- Use a small `TemplateStore` service.
- Back it with JSON files for v1.
- Debounce frequent changes like sliders or delay steppers.
- Save immediately for structural changes like create/delete/select.
- Validate every loaded value before applying it to the live routing engine.
- Write a small line to `logs/app.log` before saving template changes.

Avoid writing on every slider tick without debounce.

Safe save sequence:

1. Log the intended change to `logs/app.log`.
2. Validate the new in-memory template state.
3. Copy current `templates.json` to `templates.backup.json` if it exists.
4. Write the new state to `templates.tmp`.
5. Replace `templates.json` with `templates.tmp`.
6. If replace fails, keep the backup and log the failure.

## Draft vs Global Routing State

The GUI currently separates these concepts:

- Template draft/edit state.
- Calibration active template state.
- Global/routing-active state shown on the default Devices page.

Recommended persistence rule:

- Save template draft edits.
- Save which template is selected.
- Save whether routing was active on last exit as metadata only.
- Do not treat saved `isRoutingActiveOnLastExit` as proof that routing is active
  on next launch.

On startup, backend state should be queried separately:

```text
IAudioRouterService.IsRoutingActive
```

The backend should remain the authority for live routing state.

## Installation Process

The installed product name is:

```text
SoundBridge
```

The final app package has two app-owned pieces:

1. WPF GUI executable.
2. Native routing DLL.

The virtual audio device is a prerequisite, but SoundBridge MSI v1 will not
install it automatically. The user is responsible for downloading and installing
the virtual device separately.

MSI-only packaging decision:

- Use MSI for installation.
- Do not use MSIX for v1.
- Do not use Inno Setup for v1.
- Do not bundle or install the virtual audio driver/device in the SoundBridge
  MSI.

Recommended MSI user flow:

1. User launches `SoundBridgeSetup.msi`.
2. Installer shows a prerequisite notice:

   ```text
   SoundBridge requires a virtual audio device to route system audio.
   Please install the recommended virtual audio device before using routing.

   This installer will continue with SoundBridge app installation only.
   ```

3. User clicks `OK` or `Continue`.
4. MSI installs SoundBridge app files.
5. User launches SoundBridge normally.
6. On first launch, the app scans render devices.
7. If the virtual device is missing, the app shows a clear setup status and
   repair/help action.

The prerequisite dialog is informational. It must not block app installation
forever, because the user may choose to install the virtual device later.

Recommended install flow:

1. Install app files.
2. Show the virtual-device prerequisite notice in the MSI UI.
3. Launch app normally as the user.
4. On first launch, scan audio devices.
5. If the virtual device is missing, show a clear setup status and repair action.

Recommended install scope:

- Prefer per-machine install under `Program Files\SoundBridge` for v1 MSI.
- User-created templates remain per-user under `%LocalAppData%\SoundBridge`.
- MSI uninstall should remove installed app files.
- MSI uninstall should not delete `%LocalAppData%\SoundBridge` by default,
  because it contains user-created templates.

## MSI Build Creation Plan

Use WiX Toolset or an equivalent MSI authoring workflow.

Target installer output:

```text
dist\installer\SoundBridgeSetup.msi
```

Published app input folder:

```text
dist\SoundBridge\
```

Expected installed files:

```text
Program Files\SoundBridge\
  SoundBridge.exe
  SoundBridge.dll
  SoundBridge.deps.json
  SoundBridge.runtimeconfig.json
  SoundBridgeBridge.dll
  asset\
    beep.wav
```

If self-contained publish is used, the install folder will also include the .NET
runtime files generated by `dotnet publish`.

MSI should create:

- Start Menu shortcut named `SoundBridge`.
- Optional Desktop shortcut named `SoundBridge`.
- Add/Remove Programs entry named `SoundBridge`.
- Publisher name once we decide one.
- Product icon once `app\assets\icon.ico` is available.

MSI should not:

- Install the virtual audio driver in v1.
- Start routing automatically.
- Change Windows default output during installation.
- Delete user template data on uninstall.

MSI prerequisite notice:

- Add a UI dialog before the install confirmation step.
- Message tells the user a virtual audio device is required.
- Button continues the SoundBridge app installation.
- Later, this dialog can include a link to the recommended virtual device
  download page.

Implementation order:

1. Rename external app output to `SoundBridge.exe`.
2. Add icon file at `app\assets\icon.ico`.
3. Enable `<ApplicationIcon>assets\icon.ico</ApplicationIcon>`.
4. Build native backend Release.
5. Publish WPF app self-contained to `dist\SoundBridge`.
6. Add WiX/MSI project under `installer\`.
7. Harvest or explicitly list files from `dist\SoundBridge`.
8. Add Start Menu shortcut.
9. Add prerequisite notice dialog.
10. Build `SoundBridgeSetup.msi`.
11. Test install, launch, uninstall, and reinstall.
12. Verify templates persist across app reinstall.

## Handling the Native Backend Process

Current architecture loads the native bridge DLL in-process from WPF:

```text
SoundBridge.exe -> SoundBridgeBridge.dll -> native WASAPI engine
```

This is simple and good for the current stage.

Benefits:

- No separate service process to manage.
- Easier debugging.
- Direct P/Invoke calls.
- Lower operational complexity.

Risks:

- Native crash can crash the GUI.
- Long-running audio engine shares lifetime with the GUI.
- Future background routing while GUI is closed is not possible.

For v1, in-process native DLL is acceptable.

For a later production build, consider a separate background process or Windows
service only if SoundBridge must keep routing after the GUI closes.

Possible future architecture:

```text
SoundBridge.exe
    -> local IPC
SoundBridge.Engine.exe or SoundBridge.Service
    -> SoundBridgeBridge.dll / native engine
```

Use this only when needed. It adds complexity around IPC, permissions, crash
recovery, service installation, and updates.

## Default Output Switching

The GUI currently switches Windows default output to the selected master/source
device when routing starts.

Recommended behavior:

- Switch default output only when the user starts routing.
- Do not switch default output just because a template is selected.
- Consider restoring the previous default output when routing is disabled, but
  only after asking the user or adding a setting.

Potential setting:

```text
Restore previous Windows output device when routing stops
```

This should probably default to on, but only after we implement reliable previous
device tracking.

## Updates and Schema Migration

The template file should include schema metadata:

```json
{
  "schemaVersion": 1
}
```

When the app starts:

- If schema is current, load normally.
- If schema is older, migrate.
- If schema is newer, keep a backup and load what can be safely read.

Recommended backup behavior:

```text
templates.backup.json
```

Create a backup before migration.

## Missing Device Handling

Audio device IDs can change when:

- Driver is reinstalled.
- Bluetooth device is re-paired.
- USB device is moved or reinstalled.
- Windows audio stack changes device identity.

When a saved device ID is missing:

- Keep the row in the template.
- Show it as unavailable.
- Let the user remove or remap it.
- Do not silently delete it.

Later enhancement:

- Try fuzzy matching by friendly name and category.

## Implementation Plan

Suggested order:

1. Add DTO models for persisted templates.
2. Add `ITemplateStore`.
3. Add `JsonTemplateStore`.
4. Save templates into `%LocalAppData%\SoundBridge\templates.json`.
5. Add `templates.backup.json` recovery behavior.
6. Add `logs/app.log` template-change and recovery logging.
7. Add corrupted file quarantine under `%LocalAppData%\SoundBridge\corrupted`.
8. Load templates during `MainViewModel` startup.
9. Recreate sidebar nested items from loaded templates.
10. Mark missing devices as unavailable.
11. Save on template create/select/apply/update.
12. Add a Settings page action: `Open config folder`.
13. Add installer packaging.

## App Icon Requirement

Before final Release packaging, the app should use a real `.ico` file.

Expected icon path:

```text
app\assets\icon.ico
```

Planned project setting:

```xml
<ApplicationIcon>assets\icon.ico</ApplicationIcon>
```

The icon file is not required to implement template persistence, but it should be
present before final `.exe` packaging. Until the icon is provided, keep the build
working without forcing the icon path.

## Build Commands

Current verified status as of 2026-06-21:

- `app\assets\icon.ico` is required and is passed into both the app and MSI.
- The MSI build script builds backend, publishes the GUI, then packages with WiX.
- The generated installer is versioned, for example `dist\installer\SoundBridgeSetup-v1.4.0.msi`.
- Before shipping a new update MSI, bump `product.version` in `app.meta.json`.
- `product.displayVersion` must match `v ` + `product.version`.
- `installer.upgradeCode` is fixed metadata and must not be changed after release.
- `installer.productCodeMode` remains `perVersion`.
- `installer.productCodes` stores one ProductCode per app version.
- `installer.packageCodeStrategy` remains `auto`.
- Installer scope is per-machine only to keep upgrades on one stable MSI path.
- Same-version upgrades are allowed for local testing, but final releases should still use a new version.

Run all commands from the repository root:

```text
D:\Lucifer-Drive\Programs\Virtual Audio Drive Program\SoundBridge
```

Configure the native backend build folder if it does not exist yet:

```powershell
cmake -S backend -B backend\build-integration -A x64
```

Build the native backend:

```powershell
cmake --build backend\build-integration --config Release
```

Build the WPF GUI in Release mode:

```powershell
dotnet build app\SoundBridge.Gui.csproj -c Release
```

Run the Release build:

```powershell
app\bin\Release\net8.0-windows\SoundBridge.exe
```

By default, the app uses the native audio backend and loads real Windows render
devices. To force demo/mock devices for UI-only testing:

```powershell
$env:SOUNDBRIDGE_USE_MOCK='1'
app\bin\Release\net8.0-windows\SoundBridge.exe
```

To return to real devices in the same terminal:

```powershell
Remove-Item Env:\SOUNDBRIDGE_USE_MOCK
```

If the build fails because `SoundBridge.exe` is locked, close the running
SoundBridge app and rerun:

```powershell
dotnet build app\SoundBridge.Gui.csproj -c Release
```

Publish a self-contained Windows x64 folder:

```powershell
dotnet publish app\SoundBridge.Gui.csproj -c Release -r win-x64 --self-contained true -o dist\SoundBridge
```

Run the published app:

```powershell
dist\SoundBridge\SoundBridge.exe
```

Build the full MSI package:

```powershell
.\installer\build-msi.bat
```

The script runs these steps:

1. Configure and build `backend\build-integration` in Release.
2. Publish the WPF app self-contained to `dist\SoundBridge`.
3. Build the WiX MSI from `installer\Package.wxs`.

Expected output:

```text
dist\installer\SoundBridgeSetup-v<version>.msi
```

The `<version>` portion comes from `product.displayVersion` in `app.meta.json`
with spaces removed. If `displayVersion` is missing, the script falls back to
`v` + `product.version`.

Installer behavior currently implemented:

- Product name is `SoundBridge`.
- Product metadata comes from `app.meta.json`.
- Start Menu shortcut is always created.
- Desktop shortcut is optional through the installer checkbox.
- Install scope is per-machine only.
- Privacy agreement is shown first.
- Virtual audio device notice is shown before install directory selection.
- Add/Remove Programs icon uses `app\assets\icon.ico`.
- Stable `UpgradeCode` is used.
- Same-version upgrades are allowed for testing.
- User data under `%LocalAppData%\SoundBridge` is not removed by MSI uninstall.

## Open Design Questions

- Should routing auto-start on launch if it was active on last exit?
- Should the app restore the previous Windows default output device when routing
  stops?
- Should templates be per-user only, or should machine-wide templates be
  supported later?
- Should the native engine remain in-process, or eventually move into a
  separate background service?
- Should aggregate devices share the same persistence schema as multi-output
  devices with a `type` discriminator?

## Current Recommendation

For the next development phase:

- Implement per-user JSON template persistence with backup, logging, atomic
  replace, and corrupted-file quarantine.
- Keep the native backend in-process.
- Do not auto-start routing on launch.
- Restore templates and selected state only.
- Continue making Calibration the explicit place where routing starts.
