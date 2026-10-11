---
graph_id: "function-cs-8e0d0e01"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Backend.BackendServer.SendNotificationAsync(string,object)

private Task SendNotificationAsync(string method, object parameters)

## Connections

- impact (called by): [[function-18098-c450a7ac|lambda@src/Automator.Backend/BackendServer.cs:339]] (src/Automator.Backend/BackendServer.cs:341)
- impact (called by): [[function-44000-becbc395|lambda@src/Automator.Backend/BackendServer.cs:830]] (src/Automator.Backend/BackendServer.cs:830)
- impact (called by): [[function-86276-05faa1cf|lambda@src/Automator.Backend/BackendServer.cs:1609]] (src/Automator.Backend/BackendServer.cs:1626)
- impact (called by): [[function-cs-4065678b|Automator.Backend.BackendServer.PublishStateAsync()]] (src/Automator.Backend/BackendServer.cs:1642)
- impact (called by): [[function-cs-42dc29b3|Automator.Backend.BackendServer.PublishGlobalVariablesChangedSafelyAsync()]] (src/Automator.Backend/BackendServer.cs:546)
- impact (called by): [[function-cs-a78b6d91|Automator.Backend.BackendServer.PublishAutomationNotificationAsync(AutomationNotification,CancellationToken)]] (src/Automator.Backend/BackendServer.cs:1818)
- impact (called by): [[function-cs-f2b7250b|Automator.Backend.BackendServer.PublishActivityChangedSafelyAsync()]] (src/Automator.Backend/BackendServer.cs:536)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L1645`
