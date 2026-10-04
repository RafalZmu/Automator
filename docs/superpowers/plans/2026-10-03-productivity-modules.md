# Automator Productivity Modules

## Goal

Add reusable automation modules to the existing Electron/React and .NET host without changing the launcher contract or adding external plugin loading.

## Delivery order

1. Add versioned SQLite library storage and typed module-scoped host services.
2. Ship Script Runner in slot 2 with saved Python/Bash profiles, structured process arguments, bounded output, timeout, and cancellation.
3. Ship API profiles in slot 3, backed by shared HTTP and Windows Credential Manager secret references.
4. Ship Playwright Browser Automation in slot 4 with a managed isolated browser session.
5. Ship linear Workflows in slot 5, passing JSON between saved script/API/browser profiles and stopping after the first failure.
6. Ship the tray-lifetime Scheduler in slot 6: interval/daily/weekly local-time schedules, skip missed runs by default, optional run-once catch-up.
7. Ship Focus Sessions in slot 7 with pause/resume, editable focus/break duration, persistent history, and completion notification.
8. Extend library import/export, then verify and package using `AGENTS.md`.

## Constraints

- Keep `settings.json` for small preferences; persist profiles, workflows, schedules, and histories in the local SQLite library.
- Keep process, browser, storage, credentials, HTTP, scheduler, and notification implementations in host-owned services. React receives only typed, allowlisted operations.
- Scripts are trusted user code and run as the Automator user. Pass arguments individually, cap captured output, and terminate the full process tree on cancellation/timeout.
- Keep secret values out of settings, SQLite records, logs, and exports. Export secret references and report missing paths/references for repair.
- Keep module/action/result versions stable, reserve slots 8–9, and preserve Tab 1 behavior.

## Verification

Use failing tests before each production behavior, run the targeted .NET/TypeScript/Electron specs, then run `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle`.
