# Bundled Automation Tab Host Foundation

Date: 2026-10-03

## Goal

Prepare the current Electron, React, and .NET application to add functional tabs without duplicating host plumbing. Establish a versioned bundled-module contract, serializable results and follow-up actions, validated action dispatch, and per-module settings with safe migration. Keep Tab 1 behavior and appearance unchanged.

## Architecture

- Keep portable identities and result/action DTOs in `Automator.Core`; do not add Electron, React, WinUI, or Windows dependencies there.
- Put bundled module descriptors, action dispatch, module lifecycle, and typed access to the existing shared automation services in `Automator.Application`.
- Keep the backend as the authority for module registration, action validation, settings migrations, and dispatch. React supplies a view for each registered `kind`; it does not load code dynamically.
- Keep per-module preferences separate from transient renderer state/results. Persist settings as module ID + module schema version + JSON value, preserving entries for modules that are currently unknown.
- Use the existing backend JSON-RPC process, Electron main/preload allowlists, and strict TypeScript/Zod contracts for the action/result path.
- Preserve the small Core `IAutomationTab` identity contract used by the WinUI rollback build; do not make Core depend on the active GUI framework.

## Scope and constraints

- Add no functional Tab 2–9, visual redesign, external plugin loading, Python/Bash execution, arbitrary process access, or long-running job scheduler.
- Do not alter the keyboard or HTTP implementations; a module receives only declared shared services through its scoped context.
- Preserve current launcher commands, settings, startup behavior, and the existing settings import/export path.
- Migrate schema-1 settings to the new schema without dropping launcher bindings, theme, hotkey, startup preference, or unknown module settings. Keep the existing backup-before-replacement behavior.
- Use isolated test data and preserve production settings during verification. Do not initialize Git or create commits in this workspace.

## Implementation plan

### Task 1: Define versioned module and result contracts

Files: `src/Automator.Core/Plugins/AutomationResult.cs`, Core specs, the appropriate `Automator.Application` module-contract files, Application specs.

- Replace string-only result data and label-only actions with versioned, JSON-serializable structured payloads. Keep status and display text concise and framework-independent.
- Define stable module/action identifiers, contract version, metadata/view kind, declared capability requirements, action input, and a result containing follow-up actions with their own typed/serialized payloads.
- Define module lifecycle/action execution boundaries in Application. Actions must accept cancellation and an active module service context; service capabilities must still be declared and enforced by the existing registry.
- Validate duplicate module IDs/slots, invalid action IDs, unsupported contract versions, unknown capabilities, and duplicate action IDs at registration.
- Add failing Core/Application specs first for result round-tripping, validation, cancellation propagation, and a module returning a follow-up action; implement until they pass.

### Task 2: Replace metadata-only lookup with a bundled registry and safe dispatch

Files: `src/Automator.Application/Launcher/LauncherTabRegistry.cs` and related Application files, `src/Automator.Backend/BackendServer.cs`, `src/Automator.Backend/BackendUiState.cs`, `src/Automator.Protocol/RpcProtocol.cs`, Protocol/Application specs.

- Evolve the current metadata provider into a registry of bundled module descriptors/handlers while retaining slots 1–9, Tab 1's existing metadata, and reserved placeholders.
- Add a versioned `module/action` RPC request carrying module ID, action ID, and JSON input. Resolve only registered actions, require the module to be the active tab, enforce its declared capabilities, and return a structured result. Reject malformed payloads, unknown IDs, and stale versions before calling a module.
- Keep current Tab 1 operations on their existing routes unless a small, fully tested adapter is needed; do not rewrite launcher command behavior as part of this foundation.
- Have the registry validate renderer view kinds against the bundled React view registry in a cross-layer contract test. Unknown view kinds must render the existing safe unavailable/reserved state instead of crashing.
- Add a test-only module fixture (not a shipped tab) to prove registration, dispatch, structured result serialization, follow-up action data, inactive-tab rejection, cancellation, and error mapping.

### Task 3: Add module-owned settings and schema migration

Files: `src/Automator.Core/Configuration/LauncherSettings.cs`, settings serializer/store, backend settings import/export handling, Core/Windows/Backend specs, shared settings fixtures as needed.

- Extend settings with a collection of versioned module settings keyed by module ID. Each registered module provides defaults and a deterministic migration path to its current settings version.
- Bump the settings schema and migrate existing schema-1 files on load and import while preserving all existing fields. Validate module IDs, versions, payload size, duplicate entries, and JSON values; retain unknown-module entries for forward compatibility.
- Keep the current corrupt-file protection and atomic backup behavior. A failed migration must leave the original file untouched and report a useful error through existing logs/UI state.
- Test schema-1 load/import migration, schema-current round trips, module migration, unknown module preservation, invalid/oversized data rejection, and failed migration preserving the original file.

### Task 4: Wire the cross-process TypeScript contracts and renderer action entry point

Files: `contracts/rpc.ts`, `electron/main.ts`, `electron/preload.ts`, the exposed bridge types, `ui/bridge.ts`, `ui/viewRegistry.tsx`, and their contract/bridge tests.

- Add strict Zod schemas for module action requests and structured results; preserve the preload allowlist and structured-clone-safe JSON values.
- Expose a typed renderer method for dispatching a registered module action. Route follow-up actions through that same validated method rather than arbitrary command strings.
- Keep React view selection tied to registered `kind` values. Add a registry consistency check so metadata cannot advertise a missing view and a view cannot silently claim an incompatible contract version.
- Do not add controls or new user-visible flows to Tab 1 or reserved tabs. The new action entry point is for the next tab implementation.
- Add failing RPC/bridge/type-level tests for valid round trip, malformed/oversized input, unknown action result, and rejected backend errors; then implement to green.

### Task 5: Integrate, verify, and document extension points

Files: focused architecture notes and any affected test fixtures.

- Add a short module authoring guide documenting registration, contract/version changes, UI `kind` mapping, declared shared services, result/follow-up actions, settings defaults/migration, cancellation, and failure reporting.
- Confirm no external code loading or unscoped service locator was introduced and that the WinUI rollback still builds against the retained Core identity contract.
- Run `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle` as required by `AGENTS.md`; inspect failures and repair them before completion.
- Do not restart the desktop app unless a visible UI change is made. If a restart becomes necessary, use only `scripts/Restart-Automator.ps1` with the documented mode.

## Acceptance criteria

- A test-only bundled module can register, dispatch an action, return structured data and a follow-up action, and receive only the declared services.
- Invalid, stale, inactive, and unregistered module actions fail safely and produce useful diagnostics.
- Existing launcher behavior remains covered and unchanged; no new visible tab is shipped.
- Schema-1 user settings migrate without losing launcher preferences; unknown module settings survive load/save/export; failed migration leaves source data intact.
- C# and TypeScript agree on the versioned JSON shapes, and every registered view kind maps to a safe renderer view.
- Full workspace verification, Windows package creation, and packaged lifecycle checks pass.

## Review checklist

- Is transient result/state data accidentally persisted as preferences?
- Can unknown module settings be silently erased by import, save, or older-module startup?
- Can renderer input bypass registry validation, active-tab checks, or capability authorization?
- Do module errors/cancellation leave the backend command loop responsive?
- Did any change introduce UI-framework dependencies into Core/Application or break the WinUI rollback project?
- Are current Tab 1 routes, shortcut/focus behavior, and settings schema-1 compatibility preserved?
