---
graph_id: "registry-dispatch"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/module-registry"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationModuleRegistry.DispatchAsync(string,string,int,int,JsonElement,string,AutomationServicesContext,IReadOnlyList<ModuleSettingsEntry>,CancellationToken)

public Task DispatchAsync(string moduleId, string actionId, int contractVersion, int actionVersion, JsonElement input, string activeModuleId, AutomationServicesContext services, IReadOnlyList moduleSettings, CancellationToken cancellationToken)

## Connections

- impact (the backend callback invokes module registry dispatch): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs#L437)
- impact (called by): [[function-23518-a8c83fb0|lambda@src/Automator.Backend/BackendServer.cs:429]] (src/Automator.Backend/BackendServer.cs:437)

## Source anchors

- `src/Automator.Application/Automation/AutomationModuleRegistry.cs#L74`
