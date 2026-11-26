# Devices Visual Implementation Plan

Last updated: 2026-06-21
Status: Implementation in progress

## Decision

The `Devices` page becomes the single live-status/default page.

Dashboard is removed as a separate page because its status role is absorbed by
Devices. This removes a duplicated navigation choice and avoids the Dashboard
crash path seen in the installed build.

## Non-Negotiable Visual Rule

The old default flowchart stays.

When no multi-output template is active, Devices must still show:

```text
Master -> SoundBridge -> Default Audio
```

The default route is shown muted/disabled:

- muted grey icons/text/lines
- no glowing dots
- no active animation
- no replacement blank/empty canvas

This keeps the polished visual identity we built earlier.

## Devices Page Layout

Top to bottom:

```text
Page title
Master device row
Sample rate row
Description text
Flowchart panel
Console status section
```

The older standalone delay table is replaced by the console output tiles, but
its useful information is preserved there.

## Header Rows

Preserve the old header rows:

- Page title: `Multi-Output Routing` or `Aggregate Routing`
- `Master device:` read-only box
- `Sample Rate:` compact read-only value with `kHz`
- Description line:
  `Audio Routing from Master Device to Multi Peripherials`

Editing still happens on nested template pages and Calibration, not on the
default Devices page.

## Flowchart

Use the existing `DevicesFlowchartControl`.

Keep:

- icon-first nodes
- no rectangle wrapper around flowchart icons
- 3-column layout
- direct source-to-router line
- Bezier router-to-output curves
- 14-character node label truncation
- full node name tooltip
- glowing dots when active
- muted default route when idle/no active template

Do not rebuild the flowchart from scratch.

## Console Status Section

The console section sits below the flowchart.

When routing is inactive:

- starts collapsed
- shows slim header row:
  `Console status` and `Not routing`
- can be manually expanded
- expanded idle content is dimmed
- output tiles show device name and delay only
- Start routing button remains available

When routing is active:

- always expanded
- cannot be collapsed
- shows status row, master meter, active output tiles, and Stop routing button

Output tiles use live data from the active template. No placeholder device names
are allowed in implementation.

## Dashboard Removal

Remove all source-level Dashboard references:

- `DashboardView.xaml`
- `DashboardView.xaml.cs`
- `DashboardViewModel.cs`
- Dashboard data template in `App.xaml`
- Dashboard sidebar item in `MainWindow.xaml`
- Dashboard navigation item in `MainViewModel.cs`

Generated files under `bin/` and `obj/` are ignored; they will refresh on build.

## Tray Flyout

The tray single-click flyout is still future work. It should be implemented
after the Devices/Dashboard merge is stable.

-----------------------------------------------------------------------------

### New visual (only related to Devices-default page)

## 1. Context

The Devices page becomes the single default page. It absorbs the live
status content originally planned for a separate Dashboard page
(flowchart + console-style meters). The accordion template-browser idea
was considered and dropped.

Rationale: Devices page no longer needs to be a "management" surface once
the accordion idea was dropped — template creation/switching already
lives in the sidebar's nested `+` tree. With Devices reduced to pure
status, it can absorb the live-status role cleanly without becoming two
different kinds of page at once.

---

## 1.1 — IMPORTANT: All Device Names In This Document Are Placeholders

Every device name used as an example throughout this plan — "Sony
WH-1000XM5", "USB Headset Pro", "Realtek HD Audio", "SoundBridge Virtual
Device", etc. — is **illustrative only**, copied from earlier mockup
screens for layout reference. None of these are to be hardcoded anywhere
in the implementation.

**Every device name, count, status dot, and meter value shown in the
implemented UI must come from live data:**

- Output tiles (§2.4 Row 3) — populate from the *actual* output devices
  configured in the currently active template, sourced through
  `IAudioRouterService` (which in turn calls
  `NativeAudioRouterService.GetRenderDevices()` /
  `SoundBridge_EnumerateRenderDevices` under the hood). Never a static
  list.
- Master source name (§2.4 Row 1) — the real `friendlyName` of whichever
  device is configured as that template's source, not a placeholder
  string.
- Flowchart nodes (§2.3) — same; node labels are the real enumerated
  device names from the active template.

If at any point during implementation no real device data is available
(e.g. template references a device ID that's no longer connected), fall
back to the empty/error states defined in this document — never to a
sample device name.

---

## 1.2 — Reconciling With the Already-Implemented Flowchart

A separate handoff doc ("SoundBridge Old Devices Default Page Visual
Design") describes the flowchart as it already exists in the shipped app,
built by `DevicesFlowchartControl.cs`. That implementation is more
detailed and more polished than the generic version originally sketched
in §2 below, and this plan now defers to it. The facts that follow
**override** any conflicting earlier description in this document and are
treated as already built — not new work:

- **Node design is icon-first, text-second, with no rectangle wrapper.**
  Each node is a large icon, device/router name below it, a small
  subtitle below that. Earlier rectangle wrappers around node icons were
  deliberately removed so the icons "breathe" visually. Do not
  reintroduce boxed/card-style nodes around flowchart icons.
- **Layout is a fixed 3-column structure**: Master Device column,
  SoundBridge column, Output Devices column — not a freeform diagram.
- **Connection lines differ by segment.** Source-to-router is a direct
  line. Router-to-output connections use cubic Bezier curves (a soft
  S-curve with horizontal tension), not straight or dashed lines.
- **Active-state animation is glowing dots traveling along the output
  paths**, not a dashed "marching ants" stroke. This is the existing,
  correct animation.
- **Device names are truncated to 14 characters** in the node label, with
  the full name available via tooltip on hover.
- **Output node vertical placement is dynamic** — when multiple outputs
  are active, they distribute vertically in the output column to avoid
  overlap and stay readable as count changes.
- **The muted default/idle state already matches this plan's intent**:
  `Master → SoundBridge → Default Audio`, all muted grey (lines, text,
  icons), no glow, no animation. Critically, this is a **single straight
  connector line** all the way through — one line Master→SoundBridge, one
  line SoundBridge→Default Audio. It must **not** render as multiple
  branching paths fanning out from SoundBridge while idle, even faintly.
  Branching paths are reserved exclusively for the active state, where
  each real output device gets its own curve. A common implementation
  mistake is drawing all potential output branches at low opacity even
  when idle — avoid this; the idle state has exactly one visible path.
  §2.3 below aligns terminology with this existing behavior rather than
  introducing a new one.
- **The page title already swaps based on active template type**:
  "Multi-Output Routing" or "Aggregate Routing", defaulting to
  "Multi-Output Routing" when nothing is active. This sits above the
  flowchart and must be preserved in the merged layout (§2.2).
- **A Master device row, Sample rate row, and short description line**
  already exist above the flowchart panel, all read-only, all sourced
  from live device data. These are preserved in the merged layout (§2.2)
  — this plan does not replace them, only repositions them alongside the
  new Console Status section below.

Net effect: very little of `DevicesFlowchartControl.cs` needs to change
for this plan. The primary new work is everything in §2.4 (Console
Status) plus repositioning existing elements — not rebuilding the
flowchart's visual design.

---

## 2. Devices Default Page — New Layout

### 2.1 Structure (top to bottom)

```
┌─────────────────────────────────────────────────────────┐
│  Header Rows (existing, preserved)                       │
│  Page title · Master device row · Sample rate row ·      │
│  Description text                                        │
└─────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────┐
│  Section A — Flowchart Panel                              │
│  (existing DevicesFlowchartControl — unchanged design,    │
│  just repositioned above the new Section B)               │
└─────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────┐
│  Section B — Console Status                               │
│  Status row · Master meter · Output tiles (status-only)   │
│  (NEW — absorbs the old Output Delay Table, see §2.4)     │
└─────────────────────────────────────────────────────────┘
```

No accordion template-browser. No standalone delay table beneath the
flowchart anymore (absorbed into Section B, §2.4). Template creation and
switching remain in the sidebar's nested `+` expansion under Devices
(already implemented, unchanged by this plan).

### 2.2 Header Rows (existing — preserved as-is)

These already exist above the flowchart panel and carry over unchanged
into the merged page. All rows are **read-only** — editing happens on the
nested template page / Calibration, never here, consistent with §1.1.

**Page title** — swaps based on the active template's type:

```
Multi-Output Routing
Aggregate Routing
```

Defaults to "Multi-Output Routing" when no template is active.

**Master device row**

```
Master device:  [ Virtual Device (default)            ]
```

Rounded dark input-style box, read-only. Shows the real source device's
`friendlyName` (or "System-Default" / similar fallback) — never a
hardcoded example name, per §1.1.

**Sample rate row**

```
Sample Rate:  [ 48 | kHz ]
```

Number and `kHz` suffix visually separated inside a compact rounded box.
Reflects the real source device's actual sample rate from its
`WAVEFORMATEX`, not a static value.

**Description text**
A single muted helper line beneath the two rows above, describing the
routing concept in plain language (existing copy, unchanged by this
plan).

### 2.3 Section A — Flowchart Panel

**Source:** `app\Views\DevicesFlowchartControl.cs` — this is the
already-implemented, polished design described in full in §1.2. This
plan does **not** redesign it. The only change from current behavior is
positional: it now sits directly above the new Section B instead of
standing alone with a separate delay table beneath it.

Panel container: rounded dark/glass panel, `PanelAltColor` background,
`StrokeColor` border, ~280px height, 16px padding — existing styling,
unchanged.

Recap of the design (full detail in §1.2 — do not re-derive a different
version of this from scratch):

- 3-column layout: Master Device → SoundBridge → Output Devices.
- Icon-first nodes, no rectangle wrapper around icons.
- Direct line source→router; cubic Bezier curves router→each output.
- Active state: glowing animated dots travel along output paths,
  accent cyan/blue connection lines, readable white/accent node text.
- Idle/no-template state: muted three-node flow,
  `Master → SoundBridge → Default Audio`, all grey, no glow, no
  animation — rendered as a **single straight connector line** the
  whole way through, not multiple faint branches. This is the existing
  default behavior, already correct, nothing new to build here.
- Active state only: SoundBridge→output connections split into one
  Bezier-curve branch per real output device, each carrying its own
  traveling glow dot. The split only ever appears once routing is
  actually active — never shown (even faintly) while idle.
- Device names truncated to 14 characters with full name on tooltip.
- Multiple active outputs distribute vertically to avoid overlap.

Supporting empty-state text beneath the panel (existing, unchanged):

```
"No active routing template"
"Select a template from the sidebar to begin"
```

### 2.4 Section B — Console Status

Directly below the flowchart panel, separated by a thin horizontal divider
(`StrokeColor`, 0.5px). This section is **new** — it replaces the old
standalone Output Delay Table that used to sit beneath the flowchart.

**Reconciling with the old Output Delay Table:** the previous
implementation had a simple read-only table below the flowchart —
columns `Peripheral` and `Drift audio`, one row per configured
output, always visible regardless of routing state. That table's
*information* (device name + delay) is preserved here, but it now lives
inside this accordion rather than as a permanent fixture, and gains live
volume/level data when routing is active. Concretely:

- **Idle, manually expanded** (accordion behavior below): tiles show
  **every output configured in the active template** (not just ones
  currently routing — there's nothing routing yet), each showing just the
  device name and its configured delay value, matching the old table's
  content exactly. No meter, no volume — that data doesn't exist without
  live routing.
- **Active/routing**: tiles upgrade to the full live version — name,
  status dot, mini meter, volume %, and delay — as already specified in
  Row 3 below.

This means Section B fully subsumes the old delay table; there is no
separate table anywhere else on the page.

**Accordion behavior based on routing state:**

Section B is itself a collapsible accordion, not a fixed-height block.
Its default (collapsed/expanded) state depends on `IsRoutingActive`:

- **When `IsRoutingActive == false`:** Section B starts **collapsed** —
  shown as a single slim header row only:

  ```
  ▸  Console status                    Not routing
  ```

  - Chevron (`▸`), label "Console status", and a right-aligned muted
    "Not routing" tag — all in `MutedTextColor`.
  - The user **can still manually expand** this collapsed header (click
    anywhere on the row, or the chevron specifically) to inspect the
    console layout. When expanded in this idle state, all of Section B's
    inner content (status row, master meter, output tiles) renders
    using its **disabled visual treatment**:
    - Meters show empty/zero tracks only — no fill, no animation, no
      jitter.
    - Output tiles render per the "Idle, manually expanded" rule above —
      name + delay only, dimmed/greyed (40% opacity), no meter/volume.
    - There is no action button anywhere in this view (see "No action
      button" note below Row 3) — this expand-while-disabled view exists
      purely so the user can preview the configured outputs and delays
      without leaving the page, not to trigger routing. Starting routing
      happens on Calibration.
  - Collapsed is the **default** on every page load / navigation back to
    Devices while idle — any manual expand by the user does not persist
    across navigation away and back; it resets to collapsed each time
    `IsRoutingActive` is false at page-load.

- **When `IsRoutingActive == true`:** Section B is **always expanded**,
  cannot be manually collapsed by the user. The chevron/header row is
  hidden entirely (no accordion chrome shown at all while routing) and the
  full live console content (Rows 1–3 below) renders directly, exactly as
  originally specified — live meters, real tiles, jitter animation,
  everything active.

- **Transition:** when `IsRoutingActive` flips from false → true (e.g. the
  user started routing from Calibration and then navigates back to
  Devices), Section B animates open if it was collapsed (smooth
  height/opacity expand, ~200ms) and immediately begins showing live data
  — no separate "loading" state needed since `PeakLevel`/tile data is
  already being published the moment routing starts. The reverse
  (true → false) does not auto-collapse immediately — Section B stays
  expanded showing the now-idle/disabled content for that session, only
  collapsing back to the slim header on the next page navigation/load.

The detailed row-by-row content below (Rows 1–3) describes the
**expanded, live/active** rendering. The collapsed and
expanded-but-disabled states are layout variations of the same section,
not separate components.

**Row 1 — Status line**

```
[●] Routing active          Source: <template's master device name>
```

- Status dot: `SuccessColor` (#4ADE80) filled circle, 8px, when active.
  `MutedTextColor` (#AAAAAA) when idle.
- Label text: "Routing active" in `SuccessColor` when active,
  "Not routing" in `MutedTextColor` when idle.
- Source name right-aligned, `MutedTextColor`, 13px.

**Row 2 — Master level meter**

```
Master output
[████████████░░░░░░░░░░░░░░░░░░░░]
```

- Label: 13px, `MutedTextColor`.
- Bar: 14px tall, track = `PanelAltColor`, fill = `AccentColor` (#60CDFF),
  rounded `border-radius: 4px` (WPF: `CornerRadius="4"` on the fill Border).
- Bound to `IAudioRouterService.PeakLevel`, refreshed on a 100ms
  `DispatcherTimer` (already specified in architecture.md's threading model
  — reuse, do not create a second timer).
- When idle: bar shows empty track only, no fill, no animation.

**Row 3 — Output tiles**
Grid of cards, `repeat(auto-fit, minmax(150px, 1fr))` equivalent in WPF
(`UniformGrid` with dynamic columns, or `ItemsControl` + `WrapPanel`).
One tile per *active* output device only (disabled/unselected outputs in
the template are not shown here — this section is live state, not
configuration).

Each tile:

```
┌─────────────────────────┐
│ Sony WH-1000XM5      [●]│
│ [▓▓▓▓░░░░░░░░░░░░░░░░]   │
│ 80% vol · +0ms           │
└─────────────────────────┘
```

- Card background: `PanelColor`, border: `StrokeColor` 0.5px,
  `border-radius: 8px`.
- Device name: 13px, 500 weight, `TextColor`.
- Status dot: 7px, `SuccessColor` when that specific device is actively
  receiving audio, `MutedTextColor` if it failed to open.
- Mini meter: 6px tall, same track/fill colors as master meter but smaller.
  This can show the same peak level as master (per-output peak metering is
  not currently exposed by the backend — see §2.5 note) or remain a static
  representation of configured volume until that capability exists.
- Subtext: volume % and delay ms, 11px, `MutedTextColor`.

**No action button.** Section B is status-only and contains no Stop/Start
control. Routing lifecycle (start/stop) is controlled exclusively from
the Calibration page's OK/Disable buttons, consistent with the page
purpose established earlier in this project: Devices is read-only,
Calibration is where changes and routing actions happen. Three rows
only — status line, master meter, output tiles — both in the live
expanded state and in the idle expanded-disabled state described above.

### 2.5 Data Binding Notes

- All of Section B binds to the existing `IAudioRouterService` surface:
  `IsRoutingActive`, `PeakLevel`, `ActiveOutputCount`, plus the currently
  active template's output list (already available via whatever exposes
  the active template to `CalibrationViewModel` today — reuse that source,
  do not duplicate template-loading logic in a new ViewModel).
- **Known gap:** per-output live peak level is not currently exposed by
  `SoundBridgeBridge.dll` (only aggregate `GetPeakLevel()` exists). Until
  a `SoundBridge_GetOutputPeakLevel(deviceId)` export is added natively,
  per-tile meters should either mirror the master level or render as a
  static bar reflecting configured volume. Do not block this UI work on
  that backend addition — ship with the fallback, upgrade later.
- **Open decision — Test Audio button:** the old Devices page had a
  bottom-right "Test Audio" button and a muted status text line
  (`Ready` / `Scanned 4 devices` / `Routing active` / etc.). The current
  Calibration page already owns test-tone playback
  (`PlayCalibrationBeeps()`) and a status surface. To avoid duplicating
  the same action in two places, this plan's default recommendation is:
  **do not** re-add a Test Audio button to the merged Devices page —
  Section B's header tag (`"Not routing"` / `"Routing active"`) already
  covers the old status-text role. Revisit only if user testing shows
  people expect a quick-test affordance without leaving Devices.

### 2.6 Files Touched

```
app\Views\DevicesView.xaml              — restructure: header rows (existing,
                                           repositioned) + flowchart (existing,
                                           repositioned) + new Section B,
                                           console section wrapped in collapsible
                                           accordion container (collapsed/expanded
                                           state per §2.4)
app\Views\DevicesFlowchartControl.cs    — no design changes (see §1.2); confirm
                                           it still renders correctly in its new
                                           position above Section B instead of
                                           above the old standalone delay table
app\ViewModels\DevicesViewModel.cs      — absorb status properties (IsRoutingActive,
                                           PeakLevel, ActiveOutputTiles, etc.) plus
                                           IsConsoleExpanded state (manual toggle
                                           while idle, forced true while routing,
                                           reset to collapsed on page load per §2.4);
                                           existing title-swap, master device,
                                           sample rate, and description properties
                                           are unchanged
app\Views\Controls\OutputTileControl.xaml (new) — reusable tile; must support both
                                           the idle "name + delay only" rendering
                                           (replacing the old delay table rows)
                                           and the active "name + meter + volume +
                                           delay" rendering, used here and
                                           optionally reused in Calibration page
```

---

## 3. Style Reference — App Palette Applied

All colors below are sourced from the existing `app\App.xaml` resource
dictionary (`Project.md` §"Current Theme Palette") — no new colors are
introduced by this plan.

| Element | Color resource | Hex |
| --- | --- | --- |
| Page background | `BgColor` | `#FF000000` |
| Section card background | `PanelColor` | `#F21E1E1E` |
| Meter track | `PanelAltColor` | `#0FFFFFFF` |
| Meter fill / accent | `AccentColor` | `#FF60CDFF` |
| Active status / success | `SuccessColor` | `#FF4ADE80` |
| Stop button / danger | `DangerColor` | `#FFF87171` |
| Card border / divider | `StrokeColor` | `#1AFFFFFF` |
| Primary text | `TextColor` | `#FFFFFFFF` |
| Secondary/muted text | `MutedTextColor` | `#FFAAAAAA` |

Typography: follow existing app conventions (no new font sizes beyond what
DevicesView/CalibrationView already use). Tile/card title ≈13px/500 weight,
subtext ≈11px/400 weight, matching the existing Calibration page's volume
slider labels for visual consistency.

Spacing: 12px gap between output tiles, 16–20px section padding, 0.5px
dividers — consistent with current `PanelColor` card usage elsewhere in
the app (Calibration page's template summary card is the closest existing
reference to copy spacing values from).

---

## 4. Implementation Order (Devices-page-relevant steps only)

```
1. Build OutputTileControl.xaml reusable tile, both its
   idle (name+delay) and active (name+meter+volume+delay)
   render modes (§2.4)
2. Restructure DevicesView.xaml: keep existing header rows
   and flowchart in place, add new Section B below them,
   wire to DevicesViewModel (§2.2, §2.3, §2.4, §2.5)
3. Confirm DevicesFlowchartControl renders correctly in its
   new position (no design changes needed — see §1.2);
   this is a verification pass, not new build work
4. Add idle-expanded accordion state to Section B
5. Manual pass: toggle routing on/off repeatedly, confirm
   the Devices page stays in sync with IsRoutingActive
   without manual refresh
```

Full sequence (including Dashboard removal and tray work) lives in the
master doc, `devices-visual.md` §7.

---

## 5. Open Items (Devices-page-relevant only)

- Per-output live peak metering (§2.5) — backend export not yet added.
  Ship with fallback (static bar or mirrored master level), revisit once
  `SoundBridge_GetOutputPeakLevel` exists natively.
- Test Audio button placement (§2.5) — recommendation is to not duplicate
  it on Devices since Calibration already owns it; confirm this doesn't
  regress any existing user expectation before removing it if it's
  currently still present on the live Devices page.
