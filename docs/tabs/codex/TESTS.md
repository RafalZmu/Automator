# Codex task builder test coverage

This guide describes the Codex tab's renderer, CLI, approval, save/export, and execution integration checks. Node test titles match their source tests; backend specifications are listed by their registered suite name or method.

Run focused Node checks with `node --experimental-strip-types --test tests/rpc-contracts.test.mjs tests/electron/codex-builder.test.cjs`. Run the Codex UI flow after `npm.cmd run build:desktop` with `node --experimental-strip-types --test --test-concurrency=1 tests/electron/ui-flow.test.cjs`. Run Codex Application specifications with `dotnet run --project tests/Automator.Application.Specs/Automator.Application.Specs.csproj --no-restore`; Workflow specifications are in `tests/Automator.Workflows.Specs`.

## RPC and DTO — tests/rpc-contracts.test.mjs

### Codex DTO contracts validate bounded status, draft, run, save, and requested format data

**Steps**

1. Parse valid CLI status, draft with declared inputs/effects, run and save results, and an optional requested format.
2. Try an unknown CLI state, oversized source, and unsupported format.

**Expected result:** Supported bounded shapes pass; malformed or unsupported values fail validation.

## Codex UI and export — tests/electron/codex-builder.test.cjs

### Codex export requires the first-party main frame, active slot 9, and a valid draft ID

**Steps**

1. Authorize an export from the first-party launcher frame while Codex slot 9 is active.
2. Try a non-main frame, another selected tab, missing capability, and malformed draft ID.

**Expected result:** Only a valid request from the active Codex tab is authorized.

### Codex export chooses format extensions and create-new writes never replace user files

**Steps**

1. Resolve the Python, Playwright, and Workflow export extensions.
2. Write a source file, attempt to write over it, and exceed the export size limit.

**Expected result:** Each format gets its matching extension; existing files are preserved and oversized exports fail.

### Codex scope picker selects existing files or folders only from active slot 9

**Steps**

1. Request file and folder dialog options from active Codex slot 9.
2. Try another tab, an inactive launcher, and an unsupported path kind.
3. Inspect the Codex form for both browse actions and the bridge call.

**Expected result:** Users can add existing local files and folders to task scope; websites remain typed as URLs; invalid or inactive-tab picker requests are rejected.

### Codex review flow exposes first-run approval, separate effect confirmation, and success-only saving

**Steps**

1. Inspect the Codex review view and its source display.
2. Check first-run approval, effect confirmation, accepted-scope regeneration, and save visibility conditions.
3. Inspect the browser-preview bridge behavior.

**Expected result:** Generation is configured with no local, browser, app, MCP, hook, or search tools. Source is reviewable, first run requires approval, declared sensitive effects require a separate confirmation, expanded scope requires a fresh draft, and save is available only after a successful unsaved run. Browser preview reports that no Codex process is available.

## Electron UI flows — tests/electron/ui-flow.test.cjs

### Codex slot 9 shows recoverable CLI readiness and blocks export unless active

**Steps**

1. Launch an isolated host and try export before selecting Codex.
2. Open slot 9 and inspect the CLI readiness state.
3. Try export again while Codex is active.

**Expected result:** Inactive-tab export is rejected; the UI reports ready, missing, signed-out, or configuration-error status with a retry/setup path.

### Codex native export does not open a save dialog for an unsaved draft

**Steps**

1. Launch an isolated host with an unsaved draft record.
2. Instrument the native save dialog and request export for that draft.

**Expected result:** Export returns the save-first error and the native dialog never opens.

## Application specifications — tests/Automator.Application.Specs/CodexTaskSpecs.cs

The registered Application case is **Codex task builder validates capability, actions, CLI output and draft IDs**. Its focused specifications cover Codex-only capability binding, malformed actions and structured payloads, CLI readiness and authentication classification, read-only generation arguments, bounded structured results, create-new draft writes, first-run effect confirmation, saving only the run revision, saved Playwright reapproval, workflow semantic-change detection, and rejection of tampered managed Playwright source.

## Workflow and Scheduler compatibility

The Workflow specification **workflow drafts run transiently using only currently saved profile IDs** checks transient Workflow validation and execution. Codex Playwright profiles use named JSON inputs and the registered `playwright-task` profile handler. Workflow runs check the saved task's current approval revision; Scheduler can then run the saved Workflow containing that task. Scheduler does not target Playwright tasks directly.
