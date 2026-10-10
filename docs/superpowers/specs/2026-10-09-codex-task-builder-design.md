# Codex task builder design

## Goal

Let Automator users describe repeatable work in everyday apps, files, and websites, have the local Codex CLI turn it into an inspectable Python script, Playwright task, or workflow, run it through Automator, and choose whether to save it for reuse.

The user asks for an outcome, not a particular implementation. Codex recommends a format and the user can change it.

## User experience

Add a dedicated **Codex** tab in reserved slot 9.

1. **Describe the task.** The user enters what to do, names or selects the files/folders/sites involved, and supplies any values the task needs. Automator displays the selected scope and can ask for additional inputs when Codex identifies missing details.
2. **Generate a plan and artifact.** The local Codex CLI proposes a plain-language plan, an output format, inputs, and effects. It produces code for Automator to inspect; the generation phase does not carry out the requested user task.
3. **Review.** Show the complete plan, selected scope, recommended format, and source code before any task run. The user can edit the request or ask Codex to revise the result. Format choices are Python, Playwright, and workflow where supported.
4. **Run once.** The user approves the first run after reviewing the plan. Automator creates a uniquely named draft in its managed data folder and routes it through its existing Script Runner or Playwright execution path. A draft run does not create a saved profile. Show progress, bounded output, cancellation, and the result.
5. **Save after success.** After a successful run, ask whether to save it. Saving creates a normal editable Automator task/profile with a stable managed script asset. The user can export the script to another location. A failed or canceled run does not offer a saved automation as if it succeeded.
6. **Reuse.** Saved Playwright tasks can be called by Automator Workflows. Workflow steps can pass named inputs. The existing Scheduler remains the place to make a saved task or workflow recur.

## Codex connection

Automator invokes the installed local Codex CLI as a process and consumes structured progress/results from `codex exec --json`. Automator does not add its own Codex login or API-key field. The CLI must already be installed and authenticated/configured on this machine; its own sign-in may use the existing Codex CLI account session or its configured credentials. A Codex desktop sign-in alone is not assumed to configure the CLI.

Preflight checks report whether the CLI is missing or cannot start, and distinguish authentication or configuration failures from task-generation failures. Show setup guidance and wait for the user to fix the local CLI. Do not switch to a cloud service, silently change authentication methods, or edit the user's Codex configuration.

Codex generation returns a plan and code for Automator to validate and store. Automator, rather than the Codex agent, executes the task through registered application services. Treat generated code and structured output as untrusted input: validate lengths, paths, format, input declarations, and IDs at the application boundary.

## Execution and persistence

- Python drafts use a non-persisting execution path that shares Script Runner's interpreter validation, argument handling, timeout, cancellation, bounded output, and result parsing. After success, saving creates an ordinary Script Runner profile.
- Playwright drafts use the app's managed Playwright project and bundled runner when configured. Generated test files go under a dedicated generated-tests directory with create-new semantics, path containment/reparse checks, supported extensions, and source-size limits. Run through the existing Playwright Explorer path. A user-selected external project can be a later explicit mode; the default flow should not ask users to configure one.
- Saved Playwright tasks need a stable profile record that the existing Workflow executor can run. Workflows pass declared named inputs into saved tasks; the Scheduler continues to target saved profile/workflow IDs.
- Drafts and saved script assets live in Automator's per-user data folder. Settings/profile data refer to those managed assets; export copies source code to a user-selected location. Never overwrite an existing asset or user-modified script without a separate explicit confirmation.
- Keep process arguments structured. Do not build shell command strings from task text or generated code.

## Scope, effects, and approval

The plan and preview show which user-selected files, folders, and sites the task is intended to use. Ask for approval if the proposed scope expands. Ask for a further confirmation before an effect described by the plan would overwrite a file, delete data, or submit/send data. Scheduling authorizes the saved task and its reviewed effects to recur. If saved code or declared scope changes, require renewed approval before a run or schedule continues.

There is an important implementation limit: existing Python and Playwright runners execute with the current Windows user's permissions. Showing selected scope is a consent and review boundary, not an OS-enforced filesystem or website sandbox. Arbitrary generated code can access other resources available to that user, and static code/plan review cannot reliably detect every side effect. State this plainly before the first run and schedule; do not claim that the selected scope is technically enforced. For generation, use an ephemeral CLI turn, ignore user tool configuration, disable the known local, browser, app, MCP, plugin, hook, and search features, and keep read-only mode as defense in depth. The UI must describe the feature groups disabled by the CLI without promising technical enforcement of the selected scope.

The first-run approval and effect confirmations must map clearly to task boundaries. If the host cannot pause at a specific destructive or submission step, require the extra confirmation immediately before launching the task that contains that planned effect, and keep the limitation visible in the plan. Approval to run does not grant permission to broaden scope or replace/delete an existing script asset.

## First-version scope

Include the dedicated tab, local CLI discovery and preflight/error guidance, task description and selected-scope display, Codex plan/code generation and review, Python and managed-project Playwright draft runs, post-success save/export, saved Playwright workflow steps with named inputs, and explicit code/scope approval tracking for recurrence.

Use the existing Scheduler tab for recurring runs. Do not add a separate scheduler, cloud Codex connection, Automator-owned credential flow, remote script catalog, arbitrary plugin system, external-project dependency installer, or source-code editing of Automator from the Codex tab.

## Acceptance criteria

1. Slot 9 opens the Codex builder and its CLI status is clear for ready, missing, authentication, configuration, generation, and execution states.
2. Automator itself asks for no API key or separate login; generation uses the local Codex CLI and stops with setup guidance if that CLI is unavailable or not configured.
3. The task plan, declared inputs/effects, selected scope, and complete code are inspectable before a first run. The first run requires explicit user approval.
4. The Codex generation process does not execute the requested task. Draft creation is create-new and constrained to Automator-managed storage.
5. A Python draft can run once without creating a profile; a Playwright draft can run in the configured managed project without external project setup.
6. Existing Script Runner and Playwright runner behavior is reused for cancellation, timeouts, and bounded results. No shell interpolation is introduced.
7. Saving is offered only after a successful first run and creates an editable saved task. Export leaves the managed source intact.
8. A saved Playwright task can be invoked as a Workflow step with named input mappings. Existing Scheduler targeting remains compatible.
9. Scope expansion, overwrite/delete/submit effects, and code/scope changes for recurrence have explicit approval points. The UI describes the CLI features disabled for authoring and says selected scope is not technically sandboxed for generated code.
10. Existing saved profiles, workflows, schedules, and old tab settings remain readable.

## Implementation boundaries

- Launcher registry and Electron renderer: reserve slot 9 for Codex, register the view/actions, and show CLI status, task description, plan/code review, execution results, and save/export interaction.
- Codex module and application services: CLI process discovery/invocation, structured event handling, validation, draft generation, approvals, and non-persisting Python/Playwright execution adapters.
- Script Runner: factor reusable validation/execution so draft runs and saved-profile runs share the same runtime behavior.
- Browser Automation: create and run generated Playwright specs safely in the managed project, then persist a workflow-callable saved task.
- Workflow contracts/engine/UI: add Playwright task profile support and named input mapping without breaking existing module profile IDs.
- Persistence and Scheduler: preserve managed asset identity and the reviewed code/scope revision used for recurring authorization.
- Documentation and verification: add the Codex tab contributor guide and overview, update the root README tab index, and add focused unit/contract/Electron coverage. Run the repository's full `npm.cmd run verify` after implementation.
