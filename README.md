<div align="center">

# CrosshairY

**A lightweight crosshair overlay for Windows. Always on top, fully customizable, zero bloat.**

![Platform](https://img.shields.io/badge/platform-Windows-blue?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)
[![Discord](https://img.shields.io/badge/Discord-Join-5865F2?style=flat-square&logo=discord&logoColor=white)](https://discord.gg/tvJpwkyBmN)
![Language](https://img.shields.io/badge/language-C%23-green?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-orange?style=flat-square)

</div>

---

## What is it

CrosshairY draws a persistent crosshair directly on your screen as a transparent overlay. It works in any game or app, sits on top of everything, and is completely click-through so it never gets in the way. No injection, no drivers, just a window.

---

## Features

- **22 crosshair templates** - Dot, Ring, Square, Thin Cross, Thick Cross, Cross·, T-Shape, Circle+, Small Plus, Large Plus, Sniper, X Cross, X·, Arrows, Chevrons, Triangle, Diamond, Dot Ring, 2 Rings, Plus·, Corners, X Thick
- **8 color swatches** with live preview plus a full **custom color picker** - square saturation/value field, hue bar and free hex input
- **Crosshair builder** - draw your own crosshair on a 15x15, 30x30 or 60x60 grid with a 16-color palette and a full tool set: **pencil, eraser, fill (bucket), line, rectangle and ellipse**
  - **Undo / redo** with Ctrl+Z / Ctrl+Y
  - **Mirror symmetry** - left/right, up/down or 4-way mirroring while you draw
  - **Save as crosshair** - pin your drawing into the "My Crosshairs" grid for one-click reuse
- **Randomize button** - picks a random template and color instantly
- **Outline toggle** with adjustable thickness (1-5)
- **Size slider** from 50% to 200%
- **Opacity slider** from 10% to 100%
- **Center gap slider** - control the gap between crosshair arms (0-20)
- **Multi-monitor** - pick which display the crosshair appears on
- **Profile system** - save, load, overwrite and delete configs stored locally in `%APPDATA%\CrosshairY\Configs`. Drop a friend's `.json` in the folder and hit reload
- **Share codes** - export the current config to the clipboard as a compact code and import a friend's code in one click
- **Last used config** auto-loads on startup
- **Proof mode** - hides the window from screen capture software with a single keypress
- **Profile cycle hotkey** - cycle through saved profiles without opening the UI
- **No taskbar icon**, borderless, transparent, runs silently in the background
- **Launch surveys** - occasional single-question popups at launch milestones, never more than once each

---

## Requirements

- Windows 10 or 11 (x64)
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

---

## Building from source

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or [Visual Studio 2022](https://visualstudio.microsoft.com/) with the **.NET Desktop Development** workload

### Steps

1. Clone the repo
2. Drop the two font files into the `Fonts/` folder (see below)
3. Run `build.bat`

```
build.bat
```

Output lands at `bin\publish\CrosshairY.exe` as a single self-contained executable.

### Fonts needed

Both are free and available on [Google Fonts](https://fonts.google.com):

| File | Font |
|------|------|
| `Fonts/BebasNeue-Regular.ttf` | Bebas Neue |
| `Fonts/IBMPlexMono-Regular.ttf` | IBM Plex Mono |

---

## Profiles

Configs are plain `.json` files stored in `%APPDATA%\CrosshairY\Configs\`.

- **Save** - type a name in the Profiles tab and hit Save
- **Load** - click Load next to any config to apply it instantly
- **Overwrite** - hit Save next to an existing config to update it with your current settings
- **Share file** - paste a friend's `.json` into the folder and hit Reload to see it appear
- **Export code** - copies the current config to the clipboard as a `CY1:` share code
- **Import code** - reads a `CY1:` code from the clipboard and saves it as a profile (named from the box, or `imported`)

A profile stores the template, color, outline, size, opacity, gap and any custom builder crosshair. The last loaded config is remembered and auto-applied on the next launch.

Hotkeys (proof key, cycle key) are global settings and are stored separately in `%APPDATA%\CrosshairY\settings.dat`. They are never overwritten by loading a profile.

---

## Proof mode

Pressing the configured proof key calls `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` on the overlay window, hiding it from OBS, Discord screenshare, and similar capture software. Press again to restore. Configurable under Settings.

---

## Surveys

At launch counts 3, 7, 15, and 30 a small popup appears with a single question before the main UI loads. Each survey shows at most once. Closing without answering defers it to the next launch. The launch counter is stored in `%APPDATA%\CrosshairY\launches.dat`.

Survey responses are sent to a **Supabase Edge Function**, which forwards them to a Discord webhook stored as a server-side secret. The real webhook never ships in the client - the app only knows the public Supabase endpoint and publishable key. To point it at your own backend, change `SurveyEndpoint` and `SupabaseAnonKey` in `SurveyWindow.xaml.cs` and set the `DISCORD_WEBHOOK_URL` secret on your Supabase project.

---

## Project structure

```
CrosshairY/
|
+-- src/
|   +-- AppState.cs                 - runtime state
|   +-- GlobalKeyboardHook.cs       - low-level keyboard hook
|   +-- MainWindow.xaml             - main UI layout
|   +-- MainWindow.xaml.cs          - UI logic, color picker, builder, profiles
|   +-- CrosshairOverlay.xaml       - transparent overlay window
|   +-- CrosshairOverlay.xaml.cs    - crosshair draw logic (CrDraw)
|   +-- SurveyWindow.xaml           - survey popup layout
|   +-- SurveyWindow.xaml.cs        - survey logic, posts to the Supabase endpoint
|
+-- Fonts/                          - Bebas Neue + IBM Plex Mono
+-- App.xaml                        - styles and resources
+-- App.xaml.cs                     - global exception handling
+-- GlobalUsings.cs                 - shared global using directives
+-- CrosshairY.csproj
+-- app.manifest                    - DPI awareness
+-- build.bat
+-- clean.bat
```

---

## License

MIT - do whatever you want with it.
