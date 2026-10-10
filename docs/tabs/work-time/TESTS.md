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

## Shared numbered-tab icon coverage — tests/electron/tab-happy-path-visibility.test.cjs

### numbered tabs show an identifying icon in the launcher and Workspace

**Source:** tests/electron/tab-happy-path-visibility.test.cjs

**Steps**

1. Launch the isolated compact host and locate slots 1–8 by their exact accessible tab names; verify each contains one visible decorative icon with dimensions of at least 12 by 12 pixels.
2. Open Workspace and verify slots 2–8 each contain one visible decorative icon with dimensions of at least 12 by 12 pixels.

**Expected result:** The numbered tabs retain their accessible names and show one visible icon in both windows.
