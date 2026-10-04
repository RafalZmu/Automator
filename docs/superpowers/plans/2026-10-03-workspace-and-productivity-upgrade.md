# Workspace and Productivity Upgrade Implementation Plan

> **For implementation agents:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` when available; execute the tasks in order and report cross-boundary changes before editing shared host contracts. User asked to plan and implement this work. Keep tests focused during each task, then run the workspace-wide verification and packaged checks.

**Goal:** Implement the API, Playwright Test Explorer, workflow variables, script scheduling, work-time logging, separate Workspace window, and shared command search described in [the approved product spec](../specs/2026-10-03-workspace-and-productivity-upgrade.md).

**Architecture:** Keep Electron/React as the UI and the .NET backend as the owner of privileged services and durable data. Add typed capability/action contracts behind the existing bridge. A host-owned workspace window manager must assign role and per-window tab context; module scheduling and work-time sessions remain backend-lifetime services. Keep changes additive with safe defaults for existing records.

**Verification:** `npm.cmd run verify`, `npm.cmd run package:win`, then `npm.cmd run test:packaged-lifecycle`. Run visual startup only with `scripts/Restart-Automator.ps1`; do not start a second host directly. Use isolated test settings and preserve production settings.

## Task 1 — Stabilize shared window, command, and execution contracts (parent integration)

**Files:** `electron/main.ts`, preload/bridge/types, `ui/App.tsx`, `ui/viewRegistry.tsx`, `ui/automationServices.ts`, `src/Automator.Backend/BackendServer.cs`, module authorization/registry, shared RPC contracts, focused Electron/RPC tests.

1. Add a host-derived `WindowRole` (`launcher` or `workspace`) and a local selected slot per BrowserWindow. Reject role claims from renderer parameters.
2. Give the workspace a normal resizable window and independent lifecycle. Keep the launcher hide/show/hotkey handshake isolated. Allow a workspace module call through an explicit role-aware backend context without requiring the launcher to be visible/focused. Keep keyboard-hook capabilities unavailable to an unfocused/unauthorized context.
3. Route state/notifications to both windows without allowing launcher visibility changes to show/hide the workspace. Scope settings state globally and selected tab locally. Revalidate native dialogs and module authorization against the caller's window and local selected slot.
4. Add workspace open/toggle IPC and a launcher affordance/menu item. Close the workspace without exiting the tray app.
5. Add typed module command descriptors and a registration API for the active view. Render a keyboard-accessible command bar in slots 2–7; `Enter` executes only one unique command, ambiguous matches present options, and command forms can request focus targets. Preserve Tab 1 search.
6. Add focused tests for two-window ownership, workspace actions while launcher is hidden, independent tabs/visibility, invalid role spoofing, and command matching/ambiguity/explicit execution.

**Checkpoint:** Parent publishes the role-aware request shape and command registration interface before tab agents depend on them.

## Task 2 — API profiles and Chrome cURL import (slot 3)

**Files:** `ui/contracts/api.ts`, `ui/modules/ApiView.tsx`, API profile model/validator/runner, RPC tests and API module specs.

1. Add optional `defaultInput` to each profile with backward-compatible deserialization; remove the shared tab-global input control. Run input defaults to the profile's value and can be edited per request.
2. Remove editable `allowedHosts` from the standard form and request input. Derive an exact host grant from the saved URL at the host boundary, preserve same-host redirects and network/DNS/private-address safeguards, and fail closed on cross-host redirects.
3. Add a paste-cURL dialog accepting Chrome's Copy as cURL (Windows cmd and Bash quoting). Parse supported `-X/--request`, `-H/--header`, `--data*`, and URL forms into an editable unsent profile draft; reject unsupported shell substitutions instead of evaluating them.
4. Detect Authorization, Cookie, and proxy credential headers. Provide a vault-save/secret-reference step; redact values in UI preview, logs, exports, and saved non-secret headers. Never auto-run after import.
5. Test parser quoting/edge cases, URL-only grant behavior, profile-local input, secret redaction, and legacy profile loading.

## Task 3 — Workflow variables (slot 5)

**Files:** workflow UI contracts/view, `AutomationWorkflowContracts`, `AutomationWorkflowEngine`, `WorkflowModule`, workflow/application specs, library import/export compatibility.

1. Add `variables` as an optional dictionary of bounded JSON values to saved workflow profiles; old records default to `{}`.
2. Replace the transient initial-input textarea with editable key/value rows. Parse values as JSON, enforce key/size/duplicate limits, and preserve arrays and primitive types.
3. Use the saved dictionary as the root input object when the run starts; retain step references to root variables and earlier outputs. If a legacy caller supplies `initialInput`, define deterministic compatibility precedence and document it.
4. Test save/reload/import, array and scalar values, invalid/duplicate keys, input mappings, and legacy profiles.

## Task 4 — Script and workflow schedules (slot 6)

**Files:** scheduler contracts/view/module/coordinator and saved-profile executor, import/export service, scheduler specs and UI tests.

1. Replace `workflowId` with a versioned target union (`workflow`/`script-profile`) while accepting legacy `workflowId` records as workflow targets.
2. Extend scheduler coordinator dependency injection to the host-owned saved-profile executor; validate module/profile references before save and again before execution.
3. Run scripts as the current user through the existing structured process API, bounded lifetime/output, cancellation, and safe summary history. Do not store arbitrary paths/arguments in a schedule.
4. Update the UI to choose target kind and then a saved workflow or script profile; show target state, last run, run-now, pause, edit, and delete.
5. Test old schedule migration, both targets, missing profiles, script exit/timeout/cancel, occurrence claim/restart policy, and duplicate execution prevention.

## Task 5 — Work Time tab (slot 7)

**Files:** focus module/provider and host coordinator/contracts, new work-time records/repository, slot metadata/view registry, `WorkTimeView`, specs, module/bridge tests.

1. Add a backend-owned stopwatch coordinator with one active record, persisted start/pending-stop state, and start/stop/resume/save/discard/list actions. Preserve the old focus coordinator and old records for migration safety, but remove its slot-7 presentation.
2. Capture stop time before prompting; persist a pending record. Require description to finalize, accept optional normalized tags, and allow resume if the user wants to continue tracking.
3. Build a work log list, elapsed-time display based on timestamps, daily/tag totals, and recent entries in a keyboard-friendly view. Do not add Pomodoro phases, break settings, or auto notifications.
4. Test restart survival, pending metadata flow, resume/discard, duration correctness, description/tag validation, history queries, and legacy focus data preservation.

## Task 6 — Playwright Test Explorer (slot 4)

**Files:** Browser Automation module/capability/service, process runner integration, local project/tag persistence, `BrowserAutomationView`, preload dialog API, Browser Automation contracts/specs, packaging only if the existing self-contained Node/browser runtime is insufficient.

1. Add local project-root selection and safe path validation. Resolve the project-owned Playwright Test CLI; do not run package installation automatically.
2. Add host operations to discover with `playwright test --list`, create a test file from a validated relative name/template, write exact Playwright test-list filters for one file/test/tag group, and run with bounded time/output/cancellation. Retain existing workflow browser-profile handler for saved workflows.
3. Parse test-list output into file sections and test identities (relative file, source location, title, project). Persist tag metadata keyed to project + identity, not source text. Reconcile stale identities when listing.
4. Add UI for project selection, file sections, test rows/results, file/test/tag run, tag assignment, create section/test, refresh, cancel, and missing-runner guidance.
5. Add tests for listing parse/identity, section creation path traversal rejection, per-test exact list, tag group selection, runner missing, output bounds, cancellation/process tree, and project isolation.

## Task 7 — Per-tab command search and Workspace integration

**Files:** shared command contracts, `ui/App.tsx`, per-tab views, host surface routes/tests.

1. Connect each tab to command registration with built-in commands and dynamic saved-profile/test/schedule commands. Add stable keyboard focus shortcut, arrows/Enter/Escape behavior, accessible labels, and no execute-on-type behavior.
2. Ensure command actions focus their editor or field; add needed IDs/refs in views. Mark risky commands with clear confirmation or a second explicit Enter when they would run scripts, APIs, browser code, or delete data.
3. Add Workspace open button/tray action and responsive full-window layout. Keep the quick launcher narrow and its existing alias workflow unchanged.
4. Test every registered tab's minimum commands, uniqueness/ambiguity handling, keyboard-only create/run/edit path, and separate window open/close/selection behavior.

## Task 8 — Integration, packaging, and launch

1. Run focused suites after each task, fixing shared contract regressions before continuing.
2. Run `npm.cmd run verify` and record exact passing counts.
3. Run `npm.cmd run package:win` and `npm.cmd run test:packaged-lifecycle`.
4. Restart through `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Restart-Automator.ps1 -Show` so the visible instance is the built package. Inspect startup logs and verify the launcher plus workspace entry point open.
5. Update `docs/superpowers/plans/2026-10-03-workspace-and-productivity-upgrade-progress.md` with implementation status, focused verification, full verification, package/lifecycle results, and outstanding limitations.
