# File Explorer Actions for Script Runner Implementation Plan

> **For agentic workers:** Execute sequentially through the approved subagent workflow. Each task uses test-first implementation and a reviewed local commit.

**Goal:** Add a per-user classic File Explorer submenu that maps file extensions to saved Script Runner profiles and opens a confirmed run form with the selected file supplied.

**Architecture:** Persist action mappings in Script Runner's Library collection. A typed Windows capability reconciles those mappings into Automator-owned HKCU cascading-menu keys. Electron forwards startup and second-instance selections through preload to Script Runner; the backend validates mappings and paths before the form can run an action.

**Tech Stack:** TypeScript, React, Electron, C#/.NET 10 Windows, Node tests, .NET specs, Windows Registry.

**Spec:** User-approved implementation request in the current conversation; no separate design spec.

## Global Constraints
- Scripts remain trusted and execute as the signed-in Windows user.
- Keep file paths as individual process arguments; never concatenate user text into a shell command.
- Write only Automator-owned per-user registry keys; do not change default file associations or require elevation.
- Test mode must never write the production registry or settings.
- Template secrets and per-run values remain transient and are not logged.
- Enable registration only for the stable packaged host, not development or portable builds.
- Preserve all uncommitted edits in the original checkout; work only in the managed feature worktree.
- Update Script Runner AGENTS.md, README.md, and TESTS.md with behavior and test changes.
- Before completion run `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle`.

## Review Focus
1. Paths with spaces/Unicode remain one argument and reach the mapped profile.
2. Unknown actions, mismatched extensions, stale profiles, and invalid paths cannot execute.
3. Removing a mapping deletes only Automator-owned registry entries.
4. Development, portable, and test hosts do not register actions.
5. Firebird credentials and one-run edits are not persisted or logged.

---

### Task 1: Script Runner action contracts and templates

Create `ExplorerActionDefinition { id, profileId, label, extensions, fileParameterKey? }` in the `script-runner/explorer-actions` Library collection. Add version-1 module actions `listExplorerActions`, `saveExplorerAction`, `deleteExplorerAction`, and `runExplorerAction`. `runExplorerAction` resolves the action and profile on the backend, validates a fully qualified existing file and matching extension, accepts transient arguments/template values, and invokes only the existing structured process service. Regular profiles require `{{file.path}}` in their saved arguments; template mappings select a declared File/Directory parameter that receives the clicked path. Profile deletion removes its mappings. Add an opt-in plain Firebird Backup template beside the existing Backup and ZIP template; transient inputs and redaction rules remain in effect.

**Files:** `ScriptRunnerModule.cs`, `ScriptRunnerTemplateCatalog.cs`, script-runner contracts, Application specs, and the bundled backup asset/catalog specs.

- [ ] Write and run failing specs for mapping CRUD/validation, profile deletion, path token substitution, invalid paths/extensions/actions, transient values, and Firebird Backup template behavior.
- [ ] Implement the records, actions, capability contract, and template; run focused specs and commit.

### Task 2: Electron launch and single-instance handoff

Add `FileExplorerLaunchRequest { actionId, filePath }` and a typed preload subscription with unsubscribe. Parse `--automator-file-action <actionId> -- <filePath>` at first launch and in `second-instance`; queue requests until the renderer is ready, focus Automator, select Script Runner, then deliver the request. Do not expose generic process execution.

**Files:** `electron/main.ts`, `electron/preload.ts`, `contracts/rpc.ts`, `ui/bridge.ts`, Electron tests.

- [ ] Write and run failing tests for cold launch, existing-instance launch, queuing, malformed args, Unicode/spaced paths, and single-backend behavior.
- [ ] Implement the handoff; run focused Electron and packaged lifecycle specs and commit.

### Task 3: Windows static cascading-menu adapter

Implement `IAutomationFileExplorerMenu.ReconcileAsync(entries, executablePath, cancellationToken)` in the Windows adapter and grant it only to Script Runner. Register an `Automator` parent and action children under per-user `SystemFileAssociations` extension keys. Reconcile only Automator-owned keys and notify the shell after updates. Backend receives the host executable path from Electron only for stable packaged, non-portable, non-test runs.

**Files:** Windows adapter/capability composition, `AutomationCapabilityRegistry.cs`, backend host composition, Windows specs.

- [ ] Write and run failing specs using an injected registry store for add/update/remove, grouping, preservation of unrelated entries, and disabled-host behavior.
- [ ] Implement and compose the adapter; run focused Windows/Application specs and commit.

### Task 4: Script Runner UI, docs, and complete verification

Add a Script Runner Explorer Actions section to map a saved profile to one or more normalized extensions, set its label, and choose the source file parameter for templates. A launch request opens a transient form. Template forms prefill the selected file; regular profiles show their saved arguments with `{{file.path}}` resolved and allow one-run edits. Only explicit Run dispatches `runExplorerAction`; per-run edits are not saved. Show useful stale-profile, invalid-path, and registry errors. Update the Script Runner contributor guide, README, and TESTS.md.

**Files:** `ui/modules/ScriptRunnerView.tsx`, Script Runner styles, Script Runner docs pair and test guide, Electron UI tests.

- [ ] Write and run failing UI/Electron cases for map/edit/remove, file prefill, transient edits, template form behavior, Run/Cancel, and stale-profile errors.
- [ ] Implement UI and docs; run `npm.cmd run verify`, package with `npm.cmd run package:win`, then run `npm.cmd run test:packaged-lifecycle`.
- [ ] Smoke-test the classic menu in File Explorer on Windows 10/11: mapped types show actions, unmapped types do not, and execution waits for Run. Commit.

