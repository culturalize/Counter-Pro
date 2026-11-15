# Changelog

All notable changes to **Counter-Pro** are documented in this file.

## [2.0.1] - 2026-11-15

### Fixed & Removed (Game Launch & EasyAntiCheat Compatibility Fix)
- **Removed Automatic Fortnite Launch Watcher (`-Watch`, `GameWatch`, `AutoStart`):**
  - Permanently removed the background process watcher (`-Watch`), `HKCU\...\Run\CounterProFortniteWatcher` auto-start registration, `CreateToolhelp32Snapshot` process polling, and automatic desktop resolution switching on game launch.
- **Replaced Synchronous `EVENT_SYSTEM_FOREGROUND` Hook with Passive Z-Order Keeper:**
  - Removed `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` and `SWP_SHOWWINDOW` re-assertions during window creation. The overlay now uses a passive 1500ms timer with `SWP_NOACTIVATE | SWP_NOSENDCHANGING` only when already visible, preventing any contention with DXGI Exclusive Fullscreen transitions.
- **100% Safe Managed Code:**
  - Removed all `unsafe` blocks and process enumeration P/Invokes (`CreateToolhelp32Snapshot`, `Process32FirstW`, `Process32NextW`).

### Technical Postmortem: Why Auto-Launching on Game Startup Broke Fortnite (`v2.0.0` -> `v2.0.1`)
Forensic comparison of `anticheatlauncher.log` and `FortniteLauncher.log` across pre-watcher and watcher-active launches identified three compounding interactions during EasyAntiCheat (EAC) bootstrap:
1. **Premature Display Mode Switch (`ChangeDisplaySettingsW`) During EAC Bootstrap:**
   - The 2-second `CreateToolhelp32Snapshot` loop matched `FortniteLauncher.exe` and `FortniteClient-Win64-Shipping_EAC_EOS.exe` within 0–2 seconds of clicking Launch in Epic Games Launcher—while the EasyAntiCheat splash screen was still initializing and before Unreal Engine created its D3D11/DXGI device.
   - Firing `ChangeDisplaySettingsW(1720x1080)` mid-bootstrap broadcast `WM_DISPLAYCHANGE` across all desktop threads while EAC and Unreal Engine were negotiating display modes.
2. **Synchronous `HWND_TOPMOST` Contention Blocking `IDXGISwapChain::SetFullscreenState`:**
   - Spawning a `WS_EX_TOPMOST` layered window and synchronously re-asserting `SetWindowPos(HWND_TOPMOST, ..., SWP_SHOWWINDOW)` inside an `EVENT_SYSTEM_FOREGROUND` WinEvent hook interrupted Unreal Engine's initial `FullscreenMode=0` (Exclusive Fullscreen) swapchain acquisition.
   - As a result, the game window failed to become visible within EasyAntiCheat's 60-second visibility watchdog (`Exit defer time crossed, the bootstrapper will leave the visible state despite the game window not being visible. Time spent waiting for the game window to become visible: 60013 ms`), causing EAC to terminate the launch with `exit status 1`.
3. **Continuous `TH32CS_SNAPPROCESS` Polling + Global Hook Installation During Anti-Cheat Init:**
   - Polling system-wide process tables every 2 seconds and installing `WH_MOUSE_LL` / `WH_KEYBOARD_LL` hooks at the exact moment EAC loads its kernel driver and in-game module (`Launcher finished with: 301`) is an anti-pattern that interferes with anti-cheat initialization.

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
