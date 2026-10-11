---
graph_id: "function-cs-444886a3"
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

# Automator.Application.Automation.IAutomationBrowserService.ExecuteAsync(string,AutomationBrowserAction,CancellationToken)

Task ExecuteAsync(string profileId, AutomationBrowserAction action, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-3bc99e91|Automator.Application.Automation.BrowserSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:537)
- impact (called by): [[function-cs-580ac465|Automator.Application.Automation.BrowserAutomationModule.RunActionAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:209)
- impact (called by): [[function-cs-c57cd836|Automator.Application.Automation.AutomationServicesContext.ContextBrowserService.ExecuteAsync(string,AutomationBrowserAction,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:257)

## Source anchors

- `src/Automator.Application/Automation/AutomationBrowserContracts.cs#L50`
