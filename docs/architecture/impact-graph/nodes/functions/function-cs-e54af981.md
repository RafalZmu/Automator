---
graph_id: "function-cs-e54af981"
graph_layer: "function"
graph_kind: "constructor"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/constructor"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Launcher.AliasLaunchScheduler.AliasLaunchScheduler(ILaunchDelay,IApplicationActivator)

public AliasLaunchScheduler(ILaunchDelay delay, IApplicationActivator activator)

## Connections

- impact (called by): [[function-cs-192e2a7c|Program.PendingAliasDelayCanBeCancelled()]] (tests/Automator.Application.Specs/Program.cs:1214)
- impact (called by): [[function-cs-1f086787|Program.ModeTransitionCancelsPendingActivation()]] (tests/Automator.Application.Specs/Program.cs:180)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:974)
- impact (called by): [[function-cs-b314ad94|Program.QueryChangeDoesNotCancelActivation()]] (tests/Automator.Application.Specs/Program.cs:1229)

## Source anchors

- `src/Automator.Application/Launcher/AliasLaunchScheduler.cs#L28`
