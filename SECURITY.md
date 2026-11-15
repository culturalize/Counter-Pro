# Security & Anti-Cheat Policy

## How Counter-Pro Interacts with Windows and Games

**Counter-Pro** is designed from the ground up to be safe alongside kernel-level anti-cheat systems (such as EasyAntiCheat, BattlEye, and Vanguard):

1. **Zero Game Process Access** — The application never calls `OpenProcess`, `ReadProcessMemory`, `WriteProcessMemory`, or `CreateRemoteThread` on any external process.
2. **Standard Win32 Window Overlay** — The overlay is a normal top-level Windows desktop window using standard documented window styles (`WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TRANSPARENT`). It does not hook DirectX, Vulkan, OpenGL, or DWM.
3. **Standard Low-Level Input Hooks** — Global hotkeys use standard `SetWindowsHookEx` (`WH_MOUSE_LL` and `WH_KEYBOARD_LL`) callbacks that immediately call `CallNextHookEx` and post an asynchronous message to the counter window without modifying or blocking game input.
4. **Read-Only Process Snapshot (Optional Watcher)** — When `-Watch` mode is enabled, process presence is checked via `CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0)` name matching without opening any process handles.

## Reporting a Vulnerability

If you discover a security issue or bug, please open an issue on GitHub with steps to reproduce.
