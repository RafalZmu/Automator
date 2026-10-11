---
graph_id: "function-cs-8b971eca"
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

# Automator.Backend.BackendServer.GetState()

private BackendUiState GetState()

## Connections

- impact (called by): [[function-cs-0fedf070|Automator.Backend.BackendServer.SaveAliasAsync(SaveAliasParams)]] (src/Automator.Backend/BackendServer.cs:1187)
- impact (called by): [[function-cs-30f73aac|Automator.Backend.BackendServer.ImportSettingsAsync(string)]] (src/Automator.Backend/BackendServer.cs:1299)
- impact (called by): [[function-cs-4065678b|Automator.Backend.BackendServer.PublishStateAsync()]] (src/Automator.Backend/BackendServer.cs:1641)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:1002)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:199)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:202)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:205)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:227)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:235)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:240)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:253)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:264)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:278)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:297)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:304)
- impact (called by): [[function-cs-b15c1e38|Automator.Backend.BackendServer.SetQueryAsync(string)]] (src/Automator.Backend/BackendServer.cs:1161)
- impact (called by): [[function-cs-b9bf23d1|Automator.Backend.BackendServer.UpdateSettingsAsync(SettingsUpdateParams)]] (src/Automator.Backend/BackendServer.cs:1234)
- impact (called by): [[function-cs-e1dceae3|Automator.Backend.BackendServer.RelinkAsync(RelinkParams)]] (src/Automator.Backend/BackendServer.cs:1367)
- impact (called by): [[function-cs-e39d116e|Automator.Backend.BackendServer.RemoveBindingAsync(RemoveBindingParams)]] (src/Automator.Backend/BackendServer.cs:1209)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L1005`
