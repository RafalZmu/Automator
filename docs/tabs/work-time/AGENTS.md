# Work Time contributor guide

Read the root AGENTS.md first. Slot 7 is the Work Time logger; the legacy Focus Sessions module name is retained internally for compatibility.

## Work-time behavior

- Preserve the simple start/stop flow: starting captures a timestamp; stopping creates a pending interval that asks for a description and optional comma-separated tags before it is saved.
- Measure elapsed time with sub-minute precision. Do not round stored durations to whole minutes; format short intervals in seconds.
- Keep persistence and interval calculations in AutomationWorkTimeCoordinator.cs; renderer filtering, duration formatting, reports, and CSV formatting belong in ui/modules/workTimeViewModel.ts.
- Preserve edit and delete behavior for saved entries, date/tag filters, summaries, CSV export, and recovery of a pending entry after restart.
- Avoid silently saving an empty description or losing a pending interval when the user changes tabs or the process restarts.
- Keep the history bounded and do not put work descriptions or tags in application logs.
- Add coordinator specs for time boundaries and renderer tests for duration/report formatting.

## Code and checks

UI: ui/modules/FocusSessionsView.tsx and FocusSessionsView.css (the component name is legacy); view model: ui/modules/workTimeViewModel.ts.
Contracts/coordinator: src/Automator.Application/Automation/AutomationWorkTimeContracts.cs, AutomationWorkTimeCoordinator.cs, and FocusSessionsModule.cs.
Tests: tests/electron/work-time-model.test.cjs and tests/Automator.Focus.Specs.

Update the sibling README when entry fields, recovery behavior, reports, or precision changes.
