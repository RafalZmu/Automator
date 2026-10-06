# Automator

Automator is a Windows desktop launcher with a tray icon, a configurable global opener key, numbered workspace tabs, application discovery, and short aliases. The desktop shell and launcher UI use Electron, React, and TypeScript. Windows hooks, app discovery, launching, settings, and native dialogs run in a separate .NET 10 backend process.

## Requirements

- Windows 10 version 1809 or later, or Windows 11
- Node.js 22.12 or later and npm 10 or later
- .NET 10 SDK and Windows targeting packs

Windows 11 build 22621 and later can use native Acrylic. Windows 10 uses a solid rounded surface with transparent outer corners. The app theme is saved independently of the Windows theme.

## Build and run

```powershell
npm ci
dotnet restore Automator.sln
npm run dev
```

`npm run dev` stops previous workspace Automator hosts with the restart helper, builds the Electron host and backend, and launches the real desktop app with isolated development settings and logs. The renderer uses Vite at `127.0.0.1:5173`. To preview the renderer in a browser with a deterministic fixture bridge, run `npm run dev:browser` or `npm run preview:browser`.

The complete verification and Windows x64 package commands are:

```powershell
npm run verify
npm run package:win
npm run test:packaged-lifecycle
```

`verify` runs TypeScript typechecking, frontend/host/backend builds, RPC/platform fixtures, the Vite generated-file watch regression, Electron integration tests, and the Core/Application/Protocol/Windows .NET specification suites. After packaging, `test:packaged-lifecycle` starts the stable host twice in an isolated test directory, checks the single-instance lock starts only one backend, and cleans up through the workspace helper. `package:win` publishes a self-contained Windows x64 backend and creates the stable unpacked app at `artifacts\electron-dist\win-unpacked\Automator.exe` plus a portable executable in `artifacts\electron-dist`.

Use the workspace helper to restart the packaged desktop app:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Restart-Automator.ps1 -Show
```

The helper waits for a fresh `App.Ready` event matching the launched host and build. `-Development -Show` starts the source build with isolated data, `-TestMode -Show` runs a packaged build with isolated settings and no startup registration, and `-StopOnly` stops workspace Automator hosts without starting one. The retained WinUI rollback build remains available by passing its executable with `-ExecutablePath`.

## Tabs and section documentation

The app keeps its compact numbered launcher tabs and also provides a larger Workspace window for detailed editing and reports. Each tab and shared surface has an agent guide and a README that describe how to extend it and where its code lives.

| Surface | Documentation |
| --- | --- |
| Slot 1 — Launcher | [Agent guide](docs/tabs/launcher/AGENTS.md) · [Functionality and code](docs/tabs/launcher/README.md) |
| Slot 2 — Script Runner | [Agent guide](docs/tabs/script-runner/AGENTS.md) · [Functionality and code](docs/tabs/script-runner/README.md) |
| Slot 3 — API | [Agent guide](docs/tabs/api/AGENTS.md) · [Functionality and code](docs/tabs/api/README.md) |
| Slot 4 — Browser Automation | [Agent guide](docs/tabs/browser-automation/AGENTS.md) · [Functionality and code](docs/tabs/browser-automation/README.md) |
| Slot 5 — Workflows | [Agent guide](docs/tabs/workflows/AGENTS.md) · [Functionality and code](docs/tabs/workflows/README.md) |
| Slot 6 — Scheduler | [Agent guide](docs/tabs/scheduler/AGENTS.md) · [Functionality and code](docs/tabs/scheduler/README.md) |
| Slot 7 — Work Time | [Agent guide](docs/tabs/work-time/AGENTS.md) · [Functionality and code](docs/tabs/work-time/README.md) |
| Slot 8 — Website Launcher | [Agent guide](docs/tabs/website-launcher/AGENTS.md) · [Functionality and code](docs/tabs/website-launcher/README.md) |
| Global action search | [Agent guide](docs/sections/global-action-search/AGENTS.md) · [Functionality and code](docs/sections/global-action-search/README.md) |
| Options | [Agent guide](docs/sections/options/AGENTS.md) · [Functionality and code](docs/sections/options/README.md) |
| Activity and notifications | [Agent guide](docs/sections/activity-center/AGENTS.md) · [Functionality and code](docs/sections/activity-center/README.md) |
| Workspace window | [Agent guide](docs/sections/workspace/AGENTS.md) · [Functionality and code](docs/sections/workspace/README.md) |

Slot 9 is reserved.

## Controls and tab behavior

- Tap **Right Ctrl** to open or close the panel. Opening selects slot 1.
- Press **1–9** to select a workspace tab. Slot 1 is the app launcher; slots 2–8 are Script Runner, API, Browser Automation, Workflows, Scheduler, Work Time, and Website Launcher.
- Opening the compact launcher shows Global Action Search as its home surface. It searches registered actions across tabs; a unique match runs, and choosing a tab-specific action switches to that tab. This home surface is not a numbered tab.
- Press **/** in slot 1 to open the app catalog. The app under the pointer when the panel opens is pinned first.
- Choose a listed app or browse for an EXE/shortcut, then assign a unique letters-only alias.
- Type an alias to launch the app after a short delay. The panel closes; an app already in the foreground is minimized, a running background app is restored and maximized, and a new app is maximized when its window appears.
- Press **Escape** or click outside the panel to close it.
- Settings can record another opener key, set the theme and startup behavior, import/export settings, relink missing paths, edit reusable Variables, set default interpreters, and open the log folder.
- Script Runner stores Python, Bash, and PowerShell profiles in the local SQLite library. Profiles keep interpreter/script paths, individual arguments, working directory, output format, and timeout; runs can be canceled and show bounded stdout/stderr or parsed JSON.
- API stores request profiles, supports Chrome Copy as cURL import, uses profile defaults for request JSON, and keeps credentials in Windows Credential Manager.
- Browser Automation displays Playwright tests grouped by test file, supports Automator-managed tags, and can run a test, file section, or tag group. Automator seeds its configured managed project with an example test; users can also select an existing project.
- Workflows run saved Script Runner, API, or Browser Automation profiles in order, map JSON values between steps, and stop after the first failed step.
- Scheduler creates interval, daily, and weekly local-time schedules for saved scripts or workflows and includes an upcoming agenda/calendar.
- Work Time records elapsed intervals, then saves a description and tags. Its history can be filtered, edited, summarized, and exported.
- Website Launcher saves named shortcuts with ordered browser groups and website tabs. Type a row's letters-only alias in its tab, or append `w` to the alias in Global Action Search to launch it through the Windows default browser.
- The Workspace window provides more room for editing modules, reviewing run activity, and opening notification summaries.

Script profiles execute as the current Windows user and are not sandboxed. Only configure scripts and interpreters you trust. Bash uses a user-configured executable such as Git Bash.

## Data and diagnostics

Small production preferences remain in `%LOCALAPPDATA%\Automator\settings.json`. Script profiles and reusable module records use the versioned SQLite library at `%LOCALAPPDATA%\Automator\library.db`. Host and backend structured logs are stored under `%LOCALAPPDATA%\Automator\Logs`. Run activity keeps bounded metadata; command output is held in the current UI session and is not added to application logs. API secrets stay in Windows Credential Manager. The renderer does not access Node APIs directly; it uses a context-isolated preload bridge and newline-delimited JSON-RPC over redirected process stdin/stdout.

The production startup entry launches the desktop host, not the backend. Automated and development hosts use isolated data under `artifacts\test-data` and do not write the user's startup entry or settings.

## Project structure

- `ui` and `contracts` contain the React renderer, typed bridge-facing DTOs, Zod validation, and bundled view registry.
- `electron` contains the desktop window, tray, secure preload boundary, native dialogs, foreground handshake, and backend process lifecycle.
- `src/Automator.Core` and `src/Automator.Application` contain platform-independent rules and launcher state/scheduling.
- `src/Automator.Windows` implements Windows hooks, catalog/icon discovery, settings, and app activation.
- `src/Automator.Infrastructure` contains SQLite, process, HTTP, secret-store, and Playwright runtime adapters.
- `src/Automator.Backend` hosts the Windows services and their native message loops outside Electron.
- `src/Automator.App` retains the previous WinUI application for rollback and compatibility builds.
- `tests` contains RPC fixtures, platform rules, .NET specifications, and Electron/Playwright integration tests.
- `docs/tabs` and `docs/sections` contain the paired contributor guides and feature/code overviews indexed above.

External plugin loading remains a future phase. Slot 9 is intentionally unassigned; slot 7 is the Work Time logger, while the older focus-session code remains for compatibility.
