---
graph_id: "function-cs-6ff64a1a"
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

# Automator.Application.Launcher.SerializedCommandQueue.SerializedCommandQueue(Action<Exception>?)

public SerializedCommandQueue(Action unhandledError)

## Connections

- impact (called by): [[function-cs-49a4f682|Program.ConcurrentCommandsKeepRevisionOrder()]] (tests/Automator.Application.Specs/Program.cs:272)
- impact (called by): [[function-cs-51106fb5|Automator.Backend.BackendServer.BackendServer()]] (src/Automator.Backend/BackendServer.cs:77)
- impact (called by): [[function-cs-b46aaf61|Program.ServiceWorkDoesNotBlockLauncherCommands()]] (tests/Automator.Application.Specs/Program.cs:1194)

## Source anchors

- `src/Automator.Application/Launcher/SerializedCommandQueue.cs#L14`
