# Activity and Notifications

Run Activity lists recent cross-module runs with the module, action/profile, status, origin, time, and duration. Playwright test runs have no saved profile and appear with a null profile ID. The Workspace activity view can filter runs, open the related tab, and display a run's result while its renderer-session output is still available.

The structured result viewer supports readable status, text/JSON data, workflow step output, and validated follow-up buttons. A follow-up runs only after the user clicks it. Raw output is transient and is cleared when Automator restarts; persisted activity retains metadata only.

The notification popover shows short completion/failure summaries and navigates to the associated module/activity record. It currently has no separate read/unread or dismissal state.

## Task finished popups

When a run ends (manual, scheduled, workflow, or started by an AI agent over MCP), the Electron host shows a small always-on-top popup in the bottom-right of the primary display's work area. It shows the profile/action name, the module, and whether the run succeeded, failed or warned, does not take focus, auto-dismisses after about 5 seconds, and stacks up to three. The host reads `activity/list` on each `activity/changed` notification and only announces run ids it has not seen; runs that already existed at startup are not replayed. Options > Notifications > Task finished popups turns this off (on by default); the choice is stored by the host in `notifications.json` beside `mcp.json` and applies immediately. Popup text is fixed metadata only.

## Code map

- electron/taskToasts.ts (new-run tracking, text, placement, popup HTML), electron/notificationPrefs.ts (setting storage) and the toast window code in electron/main.ts show the popups. Tests: tests/task-toasts.test.mjs.

- ui/activity/RunActivityView.tsx renders searchable history and its selected result.
- ui/activity/ResultViewer.tsx displays status, output formats, workflow steps, and follow-up actions.
- ui/activity/NotificationCenter.tsx renders notification summaries.
- ui/activity/transientResults.ts keeps current-window result payloads out of durable history.
- contracts/activity.ts validates and filters metadata records.
- src/Automator.Application/Automation/AutomationRunActivityService.cs persists bounded metadata.
- Tests: tests/activity.test.mjs and tests/Automator.Application.Specs/RunActivitySpecs.cs.
