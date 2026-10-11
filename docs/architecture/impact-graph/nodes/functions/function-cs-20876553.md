---
graph_id: "function-cs-20876553"
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

# Automator.Application.Automation.IAutomationBrowserService.CloseSessionAsync(string,CancellationToken)

Task CloseSessionAsync(string profileId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-41e9841d|Automator.Application.Automation.BrowserAutomationModule.CloseSessionAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:220)
- impact (called by): [[function-cs-97c1f5b7|Automator.Application.Automation.AutomationServicesContext.ContextBrowserService.CloseSessionAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:263)
- impact (called by): [[function-cs-e729abf2|Automator.Application.Automation.BrowserAutomationModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:174)

## Source anchors

- `src/Automator.Application/Automation/AutomationBrowserContracts.cs#L51`
