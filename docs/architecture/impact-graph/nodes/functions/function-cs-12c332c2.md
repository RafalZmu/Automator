---
graph_id: "function-cs-12c332c2"
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

# Automator.Application.Automation.IAutomationModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)

ValueTask ExecuteAsync(string actionId, JsonElement input, JsonElement moduleSettings, AutomationServicesContext services, CancellationToken cancellationToken)

## Connections

- impact (called by): [[registry-dispatch|Automator.Application.Automation.AutomationModuleRegistry.DispatchAsync(string,string,int,int,JsonElement,string,AutomationServicesContext,IReadOnlyList<ModuleSettingsEntry>,CancellationToken)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs:111)

## Source anchors

- `src/Automator.Application/Automation/AutomationModuleRegistry.cs#L46`
