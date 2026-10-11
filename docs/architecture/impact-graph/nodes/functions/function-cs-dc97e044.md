---
graph_id: "function-cs-dc97e044"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# FixtureWorkflowLibraryStore.UpsertAsync(AutomationLibraryRecord,CancellationToken)

public Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-148774f8|Program.WorkflowEngineStopsAfterFailure()]] (tests/Automator.Application.Specs/Program.cs:1145)
- impact (called by): [[function-cs-42b3ba9a|Program.WorkflowEngineMapsProfilesAndPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1110)
- impact (called by): [[function-cs-f47ecfea|Program.CanceledWorkflowPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1164)

## Source anchors

- `tests/Automator.Application.Specs/Program.cs#L1700`
