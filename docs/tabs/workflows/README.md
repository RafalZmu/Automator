# Workflows — slot 5

Workflows chain saved Script Runner, API, Browser Automation, and Codex-generated Playwright task profiles. Each workflow is an ordered list of steps. The editor lets users add, remove, and reorder steps, select a saved profile, and map JSON values into that profile's input.

Mappings can use literal JSON, workflow variables, or a JSON Pointer into an earlier step's output. A blank source pointer selects the full output. Steps run in order and the initial engine stops at the first failed step. The run view shows status and available structured output for each step.

## Code map

- ui/modules/WorkflowsView.tsx and WorkflowsView.css contain the profile list and structured step editor.
- contracts/workflows.ts validates renderer-side profiles, bindings, variables, and run results.
- src/Automator.Application/Automation/WorkflowModule.cs handles module actions and profile validation.
- AutomationWorkflowEngine.cs resolves saved profiles, applies mappings, runs steps, and writes metadata-only history.
- `src/Automator.Application/Automation/PlaywrightTaskSavedProfileHandler.cs` adapts saved Codex Playwright tasks to the Workflow saved-profile runner.
- src/Automator.Application/Automation/AutomationWorkflowContracts.cs defines the persisted workflow and mapping records.
- Tests: tests/workflows-contract.test.mjs and tests/Automator.Workflows.Specs.

Credentials remain in the API secret store; workflows refer to saved profiles rather than copying secret values.

Codex-generated Playwright profiles use module ID `playwright-task`. Their declared named JSON inputs can receive literals, workflow variables, or values from earlier steps. Before execution, the saved task handler verifies that the current source and declared review details still match the approved revision. Review and reapprove changed tasks in the Codex tab. To schedule a generated Playwright task, add it to a saved Workflow and schedule that Workflow; Scheduler does not target Playwright tasks directly. See [Codex](../codex/README.md) for the authoring and approval process.

## Test case steps

See [TESTS.md](TESTS.md) for workflow profile, variable, and input-mapping cases with their steps and expected results.
