---
graph_id: "explorer-run"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.PlaywrightTestExplorer.RunAsync(JsonElement,CancellationToken)

public Task RunAsync(JsonElement input, CancellationToken cancellationToken)

## Connections

- impact (the runTests action delegates to explorer RunAsync): [[browser-module-execute|Automator.Application.Automation.BrowserAutomationModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs#L97)
- impact (called by): [[browser-module-execute|Automator.Application.Automation.BrowserAutomationModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:97)

## Source anchors

- `src/Automator.Application/Automation/PlaywrightTestExplorer.cs#L172`
