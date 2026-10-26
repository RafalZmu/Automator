# Scheduler — slot 6

Scheduler creates interval, daily, and weekly schedules in local time. A schedule targets a saved Workflow, Script Runner profile, Browser Automation profile, or Codex-generated Playwright task. Users can enable/disable it, run it now, edit its recurrence, and choose whether one missed run should execute after Automator restarts. By default, missed runs are skipped.

The tab shows schedule history and an upcoming agenda/calendar. The agenda uses the next-run values supplied by the host scheduler. Scheduling continues while Automator remains in the tray and pauses after a full exit.

## Deleting schedules

The Delete button on a schedule uses a two-step inline confirmation: the first click shows Yes and Cancel, Escape or Cancel dismisses them, and they reset after six seconds.

## Code map

- ui/modules/SchedulerView.tsx contains schedule editing, run/pause controls, history, agenda, and calendar.
- ui/modules/schedulerAgendaViewModel.ts groups host-provided upcoming runs for display.
- contracts/scheduler.ts validates schedule snapshots and recurrence input.
- src/Automator.Application/Automation/SchedulerModule.cs routes tab actions.
- AutomationSchedulerCoordinator.cs persists schedules, claims, and run history; AutomationScheduleRecurrenceCalculator.cs calculates local-time recurrence.
- Tests: tests/scheduler-view.test.mjs and tests/Automator.Scheduler.Specs.

Schedule a saved profile by ID so interpreter and profile validation stay owned by the source module.

A Codex-generated Playwright task (`playwright-task` profile) can be scheduled directly or through a Workflow. Browser Automation Playwright tags and individual tests can be scheduled directly (`playwright-tests` module; ids `tag-<name>` and `test-<id>`); the tag list comes from saved tags and the test list from a cached, time-bounded `playwright test --list`. A Browser Automation profile is scheduled directly and runs its start-URL navigation with no input. The task's current approval revision is checked at run time. The snapshot carries one catalog per target kind (`workflows`, `scripts`, `browserProfiles`, `playwrightTasks`, `playwrightTests`).

## Test case steps

See [TESTS.md](TESTS.md) for recurrence, schedule snapshot, agenda, and calendar cases with their steps and expected results.

## History side panel

Recent runs open as a left side panel in the compact window (the window widens to the left, same behavior as Browser Automation) via the shared `ui/components/SideHistoryPanel.tsx`; the workspace window keeps the inline section. Open state is remembered per tab on this computer. In the side panel the full run record lives in the panel (the run list plus the selected run's details, 520 px wide) and the main panel shows no run results or last-run info; the panel opens itself when a run finishes.
