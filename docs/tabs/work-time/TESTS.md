# Work Time test coverage

This file documents Work Time snapshot validation, elapsed-time and report calculations, keyboard shortcut behavior, and its Workspace reporting flow. Test titles match their node:test descriptions.

Run focused model cases with node --experimental-strip-types --test --test-concurrency=1 tests/electron/work-time-model.test.cjs. Run the Electron report flow after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/ui-flow.test.cjs. The complete suite is npm.cmd run verify; backend coordinator specs are included in npm.cmd run test:dotnet.

## Work Time model tests — tests/electron/work-time-model.test.cjs

### work-time snapshot validates active, pending, and saved entries

**Steps**

1. Read a snapshot with a valid active interval.
2. Try a snapshot containing active and pending intervals together.
3. Try a negative active duration and a saved history entry with a blank description.

**Expected result:** Valid state is accepted; conflicting active/pending state, negative duration, and undescribed saved history are rejected.

### work-time snapshot accepts an undescribed pending interval but rejects it as saved history

**Steps**

1. Read an undescribed stopped interval in the pending field.
2. Move the same record into saved history and read again.

**Expected result:** The pending interval is preserved for later completion; saved history still requires a description.

### work-time display clock advances from a sampled backend duration

**Steps**

1. Calculate elapsed time ten seconds after the backend sample.
2. Format durations from one second through one hour and two minutes.

**Expected result:** The display advances from the sampled duration and formats seconds, minutes, and hours without rounding short intervals up.

### work-time working-period duration counts only local 08:00 to 16:00 overlap across midnight

**Steps**

1. Calculate working time for an interval spanning 07:30–17:00.
2. Calculate an interval spanning 15:30 to 09:30 the next day.
3. Calculate an interval entirely outside working hours.

**Expected result:** Only each local day's 08:00–16:00 overlap counts, including across midnight.

### work-time reports calculate local daily and Monday-based weekly totals

**Steps**

1. Create entries on the current day, the current Monday-based week, and the preceding Sunday.
2. Calculate report totals with a fixed local current date.

**Expected result:** Today's totals include today's entry; weekly totals include Monday onward and exclude the prior Sunday.

### work-time report totals and CSV can use working-period durations while preserving timestamps

**Steps**

1. Create an overnight entry spanning two local work periods.
2. Calculate its working-hours duration, daily and weekly report totals, and CSV output with the option enabled.
3. Inspect the stored elapsed duration and timestamps.

**Expected result:** Report and CSV durations use working-hour overlap while the original timestamps and stored elapsed duration remain unchanged.

### work-time reports filter inclusive local dates and tags without changing source entries

**Steps**

1. Create entries on three dates with tags that differ in case.
2. Filter by an inclusive date range and an uppercase tag.
3. Read the distinct display tags and original entry list.

**Expected result:** Matching entries are returned case-insensitively, date bounds are inclusive, tags are normalized for display, and the source list is unchanged.

### work-time CSV export preserves exact milliseconds and escapes spreadsheet formulas and delimiters

**Steps**

1. Create a 1,234 millisecond entry with a formula-like description and a tag containing a comma.
2. Generate its CSV representation.

**Expected result:** Milliseconds and timestamps are exact; CSV quoting escapes delimiters and quotes, and the formula-like description is prefixed safely.

## Legacy Focus Sessions view-model compatibility — tests/electron/focus-sessions-model.test.cjs

The test filename and view-model module retain the old Focus Sessions name for compatibility. These cases cover countdown formatting and snapshot parsing used by the legacy model; they are not the current Work Time interval report flow.

### focus view derives a running countdown from the host deadline and a paused one from the saved remainder

**Steps**

1. Calculate remaining time for a running session with a host-supplied deadline.
2. Calculate remaining time for a paused session with a saved remainder.
3. Calculate remaining time for an idle session.

**Expected result:** Running time derives from the deadline, paused time uses the saved remainder, and an idle session has zero remaining time.

### focus view formats countdowns without showing a completed phase as negative time

**Steps**

1. Format positive, zero, negative, and one-hour durations.

**Expected result:** The clock uses zero-padded minute/second values, formats hours when needed, and clamps completed phases to 00:00.

### focus view accepts only a complete host snapshot with supported settings and session states

**Steps**

1. Parse a complete snapshot whose next-session defaults differ from the active session's frozen settings.
2. Try an unknown session state and an unsupported zero-minute active setting.

**Expected result:** The complete snapshot preserves both sets of settings; unsupported state or settings make the snapshot invalid.

## Shortcut and service tests

### Work Time S shortcut toggles only without modifiers and outside editable controls

**Source:** tests/electron/work-time-shortcut.test.cjs

**Steps**

1. Try lowercase and uppercase S on a non-editable target.
2. Repeat with a modifier, an input, and a content-editable target.

**Expected result:** Only unmodified S outside editable controls is treated as the Work Time toggle shortcut.

### Work Time CSV saving is exposed only to the Work Time module and forwards the file content

**Source:** tests/automation-services.test.mjs

**Steps**

1. Create a module service facade and attempt to access CSV saving without the Work Time module grant.
2. Create the Work Time-scoped facade with its file-save capability.
3. Save CSV content through a fake bridge and inspect the request.

**Expected result:** Only the Work Time module can request the save; the host bridge receives the selected path and file content.

## Workspace end-to-end test — tests/electron/ui-flow.test.cjs

### Work Time reports are Workspace-only and support editing, filtering, CSV export, and deletion

**Steps**

1. Open Work Time in the compact launcher and verify that report controls are absent.
2. Open Workspace, start and stop an interval, then save a description and tags.
3. Filter the saved row, edit its description and tags, then export the filtered report to an isolated CSV path.
4. Verify CSV content, confirm deletion, and check that reports remain absent from the compact launcher.

**Expected result:** Reporting and history editing are available in Workspace only; filtering, editing, export, and deletion work without exposing report controls in the compact launcher.

## Electron happy-path visibility — tests/electron/tab-happy-path-visibility.test.cjs

### Work Time entry happy path keeps timer save and report buttons within visible bounds

**Steps**

1. Open Workspace through its checked button and select Work Time.
2. Check Start work and Stop work before using them; enter a description and tag and use checked Save entry.
3. Check the saved entry's Edit button, change the description, and use checked Save work entry changes.
4. Route the save dialog to isolated data, check Export filtered work log as CSV before exporting, and inspect the CSV.
5. Check Delete and Confirm delete work entry before deleting, then verify the saved row disappears.

**Expected result:** Required buttons are CSS-visible, fully inside viewport and clipping ancestor bounds, and uncovered before use. User wheel scrolling may reveal controls before assertion; click auto-scrolling cannot satisfy it. Timer, save, edit, CSV export, and confirmed deletion complete successfully.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.

## Additional backend coverage

Persistence, recovery, coordinator state transitions, and notification behavior are covered by tests/Automator.Focus.Specs. Run the backend specification suites with npm.cmd run test:dotnet.

## Work Time backend/API version 2

The cases below are executed by `npm.cmd run test:dotnet`. The existing renderer cases above still describe the version 1 UI until its separate migration. Backend tests exercise the current version 2 contract.

### work time requires a description and persists optional tags to history

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Reject blank and overlong start descriptions.
2. Start a named timer, advance twelve minutes, and end it into a draft.
3. Reject blank save descriptions, overlong tags, and more than thirty tags; save valid normalized tags.

**Expected result:** A named draft retains 720,000 milliseconds; save removes the draft and writes schema 2 history with trimmed, deduplicated tags.

### paused work time resumes accumulated time and discards by ID

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Run twenty minutes, pause, and advance thirty minutes while paused.
2. Resume, run seven minutes, pause again, then discard that paused timer by ID.

**Expected result:** The resumed timer begins at 1,200,000 milliseconds; its final 1,620,000 milliseconds exclude the pause, and discard removes it without saving history.

### saved work-time entries can be edited and deleted without losing interval timing

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Run a named timer for seventy-five seconds, end it, and save an entry.
2. Edit its description and tags, then compare timestamps, duration, and segments.
3. Delete it and attempt to delete the same ID again.

**Expected result:** Metadata changes preserve all timing and segments; deletion removes the durable record and repeating deletion fails.

### active work time persists across backend restart

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Start a named timer and advance two minutes before disposing the coordinator.
2. Recreate the coordinator from the same store, advance another three minutes, and end the recovered timer.

**Expected result:** The recovered snapshot shows 120,000 milliseconds; its ended draft preserves the ID and records 300,000 milliseconds.

### multiple named timers exclude pauses and retain independent drafts

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Start A through the real module API, run ten minutes, and pause it.
2. Start B, run twenty minutes, then end and save B.
3. Resume A, run five minutes, end A, inspect its segments, then save A.

**Expected result:** A records 900,000 milliseconds with two completed segments separated by the twenty-minute pause. Both entries are saved and no timers or drafts remain.

### only one timer runs and ending a paused timer preserves another runner

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Start A and reject starting another timer or discarding the running timer.
2. Pause A after five minutes and start B; reject resuming A or B while B runs.
3. Advance ten minutes and end paused A, then save its draft.
4. Reject pause of ended A and discard of a missing timer.

**Expected result:** A keeps 300,000 milliseconds and its actual pause timestamp. B stays running at 600,000 milliseconds through ending and saving A.

### timers and independent drafts survive restart and targeted discard

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Create ended drafts A and B, a two-second paused timer, and a running timer.
2. Dispose, advance twenty minutes, and restore the coordinator.
3. Discard only the paused timer, discard only draft A, then save draft B.

**Expected result:** Both drafts and timers recover independently; paused elapsed stays 2,000 milliseconds and running elapsed advances to 1,200,000. Each targeted removal preserves the other records.

### legacy active pending and history records migrate without losing elapsed time

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Seed schema 1 active state with accumulated elapsed and schema 1 saved history.
2. Restore, inspect the running Untitled timer and continuous legacy history span, then end and save a renamed entry.
3. Seed schema 1 pending state, restore it as paused, resume it, and end it after two seconds.

**Expected result:** Legacy IDs and elapsed values remain recoverable, missing labels become Untitled timer, migrated state is schema 2, and resumed elapsed adds only the new running time.

### work time state writes are atomic and cancellation leaves state intact

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Fail a state write during pause and inspect the still-running timer.
2. Cancel an end operation before dispatch and inspect the unchanged timer.
3. End the timer, then fail draft removal after the history write succeeds; restart.

**Expected result:** Failed or canceled state operations preserve in-memory state. Restart keeps the saved history entry and removes its duplicate draft.

### work time history is bounded and corrupt versions are rejected

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Seed 501 legacy history entries with ordered timestamps and initialize the coordinator.
2. Inspect the retained count and deletion of the oldest record.
3. Try initializing an unsupported schema 99 state record.

**Expected result:** Only the newest 500 entries remain, and unsupported persisted schemas raise InvalidDataException.

### work-time log actions validate metadata and delegate to the host coordinator

**Source:** tests/Automator.Application.Specs/Program.cs

**Steps**

1. Register the Work Time module and verify all declared version 2 action IDs.
2. Dispatch getSnapshot, start, pause, resume, end, discardTimer, and discardPending; inspect snapshot keys and target IDs.
3. Save, update, and delete specific entries, checking descriptions, tags, and IDs.
4. Reject missing inputs, invalid descriptions/tags, and stale module/action version 1 requests.

**Expected result:** Only valid version 2 inputs reach the coordinator. Snapshots expose timers, pendingEntries, and history; all state-changing actions target the supplied ID and the shared result envelope remains supported.

### resumed legacy timer preserves elapsed without fabricating paused segments

**Source:** tests/Automator.Focus.Specs/Program.cs

**Steps**

1. Seed a schema 1 active timer originally started at 09:00, with one hour of accumulated work and a current resume at 15:00.
2. Restore it at 15:00 and inspect its original start, elapsed value, and sole open segment.
3. Advance thirty minutes, end the timer, and save it; inspect the draft and saved running segment.

**Expected result:** The migrated timer preserves its 09:00 original start and 3,600,000 milliseconds of accrued work. Its only known segment starts at 15:00 and closes at 15:30. Draft/history retain 5,400,000 milliseconds total elapsed, while the recorded segment covers only 1,800,000 milliseconds; no segment fabricates the unknown period between original start and resume.
