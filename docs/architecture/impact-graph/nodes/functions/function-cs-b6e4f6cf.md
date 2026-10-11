---
graph_id: "function-cs-b6e4f6cf"
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

# Automator.Application.Automation.IAutomationKeyboardInput.SubscribeAsync(Func<KeyInputEvent, ValueTask>,CancellationToken)

ValueTask SubscribeAsync(Func receiver, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-f138384f|Automator.Application.Automation.AutomationServicesContext.ContextKeyboardInput.SubscribeAsync(Func<KeyInputEvent, ValueTask>,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:155)
- impact (called by): [[function-cs-f462a920|Automator.Backend.BackendServer.SubscribeAutomationKeyboardAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:339)

## Source anchors

- `src/Automator.Application/Automation/AutomationServicesContext.cs#L16`
