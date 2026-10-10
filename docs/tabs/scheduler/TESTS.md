# Scheduler test coverage

This file documents Scheduler recurrence, persisted snapshot, agenda, calendar, and Electron editor visibility tests. Test titles match their source tests. The Electron case creates and edits a disabled schedule using a saved Script Runner fixture; execution remains covered by backend specifications.

Run the focused view-model cases with:

    node --experimental-strip-types --test tests/scheduler-view.test.mjs

Run all contract tests with npm.cmd run test:contracts. Backend recurrence and coordinator specifications are included in npm.cmd run test:dotnet.

## Scheduler view and contract tests — tests/scheduler-view.test.mjs

### scheduler accepts interval/daily/weekly recurrence and rejects stale incompatible fields

**Steps**

1. Validate an interval schedule.
2. Add incompatible local-time data or an invalid interval.
3. Validate a weekly schedule, then introduce duplicate weekdays and an invalid time.

**Expected result:** The valid interval and weekly shapes are accepted; stale fields, invalid intervals, duplicate weekdays, and invalid local times are rejected.

### scheduler snapshot tolerates optional catalogs and excludes arbitrary history output

**Steps**

1. Read a snapshot containing one schedule and a history record with stdout and arbitrary data.
2. Omit optional workflow and script catalogs.
3. Inspect the parsed snapshot and its serialized form.

**Expected result:** Optional catalogs default to empty lists, allowed metadata is retained, and output bodies or arbitrary data are excluded.

### scheduler reader keeps missing workflow references for repair and exact next-run timestamps

**Steps**

1. Read a workflow schedule whose workflow is absent from the catalog.
2. Include an exact timestamp with a non-UTC offset as the next run.
3. Read the schedule recurrence label.

**Expected result:** The missing workflow reference remains available for repair, the timestamp is preserved exactly, and the interval label is derived from the recurrence.

### scheduler reader accepts script profile targets while migrating old workflowId records

**Steps**

1. Read a legacy workflow schedule with workflowId.
2. Add a script-profile target and its catalog entry.
3. Inspect normalized target kinds and script catalog.

**Expected result:** Legacy workflow records remain workflow targets; script-profile targets are accepted and their names are retained.

### agenda sorts one upcoming local-time run per enabled schedule and preserves target and running state

**Steps**

1. Create an agenda snapshot with enabled, disabled, past, workflow, and script-profile schedules.
2. Provide each schedule's host-computed next-run timestamp and a fixed local current time.
3. Build the upcoming entries and inspect order, labels, and running status.

**Expected result:** Only enabled future schedules appear, sorted by their supplied next-run time with target and running state intact; recurrence instances are not generated in the view model.

### calendar groups the next run on its local day and fills a Sunday-first month grid

**Steps**

1. Build one upcoming schedule entry on October 4, 2026.
2. Build the calendar month around October 15, 2026.
3. Inspect grid size, first and last day, and the scheduled and adjacent-month days.

**Expected result:** The calendar has a Sunday-first 35-day grid, places the run on its local date, and leaves adjacent-month dates unmarked.

## Electron happy-path visibility — tests/electron/tab-happy-path-visibility.test.cjs

### Scheduler save happy path keeps create calendar and editor buttons within visible bounds

**Steps**

1. Save an isolated Script Runner profile through the UI to supply a valid schedule target.
2. Open Scheduler, check Calendar and Agenda before switching them, and verify Calendar becomes selected.
3. Check New and Close editor, name the schedule, and clear Enabled so the fixture cannot run automatically.
4. Check Cancel and Save against viewport and clipping ancestors, then save the disabled schedule.
5. Check Enable and Run now for reachability, use checked Edit to rename the schedule, save, and check the updated Edit and Enable buttons.

**Expected result:** Required buttons are CSS-visible, fully inside viewport and ancestor content bounds, and uncovered before use. User wheel scrolling may reveal controls before assertion; click auto-scrolling cannot satisfy it. Creation and editing succeed; the schedule remains disabled. This case does not test schedule execution or month navigation, which is absent when there are no upcoming enabled runs.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.

## Additional backend coverage

Recurrence calculation, restart policy, run claiming, cancellation, and saved script/workflow execution are covered by tests/Automator.Scheduler.Specs and supporting Application specifications. Run the .NET specifications with npm.cmd run test:dotnet.

## Shared numbered-tab icon coverage — tests/electron/tab-happy-path-visibility.test.cjs

### numbered tabs show an identifying icon in the launcher and Workspace

**Source:** tests/electron/tab-happy-path-visibility.test.cjs

**Steps**

1. Launch the isolated compact host and locate slots 1–8 by their exact accessible tab names; verify each contains one visible decorative icon with dimensions of at least 12 by 12 pixels.
2. Open Workspace and verify slots 2–8 each contain one visible decorative icon with dimensions of at least 12 by 12 pixels.

**Expected result:** The numbered tabs retain their accessible names and show one visible icon in both windows.
