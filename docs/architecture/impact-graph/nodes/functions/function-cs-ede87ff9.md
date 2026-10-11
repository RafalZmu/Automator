---
graph_id: "function-cs-ede87ff9"
graph_layer: "function"
graph_kind: "constructor"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/constructor"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationSavedProfileExecutor.AutomationSavedProfileExecutor(IEnumerable<IAutomationSavedProfileHandler>)

public AutomationSavedProfileExecutor(IEnumerable handlers)

## Connections

- impact (called by): [[function-cs-148774f8|Program.WorkflowEngineStopsAfterFailure()]] (tests/Automator.Application.Specs/Program.cs:1148)
- impact (called by): [[function-cs-31f6febf|Program.RunsWithSavedVariablesAndSafeHistory()]] (tests/Automator.Workflows.Specs/Program.cs:284)
- impact (called by): [[function-cs-42b3ba9a|Program.WorkflowEngineMapsProfilesAndPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1114)
- impact (called by): [[function-cs-874970c5|Program.SavedProfileCatalogsExposeSafeMetadata()]] (tests/Automator.Application.Specs/Program.cs:1086)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:923)
- impact (called by): [[function-cs-e55db0ee|Program.RunsSavedScriptProfile()]] (tests/Automator.Scheduler.Specs/Program.cs:76)
- impact (called by): [[function-cs-f2c3e587|Program.SavedProfileExecutionUsesRegisteredHandlers()]] (tests/Automator.Application.Specs/Program.cs:1068)
- impact (called by): [[function-cs-f47ecfea|Program.CanceledWorkflowPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1167)

## Source anchors

- `src/Automator.Application/Automation/AutomationSavedProfileExecution.cs#L57`
