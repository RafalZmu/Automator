---
graph_id: "function-cs-182fb6fb"
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

# Automator.Application.Automation.AutomationWorkflowProfile.AutomationWorkflowProfile(string,string,IReadOnlyList<AutomationWorkflowStep>,IReadOnlyDictionary<string, JsonElement>?)

public AutomationWorkflowProfile(string id, string name, IReadOnlyList steps, IReadOnlyDictionary variables)

## Connections

- impact (called by): [[function-cs-148774f8|Program.WorkflowEngineStopsAfterFailure()]] (tests/Automator.Application.Specs/Program.cs:1141)
- impact (called by): [[function-cs-42b3ba9a|Program.WorkflowEngineMapsProfilesAndPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1103)
- impact (called by): [[function-cs-f47ecfea|Program.CanceledWorkflowPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1161)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkflowContracts.cs#L8`
