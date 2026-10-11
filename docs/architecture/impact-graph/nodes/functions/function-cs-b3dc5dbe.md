---
graph_id: "function-cs-b3dc5dbe"
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

# Automator.Backend.BackendServer.RequireActiveServiceContext(string,string?,RpcRequest,HostWindowContextParams?,bool,bool)

private AutomationServicesContext RequireActiveServiceContext(string moduleId, string capability, RpcRequest request, HostWindowContextParams hostWindowContext, bool requireEligibleContext, bool allowMissingContext)

## Connections

- impact (called by): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:424)
- impact (called by): [[function-cs-4a974b84|Automator.Backend.BackendServer.UnsubscribeAutomationKeyboardAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:381)
- impact (called by): [[function-cs-993f9d46|Automator.Backend.BackendServer.SendAutomationHttpRequestAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:390)
- impact (called by): [[function-cs-f462a920|Automator.Backend.BackendServer.SubscribeAutomationKeyboardAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:337)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L631`
