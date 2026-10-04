# SDD ledger — plan: docs/superpowers/plans/2026-10-03-remaining-productivity-modules.md

## Execution context

- User approved implementation of the remaining module roadmap.
- Workspace has no `.git` directory; no isolated git worktree or commit review packages are available. Keep concurrent edits on disjoint files and review each delivered change in the shared workspace.
- Workspace verification requires `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle`; use `scripts/Restart-Automator.ps1` for app lifecycle.
- Existing Script Runner PowerShell support and remembered per-runtime defaults are complete and verified.

## Preflight interface scan

| Work areas sharing a contract/file | Producer → consumer | Scan result |
|---|---|---|
| Shared process input ↔ Workflows / Script Runner | Bounded stdin JSON → saved script profile input | Must extend process DTO and service together; do not place workflow JSON in CLI args; test limit and EOF behavior. |
| Secrets + profile HTTP ↔ API / Workflows | Saved API profile policy and opaque secret refs → API provider / workflow handler | Host reloads grants by profile ID; renderer and workflow inputs cannot widen allowed hosts or read secret values. |
| Browser session ↔ Browser / Workflows | Fixed browser-profile handler → bounded typed action result | No arbitrary code evaluation; session data stays local; enforce saved host policy for all browser traffic. |
| Saved-profile executor ↔ Script Runner / API / Browser / Workflows / Scheduler | Fixed handler registry → host-authorized profile run | Include origin/correlation; never route through active-tab renderer dispatch; expose full bounded data only to requested UI and return safe internal summaries for history. |
| Backend lifetime ↔ Scheduler / Focus | Host-owned recurrence and focus coordinators → active-tab CRUD/actions | Work continues across tab switches and cancels at backend shutdown; service contexts remain active-module scoped. |
| Notification service ↔ Focus / Scheduler | Typed Windows notification boundary → module coordinator | State is persisted before a notification attempt; failure cannot roll back a completed phase/run. |
| Library store ↔ all modules | Module-owned collections → SQLite partition by module id | Existing versioned JSON records are sufficient initially; keep secrets, browser data, and raw run outputs out of records/exports. |

## Task consistency scan

| Task | Self-consistency check |
|---|---|
| Shared contracts and host adapters | Contract types precede providers; integration wiring is parent-owned; tests cover boundaries and lifecycle. |
| API | Profiles persist policy and references; secret values stay in Credential Manager; only saved-profile execution can inject credentials. |
| Browser Automation | UI calls bounded actions; worker/session is app-owned; profile grant and isolated data directory are host-controlled. |
| Workflows | Ordered saved-profile references feed structured JSON in memory; stop on failure; output persistence is summary-only. |
| Scheduler | Recurrence records resolve to saved workflow IDs; a process-lifetime coordinator claims occurrences, prevents concurrent same-schedule runs, and records safe status. |
| Focus Sessions | A process-lifetime coordinator owns timer state; snapshots and settings/history are UI-facing; notification is best-effort after state transition. |
| Integration and release | Slot registration and host wiring occur after module providers exist; tests, package, lifecycle, and helper-managed restart are final gates. |

## Progress

- Completed preflight scan; no plan conflict found. The implementation is underway with tab providers and views delegated and host integration coordinated by the parent.
- Shared process execution now accepts optional standard input bounded to 1 MiB (character and UTF-8 byte limits); the child stdin stream is flushed and closed. The first regression failed at compile time on the missing contract, then `Automator.Infrastructure.Specs` passed 18/18 after implementation.

- Slots 3–6 now have their API, Browser, Workflows, and Scheduler providers/views, and slot 7 has the Focus Sessions provider/view. Focus Sessions is registered at slot 7; slots 8–9 remain reserved. The countdown UI now refreshes at a host phase deadline until the host advances it.
- Host startup now constructs the saved Script Runner/API handlers, workflow engine, process-lifetime scheduler and focus coordinators, and typed notification adapter. These integrations remain unverified until the Focus coordinator and Browser runtime are complete.
- Contract/view checks passed: `npm.cmd run typecheck`; direct API, Browser, Workflows, Scheduler, and Focus model tests passed (4, 3, 9, 3, and 3 checks respectively).
- Scheduler hardening is complete. Its isolated suite passes 13/13, covering restart/catch-up recovery, durable claim failure, duplicate prevention, cancellation/drain, worker resilience, edited recurrence cursors, and DST gaps/folds.
- Focus coordinator integration is in progress with a new isolated specification project that covers persistence ordering, lifecycle recovery, duration snapshots, and notifications.
- Browser Automation is integrated through a host-owned Playwright worker and loopback policy proxy. Each profile has an isolated persistent browser directory, an exact-host policy, DNS/IP validation with address pinning, and a 500 ms host-side policy watcher that revokes active tunnels when grants change. Playwright and its matching core package are staged outside `node_modules` so electron-builder retains them in both outputs; a staged Node smoke check verifies package resolution.
- API, Workflows, Scheduler, Focus Sessions, and Browser Automation are registered in slots 3–7; launcher and Script Runner remain slots 1–2, slots 8–9 stay reserved. The combined import/export envelope includes reusable definitions and schedules, exposes bounded repair warnings, and excludes credentials, browser session data, and run output.
- Final `npm.cmd run verify` passed: TypeScript and app/backend builds, 36 contract checks, 28 Electron integration checks, and all .NET spec projects. The actual Windows x64 package built, `npm.cmd run test:packaged-lifecycle` passed 1/1, and the workspace restart helper reached visible `App.Ready` for packaged build `host-release-1.0.0-20261003135454782-20261003-135905-a6272b50`.
- The native hotkey check initially collided with the already-running packaged Automator global hook; after stopping the workspace process tree with the required helper, the test passed in both full-suite runs.
- Verification did not download Chromium or exercise a real external browser navigation. The packaged runtime modules load correctly; the first-use browser download/launch remains to be exercised when Browser Automation is first used.

## Rulings

- Scheduler occurrence claims are written before workflow execution. A claimed automatic run is never replayed after interruption; this is at-most-once scheduling, not exactly-once external side effects. A new/imported schedule without a prior occurrence cursor establishes a baseline instead of inventing a missed run.
- Catch-up is limited to one missed occurrence when a durable prior cursor exists. Manual run claims never move scheduled cursors. Ambiguous fall-back local times choose the first UTC occurrence; invalid spring-forward times move to the first valid minute.
- Focus phase advancement must persist the next phase before attempting its OS notification. Notifications are best-effort and do not reverse the durable phase transition.
