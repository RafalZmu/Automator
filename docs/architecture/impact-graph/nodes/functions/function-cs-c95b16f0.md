---
graph_id: "function-cs-c95b16f0"
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

# Automator.Application.Launcher.PanelActivationEpoch.BeginActivation()

public ActivationTicket BeginActivation()

## Connections

- impact (called by): [[function-cs-52c95cb3|Automator.Backend.BackendServer.StartTrackedActivationAsync(AppBinding,string?)]] (src/Automator.Backend/BackendServer.cs:1483)
- impact (called by): [[function-cs-c02dfa46|Program.ReopenedPanelInvalidatesActivationResult()]] (tests/Automator.Application.Specs/Program.cs:210)

## Source anchors

- `src/Automator.Application/Launcher/LauncherModeCoordinator.cs#L26`
