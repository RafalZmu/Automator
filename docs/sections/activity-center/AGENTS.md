# Activity and Notifications contributor guide

Read the root AGENTS.md first. Run Activity stores bounded metadata across modules; output is transient and must not become durable history or application log content.

- Keep activity records metadata-only: module, action/profile, status, origin, start/finish times, and duration. Never persist stdout, stderr, HTTP bodies, credentials, or browser page content in run history.
- Keep the shared structured/text result presentation in ui/activity/ResultViewer.tsx; transient live results are managed by transientResults.ts.
- Follow-up actions must be validated against the owning module's currently registered action list and only execute after an explicit button click.
- Notification summaries should be short and safe to display. Selecting one must navigate to the correct tab and related run; do not expose raw result payloads in notification text.
- Preserve search/filter and unavailable-output states when the app restarts or a transient result expires.
- Add application and renderer tests when changing persisted metadata, result rendering, action dispatch, or notification routing.

Code: ui/activity, contracts/activity.ts, AutomationRunActivityService.cs, and the activity RPC boundary.
