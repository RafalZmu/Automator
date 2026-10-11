---
graph_id: "function-cs-f12af45f"
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

# Automator.Backend.BackendServer.ValidateRegisteredModule(LauncherTabMetadata,RpcRequest,bool)

private AutomationModuleDefinition ValidateRegisteredModule(LauncherTabMetadata tab, RpcRequest request, bool requireSettingsVersion)

## Connections

- impact (called by): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:420)
- impact (called by): [[function-cs-db3278a9|Automator.Backend.BackendServer.GetModuleSettingsCore(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:617)
- impact (called by): [[function-cs-f3c0dc09|Automator.Backend.BackendServer.UpdateModuleSettingsCoreAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:579)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L719`
