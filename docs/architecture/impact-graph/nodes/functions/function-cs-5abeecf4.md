---
graph_id: "function-cs-5abeecf4"
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

# Automator.Application.Automation.AutomationServicesContext.UnsubscribeKeyboardAsync()

public ValueTask UnsubscribeKeyboardAsync()

## Connections

- impact (called by): [[function-cs-2b273f70|Automator.Application.Automation.AutomationServicesContext.DisposeAsync()]] (src/Automator.Application/Automation/AutomationServicesContext.cs:125)
- impact (called by): [[function-cs-4a974b84|Automator.Backend.BackendServer.UnsubscribeAutomationKeyboardAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:383)
- impact (called by): [[function-cs-e542bc30|Program.KeyboardUnsubscribeDisposesScopedSubscriptions()]] (tests/Automator.Application.Specs/Program.cs:878)

## Source anchors

- `src/Automator.Application/Automation/AutomationServicesContext.cs#L133`
