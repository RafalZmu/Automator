---
graph_id: "backend-execute-module"
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

# Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)

private Task ExecuteModuleActionAsync(RpcRequest request)

## Connections

- impact (the module action result is returned to moduleAction routing): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs#L316)
- impact (the action result is serialized into the JSON-RPC response): [[backend-write-protocol|Automator.Backend.BackendServer.WriteProtocolAsync(object)]] (src/Automator.Backend/BackendServer.cs#L158-L159)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:316)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L416`
