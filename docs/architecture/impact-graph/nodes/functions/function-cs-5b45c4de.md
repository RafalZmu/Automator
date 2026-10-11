---
graph_id: "function-cs-5b45c4de"
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

# Automator.Application.Automation.AutomationModuleRegistry.CreateUpdatedModuleSettings(string,int,int,JsonElement,string,IReadOnlyList<ModuleSettingsEntry>)

public List CreateUpdatedModuleSettings(string moduleId, int contractVersion, int settingsVersion, JsonElement value, string activeModuleId, IReadOnlyList currentSettings)

## Connections

- impact (called by): [[function-cs-f3c0dc09|Automator.Backend.BackendServer.UpdateModuleSettingsCoreAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:583)

## Source anchors

- `src/Automator.Application/Automation/AutomationModuleRegistry.cs#L178`
