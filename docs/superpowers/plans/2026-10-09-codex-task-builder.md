# Codex task builder implementation plan

> **For agentic workers:** Implement one task at a time using `superpowers:subagent-driven-development`. Each implementation task gets a fresh Luna 6 medium worker and an independent Luna 6 medium reviewer. Do not start the next task until the current task review and focused verification are complete. The user asked for no stage-by-stage status messages; keep the checks and progress in this plan and report once at the end.

**Goal:** Add a dedicated Automator Codex tab that uses the installed Codex CLI to turn a user-described task into a reviewed, runnable Python script, Playwright task, or workflow; only save reusable work after a successful first run and an explicit save choice.

**Architecture:** The slot-9 Codex module owns declared RPC actions. A host-only typed Codex task service handles local CLI discovery, JSON-schema generation, draft storage, execution, profile persistence, revision approval, and export. Codex receives an ephemeral authoring request with known execution and integration features disabled plus a read-only sandbox; Automator's existing process, Script Runner, Playwright Explorer, saved-profile, workflow, and Scheduler paths perform the reviewed task. Generated Python is saved as a normal Script Runner profile. Generated Playwright specs are saved under the separate `playwright-task` profile handler, are callable as Workflow steps with JSON inputs, and can be scheduled by scheduling a containing Workflow (the current Scheduler has no direct Playwright target). Generated workflow drafts run against existing saved profile IDs and save as ordinary Workflow profiles.

**Spec:** `docs/superpowers/specs/2026-10-09-codex-task-builder-design.md`

**Base:** `8d02dfafc8f4d6c892957f76c333cc6a8af12db5` in the isolated worktree. The original checkout has uncommitted changes and is not part of this branch.

## Progress

- **Task 1 complete.** Added the host-side Codex CLI and draft service, preserved proposed scope/input/effect declarations, bounded result-file reads, and made draft writes atomic and create-new. Focused Application specs: `dotnet run --project tests/Automator.Application.Specs/Automator.Application.Specs.csproj` — 53/53 passed. Independent read-only review: clear.
- **Task 2 complete.** Reused host runners and added saved-profile/workflow support. Focused Application, Workflow, Infrastructure, and Browser Infrastructure specs passed; an independent final review found no actionable issues. Additional regressions cover crash recovery, cross-format mismatches, and source changes during reapproval.
- **Task 3 complete.** Added the Codex slot-9 view, bounded DTOs, requested-format handling, first-run/effect/scope approval flow, native file/folder scope pickers guarded to the active Codex tab, saved-task review/export, and active-tab-only create-new native export. Export is enforced as saved-only in both the Application service and before the Electron save dialog. Focused checks passed: TypeScript typecheck; renderer and Electron builds; Application specs (53/53, including unsaved-export rejection); RPC/Codex tests; and focused Electron slot-9/export flows (including the unsaved-draft flow proving no dialog opens). Independent reviews found no actionable issues. Full repository verification remains Task 5.
- **Task 4 complete.** Added the Codex contributor guide, feature/code overview, and test steps; updated the bundled-tab architecture overview, root tab index, and Script Runner, Browser Automation, Workflows, and Scheduler documentation for generated profiles, approval boundaries, Workflow inputs, and scheduling through a saved Workflow. Focused checks passed: RPC, Workflow, and Browser Automation contracts (28/28); Codex review/export checks (3/3); local Markdown links across 18 changed/relevant files; whitespace checks; and `git diff --check`. A workspace-wide link scan also found a pre-existing unresolved artifact link in `docs/superpowers/plans/2026-10-03-workspace-and-productivity-upgrade-progress.md`; that unrelated file was not changed. The first sandboxed Node test attempt hit `spawn EPERM`; rerunning the focused checks with host permission passed. Full repository verification remains Task 5.
- **Task 5 complete.** After adding the active-tab Codex file/folder scope picker, `npm.cmd run verify` passed all TypeScript, renderer/Electron/backend build, contract, Electron, and .NET specification checks. `npm.cmd run package:win` produced the unpacked and portable Windows x64 packages; `npm.cmd run test:packaged-lifecycle` passed (1/1). A help-only CLI invocation confirmed the supported global approval-option position, and an isolated `features list` check showed the known action-capable feature flags disabled. No live generation request was sent and no Codex user configuration was edited. The fixed launcher-size assertion was adjusted for the observed one-pixel Windows bounds rounding and passed.

## Global constraints

- Use `codex exec --json` with a generated schema and read-only sandbox for authoring. Never use Codex to run the requested user task. Do not edit Codex CLI configuration or add an Automator login/API-key path.
- Invoke the CLI through structured process arguments. Do not shell-concatenate user text, code, paths, or inputs. Keep requests, event output, script output, and secrets out of application logs.
- Run authoring in an ephemeral CLI turn: preserve CODEX_HOME sign-in, ignore user CLI configuration for this turn, disable known local/browser/app/MCP/plugin/search features and hooks, and retain the read-only sandbox as defense in depth. The configured default model is used for this call. Generated Python and Playwright execute with the current user's permissions. Never claim scope enforcement for generated code.
- Validate every action payload, generated JSON response, script size, identifier, path, scope item, input declaration, and effect declaration in the host. Draft and generated files use unique create-new paths under Automator-managed storage; never silently overwrite an asset.
- Require explicit first-run approval. When planned effects include overwrite, delete, or submit/send, require an additional confirmation immediately before starting that effectful task. Saving is available only after success. Revisions to source/scope invalidate prior approval; expose a review/reapprove action before manual/workflow/scheduled execution.
- Keep Playwright project and dependency selection host-owned. Use the configured managed project by default, enforce containment/reparse-point checks, and do not change external project dependencies.
- Preserve old Script Runner profiles, Browser Automation profiles, workflow records, schedule records, settings schema 1, and existing tab behavior. Existing Scheduler can schedule Script Runner profiles or Workflows; schedule Playwright tasks by placing them in a saved Workflow.
- At each task end, run its focused checks and an independent diff review before starting the next task. Final verification follows root `AGENTS.md`: `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle`; use isolated test data and preserve production settings.

---

### Task 1: Add the host-side Codex CLI and task builder service

**Files:**
- Create: `src/Automator.Application/Automation/CodexTaskContracts.cs`
- Create: `src/Automator.Application/Automation/CodexCliClient.cs`
- Create: `src/Automator.Application/Automation/CodexTaskService.cs`
- Create: `src/Automator.Application/Automation/CodexTaskModule.cs`
- Modify: `src/Automator.Application/Automation/AutomationServicesContext.cs`
- Modify: `src/Automator.Application/Automation/AutomationCapabilityRegistry.cs`
- Modify: `src/Automator.Application/Launcher/LauncherTabRegistry.cs`
- Modify: `src/Automator.Backend/BackendServer.cs`
- Create/modify: `tests/Automator.Application.Specs/CodexTaskSpecs.cs`
- Modify: `tests/Automator.Application.Specs/Program.cs` only to register the new specs

**Interfaces and behavior:**
- Add a host-owned `codex.task-builder` capability exposed only to module ID `codex`. Its service has typed operations for CLI status, generate draft, inspect/list drafts, run a draft, save a successful draft, reapprove a changed saved task, and export source.
- Add module actions `getStatus`, `generateDraft`, `getDraft`, `listDrafts`, `runDraft`, `saveDraft`, `approveChanges`, and `exportDraft`; validate action payloads in the module and return bounded `AutomationResult` values.
- CLI discovery supports the standard Windows Codex CLI installation. Use a supported `codex.exe` or execute the discovered npm `codex.ps1` through the absolute Windows PowerShell executable with argument-list entries. A missing/unsupported launcher, signed-out CLI, or configuration parse error returns a recoverable setup status and does not modify user config.
- Run `codex --ask-for-approval never exec --json --ephemeral --ignore-user-config --sandbox read-only ...` with the global approval option before the `exec` subcommand. The request arrives over stdin from a unique Automator-managed request directory. Disable known action-capable features, parse the schema-validated result, and use the read-only sandbox as defense in depth.
- Persist drafts under the per-user Automator data directory using unique IDs, safe file names, create-new writes, JSON size bounds, source hashes, declared scope, inputs, effects, run outcome, and save status. Draft actions use IDs only; renderer cannot supply executable paths or script text for a run.
- Register the slot-9 module with the title `Codex`, `codex` view kind, and `codex.task-builder` capability. Include module tests for capability binding, CLI discovery/status/error classification, schema output parsing, size/path validation, create-new draft files, and failure behavior.

**Steps:**
1. Add failing Application specs for the typed capability, module actions, CLI result parsing, invalid/truncated JSON, unavailable CLI, authentication/configuration errors, path validation, and no-overwrite draft writes.
2. Run the focused Application specs and confirm they fail for the missing behavior.
3. Implement the typed service, safe CLI client, draft store, module action validation, provider registration, and backend factory wiring.
4. Re-run focused Application specs; inspect that Codex's execution arguments select the read-only sandbox and never run a generated artifact.

### Task 2: Reuse current runners and save workflow-callable tasks

**Files:**
- Create: `src/Automator.Application/Automation/CodexTaskExecution.cs` or an equivalent shared executor
- Modify: `src/Automator.Application/Automation/ScriptRunnerModule.cs`
- Modify: `src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs`
- Modify: `src/Automator.Application/Automation/BrowserAutomationModule.cs` only where explorer actions need shared generated-test support
- Modify: `src/Automator.Application/Automation/PlaywrightTestExplorer.cs`
- Create: `src/Automator.Application/Automation/PlaywrightTaskSavedProfileHandler.cs`
- Modify: `src/Automator.Application/Automation/AutomationSavedProfileExecution.cs` only if a typed registered extension is needed
- Modify: `src/Automator.Application/Automation/AutomationWorkflowContracts.cs`
- Modify: `src/Automator.Application/Automation/AutomationWorkflowEngine.cs`
- Modify: `src/Automator.Application/Automation/WorkflowModule.cs`
- Modify: `src/Automator.Core/Automation/AutomationProcessContracts.cs`
- Modify: `src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs`
- Modify: `src/Automator.Backend/BackendServer.cs`
- Create/modify: `tests/Automator.Application.Specs/CodexTaskExecutionSpecs.cs`
- Modify: `tests/Automator.Workflows.Specs/Program.cs`
- Modify: `tests/Automator.Infrastructure.Browser.Specs/PlaywrightAutomationBrowserServiceSpecs.cs` only if needed for the generated-task execution seam

**Interfaces and behavior:**
- Factor Script Runner's profile validation/process invocation/output parsing into a shared host method. A Python draft uses that path once without creating a Script Runner profile. On save, create a normal `script-runner/profiles` record and managed `.py` asset with optional Codex revision/approval metadata.
- Extend `AutomationProcessRequest` with a bounded optional environment map to pass the single serialized workflow input to generated Playwright code as `AUTOMATOR_WORKFLOW_INPUT_JSON`; validate key/value counts and lengths in Core/Infrastructure. Keep all ordinary process arguments structured.
- Add generated Playwright creation/run to `PlaywrightTestExplorer`: write only create-new `.spec.ts` files under the managed project's dedicated Codex test directory; enforce root containment, reparse checks, per-file source limits, and an allowlisted relative path. Reuse managed project and runner resolution; do not require a user-selected project.
- Save Playwright tasks as `playwright-task/profiles` records with relative test path, managed project identity, named input definitions, declared scope/effects, plan, code hash, and approved revision. Add a host saved-profile handler under module ID `playwright-task`, registered in `BackendServer`, so Workflows can call the task with JSON input. Reject execution if the project/path is no longer safe or the reviewed hash no longer matches.
- Add a transient `AutomationWorkflowEngine` run path for a validated unsaved workflow definition composed only of existing host-listed profile IDs. On successful first run, save it as a normal `workflows/profiles` record. Add `playwright-task` to workflow profile module validation/catalog; mappings remain existing named JSON inputs.
- Run saved Python, Playwright, and workflow tasks only when their current source/scope revision matches the reviewed approval metadata. `approveChanges` updates that metadata only after the task's current source/declared scope are shown and the user confirms. An existing Scheduler may run generated Python profiles directly and Playwright tasks through a saved Workflow.
- Add host tests for draft run without persistence, first-success-only save, explicit effect confirmation, profile serialization compatibility, Python revision mismatch rejection/reapproval, managed-project spec creation/containment/no-overwrite, Playwright input forwarding and hash validation, workflow draft validation/run/save, and scheduled/workflow profile execution compatibility.

**Steps:**
1. Add failing specs for Python draft execution, source/scope revision checks, Playwright generated-file containment, named inputs, and transient workflows; run focused Application/Workflow specs.
2. Factor and implement the shared Python run path plus optional approval metadata and verify old Script Runner profiles still load.
3. Add generated Playwright file/run support and its saved-profile handler; verify input forwarding and path checks.
4. Add transient workflow validation/run and `playwright-task` catalog support; verify run order, failure/cancellation, and named input mappings.
5. Run all focused Application, Workflow, Browser Infrastructure, and Process Infrastructure specs; review the full diff for backward compatibility.

### Task 3: Add the Codex tab and user approval flow

**Files:**
- Create: `contracts/codex.ts`
- Modify: `contracts/bundled-tab-view-kinds.json`
- Modify: `ui/viewKinds.ts`
- Modify: `ui/viewRegistry.tsx`
- Modify: `ui/bridge.ts`
- Modify: `ui/App.tsx`
- Create: `ui/modules/CodexView.tsx`
- Create: `ui/modules/CodexView.css`
- Modify: `ui/types.d.ts`
- Modify: `electron/main.ts`
- Modify: `electron/preload.ts`
- Modify: `tests/rpc-contracts.test.mjs`
- Create/modify: `tests/electron/codex-builder.test.cjs`
- Modify: `tests/electron/ui-flow.test.cjs`
- Modify: `tests/electron/backend-process.test.cjs` only if the Codex export/dialog path needs a new bridge assertion

**Interfaces and behavior:**
- Zod DTOs validate CLI status, draft plan/code, scope, input declarations, effect declarations, run state, save result, and approval mismatch responses.
- Codex tab shows CLI readiness/setup, task prompt and selected scope, generation progress/error, a plain-language plan, format picker, complete read-only source, declared inputs/effects, and task outputs. Run and effect confirmations are separate explicit buttons. Only successful runs show the Save action. Saved task changes display a review/reapprove interaction.
- Add a narrow Codex-only native file save dialog for export. Main verifies first-party main-frame sender and active `codex` module, returns a selected destination to the action flow; host export writes create-new and never silently replaces an existing user file. No general filesystem or IPC API is exposed.
- Bind all UI calls to `services.modules.dispatch` for module `codex`; cancellation uses AbortSignal. Browser preview has no Codex process; show setup/unsupported messaging without executing a fake task.
- Add slot-9 icon, label, tab registry view-kind entries, a bounded test fixture, and tests for source visibility, required approvals, post-success save, CLI error recovery, export authorization, and no overwrite.

**Steps:**
1. Add failing RPC schema and Electron bridge/UI-flow tests for Codex module registration, export authorization, and approval UI states; run focused Node/Electron tests.
2. Implement the schemas, bridge save-dialog path, tab registry/view, and builder component using existing layout and action patterns.
3. Run focused Node/Electron tests and TypeScript typecheck; check accessibility labels, responsive source preview, and no API key/login UI.

### Task 4: Complete documentation and focused repository checks

**Files:**
- Create: `docs/tabs/codex/AGENTS.md`
- Create: `docs/tabs/codex/README.md`
- Create: `docs/tabs/codex/TESTS.md`
- Modify: `docs/tabs/script-runner/AGENTS.md`
- Modify: `docs/tabs/script-runner/README.md`
- Modify: `docs/tabs/script-runner/TESTS.md`
- Modify: `docs/tabs/browser-automation/AGENTS.md`
- Modify: `docs/tabs/browser-automation/README.md`
- Modify: `docs/tabs/browser-automation/TESTS.md`
- Modify: `docs/tabs/workflows/AGENTS.md`
- Modify: `docs/tabs/workflows/README.md`
- Modify: `docs/tabs/workflows/TESTS.md`
- Modify: `docs/tabs/scheduler/AGENTS.md` and README only if scheduling guidance changes
- Modify: `README.md` tab index

**Steps:**
1. Update feature/code guides for Codex generation, CLI setup, plan/code review, draft runs, save/export, approval invalidation, workflow inputs, and non-sandboxed execution. Keep planned behavior out of unrelated docs.
2. Add numbered test cases for Codex tab first-run/save/export/error states and workflow/scheduled generated task behavior; align names with the implemented tests.
3. Run `git diff --check` and the focused test set from Tasks 1–3.
4. Review the whole diff against the approved design and this plan; preserve compatibility and avoid logging generated code, run output, secrets, or sensitive inputs.

### Task 5: Full verification, package, and lifecycle

**Files:**
- Modify only files required by verified failures.

**Steps:**
1. Run `npm.cmd run verify`; require typecheck, renderer/host/backend build, contract tests, Electron tests, and all .NET specs to pass.
2. Run `npm.cmd run package:win`; require the Windows x64 package to be produced with a complete manifest, ASAR, and backend.
3. Run `npm.cmd run test:packaged-lifecycle`; require packaged single-instance and isolated backend cleanup checks to pass.
4. Inspect `git diff --check`, branch status, file list, and final diff. Confirm production settings and the original dirty checkout remain untouched.

## Review focus

- Codex generation disables known action-capable CLI features and uses read-only mode as defense in depth; generated scripts run only after explicit review and approval through Automator's existing process paths.
- Renderer cannot send arbitrary executables, source paths, script content for execution, or native dialog options. The module uses a stored draft ID and validates it again at run/save time.
- CLI discovery must handle the installed Windows shim without `cmd.exe /c` command-string interpolation; reveal configuration/auth errors without changing the user config.
- Create-new writes must survive races and reject symlink/reparse paths; generated Playwright paths must remain within the configured managed project.
- Scope and effect approvals are disclosures and deliberate user consent, not technical sandbox enforcement. Scheduled runs reject changed hashes/scope until reapproved.
- Workflows remain sequential and stop on first failure; adding Playwright task profiles must preserve existing module-ID/profile-ID validation and JSON Pointer input mapping.
- Existing Scheduler targets remain compatible; generated Playwright tasks are scheduled through saved Workflows unless a separate direct target is explicitly implemented and tested.
- Re-run focused verification after each implementation task and the full repo checks after documentation/feature completion.
