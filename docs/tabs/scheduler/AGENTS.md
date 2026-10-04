# Scheduler contributor guide

Read the root AGENTS.md first. Scheduler runs saved script or workflow profiles while Automator is running in the tray.

## Scheduling invariants

- Keep recurrence rules and next-run calculations in AutomationScheduleRecurrenceCalculator.cs; the UI agenda must display Scheduler-provided next-run data rather than inventing a second recurrence engine.
- Supported recurrence is interval, daily, and weekly in local time. Preserve disabled state, pause/resume, run-now, and the optional run-once-after-restart policy.
- Targets are saved Script Runner profiles or saved Workflows. Do not add arbitrary executable paths to a schedule.
- Preserve migration/loading of existing workflow-only schedules.
- The tray host owns the schedule loop. Full app exit pauses scheduling; default restart behavior skips missed executions unless the saved option requests one catch-up run.
- Persist schedule state/claims before notifications or UI events. Keep history metadata bounded and exclude script output, credentials, and HTTP bodies.
- Add deterministic coordinator specs using a fake clock for recurrence, restart, missed-run, cancellation, and script/workflow target behavior.

## Code and checks

UI and calendar view model: ui/modules/SchedulerView.tsx, ui/modules/schedulerAgendaViewModel.ts, and contracts/scheduler.ts.
Module/coordinator: src/Automator.Application/Automation/SchedulerModule.cs, AutomationSchedulerCoordinator.cs, AutomationScheduleRecurrenceCalculator.cs, and AutomationSchedulerContracts.cs.
Tests: tests/scheduler-view.test.mjs and tests/Automator.Scheduler.Specs.

Update the sibling README when recurrence types, target kinds, restart policy, or agenda semantics change.
