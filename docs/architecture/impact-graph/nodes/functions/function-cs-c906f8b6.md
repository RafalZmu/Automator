---
graph_id: "function-cs-c906f8b6"
graph_layer: "function"
graph_kind: "local-function"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/local-function"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Program.CaptureTwoRequestsAsync(TcpListener,List<string>,CancellationToken)

Task CaptureTwoRequestsAsync(TcpListener listener, List observedRequests, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-16473a16|Program.DoesNotShareCookiesAcrossModules()]] (tests/Automator.Infrastructure.Specs/Program.cs:401)

## Source anchors

- `tests/Automator.Infrastructure.Specs/Program.cs#L414`
