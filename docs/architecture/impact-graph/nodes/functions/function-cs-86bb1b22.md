---
graph_id: "function-cs-86bb1b22"
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

# Automator.Backend.BackendServer.RefreshKeyboardDeliveryContext()

private void RefreshKeyboardDeliveryContext()

## Connections

- impact (called by): [[function-cs-18e28279|Automator.Backend.BackendServer.SetWindowContext(WindowContextParams)]] (src/Automator.Backend/BackendServer.cs:1086)
- impact (called by): [[function-cs-4065678b|Automator.Backend.BackendServer.PublishStateAsync()]] (src/Automator.Backend/BackendServer.cs:1639)
- impact (called by): [[function-cs-b3dc5dbe|Automator.Backend.BackendServer.RequireActiveServiceContext(string,string?,RpcRequest,HostWindowContextParams?,bool,bool)]] (src/Automator.Backend/BackendServer.cs:649)
- impact (called by): [[function-cs-ca4c65d4|Automator.Backend.BackendServer.OnAutomationInputReceived(KeyInputEvent)]] (src/Automator.Backend/BackendServer.cs:869)
- impact (called by): [[function-cs-eb2b8741|Automator.Backend.BackendServer.ActivateCurrentServicesAsync()]] (src/Automator.Backend/BackendServer.cs:854)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L763`
