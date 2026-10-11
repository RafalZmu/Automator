---
graph_id: "function-cs-3eea1416"
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

# Automator.Application.Launcher.SerializedCommandQueue.TryPost(Func<ValueTask>)

public bool TryPost(Func command)

## Connections

- impact (called by): [[function-cs-435f42ba|Automator.Backend.BackendServer.OnToggleRequested(object?,EventArgs)]] (src/Automator.Backend/BackendServer.cs:1570)
- impact (called by): [[function-cs-43b16474|Automator.Backend.BackendServer.OnKeyRecorded(object?,string)]] (src/Automator.Backend/BackendServer.cs:1609)
- impact (called by): [[function-cs-5b4fed6e|Automator.Backend.BackendServer.UpdateKeyboardAvailability(string?,bool)]] (src/Automator.Backend/BackendServer.cs:830)
- impact (called by): [[function-cs-f306828d|Automator.Backend.BackendServer.OnNavigationRequested(object?,NavigationKeyRequested)]] (src/Automator.Backend/BackendServer.cs:1576)

## Source anchors

- `src/Automator.Application/Launcher/SerializedCommandQueue.cs#L47`
