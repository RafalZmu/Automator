# Codex task builder contributor guide

Read the root AGENTS.md first. Codex drafts repeatable tasks in Python, Playwright, or Workflow formats using the installed local Codex CLI and its existing sign-in.

## Authoring and execution rules

- Keep CLI discovery, status, structured generation, draft storage, execution, approval metadata, and saved-profile persistence in `CodexCliClient.cs`, `CodexTaskService.cs`, and `CodexTaskModule.cs`. Keep the renderer in `ui/modules/CodexView.tsx` and route actions through the declared `codex` module capability.
- Codex CLI is an authoring tool only. Generate with the CLI's read-only sandbox and structured output schema. Never ask Codex to execute the user's requested task. Do not add a separate API-key, login, or provider path; show the CLI setup/sign-in state and let the user retry.
- For draft generation, ignore user CLI configuration and disable the known local execution, browser, app, MCP, plugin, hook, and search features; keep the read-only sandbox as defense in depth. CLI authentication still comes from its existing CODEX_HOME sign-in. Do not claim the selected scope is technically enforced or that the CLI can never access other resources.
- Present the generated plan, complete source, declared scope, inputs, and effects before execution. Require an explicit first-run review and approval. Require an additional immediate confirmation for overwrite, delete, submit, or send effects.
- Let users add local files and folders to the declared scope with the native picker or by typing paths; websites are entered as URLs. Authorize the native picker only while Codex slot 9 is active.
- The selected scope is consent context, not a technical boundary. Generated Python and Playwright run with the signed-in Windows user's permissions through existing Automator runners. Never describe the scope as an OS sandbox or promise that generated code cannot access other user resources.
- If Codex proposes scope beyond the selected items, require the user to accept the additional items and generate a fresh draft for that scope. Review and approve the new source and scope before running it.
- Saving is available only after a successful run. Python saves as a normal Script Runner profile; Playwright saves as a `playwright-task` profile; Workflow saves as a normal Workflow profile made from existing saved profile IDs. Export is available only for saved tasks and uses the narrow Codex export bridge.
- A saved task whose source, scope, inputs, or effects changed must be reviewed and reapproved before manual, workflow, or scheduled execution. Keep approval tied to the current source and semantic review hash.
- Playwright tasks belong to the configured Automator-managed Playwright project, use declared named JSON inputs, and can be called by Workflows. Schedule them by scheduling a saved Workflow that contains the Playwright task; Scheduler has no direct Playwright target.
- Keep all process arguments structured. Do not log prompts, generated source, run output, task inputs, or secrets. Preserve existing Script Runner, Browser Automation, Workflow, Scheduler, and settings compatibility.

## Code and checks

- UI and DTOs: `ui/modules/CodexView.tsx`, `CodexView.css`, and `contracts/codex.ts`.
- CLI, service, module: `src/Automator.Application/Automation/CodexCliClient.cs`, `CodexTaskService.cs`, and `CodexTaskModule.cs`.
- Execution bridges: `ScriptRunnerExecution.cs`, `PlaywrightTestExplorer.cs`, `PlaywrightTaskSavedProfileHandler.cs`, and `AutomationWorkflowEngine.cs`.
- Host export authorization: `electron/codexExport.ts`, `electron/main.ts`, and `electron/preload.ts`.
- Tests: `tests/electron/codex-builder.test.cjs`, `tests/electron/ui-flow.test.cjs`, `tests/rpc-contracts.test.mjs`, and `tests/Automator.Application.Specs/CodexTaskSpecs.cs`.

Update this guide, its sibling README and TESTS, impacted execution-surface guides, and the root tab index when Codex behavior or its boundaries change.
