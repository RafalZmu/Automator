# Workflows — slot 5

Workflows chain saved Script Runner, API, and Browser Automation profiles. Each workflow is an ordered list of steps. The editor lets users add, remove, and reorder steps, select a saved profile, and map JSON values into that profile's input.

Mappings can use literal JSON, workflow variables, or a JSON Pointer into an earlier step's output. A blank source pointer selects the full output. Steps run in order and the initial engine stops at the first failed step. The run view shows status and available structured output for each step.

## Code map

- ui/modules/WorkflowsView.tsx and WorkflowsView.css contain the profile list and structured step editor.
- contracts/workflows.ts validates renderer-side profiles, bindings, variables, and run results.
- src/Automator.Application/Automation/WorkflowModule.cs handles module actions and profile validation.
- AutomationWorkflowEngine.cs resolves saved profiles, applies mappings, runs steps, and writes metadata-only history.
- src/Automator.Application/Automation/AutomationWorkflowContracts.cs defines the persisted workflow and mapping records.
- Tests: tests/workflows-contract.test.mjs and tests/Automator.Workflows.Specs.

Credentials remain in the API secret store; workflows refer to saved profiles rather than copying secret values.
