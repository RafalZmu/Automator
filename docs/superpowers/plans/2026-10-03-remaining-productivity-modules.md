# Remaining Productivity Modules Implementation Plan

## Goal

Complete the approved productivity-module roadmap on top of the existing Electron/React and .NET host. Slot 1 stays the launcher, slot 2 stays Script Runner, slots 3–7 become API, Browser Automation, Workflows, Scheduler, and Focus Sessions, and slots 8–9 remain reserved. Add PowerShell `.ps1` support to Script Runner and remember each runtime's most recently supplied interpreter path as the default for new profiles.

## Existing foundation

- Module actions already have versioned contracts, a validated renderer/preload/RPC path, cancellation, module-scoped services, and structured results.
- SQLite library storage is available to modules for profiles, workflows, schedules, and histories.
- Shared HTTP and process execution services exist. HTTP requests have host allowlists, private-address policy, limits, normalized cancellation/errors, and redacted diagnostics.
- Script Runner currently supports Python and Bash with per-profile interpreter paths; it has no remembered per-runtime defaults.
- Playwright is present in `package.json`, but its role and runtime packaging must be verified before the Browser Automation tab uses it.
- Shared contracts, protocol, capability registry, production module registry, tab-view-kind contract, and global settings are integration-owned files. Tab agents must not edit these without first sending a proposal to the parent and waiting for an assignment.

## Global implementation rules

1. A tab implementer owns its module provider, its view, and module-focused tests. It may edit those files and existing isolated service consumers.
2. Before changing a capability ID, shared service interface/adapter, JSON-RPC or preload route, protocol version, tab registry/view-kind contract, library schema, global settings, package configuration, or cross-module execution semantics, the implementer sends the parent: the missing behavior, proposed contract/signatures, affected files/modules, security/lifecycle rules, and tests. The parent decides and broadcasts approved shared changes to every affected tab agent.
3. Keep script output and HTTP bodies out of logs. Secret values and browser-session data must never enter library exports or renderer diagnostics.
4. Keep actions cancellable, scoped to the active module or to an explicitly host-authorized workflow/schedule run, and covered by tests. Validate input at the host boundary.
5. Agents use focused tests first, report exact changed files and commands/results, and avoid restarting the shared desktop app while other agents or integration tests are using it.

## Delivery phases and dependencies

### Phase 1 — Script Runner completion (slot 2)

- Add PowerShell as a supported interpreter and accept only `.ps1` scripts for that interpreter.
- Launch the selected interpreter directly with non-interactive/no-profile/file arguments and preserve user arguments as individual process arguments; do not invoke a command shell or concatenate a command string.
- Persist a default executable path by runtime (`python`, `bash`, `powershell`) in Script Runner module settings. Saving a profile records the supplied executable path as that runtime's latest default. A new profile starts with that value; existing profiles retain their own executable path. Keep defaults local and do not put interpreter paths in exports.
- Add a focused regression suite for `.ps1` validation, picker extension, argument boundaries, defaults across profile creation/reload, and unchanged saved-profile interpreter overrides.

### Phase 2 — Shared service contracts (parent-owned integration task)

Stabilize these host-owned contracts before module agents depend on them:

- Credential vault: store/read-exists/delete operations backed by Windows Credential Manager; secrets are referenced by opaque named keys and never returned to renderer on reads. Define how the API module injects a secret into an HTTP request without returning or logging it.
- Browser session: a typed host service for starting/closing an Automator-owned Playwright session, isolated user-data directory, first-use browser installation/status, navigation, and a bounded set of user-visible actions. Use the existing pinned Playwright version only after verifying production packaging/runtime resolution. Session/profile files are local and excluded from exports.
- Browser worker direction: the backend has no request path back to Electron today. The current recommendation is a dedicated bundled Node Playwright worker behind a typed .NET service, with a pinned Node runtime and Playwright dependency explicitly included in Windows resources. This avoids changing the Electron/backend protocol; the worker is an app-owned child process, not a renderer process. Browser artifacts themselves are installed on first use under Automator's data directory.
- Trusted profile execution: a host-owned coordinator and fixed registry of saved-profile handlers for workflow/schedule execution. It validates exact module/profile references, creates narrowly scoped host contexts/grants, enforces cancellation, and returns bounded, versioned UI results only to explicitly requested foreground runs. Include an execution origin (`manual`, `workflow`, or `scheduled`) and correlation/run ID so diagnostics and history can attribute runs without exposing cross-tab dispatch. Inject the same coordinator into Workflow and Scheduler. Its internal execution summary must omit raw stdout, API bodies, credentials, and other step output; persist only safe outcome/category/duration/step states. It must not call the renderer's active-tab-only generic `AutomationModuleRegistry.DispatchAsync` and must not accept arbitrary module action IDs from renderer input.
- Structured script input: extend the process request with optional size-bounded standard input so the workflow coordinator can pass JSON to a saved script without command-line interpolation. Preserve the existing individual argument contract and close stdin immediately after writing the payload.
- Notifications: typed completion notification service with a Windows adapter and no renderer access to raw OS notification APIs.
- Process-lifetime jobs: Scheduler and Focus timers must keep running when their tab is inactive, so they cannot depend on the selected tab's `AutomationServicesContext`, renderer state, or module settings. Backend-owned coordinators start with the tray host, survive tab changes, and cancel jobs on full shutdown. `IAutomationFocusSessionCoordinator` owns focus state and exposes snapshot/start/pause/resume/skip/end/settings/history operations; the UI refreshes on tab activation and derives countdown from an absolute phase end time. Scheduler owns the recurrence worker and uses the shared saved-profile executor. Module actions remain scoped to the active tab while coordinator work is tied to backend lifetime.
- Ensure library ownership supports durable records for `api`, `browser-automation`, `workflows`, `scheduler`, and `focus-sessions`; define a library schema migration only if a concrete record requires it.

The parent will send the resulting signatures, capability names/versions, protocol changes, allowed actions, cancellation model, and owning files to all affected agents before implementation proceeds against them.

### Phase 3 — API module (slot 3)

- Saved API profiles contain method, URL, exact allowed hosts, non-secret headers, optional body/template, response mode, timeout, and named secret bindings. Never store raw secret values in profiles. Host reloads the saved grant by profile ID; renderer run input cannot widen it.
- UI supports profile create/edit/delete/run, secret set/replace/clear without reading it back, and response status/headers/text or parsed JSON display.
- Use a distinct host-owned profile HTTP service that reloads each saved profile's exact-host grants and secret bindings; never accept allowed hosts or secret references from the per-run renderer input. Resolve secrets in memory, then reuse the existing shared HTTP cancellation, redirect, DNS/private-address, and size policies. Fail clearly for missing secrets, invalid profile data, denied hosts, timeout, oversized or non-text responses. Redact exact configured secret values from response-derived display data before results can cross back to React.
- Test secrets remain absent from settings, SQLite records, logs, exports, and action results; test host policy and cancellation using fake transport.

### Phase 4 — Browser Automation module (slot 4)

- Add saved browser profiles and an Automator-managed isolated Playwright session. Install the matching browser on first use and show setup progress/failure in the tab.
- Provide bounded common actions (open URL, inspect title/text/links, click/fill/select, wait, screenshot-to-view if supported) with explicit timeouts and cancellation; no arbitrary renderer-supplied code evaluation in the first release.
- Route every request, redirect, subresource, and WebSocket through the profile's saved exact-host grant. Local/private targets need an explicit profile policy. Playwright request routing does not intercept service-worker requests, so disable service workers when relying on routing and register WebSocket routing before creating pages. The implementation proposal must explain how the host's private-address/DNS policy applies to the browser's actual connections.
- Render action results and allow the user to continue an open profile session. Close sessions on module/application lifecycle as specified; never include profile/session data in exports.
- Test browser installation failures, isolated user-data location, basic navigation/action behavior, cancellation, and cleanup.

### Phase 5 — Workflows module (slot 5)

- Persist ordered steps referencing saved Script Runner, API, or Browser Automation profiles by module ID/profile ID, with JSON input mapping and stop-on-first-failure behavior. A fixed handler registry accepts only those saved-profile types; it does not route arbitrary tab actions.
- Map JSON values into step inputs from literals or earlier-step JSON pointers. Reject duplicate steps, forward/self references, conflicting destinations, unsupported module/profile types, and missing profiles. Pass prior structured output in memory; send JSON to script profiles over bounded stdin, never as a CLI argument.
- Keep current-run step values only in memory for handoff; persist bounded summary history with step status, duration, and a safe error category/message, omitting raw inputs/outputs, script stdout, API bodies, credentials, and Browser session state/path. Never copy generic renderer-facing `AutomationResult.DisplayText` or `Data` to Scheduler/Workflow history without constructing an explicit redacted summary.
- Provide workflow editor for ordered add/remove/reorder, profile selection, input mapping, save/run/cancel, and step-by-step results.
- Test cross-module reference validation, JSON handoff, cancellation, missing profile, and stop-on-failure.

### Phase 6 — Scheduler module (slot 6)

- Persist schedules that run saved workflows while Automator remains in the tray. Support interval, daily, and weekly schedules in local time.
- Skip missed runs by default; an optional setting runs one catch-up after restart. Pause scheduling on full application exit, resume on startup, and avoid duplicate concurrent runs of the same schedule.
- Show next run, enabled/paused state, last result, and run history; expose enable/disable/run-now actions.
- Persist a schedule occurrence claim before execution, lock each schedule against concurrent in-process runs, and mark runs interrupted by full shutdown/startup recovery. Do not promise exactly-once external side effects: document the claim/recovery behavior as at-most-once or at-least-once according to the chosen retry rule.
- Test daylight-saving/local-time edges, missed-run policy, restart recovery, cancellation/shutdown, duplicate prevention, and failure reporting.

### Phase 7 — Focus Sessions module (slot 7)

- Persist configurable focus/break durations, pause/resume/skip/end controls, current phase and summary history. Run one session through repeating Focus → Break → Focus phases until the user ends it; snapshot the chosen durations at session start so later default edits affect the next session only. Pause stores the remaining duration; resume restarts from it; skip advances immediately without a completion notification. Keep the running timer in a backend-lifetime `IAutomationFocusSessionCoordinator`, not module settings, renderer state, or a disposable module context. The UI refreshes a snapshot on activation and derives the countdown from `phaseEndsAtUtc` (or remaining time while paused); no new event-push route is needed in v1.
- At a natural boundary, advance/persist the phase before attempting a Windows notification through the shared typed notification service; notification failure must not roll back timer state. On app restart, resume a future Running deadline or a Paused remainder; mark an expired Running session interrupted without catch-up phase transitions or notifications.
- Test timer transitions, pause/resume, duration validation, tab/app lifecycle, history, and notification adapter outcomes.

### Phase 8 — Library portability and release verification

- Extend export/import for API profiles, workflows, and schedules. Export only secret references, never values; exclude browser user data and run output/history by default. Import reports missing scripts/interpreters/secrets and provides relink/repair state without discarding valid entries.
- Run targeted specs after each module, then `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle` as required by `AGENTS.md`.
- Restart the packaged app through `scripts/Restart-Automator.ps1 -Show` before asking the user to inspect it. Visual verification is separate from automated tests.

## Delegation map

Each tab has a dedicated agent owner. The first delegation pass produced module proposals and cross-cutting reviews; only Script Runner was assigned code implementation in this pass. The parent owns shared contracts, review of cross-cutting proposals, integration files, migration/version choices, and full-suite/package verification. Module agents work in dependency order: Script Runner; API and Browser Automation after Phase 2; Workflows after API/Browser; Scheduler after Workflows; Focus Sessions can proceed once Phase 2 notifications/storage contracts are ready. At most three agents run concurrently; new agents are dispatched as a slot becomes free. Before a shared contract changes, the implementer sends the parent: the missing behavior, proposed contract/signatures, affected files/modules, security/lifecycle rules, and tests. The parent decides and broadcasts approved shared changes to every affected tab agent.

| Slot | Delegated owner | First-pass result |
|---|---|---|
| 2 — Script Runner | `script_runner_power_shell` | Implemented PowerShell support and remembered runtime defaults; focused regressions pass. |
| 3 — API | `api_module_owner` | Proposed write-only Credential Manager and saved-grant HTTP design; no module code yet. |
| 4 — Browser Automation | `browser_module_owner` | Proposed isolated Playwright worker/session design and network-policy constraints; no module code yet. |
| 5 — Workflows | `workflow_module_owner` | Proposed fixed saved-profile executor, JSON handoff, cancellation, and safe history; no module code yet. |
| 6 — Scheduler | `scheduler_tab_plan` | Proposed recurrence, restart, locking, run-origin, and safe-history rules; no module code yet. |
| 7 — Focus Sessions | `focus_tab_plan` | Proposed backend-owned timer state, restart rules, notification behavior, and UI snapshots; no module code yet. |

The owners found a shared dependency before module implementation: one trusted saved-profile executor for workflow and scheduled work, with origin/correlation metadata and a safe internal summary, plus backend-lifetime Scheduler/Focus coordinators and a typed notification service. The parent will finalize and broadcast these interfaces before assigning module implementation work.

## Completion criteria

- Each module is registered at its stable slot with a declared view kind, versioned actions/capabilities, typed services, persistence, cancellation, and focused tests.
- Cross-module workflow and scheduled runs use the host coordinator instead of renderer cross-tab dispatch.
- Script Runner provides Python, Bash, and PowerShell profiles with remembered per-runtime defaults.
- Secret, browser, process, and network data stay within their stated boundaries.
- Full repository verification, Windows package, and packaged lifecycle checks pass; the packaged app is restarted for user inspection.
