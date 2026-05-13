<div align="center">

# CrosshairY

**A lightweight crosshair overlay for Windows. Always on top, fully customizable, zero bloat.**

![Platform](https://img.shields.io/badge/platform-Windows-blue?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=flat-square)
![Language](https://img.shields.io/badge/language-C%23-green?style=flat-square)
![License](https://img.shields.io/badge/license-MIT-orange?style=flat-square)

</div>

---

## What is it

CrosshairY draws a persistent crosshair directly on your screen as a transparent overlay. It works in any game or app, sits on top of everything, and is completely click-through so it never gets in the way. No injection, no drivers, just a window.

---

## Features

- **17 crosshair templates** - Dot, Ring, Square, Thin Cross, Thick Cross, Cross·, T-Shape, Circle+, Small Plus, Large Plus, Sniper, X Cross, X·, Arrows, Chevrons, Triangle, Diamond
- **8 color swatches** with live preview
- **Outline toggle** with adjustable thickness (1–5)
- **Size slider** from 50% to 200%
- **Profile system** - save, load, overwrite and delete configs stored locally in `%APPDATA%\CrosshairY`. Drop a friend's `.json` in the folder and hit reload
- **Last used config** auto-loads on startup
- **Proof mode** - hides the window from screen capture software with a single keypress
- **No taskbar icon**, borderless, transparent, runs silently in the background

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

Configs are plain `.json` files stored in `%APPDATA%\CrosshairY\`.

- **Save** - type a name in the Profiles tab and hit Save
- **Load** - click Load next to any config to apply it instantly
- **Overwrite** - hit Save next to an existing config to update it with your current settings
- **Share** - paste a friend's `.json` into the folder and hit Reload to see it appear

The last loaded config is remembered and auto-applied on the next launch.

---

## Proof mode

Pressing the configured proof key calls `SetWindowDisplayAffinity(WDA_EXCLUDEFROMCAPTURE)` on the overlay window, hiding it from OBS, Discord screenshare, and similar capture software. Press again to restore. Configurable under Settings.

---

## Project structure

```
CrosshairY/
│
├── src/
│   ├── AppState.cs                 — runtime state
│   ├── GlobalKeyboardHook.cs       — low-level keyboard hook
│   ├── MainWindow.xaml             — main UI layout
│   ├── MainWindow.xaml.cs          — UI logic
│   ├── CrosshairOverlay.xaml       — transparent overlay window
│   └── CrosshairOverlay.xaml.cs    — crosshair draw logic
│
├── Fonts/                          — Bebas Neue + IBM Plex Mono
├── App.xaml                        — styles and resources
├── App.xaml.cs                     — global exception handling
├── CrosshairY.csproj
├── app.manifest                    — DPI awareness
├── build.bat
└── clean.bat
```

---

## License

MIT - do whatever you want with it.
