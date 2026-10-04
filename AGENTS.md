# Automator workspace instructions

## Restarting the application

Always use the workspace helper to stop or restart Automator:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Restart-Automator.ps1 -Show
```

The default selection is the newest complete Electron package at `artifacts\electron-dist\win-unpacked\Automator.exe`; it validates the build manifest, ASAR, and self-contained backend before launch. The helper stops the workspace's packaged, development, portable, backend, and retained WinUI process trees, then waits for a fresh `App.Ready` event matching the launched host and build.

- Use `-Development -Show` for the source-built Electron host with isolated settings/logs.
- Use `-TestMode -Show` with the default packaged host to inspect a package without touching production settings or startup registration.
- Use `-StopOnly` to stop workspace-owned Automator processes without starting an app.
- Use `-ExecutablePath <path-to-Automator.App.exe>` to explicitly launch the retained WinUI rollback build.

Run the helper in the user's interactive desktop session outside a restricted sandbox when native hooks, tray behavior, or normal per-user logging need inspection. Do not start a second Automator host directly while the helper-managed app is running.

## Verification and package

Run `npm.cmd run verify` after implementation changes. It typechecks TypeScript, builds the Electron renderer/host and backend, runs the RPC/platform and Vite watch tests, Electron integration tests, and Core/Application/Protocol/Windows .NET specifications.

Create the Windows x64 distribution with `npm.cmd run package:win`. The stable host is `artifacts\electron-dist\win-unpacked\Automator.exe`; the portable artifact is also written under `artifacts\electron-dist`. Then run `npm.cmd run test:packaged-lifecycle` to verify the packaged single-instance lock and isolated backend cleanup.

Vite ignores `artifacts`, `dist-electron`, and .NET `bin`/`obj` output. Keep Tailwind source detection rooted at `ui` in `ui\styles.css`; scanning the workspace root watches Electron cache files and causes development reload loops. Launch the GUI process without `-WindowStyle Hidden`; the Electron window controls its own initial visibility. Keep background controllers and services hidden.

Development and automated runs use isolated settings under `artifacts\test-data`. Preserve the production schema-1 settings at `%LOCALAPPDATA%\Automator\settings.json` during test runs.

## Tab and shared-section guides

Before changing a tab or a shared workspace surface, read both its contributor instructions and its feature/code overview:

- Tabs: `docs/tabs/<tab-name>/AGENTS.md` and `docs/tabs/<tab-name>/README.md`
- Shared surfaces: `docs/sections/<section-name>/AGENTS.md` and `docs/sections/<section-name>/README.md`

The docs tree is organized by user-visible surface, while source code remains in the existing `ui`, `electron`, `contracts`, and `src` projects. Each README describes current behavior and points to the implementation and tests. Each AGENTS file adds the invariants and workflow specific to that surface. Treat the root instructions here as applying everywhere.

When adding or moving a tab or shared surface, add or update its documentation pair and update the index in the root README. When behavior or code boundaries change, update both files for the affected surface in the same change. Do not describe planned behavior as current behavior.
