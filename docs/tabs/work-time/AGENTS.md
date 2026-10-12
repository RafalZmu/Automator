# Work Time contributor guide

Read the root AGENTS.md first. Slot 7 is the Work Time logger; the legacy Focus Sessions module name is retained internally for compatibility.

## Work-time behavior

- The backend Work Time module and all its actions use contract/action version 2; the shared RPC result envelope remains version 1.
- Starting requires a description. Retain multiple named timers with stable IDs and explicit running/paused status, with at most one running timer.
- Pause closes the running segment and preserves accumulated elapsed time; resume opens a new segment only when no other timer runs. End accepts running or paused timers and creates an independent durable draft. Ending a paused timer must leave another runner intact.
- Target pause, resume, end, saveEntry, discardPending, and discardTimer by ID. DiscardTimer accepts paused timers only. Saving or discarding one draft must preserve all other timers and drafts.
- Persist new state/history records as schema 2. Recover schema 1 active records as running timers and pending records as paused timers, using “Untitled timer” when no description exists. Preserve legacy elapsed duration without inventing unknown paused boundaries; anchor a migrated pending timer with a zero-length segment at its known stopped time. Read schema 1 history as one continuous legacy segment and preserve its stored elapsed duration.
- Each new running segment records its actual start/end. Exclude paused gaps from elapsed time and working-period calculations. An active timer has exactly one open final segment; drafts and saved entries have completed segments.
- Measure elapsed time with sub-minute precision. Do not round stored durations to whole minutes; format short intervals in seconds.
- Keep persistence and interval calculations in AutomationWorkTimeCoordinator.cs; renderer filtering, duration formatting, reports, and CSV formatting belong in ui/modules/workTimeViewModel.ts.
- Preserve edit and delete behavior for saved entries, date/tag filters, summaries, CSV export, and recovery of timers and independent drafts after restart.
- Avoid silently saving an empty description or losing a timer or draft when the user changes tabs or the process restarts.
- Keep the history bounded and do not put work descriptions or tags in application logs.
- Add coordinator specs for time boundaries and renderer tests for duration/report formatting.
- The transient “Count only working hours” checkbox is off by default and counts only local 08:00–16:00 overlap per calendar day. Apply it to timer and draft displays, saved-entry displays, report totals, and CSV duration columns without changing stored timestamps or durations.
- Keep working-period overlap and duration derivation in `ui/modules/workTimeViewModel.ts`. The checkbox state is component-local and resets when the view is recreated; do not claim it is persisted.
- The unmodified `S` key pauses the running timer, or focuses the New timer description when none is running. Never resume a paused timer implicitly. Ignore the shortcut in editable controls and expose it in the tab command list on both surfaces.
- Keep the New timer description required and visible on both surfaces. A new timer can start only when no timer is running; users pause the current timer explicitly first.
- Render every running/paused timer and every ended draft independently. Resume is unavailable while another timer runs. End/discard/save operations must target the selected ID; discarding a paused timer and discarding an ended draft require confirmation.
- Prefill each ended draft's editable description and tags independently so saving one draft cannot overwrite details for another.

## Code and checks

UI: ui/modules/FocusSessionsView.tsx and FocusSessionsView.css (the component name is legacy); view model: ui/modules/workTimeViewModel.ts.
Contracts/coordinator: src/Automator.Application/Automation/AutomationWorkTimeContracts.cs, AutomationWorkTimeCoordinator.cs, and FocusSessionsModule.cs.
Tests: tests/electron/work-time-model.test.cjs and tests/Automator.Focus.Specs.

Update the sibling README and TESTS.md when entry fields, recovery behavior, reports, precision, shortcut behavior, or automated cases change. Keep test titles and the described steps aligned with the test sources.
