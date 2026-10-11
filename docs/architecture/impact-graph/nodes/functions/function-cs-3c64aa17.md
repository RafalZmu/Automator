---
graph_id: "function-cs-3c64aa17"
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

# Automator.Application.Automation.BrowserAutomationModule.TryReadProfile(AutomationLibraryRecord)

public static BrowserAutomationProfile TryReadProfile(AutomationLibraryRecord record)

## Connections

- impact (called by): [[function-cs-19609f5b|Automator.Infrastructure.Automation.AutomationLibraryTransferService.NormalizeRecord(AutomationLibraryRecord)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:211)
- impact (called by): [[function-cs-3a2e47b2|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.RequireProfileAsync(string,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:195)
- impact (called by): [[function-cs-3bc99e91|Automator.Application.Automation.BrowserSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:524)
- impact (called by): [[function-cs-4224ed33|Automator.Application.Automation.BrowserAutomationModule.RequireProfileAsync(string,IAutomationLibrary,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:228)
- impact (called by): [[function-cs-a07a153b|Automator.Infrastructure.Automation.PlaywrightAutomationBrowserService.WatchProfilePolicyAsync(string,BrowserAutomationProxy,CancellationToken)]] (src/Automator.Infrastructure/Automation/PlaywrightAutomationBrowserService.cs:323)

## Source anchors

- `src/Automator.Application/Automation/BrowserAutomationModule.cs#L426`
