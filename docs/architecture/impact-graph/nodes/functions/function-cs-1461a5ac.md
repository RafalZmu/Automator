---
graph_id: "function-cs-1461a5ac"
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

# Automator.Application.Automation.BrowserAutomationModule.Result(AutomationStatus,string,object,IReadOnlyList<AutomationAction>?)

private static AutomationResult Result(AutomationStatus status, string message, object data, IReadOnlyList actions)

## Connections

- impact (called by): [[function-cs-1380e8ef|Automator.Application.Automation.BrowserAutomationModule.GetRuntimeStatusAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:194)
- impact (called by): [[function-cs-41e9841d|Automator.Application.Automation.BrowserAutomationModule.CloseSessionAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:221)
- impact (called by): [[function-cs-580ac465|Automator.Application.Automation.BrowserAutomationModule.RunActionAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:212)
- impact (called by): [[function-cs-a29622d1|Automator.Application.Automation.BrowserAutomationModule.Error(string)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:471)
- impact (called by): [[function-cs-c5c06584|Automator.Application.Automation.BrowserAutomationModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:167)
- impact (called by): [[function-cs-db9d20d4|Automator.Application.Automation.BrowserAutomationModule.ListProfilesAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:145)
- impact (called by): [[function-cs-e729abf2|Automator.Application.Automation.BrowserAutomationModule.DeleteProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:176)

## Source anchors

- `src/Automator.Application/Automation/BrowserAutomationModule.cs#L473`
