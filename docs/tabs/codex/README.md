# Codex — slot 9

Codex turns a task description into a reviewed Python script, Playwright task, or Workflow. It uses the Codex CLI installed on this computer and that CLI's own sign-in. When the CLI is missing, signed out, or has a configuration problem, the tab shows the state and setup guidance; Automator does not request a separate API key or sign-in.

Enter what the task should do and list its files, folders, or sites in scope. Type paths or use the native file and folder pickers to add local paths; enter websites as URLs. Codex recommends a format unless Python, Playwright, or Workflow is selected. The result includes a plain-language plan, complete source, named inputs, and declared effects for review. If the draft proposes additional scope, accept the items you want and generate a new draft before reviewing it.

Draft generation uses the local Codex CLI in an ephemeral turn. It keeps the CLI's existing CODEX_HOME sign-in, ignores the user's Codex configuration for this call, disables the known local execution, browser, app, MCP, plugin, hook, and search features, and uses read-only mode as defense in depth. The selected task format uses the CLI default because user configuration is not loaded. The task description and selected scope are sent as text. Running generated work uses Automator's existing task runners. Scope is consent context, not technical enforcement: generated Python and Playwright code has the signed-in Windows user's permissions and may access other resources available to that user. Review the code and effects before approving. Overwrite, delete, submit, and send effects require an additional confirmation immediately before running.

The first run requires explicit review and approval. A successful run enables **Save successful task**. Python becomes a normal Script Runner profile, Playwright becomes a `playwright-task` profile in the managed Playwright project, and a Workflow becomes a normal Workflow profile composed from saved profiles. Playwright tasks declare named JSON inputs and can run as Workflow steps. To schedule one, include it in a saved Workflow and schedule that Workflow.

Saved tasks can be loaded for review or exported to a user-selected file. Export is limited to saved tasks. If saved source or its declared scope, inputs, or effects have changed, review the current task and reapprove it before running manually or through a Workflow or schedule. A changed Playwright source is also checked against its saved source hash.

## Code map

- `ui/modules/CodexView.tsx` and `CodexView.css` render CLI readiness, task prompt/scope pickers/format, review, input collection, approvals, run results, saving, and export.
- `contracts/codex.ts` validates bounded Codex status, draft, run, save, and approval data.
- `src/Automator.Application/Automation/CodexCliClient.cs` discovers and calls the local CLI with structured arguments, JSON schema output, and a read-only authoring sandbox.
- `CodexTaskService.cs` stores drafts, invokes the appropriate existing runner, checks approval revisions, and saves normal profiles.
- `CodexTaskModule.cs` exposes the Codex-only module actions; `electron/codexExport.ts`, `electron/codexScopePathPicker.ts`, `main.ts`, and `preload.ts` enforce active-tab export and scope-picker authorization.
- `ScriptRunnerExecution.cs`, `PlaywrightTestExplorer.cs`, `PlaywrightTaskSavedProfileHandler.cs`, and `AutomationWorkflowEngine.cs` connect generated artifacts to existing execution surfaces.

## Test case steps

See [TESTS.md](TESTS.md) for CLI, approval, save/export, input, revision, and generated-task workflow coverage.
