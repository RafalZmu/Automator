---
graph_id: "function-cs-0695a60e"
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

# Automator.Application.Launcher.SerializedCommandQueue.EnqueueAsync(Func<ValueTask<T>>,CancellationToken)

public Task EnqueueAsync(Func command, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-15850-28d6b9b2|lambda@tests/Automator.Application.Specs/Program.cs:273]] (tests/Automator.Application.Specs/Program.cs:273)
- impact (called by): [[backend-handle-line|Automator.Backend.BackendServer.HandleLineAsync(string)]] (src/Automator.Backend/BackendServer.cs:133)
- impact (called by): [[function-cs-5fb78e75|Automator.Backend.BackendServer.AwaitActivationAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Backend/BackendServer.cs:1549)
- impact (called by): [[function-cs-9b92ff44|Automator.Backend.BackendServer.RunPlatformActivationAsync(AppBinding,string?,ActivationTicket,TaskCompletionSource)]] (src/Automator.Backend/BackendServer.cs:1510)
- impact (called by): [[function-cs-b46aaf61|Program.ServiceWorkDoesNotBlockLauncherCommands()]] (tests/Automator.Application.Specs/Program.cs:1195)
- impact (called by): [[function-cs-b7c3b411|Automator.Backend.BackendServer.LoadCatalogAsync(long)]] (src/Automator.Backend/BackendServer.cs:1392)

## Source anchors

- `src/Automator.Application/Launcher/SerializedCommandQueue.cs#L19`
