# Work Time — slot 7

Work Time logs elapsed work intervals rather than running a Pomodoro cycle. Start a named timer with a required description. Keep several timers paused between tasks while at most one timer runs. Pause the current timer before starting another; resume or end any paused timer later. Ended timers become independent drafts with editable descriptions and optional tags. Save or discard each draft separately. Durations retain seconds, including intervals shorter than one minute.

The history supports filtering by date and tag, editing or deleting entries, viewing totals, and exporting a CSV report. Multiple timers and ended drafts remain available after a restart.

While this tab is active, press **S** to pause the running timer, or focus the New timer description when none is running. It never chooses a paused timer to resume. The shortcut is also listed in the tab command menu and does not run while editing text.

The “Count only working hours” checkbox is off by default and is a view-only option. When enabled, displayed durations, report totals, and CSV durations count the overlap with local 08:00–16:00 on each calendar day, including intervals that cross midnight. Original timestamps and stored elapsed durations remain unchanged. The option resets when the Work Time view is recreated.

## Code map

- ui/modules/FocusSessionsView.tsx renders the Work Time tab; its filename is retained from the older Focus Sessions module.
- ui/modules/workTimeViewModel.ts handles elapsed-time display, local working-period overlap, filters, tags, report totals, and CSV generation.
- src/Automator.Application/Automation/FocusSessionsModule.cs exposes the slot 7 actions under the legacy module ID.
- AutomationWorkTimeCoordinator.cs persists named running/paused timers, independent unsaved drafts, running segments, and saved entries; AutomationWorkTimeContracts.cs defines their data.
- Tests: tests/electron/work-time-model.test.cjs and tests/Automator.Focus.Specs.

Work Time entries are local application data. The coordinator stores timestamps, descriptions, elapsed durations, and running segments. Working-period calculations sum known running-segment overlaps so paused gaps do not count. A migrated legacy timer may have accrued elapsed duration whose historical boundaries are unknown; working-period totals do not invent a location for that elapsed time. The Launcher and Workspace use the same timer and draft controls; reports, filters, and saved history remain Workspace-only.

## Test case steps

See [TESTS.md](TESTS.md) for interval, duration, report, shortcut, and Workspace end-to-end cases.

## Backend API version 2

The Work Time module now exposes contract/action version 2. The shared RPC result envelope stays version 1, and settings stay version 1. Its snapshot is `{ timers, pendingEntries, history }`.

`start {description}` creates a named running timer. `pause {id}` retains the timer and closes its current segment; `resume {id}` starts a new segment when no timer is running. `end {id}` moves either a running or paused timer into an unsaved draft. Ending a paused timer leaves a different running timer intact. `discardTimer {id}` removes only a paused timer. `saveEntry {id, description, tags}` and `discardPending {id}` target one ended draft. Saved history still supports `updateEntry` and `deleteEntry` with IDs.

Timer fields are `id`, `description`, `startedUtc`, `status` (`running` or `paused`), `elapsedMilliseconds`, `sampledUtc`, and ordered `segments`. A running timer has one open final segment with `stoppedUtc: null`; drafts and saved entries contain completed segments. Paused duration is excluded from the accumulated elapsed time.

The coordinator writes schema 2 records atomically under its state lock. Timers and multiple drafts survive restart. If a history write completed before draft removal was interrupted, startup keeps the saved entry and removes the duplicate draft. Description limits remain 500 characters, tags remain at most 30 values of at most 40 characters, and history remains bounded to 500 entries. Descriptions and tags are not logged.

Schema 1 active records recover as running timers; schema 1 pending records recover as paused timers. Missing legacy descriptions use the stable “Untitled timer” label and can be edited when saving. Existing elapsed values remain intact. Legacy history has a single continuous segment to preserve its previous report interpretation; old paused-gap boundaries cannot be reconstructed from schema 1 records. A migrated active timer records only the segment boundaries known after recovery; it does not invent boundaries for its unknown prior running time.
