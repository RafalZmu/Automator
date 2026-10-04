# Activity and Notifications

Run Activity lists recent cross-module runs with the module, action/profile, status, origin, time, and duration. The Workspace activity view can filter runs, open the related tab, and display a run's result while its renderer-session output is still available.

The structured result viewer supports readable status, text/JSON data, workflow step output, and validated follow-up buttons. A follow-up runs only after the user clicks it. Raw output is transient and is cleared when Automator restarts; persisted activity retains metadata only.

The notification popover shows short completion/failure summaries and navigates to the associated module/activity record. It currently has no separate read/unread or dismissal state.

## Code map

- ui/activity/RunActivityView.tsx renders searchable history and its selected result.
- ui/activity/ResultViewer.tsx displays status, output formats, workflow steps, and follow-up actions.
- ui/activity/NotificationCenter.tsx renders notification summaries.
- ui/activity/transientResults.ts keeps current-window result payloads out of durable history.
- contracts/activity.ts validates and filters metadata records.
- src/Automator.Application/Automation/AutomationRunActivityService.cs persists bounded metadata.
- Tests: tests/activity.test.mjs and tests/Automator.Application.Specs/RunActivitySpecs.cs.
