---
graph_id: "function-cs-a60d77b9"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/workflow-engine"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationWorkflowEngine.RunAsync(string,JsonElement?,CancellationToken)

public Task RunAsync(string workflowId, JsonElement? initialInput, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-148774f8|Program.WorkflowEngineStopsAfterFailure()]] (tests/Automator.Application.Specs/Program.cs:1150)
- impact (called by): [[function-cs-31f6febf|Program.RunsWithSavedVariablesAndSafeHistory()]] (tests/Automator.Workflows.Specs/Program.cs:287)
- impact (called by): [[function-cs-42b3ba9a|Program.WorkflowEngineMapsProfilesAndPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1117)
- impact (called by): [[function-cs-f47ecfea|Program.CanceledWorkflowPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1170)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L40`
