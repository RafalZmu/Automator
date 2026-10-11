---
graph_id: "function-cs-d3bb1d4b"
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

# Automator.Backend.BackendServer.ResolveRequestedTab(string,HostWindowContextParams?,RpcRequest)

private LauncherTabMetadata ResolveRequestedTab(string moduleId, HostWindowContextParams hostWindowContext, RpcRequest request)

## Connections

- impact (called by): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:419)
- impact (called by): [[function-cs-ae048117|Automator.Backend.BackendServer.CancelAutomationModuleAction(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:557)
- impact (called by): [[function-cs-b3dc5dbe|Automator.Backend.BackendServer.RequireActiveServiceContext(string,string?,RpcRequest,HostWindowContextParams?,bool,bool)]] (src/Automator.Backend/BackendServer.cs:639)
- impact (called by): [[function-cs-d249902a|Automator.Backend.BackendServer.ValidateRequestedCapability(string,string?,HostWindowContextParams?,RpcRequest,bool)]] (src/Automator.Backend/BackendServer.cs:708)
- impact (called by): [[function-cs-db3278a9|Automator.Backend.BackendServer.GetModuleSettingsCore(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:616)
- impact (called by): [[function-cs-e1f35018|Automator.Backend.BackendServer.GetAutomationKeyboardEligibility(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:350)
- impact (called by): [[function-cs-f3c0dc09|Automator.Backend.BackendServer.UpdateModuleSettingsCoreAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:578)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L674`
