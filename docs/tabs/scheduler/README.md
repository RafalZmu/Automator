# Scheduler — slot 6

Scheduler creates interval, daily, and weekly schedules in local time. A schedule targets a saved Script Runner profile or a saved Workflow. Users can enable/disable it, run it now, edit its recurrence, and choose whether one missed run should execute after Automator restarts. By default, missed runs are skipped.

The tab shows schedule history and an upcoming agenda/calendar. The agenda uses the next-run values supplied by the host scheduler. Scheduling continues while Automator remains in the tray and pauses after a full exit.

## Code map

- ui/modules/SchedulerView.tsx contains schedule editing, run/pause controls, history, agenda, and calendar.
- ui/modules/schedulerAgendaViewModel.ts groups host-provided upcoming runs for display.
- contracts/scheduler.ts validates schedule snapshots and recurrence input.
- src/Automator.Application/Automation/SchedulerModule.cs routes tab actions.
- AutomationSchedulerCoordinator.cs persists schedules, claims, and run history; AutomationScheduleRecurrenceCalculator.cs calculates local-time recurrence.
- Tests: tests/scheduler-view.test.mjs and tests/Automator.Scheduler.Specs.

Schedule a saved profile by ID so interpreter and profile validation stay owned by the source module.

To schedule a Codex-generated Playwright task, place its saved `playwright-task` profile in a saved Workflow, then schedule that Workflow. Scheduler has no direct Playwright target. The task's current approval revision is checked when the Workflow runs.

## Test case steps

See [TESTS.md](TESTS.md) for recurrence, schedule snapshot, agenda, and calendar cases with their steps and expected results.
