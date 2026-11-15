# Changelog

All notable changes to **Counter-Pro** are documented in this file.

## [2.0.0] - 2026-11-15

### Added
- **4 View Modes:** Full Mode, Mini Card Mode, Slim Gaming HUD Pill, and Just Number Transparent Floating Mode.
- **5-Tab Customization Studio:**
  - Interactive Visual Keyboard (`100%`, `80% TKL`, `75%`, `65%`, `60%` auto-detection) & Sculpted 5-Button Gaming Mouse hotkey selector.
  - Free-draggable custom image stickers and 8 built-in vector icons (Chest, Crown, Gem, Skull, Flame, Key, Trophy, Star).
  - 19 colour themes (18 presets + Custom RGB Theme Builder).
  - 8 font families, custom prefix/suffix labels, adjustable outline width, and 2-color directional custom gradients (`GraphicsPath` vector rendering across both card and transparent floating modes).
- **Live Mouse-Wheel Scaling:** Hold Left-Click + Scroll Wheel on the counter number or any sticker to scale its size live; corner move and resize grips.
- **True Mouse Pass-Through Mode (`F8`):** Enforces `WS_EX_LAYERED | WS_EX_TRANSPARENT` and `HTTRANSPARENT` so mouse aiming and clicks pass directly through the overlay into games.
- **Automatic Game Watcher (`-Watch`):** Idle (~3.4 MB RAM) background watcher that launches the overlay when Fortnite starts, optionally matches stretched desktop resolution, and closes cleanly when the game exits.
- **30-Day Rolling Backups:** Automatic daily snapshots of `counter.txt` in `backups/`.

### Changed & Optimized
- Renamed project and executable to **Counter-Pro** (`Counter-Pro.exe`).
- Moved `WH_MOUSE_LL` and `WH_KEYBOARD_LL` hooks onto a dedicated `Highest`-priority thread with pre-JITted callbacks.
- Replaced per-click synchronous writes with coalesced asynchronous disk writes (`Interlocked.Exchange`) plus a synchronous exit flush.
- Added string-length/font signature caching to avoid redundant `CreateGraphics()` calls on every increment.
- Synthesized an in-memory 4ms PCM WAV tick buffer to eliminate Windows system sound interruptions.

## [1.0.0] - 2026-11-12

### Added
- Initial release with global mouse/keyboard hooks, non-resetting `F9` section splits, and persistent `counter.txt` storage.
