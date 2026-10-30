# AGENT.md — machine-readable install contract

**Audience: AI agents / automation.** If you are an agent asked to "install the Chinese QoL mod for ReStory" (Steam app `3812600`), follow this file literally. Every step has a verification command; do not report success before running it.

## 0. Preconditions

- OS: Windows 10/11 x64 (the game is Windows-only).
- Game owned and installed via Steam. **Do not download the game.**
- PowerShell available (5.1 is fine). `dotnet` SDK 8+ only needed to rebuild from source, not to install the prebuilt DLL.

## 1. Resolve the game directory

Read Steam's library list; the game folder must contain `Restory.exe`:

- Registry keys: `HKCU\Software\Valve\Steam` → `SteamPath`; `HKLM\SOFTWARE\WOW6432Node\Valve\Steam` → `InstallPath`
- Then for each library root, parse `<root>\steamapps\libraryfolders.vdf` for `"path" "<dir>"` entries
- Candidate: `<library>\steamapps\common\Restory`

**Verify:** `Test-Path "<GAME>\Restory.exe"` is `True`. Also confirm `"appid" "3812600"` in `<library>\steamapps\appmanifest_3812600.acf`.

Stop and ask the human if not found. Never guess a path.

## 2. Install the loader (MelonLoader v0.7.3 x64)

Rationale: the game is **Unity 6000.3.10f1**; BepInEx 5 does not support Unity 6, and the `ReStory-BepInEx-Framework` upload on Nexus is a byte-for-byte repack of `BepInEx_win_x64_5.4.23.6` (642 KB) — it cannot work here. MelonLoader is what the mod author recommends.

```
https://github.com/LavaGang/MelonLoader/releases/download/v0.7.3/MelonLoader.x64.zip
```

Extract the archive **into `<GAME>`** (overwrite allowed). Result: `<GAME>\MelonLoader\` (~44 MB) and `<GAME>\version.dll` (~11 MB).

**Verify:** both paths exist.

## 3. Install the mod

Copy `dist/RestoryQOL.dll` to `<GAME>\Mods\RestoryQOL.dll` (create `Mods` if absent).

**Verify:** file exists; `(Get-Item ...).Length` > 50 KB.

## 4. Launch + verify (mandatory)

1. Launch **through Steam**, not by running `Restory.exe`: `Start-Process "steam://rungameid/3812600"`. A bare exe launch fails with `SteamAPI_Init() failed` and the game exits by itself.
2. Wait ~30-60 s, then read `<GAME>\MelonLoader\Latest.log`.

**Pass criteria — all three must appear:**

```
Melon Assembly loaded: '.\Mods\RestoryQOL.dll'
1 Mod loaded.
[RestoryQOL] RestoryQOL 已加载（中文界面版），按 F8 开关菜单。
```

3. Optional visual check: in game press `F8`; the window title is `维修物语 QoL 设置`. If Chinese renders as boxes, the dynamic font lookup failed (see §5).

If the log shows a patch failure (`[Xyz] ... failed`) or the game exits without the mod line, report the failure with the log excerpt. **Do not claim success.**

## 5. Font handling (only if CJK glyphs are boxes)

`src/Core.cs` calls:

```csharp
Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "微软雅黑", "SimHei", "黑体", "SimSun", "宋体" }, 14);
```

and assigns the result to `GUI.skin.{font,label,box,button,window,toggle}` plus the local label/toggle styles. On a system without any CJK font installed, add one (e.g. `Microsoft YaHei` is part of Windows since Vista) or extend the name list.

## 6. Expected side effect you must disclose

The game ships `RuntimeIntegrityService`, which scans the game root for `version.dll`, `winhttp.dll`, `winmm.dll`, `dobby.dll`, `doorstop_config.ini` and the folders `BepInEx`, `MelonLoader`, `Mods`. On a hit it logs:

```
[RuntimeIntegrityService] External interference detected. Markers: version.dll, MelonLoader, Mods
```

Observed (2026-10-30, game 1.0.033r): game runs, saves, achievements work, no prompt. Unknown whether a future game update blocks on it. Tell the user; let them decide.

## 7. Uninstall

Delete `<GAME>\MelonLoader\`, `<GAME>\Mods\`, `<GAME>\version.dll`. No original game file is modified by this mod.

## 8. Rebuild from source

```powershell
dotnet build -c Release -p:GamePath="<GAME>" -p:BepInExPath="<dir with BepInEx\core\BepInEx.dll, optional>"
```

Output: `bin/Release/netstandard2.1/RestoryQOL.dll`. The project references game assemblies at `$(GamePath)\Restory_Data\Managed\*` and MelonLoader at `$(GamePath)\MelonLoader\net35\*`, so the loader must be installed (or the paths overridden) before building. This repo deliberately does **not** ship game DLLs.

## 9. Hard rules for agents

- Never fabricate log lines. Quote the file, or say you could not read it.
- Never download the game or game DLLs from third-party mirrors.
- Keep `LICENSE` and `NOTICE.md` intact when redistributing; upstream is MIT (Azalea / hykilpikonna).
- One-shot alternative: `powershell -ExecutionPolicy Bypass -File scripts/install.ps1` performs §1–§3; §4 verification is still yours to run.
