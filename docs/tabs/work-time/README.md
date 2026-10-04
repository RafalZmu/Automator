# Work Time — slot 7

Work Time logs elapsed work intervals rather than running a Pomodoro cycle. Start an interval when work begins; stop it when work ends, then add a description and optional tags to save the entry. Durations retain seconds, including intervals shorter than one minute.

The history supports filtering by date and tag, editing or deleting entries, viewing totals, and exporting a CSV report. A stopped interval can remain pending for completion after a restart.

## Code map

- ui/modules/FocusSessionsView.tsx renders the Work Time tab; its filename is retained from the older Focus Sessions module.
- ui/modules/workTimeViewModel.ts handles elapsed-time display, filters, tags, report totals, and CSV generation.
- src/Automator.Application/Automation/FocusSessionsModule.cs exposes the slot 7 actions under the legacy module ID.
- AutomationWorkTimeCoordinator.cs persists active/pending intervals and saved entries; AutomationWorkTimeContracts.cs defines their data.
- Tests: tests/electron/work-time-model.test.cjs and tests/Automator.Focus.Specs.

Work Time entries are local application data. The coordinator stores timestamps and descriptions; the UI formats durations from the saved timestamps.
