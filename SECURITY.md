# Security & Anti-Cheat Policy

## How Counter-Pro Interacts with Windows and Games

**Counter-Pro** is architected to be 100% passive and safe alongside kernel-level anti-cheat systems (such as EasyAntiCheat, BattlEye, and Vanguard) and Exclusive Fullscreen DXGI swapchains:

1. **Zero Game Process Access or Process Polling** — The application never calls `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, `CreateRemoteThread`, or `CreateToolhelp32Snapshot`. It never scans, polls, or inspects running processes or game configuration files.
2. **Standard Win32 Window Overlay** — The overlay is a normal top-level Windows desktop window using standard documented window styles (`WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TRANSPARENT`). It never hooks DirectX, DXGI, Vulkan, OpenGL, or DWM.
3. **Passive Z-Order Maintenance (Fullscreen SwapChain Safe)** — Counter-Pro does not install `SetWinEventHook(EVENT_SYSTEM_FOREGROUND)` hooks and never calls `ChangeDisplaySettingsW` automatically during game startup. When visible, it maintains Z-order passively using `SWP_NOACTIVATE | SWP_NOSENDCHANGING` so it never interrupts a game's `IDXGISwapChain::SetFullscreenState` transition or triggers EasyAntiCheat's 60-second window-visibility watchdog timer.
4. **Standard Low-Level Input Hooks** — Global hotkeys use standard `SetWindowsHookEx` (`WH_MOUSE_LL` and `WH_KEYBOARD_LL`) callbacks that immediately call `CallNextHookEx` and post an asynchronous message to the counter window without modifying or blocking game input.

## Reporting a Vulnerability

If you discover a security issue or game compatibility bug, please open an issue on GitHub with steps to reproduce.
