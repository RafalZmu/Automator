---
graph_id: "function-cs-7ef4ad68"
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

# Automator.Application.Automation.AutomationModuleRegistry.GetSettings(string,IReadOnlyList<ModuleSettingsEntry>)

public JsonElement GetSettings(string moduleId, IReadOnlyList settings)

## Connections

- impact (called by): [[registry-dispatch|Automator.Application.Automation.AutomationModuleRegistry.DispatchAsync(string,string,int,int,JsonElement,string,AutomationServicesContext,IReadOnlyList<ModuleSettingsEntry>,CancellationToken)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs:110)
- impact (called by): [[function-cs-8b971eca|Automator.Backend.BackendServer.GetState()]] (src/Automator.Backend/BackendServer.cs:1026)
- impact (called by): [[function-cs-cf21966d|Automator.Application.Automation.AutomationModuleRegistry.GetActiveSettings(string,int,int,string,IReadOnlyList<ModuleSettingsEntry>)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs:174)

## Source anchors

- `src/Automator.Application/Automation/AutomationModuleRegistry.cs#L148`
