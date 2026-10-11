---
graph_id: "function-cs-a167c527"
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

# Automator.Application.Automation.IAutomationProcessService.ExecuteAsync(AutomationProcessRequest,CancellationToken)

Task ExecuteAsync(AutomationProcessRequest request, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-32fa0481|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:49)
- impact (called by): [[function-cs-45ecdaaf|Automator.Application.Automation.PlaywrightTestExplorer.DiscoverAsync(CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:66)
- impact (called by): [[function-cs-594088fd|Automator.Application.Automation.ScriptRunnerModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:200)
- impact (called by): [[explorer-discover|Automator.Application.Automation.PlaywrightTestExplorer.DiscoverTestsAsync(string,RunnerState,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:259)
- impact (called by): [[context-process|Automator.Application.Automation.AutomationServicesContext.ContextProcessService.ExecuteAsync(AutomationProcessRequest,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:212)
- impact (called by): [[explorer-run|Automator.Application.Automation.PlaywrightTestExplorer.RunAsync(JsonElement,CancellationToken)]] (src/Automator.Application/Automation/PlaywrightTestExplorer.cs:228)

## Source anchors

- `src/Automator.Application/Automation/AutomationServicesContext.cs#L26`
