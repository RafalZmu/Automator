---
graph_id: "function-cs-00fc632e"
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

# Automator.Application.Launcher.LauncherModeCoordinator.SetMode(LauncherMode)

public bool SetMode(LauncherMode mode)

## Connections

- impact (called by): [[function-86276-05faa1cf|lambda@src/Automator.Backend/BackendServer.cs:1609]] (src/Automator.Backend/BackendServer.cs:1623)
- impact (called by): [[function-cs-175a186c|Automator.Backend.BackendServer.SetModeAsync(LauncherMode)]] (src/Automator.Backend/BackendServer.cs:1453)
- impact (called by): [[function-cs-1f086787|Program.ModeTransitionCancelsPendingActivation()]] (tests/Automator.Application.Specs/Program.cs:188)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:292)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:300)
- impact (called by): [[function-cs-bc9e1b17|Automator.Backend.BackendServer.HidePanelAsync()]] (src/Automator.Backend/BackendServer.cs:1441)
- impact (called by): [[function-cs-eb9f861d|Automator.Backend.BackendServer.OpenPanelAsync(bool)]] (src/Automator.Backend/BackendServer.cs:1421)

## Source anchors

- `src/Automator.Application/Launcher/LauncherModeCoordinator.cs#L6`
