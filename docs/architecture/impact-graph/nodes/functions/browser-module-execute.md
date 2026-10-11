---
graph_id: "browser-module-execute"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/browser"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.BrowserAutomationModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)

public ValueTask ExecuteAsync(string actionId, JsonElement input, JsonElement moduleSettings, AutomationServicesContext services, CancellationToken cancellationToken)

## Connections

- impact (the module action implementation is called by registry dispatch): [[registry-dispatch|Automator.Application.Automation.AutomationModuleRegistry.DispatchAsync(string,string,int,int,JsonElement,string,AutomationServicesContext,IReadOnlyList<ModuleSettingsEntry>,CancellationToken)]] (src/Automator.Application/Automation/AutomationModuleRegistry.cs#L111)

## Source anchors

- `src/Automator.Application/Automation/BrowserAutomationModule.cs#L79`
