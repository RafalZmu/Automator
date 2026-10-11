---
graph_id: "function-cs-1657c6f6"
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

# FixtureWorkflowLibraryStore.ListAsync(string,string,CancellationToken)

public Task ListAsync(string moduleId, string collection, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-42b3ba9a|Program.WorkflowEngineMapsProfilesAndPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1125)
- impact (called by): [[function-cs-f47ecfea|Program.CanceledWorkflowPersistsSafeHistory()]] (tests/Automator.Application.Specs/Program.cs:1174)

## Source anchors

- `tests/Automator.Application.Specs/Program.cs#L1686`
