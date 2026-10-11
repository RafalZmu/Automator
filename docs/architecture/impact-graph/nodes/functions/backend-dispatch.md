---
graph_id: "backend-dispatch"
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

# Automator.Backend.BackendServer.DispatchAsync(RpcRequest)

private Task DispatchAsync(RpcRequest request)

## Connections

- impact (the request handler consumes the dispatch result): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] (src/Automator.Backend/BackendServer.cs#L158)
- impact (called by): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] (src/Automator.Backend/BackendServer.cs:158)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L182`
