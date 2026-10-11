---
graph_id: "function-cs-cf21966d"
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

# Automator.Application.Automation.AutomationModuleRegistry.GetActiveSettings(string,int,int,string,IReadOnlyList<ModuleSettingsEntry>)

public AutomationModuleSettingsSnapshot GetActiveSettings(string moduleId, int contractVersion, int settingsVersion, string activeModuleId, IReadOnlyList currentSettings)

## Connections

- impact (called by): [[function-cs-db3278a9|Automator.Backend.BackendServer.GetModuleSettingsCore(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:621)

## Source anchors

- `src/Automator.Application/Automation/AutomationModuleRegistry.cs#L158`
