# NOTICE

## Upstream

- Project: **RestoryQOL** — https://github.com/hykilpikonna/RestoryQOL
- Author: Azalea (GitHub `hykilpikonna`, Nexus Mods `azaneko`)
- License: MIT
- Base version used here: tag `1.0.1` / commit state of `main` on 2026-08-19

## Changes in this repository

Only these files differ from upstream:

| File | Change |
|---|---|
| `src/Core.cs` | All in-game menu labels, section headers and config descriptions translated to Simplified Chinese; CJK dynamic font (`Font.CreateDynamicFontFromOSFont`) wired into `GUI.skin` and the local `GUIStyle`s; window width 340 -> 380 to fit longer CJK strings; log strings localised |
| `src/Core.cs` (bug fix) | Window dragging. Upstream passed the screen-space `_windowRect` to `GUI.DragWindow`, but that parameter is window-local, so the drag area landed in the middle of the window and the title bar could not be dragged at all (dragging the body moved the whole window instead). Replaced with a plain 26 px title-bar drag rect and dropped the redundant manual `_dragging` tracking, which compared window-local mouse coords against a screen-space rect. |
| `src/mods/AutoScrew.cs` (bug fix) | Looping screwdriver sound after "hold Z / X". `ThreadedElementSFX` starts the tool's `ScrewingSound` (a looping FMOD event) on every `OnImmediateShortInteraction` and only stops the *previous* instance, so a batch of N instant screws in one frame left the last instance running forever. Now each actuated screw is timestamped and its private `KillShortSound()` / `KillHoldSound()` is invoked 0.45 s later (and flushed immediately on key release). |
| `src/Core.cs` (diagnostics) | Logs the window position once it settles after a drag, so "cannot drag" reports can be separated from "drag landed but position unchanged" |
| `src/RestoryQOL.csproj` | `PostBuild` uses MSBuild `Copy` instead of `EXEC COPY`/`cp` (the upstream variant fails with exit 1 whenever `GamePath` uses forward slashes); `-p:SkipPostCopy=true` skips touching the install |
| `README.md`, `AGENT.md`, `docs/`, `scripts/` | New — documentation and installer |
| `dist/RestoryQOL.dll` | Build artifact of the above (Release, netstandard2.1) |

Patch logic, hook targets and feature behaviour are **unchanged** from upstream.

## Not included

This repository does **not** contain any game files. Upstream keeps decompiled/stub game assemblies under `libs/Managed` and screenshots under `docs/`; those are derivative assets of the commercial game and are intentionally excluded here. To build, point `GamePath` at your own legitimate installation.

"ReStory: Chill Electronics Repairs" is a trademark of its publisher (tinyBuild) and developer (Mandragora). This is an unofficial fan modification, not affiliated with or endorsed by them.

## Third-party components referenced at install time

- **MelonLoader v0.7.3** (Apache-2.0) — downloaded from the official GitHub release by `scripts/install.ps1`; not vendored here.
- **HarmonyX** — bundled inside MelonLoader; not vendored here.
