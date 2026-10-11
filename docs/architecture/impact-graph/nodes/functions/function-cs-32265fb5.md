---
graph_id: "function-cs-32265fb5"
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

# Automator.Application.Automation.AutomationKeyboardEventHub.SubscribeAsync(string,Func<KeyInputEvent, ValueTask>,CancellationToken)

public ValueTask SubscribeAsync(string moduleId, Func receiver, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0377774d|Automator.Backend.BackendServer.BackendKeyboardInput.SubscribeAsync(Func<KeyInputEvent, ValueTask>,CancellationToken)]] (src/Automator.Backend/BackendServer.cs:1797)

## Source anchors

- `src/Automator.Application/Automation/AutomationKeyboardEventHub.cs#L82`
