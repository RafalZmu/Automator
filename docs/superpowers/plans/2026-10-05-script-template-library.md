# Script Template Library Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task, with a fresh reviewer after each task and a final review.

**Goal:** Add a searchable bundled Script Runner template library with typed transient inputs, reusable file/directory path fields, and a Firebird 3 backup-and-ZIP template.

**Architecture:** Keep catalog metadata and parameter validation owned by Script Runner's application module. Install bundled PowerShell assets into a stable per-user directory and create ordinary saved profiles that remain compatible with workflows and schedules. Render template forms in React, pass transient values through the existing module action path, and resolve Explorer drops through a narrowly scoped Electron preload method using `webUtils.getPathForFile`.

**Tech Stack:** React, TypeScript, Electron preload/main IPC, .NET 10 application module, existing automation library/process services, PowerShell, Node tests, .NET specification projects.

**Spec:** `docs/superpowers/specs/2026-10-05-script-template-library-design.md`

## Global Constraints

- Preserve existing saved-profile serialization and legacy profile loading.
- Never concatenate shell command strings. Preserve one structured process argument per value, including paths with spaces.
- Only bundled, registered template IDs and packaged assets are installable; renderer input cannot select arbitrary script or executable paths.
- Installed templates become ordinary Script Runner profiles and keep existing Workflows/Scheduler behavior.
- Template form values are transient. Never persist or log the Firebird password. Its `SYSDBA` and `masterkey` defaults are UI defaults only; local process command-line visibility during execution is an accepted limitation.
- Do not run a script on template selection or installation. Running requires a deliberate user action.
- Treat all scripts as trusted, unsandboxed user code and retain the clear trust notice.
- Preserve bounded output, timeout, cancellation, process cleanup, and caller/tab authorization.
- Avoid disturbing unrelated working-tree changes already present at plan creation.

## File Map

- `contracts/scriptRunner.ts`: renderer-facing template and parameter schemas/type guards.
- `ui/modules/ScriptRunnerView.tsx` and `ui/modules/ScriptRunnerView.css`: installed profile/library navigation, search, template form, install/run states, and result display.
- New `ui/components/PathField.tsx` and stylesheet (or colocated styles): reusable file/directory path entry with text paste, Browse, and one-file drop handling.
- `ui/types.d.ts`, `ui/automationServices.ts`, `electron/preload.ts`, and `electron/main.ts`: typed path resolver and authorized native picker wiring.
- `src/Automator.Application/Automation/ScriptRunnerModule.cs`: template catalog actions, validated installation, transient run mapping, and backward-compatible profile metadata.
- `src/Automator.Application/Automator.Application.csproj` plus bundled template assets: package versioned template scripts as application resources.
- `tests/electron/script-runner.test.cjs`, focused Electron bridge tests, `tests/Automator.Application.Specs/Program.cs`, and any new focused UI contract tests: catalog contracts, path handling, argument mapping, security boundaries, and regressions.
- `docs/tabs/script-runner/AGENTS.md` and `docs/tabs/script-runner/README.md`: current user behavior and contributor invariants.

## Tasks

### Task 1 — Define catalog contracts and compatible profile metadata

1. Add validated TypeScript template descriptors and parameter unions for text, file, directory, boolean, and constrained-choice inputs. Include IDs/versions, interpreter, tags, display metadata, defaults, and ordered argument mappings. Reject unknown types, duplicate keys, oversized values, and invalid mappings.
2. Extend the Script Runner C# profile model with optional template origin metadata while preserving deserialization of existing records. Add catalog descriptors as application-owned data; keep script asset content out of renderer-supplied actions.
3. Add contract and Application specs for a valid Firebird template, invalid descriptor/input shapes, legacy profile round-trip, duplicate IDs, and invalid parameter-to-argument mappings.
4. Run the focused Electron contract and `Automator.Application.Specs` commands; confirm existing profile records still deserialize and invalid renderer inputs are rejected.

### Task 2 — Bundle assets and install templates as profiles

1. Add the Firebird PowerShell asset and register it as an embedded/bundled resource with a stable catalog ID and version. Keep username/password out of the script source.
2. Add a host-side installation service/action that accepts a registered template ID, writes its script into a stable per-user Automator data directory using safe filenames and create-new/atomic semantics, and creates a normal profile with a unique valid profile ID. Never overwrite an existing installed asset silently.
3. Validate that the expected interpreter exists/configures and that required profile paths are absolute. Return a useful repair/install error when PowerShell or the bundled asset is unavailable.
4. Add Application specs for successful install, duplicate install, safe asset path construction, failed/partial writes, and absence of secrets in persisted profile data. Verify existing Workflows/Scheduler saved-profile handler tests continue to pass.

### Task 3 — Add transient template runs and searchable library UI

1. Add an application action to list template metadata and update `runProfile` to accept optional transient template values for profiles with registered template origin. Validate the profile/template relationship and each value in the Application layer, map values to structured argument positions, and run through the existing process service.
2. Ensure values are not written to saved profile data, run activity, or logs; Firebird username/password fields are transient, password masked, defaulting to `SYSDBA`/`masterkey`, and cleared after completion/cancel. Keep actual process argument behavior explicit and preserve cancellation/timeout handling.
3. Add a searchable Library view in Script Runner with name/description/tag search, details/version/trust notice, install, typed form validation, explicit Run, and bounded result/error display. Do not auto-install or auto-run on selection.
4. Add UI/Application tests for search, required/default values, masked transient password, no persistence, safe structured argument mapping for paths containing spaces, run failure, and cancellation. Confirm ordinary saved profile edit/run and workflow/scheduler execution remain unchanged. A template origin may only receive sensitive template values while its script still matches the registered installed asset; editing its script path converts it to an ordinary profile. Validate file/directory inputs as fully qualified paths before dispatch. Document the Firebird profile's interactive-only requirement in the Script Runner guide because password values are not persisted.

### Task 4 — Reusable path field, drag/drop bridge, docs, and full verification

1. Implement a reusable `PathField` with editable path text, Browse callback, and drag/drop handling for exactly one item. Support typed paste naturally through the input; reject multiple/unsupported drops with an accessible message.
2. Add an allowlisted preload bridge method that resolves dropped `File` objects through Electron `webUtils.getPathForFile`. Gate picker IPC to active Script Runner and known file/directory picker kinds; keep dialog options controlled by the host. Never enable renderer Node access or read dropped file contents.
3. Reuse `PathField` for the Script Runner script path, working directory, and template file/directory parameters. Add tests for normal paste, one dropped file, multiple drops, unsupported payloads, picker authorization, and browser-preview fallback behavior.
4. Update Script Runner contributor/user docs with template lifecycle, Firebird inputs/defaults, trusted-code warning, transient credential policy and accepted local process visibility, file path behavior, and current reuse boundaries.
5. Run focused tests, then run `npm.cmd run verify` as requested. Inspect the final diff for legacy profile compatibility, unsaved secrets, exact file scope, and documentation accuracy. Do not package/restart the GUI unless needed and requested by the user.

## Review Focus

- Can malformed renderer input choose an arbitrary template asset, process executable, or argument mapping?
- Do all legacy profiles still load, export/import, run from workflows, and run from schedules without schema migration loss?
- Can Firebird form values or password leak into saved profile data, module settings, run activity, logs, or template assets?
- Does a dropped File use the supported Electron path resolver while keeping context isolation and caller/tab checks intact?
- Does the PowerShell script refuse ambiguous overwrite behavior and stop before ZIP creation when `gbak` fails?
- Are file and directory paths containing spaces preserved as single process arguments?
