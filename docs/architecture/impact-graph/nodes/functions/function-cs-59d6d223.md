---
graph_id: "function-cs-59d6d223"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/contracts"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Protocol.RpcParameterValidator.TryParseHwnd(string?,nint)

public static bool TryParseHwnd(string value, nint handle)

## Connections

- impact (called by): [[function-cs-18e28279|Automator.Backend.BackendServer.SetWindowContext(WindowContextParams)]] (src/Automator.Backend/BackendServer.cs:1078)
- impact (called by): [[function-cs-3fb84b3a|Automator.Backend.BackendServer.RestorePriorForeground(ForegroundCheckParams)]] (src/Automator.Backend/BackendServer.cs:1125)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:208)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:214)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:212)

## Source anchors

- `src/Automator.Protocol/RpcProtocol.cs#L417`
