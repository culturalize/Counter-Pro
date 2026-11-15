# Contributing to Counter-Pro

Thank you for your interest in contributing to **Counter-Pro**!

## Design Principles

1. **Zero External Dependencies** — The entire application compiles cleanly into a single executable using the built-in Windows .NET Framework C# compiler (`csc.exe`) via `build.bat`.
2. **Minimal Resource Footprint** — Keep memory allocations out of hot paths (input hook callbacks, Z-order checks, and paint loops). The input hook thread must never block or perform I/O.
3. **Anti-Cheat Safety** — Never open handles to external game processes, read game memory, or inject DLLs. All overlay features must rely strictly on standard Win32 window styles (`WS_EX_TOPMOST`, `WS_EX_TOOLWINDOW`, `WS_EX_NOACTIVATE`, `WS_EX_TRANSPARENT`).

## Building & Testing Locally

1. Clone the repository:
   ```cmd
   git clone https://github.com/culturalize/Counter-Pro.git
   cd Counter-Pro
   ```
2. Run the build script:
   ```cmd
   build.bat
   ```
3. Run the automated smoke test:
   ```cmd
   Counter-Pro.exe -TestExit
   ```

## Submitting Pull Requests

- Test your changes across all 4 view modes (`Full`, `Mini`, `HUD Pill`, and `Just Number` floating mode).
- Ensure `counter.txt`, `settings.json`, and `backups/` are not committed to version control.
- Describe the motivation and performance impact of your change in the pull request description.
