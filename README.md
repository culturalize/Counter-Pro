# Counter-Pro

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D6.svg)](#requirements)
[![Latest Release](https://img.shields.io/github/v/release/culturalize/Simple-Counter?label=Download&color=success)](https://github.com/culturalize/Simple-Counter/releases/latest)
[![Build](https://github.com/culturalize/Simple-Counter/actions/workflows/build.yml/badge.svg)](https://github.com/culturalize/Simple-Counter/actions/workflows/build.yml)

**Counter-Pro** is a fast, ultra-lightweight global hotkey counter and customizable desktop/game HUD overlay for Windows. Built directly on Win32 and GDI+ with zero external dependencies, it runs as a single ~115 KB executable using near-zero CPU and ~3.4 MB RAM when idle.

> **📥 Easy Install:** Go to the [**Releases page**](https://github.com/culturalize/Simple-Counter/releases/latest) and download **`Counter-Pro.exe`** (standalone single file) or **`Counter-Pro-Windows-x64.zip`**. No installer required.

---

## Features

### 4 Live View Modes
Cycle seamlessly between four display layouts at any time (`F6` by default or via the top bar):
1. **Full Mode** — Complete counter card with section history, live section deltas, session gain, clicks-per-minute (CPM) pace badge, milestone progress bar, and pace trend sparkline.
2. **Mini Card Mode** — Compact card displaying the main counter, active badges, and milestone bar.
3. **Slim Gaming HUD Pill** — Low-profile horizontal status bar designed for screen edges.
4. **Just Number Mode (Floating Overlay)** — Transparent floating counter (`+` optional custom prefix/suffix and stickers) that renders directly over borderless and fullscreen-optimized games.

### 5-Tab Customization Studio (`⚙ Setup`)
- **Visual Keyboard & Mouse Picker** — Interactive visual layout (automatically detects `100% Full`, `80% TKL`, `75%`, `65%`, and `60% Mini` keyboards alongside a 5-button gaming mouse diagram) for binding global hotkeys or mouse side buttons.
- **Stickers & Decor Studio** — Add free-draggable custom images (`PNG`, `JPG`, `GIF`, `ICO`) or built-in vector icons (Chest, Crown, Gem, Skull, Flame, Key, Trophy, Star) around your counter.
- **19 Colour Themes + Custom Theme Builder** — Includes 18 curated dark/OLED/neon presets (*Obsidian Cyan*, *Emerald Matrix*, *Crimson Ember*, *Violet Nebula*, *Amber Gold*, *Sakura Neon*, *OLED Pitch Black*, *Cyberpunk 2077*, *Blood Moon*, *Nordic Frost*, *Dracula*, *Monokai Pro*, *Synthwave Sunset*, *Stealth Tactical*, *Catppuccin Mocha*, *Tokyo Night*, *Solarized Dark*, *Arctic Daylight*) plus a full RGB custom theme builder.
- **Fonts, Sizes & Custom Gradients** — Choose from 8 font families, adjustable outline thickness, 5 text effect styles (Outline + Glow, Crisp Outline, 3D Shadow, 2-Tone Directional Gradient, Clean Flat), custom gradient color pickers, and custom prefix/suffix labels (e.g., `CHESTS 1,250` or `120 WINS`).
- **Game Overlay, Goals & Automation** — True click-through/mouse pass-through mode (`WS_EX_TRANSPARENT`), non-activating topmost game overlay (`WS_EX_NOACTIVATE`), custom step sizes (`+1` to `+50`), milestone targets, automatic Fortnite launch/exit watcher, stretched-resolution helper, and automatic 30-day rolling daily backups.

### Built for Gaming Performance
- **Dedicated High-Priority Input Hook Thread** — Low-level mouse (`WH_MOUSE_LL`) and keyboard (`WH_KEYBOARD_LL`) hooks run on a dedicated `Highest`-priority message pump with pre-JITted callbacks, ensuring UI rendering never delays mouse or keyboard input.
- **100% Mouse Pass-Through (`F8`)** — When enabled, mouse movement, aiming, and clicks pass straight through the overlay into your game without snagging the cursor.
- **Anti-Cheat Friendly Architecture** — Never injects DLLs, hooks DirectX, or opens handles to game processes. Process detection uses read-only `CreateToolhelp32Snapshot` system snapshots.
- **Coalesced Asynchronous Disk Saves** — Rapid bursts of clicks coalesce into a single background write to `counter.txt` with a synchronous flush on exit, plus automatic daily backups in `backups/`.

---

## Default Controls & Hotkeys

All hotkeys and mouse buttons can be rebound or cleared (`Delete` / `Backspace`) inside **⚙ Setup → ⌨ Visual Keys**.

| Action | Default Bind | Description |
| :--- | :--- | :--- |
| **Increment (`+Step`)** | `Mouse 5` (Side Front) | Adds the configured step amount to the current count |
| **New Section Split** | `F9` | Inserts a `----------` section divider in `counter.txt` without resetting your total |
| **Decrement (`-Step`)** | `F7` *(or Unbound)* | Subtracts the configured step amount |
| **Mouse Pass-Through (Ghost)** | `F8` | Toggles 100% mouse hover & click pass-through on/off |
| **Cycle View Mode** | `F6` | Cycles through Full, Mini, HUD Pill, and Just Number modes |
| **Quick Scale (Number / Sticker)** | `Hold Left-Click + Scroll` | Hold Left-Click on the number or a sticker and scroll the mouse wheel to resize it live |
| **Quick Opacity** | `Scroll on Top Bar` | Scroll the mouse wheel over the header bar to adjust card opacity |

---

## Quick Start

1. Download `Counter-Pro.exe` (or run `build.bat` to compile from source).
2. Launch `Counter-Pro.exe`. It automatically creates `counter.txt` (starting at `0`) and `settings.json` in the same folder on first use.
3. Press **Mouse 5** to increment your counter, **F9** to start a new section, or click **⚙ Setup** to customize themes, fonts, gradients, stickers, and hotkeys.

### Command-Line Options

```powershell
# Standard launch
.\Counter-Pro.exe

# Use a custom counter file path
.\Counter-Pro.exe -CounterFile "C:\path\to\my_counter.txt"

# Override the increment trigger at startup (Mouse4, Mouse5, or F9)
.\Counter-Pro.exe -Button Mouse5

# Run the ultra-lightweight (~3.4 MB RAM) background game watcher
.\Counter-Pro.exe -Watch
```

---

## Building from Source

No external SDKs, NuGet packages, or Visual Studio installations are required. Windows 10 and Windows 11 include the .NET Framework 4.x C# compiler (`csc.exe`) out of the box.

Double-click [`build.bat`](build.bat) or run:

```cmd
build.bat
```

This compiles [`src/Program.cs`](src/Program.cs) with `/unsafe /optimize+` into `Counter-Pro.exe`.

---

## Data Format (`counter.txt`)

Your counter is stored in plain text so external scripts, stream overlays (OBS), or spreadsheets can read it directly:

```text
120
----------
245
----------
310
```

Each `----------` divider marks a section split created with `F9`, preserving your cumulative total while tracking per-section gains.

---

## Requirements

- Windows 10 or Windows 11 (64-bit)
- .NET Framework 4.x (pre-installed on Windows 10/11)

## License

Released under the [MIT License](LICENSE).
