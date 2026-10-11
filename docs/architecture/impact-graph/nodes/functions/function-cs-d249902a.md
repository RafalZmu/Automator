---
graph_id: "function-cs-d249902a"
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

# Automator.Backend.BackendServer.ValidateRequestedCapability(string,string?,HostWindowContextParams?,RpcRequest,bool)

private void ValidateRequestedCapability(string moduleId, string capability, HostWindowContextParams hostWindowContext, RpcRequest request, bool allowInactiveCleanup)

## Connections

- impact (called by): [[function-cs-86c12158|Automator.Backend.BackendServer.CancelAutomationHttpRequest(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:411)
- impact (called by): [[function-cs-b3dc5dbe|Automator.Backend.BackendServer.RequireActiveServiceContext(string,string?,RpcRequest,HostWindowContextParams?,bool,bool)]] (src/Automator.Backend/BackendServer.cs:644)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L700`
