---
graph_id: "function-cs-9058ce02"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationRequestExecutor.CancelModule(string)

public int CancelModule(string moduleId)

## Connections

- impact (called by): [[function-cs-0dfa7e3c|Program.RequestExecutorCancelsRequestsByScope()]] (tests/Automator.Application.Specs/Program.cs:1042)
- impact (called by): [[function-cs-e62248eb|Automator.Backend.BackendServer.DeactivateServicesAsync()]] (src/Automator.Backend/BackendServer.cs:863)

## Source anchors

- `src/Automator.Application/Automation/AutomationRequestExecutor.cs#L47`
