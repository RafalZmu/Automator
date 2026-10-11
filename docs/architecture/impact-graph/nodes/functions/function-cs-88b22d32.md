---
graph_id: "function-cs-88b22d32"
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

# Automator.Application.Automation.AutomationRequestExecutor.Cancel(string,string)

public bool Cancel(string moduleId, string requestId)

## Connections

- impact (called by): [[function-cs-0dfa7e3c|Program.RequestExecutorCancelsRequestsByScope()]] (tests/Automator.Application.Specs/Program.cs:1024)
- impact (called by): [[function-cs-86c12158|Automator.Backend.BackendServer.CancelAutomationHttpRequest(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:413)
- impact (called by): [[function-cs-ae048117|Automator.Backend.BackendServer.CancelAutomationModuleAction(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:559)
- impact (called by): [[function-cs-de33264d|Program.RequestExecutorRegistersBeforeImmediateCancellation()]] (tests/Automator.Application.Specs/Program.cs:1060)

## Source anchors

- `src/Automator.Application/Automation/AutomationRequestExecutor.cs#L38`
