# GUI Fix Notes

## Current issues

- The GUI shell still looks like a floating dialog on a decorative canvas. The app should be the window itself, not a dialog inside another surface.
- The title bar already shows the app name, so the sidebar should not repeat product branding or explanatory header text.
- Category switching is unstable. The current navigation model stores live `UserControl` instances in the view model, which mixes UI ownership into state and can make page switching fragile.
- The backend bridge is intentionally not connected yet; current work is only for the visual layer and interaction stability.

## Fix direction

- Make the root window the app surface and remove the patterned outer canvas.
- Keep branding in the title bar only.
- Keep page views hosted persistently in the shell and switch visibility instead of recreating or moving controls through navigation state.
- Keep the device page compact, rounded, and control-focused.
