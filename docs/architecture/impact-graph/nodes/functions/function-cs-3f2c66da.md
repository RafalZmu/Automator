---
graph_id: "function-cs-3f2c66da"
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

# Automator.Application.Automation.IAutomationVariableProvider.GetAsync(CancellationToken)

Task GetAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-2ed359b5|Automator.Infrastructure.Automation.AutomationApiProfileRunner.RunProfileAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:60)
- impact (called by): [[function-cs-32fa0481|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:43)
- impact (called by): [[function-cs-3bc99e91|Automator.Application.Automation.BrowserSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:533)
- impact (called by): [[function-cs-580ac465|Automator.Application.Automation.BrowserAutomationModule.RunActionAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:205)
- impact (called by): [[function-cs-594088fd|Automator.Application.Automation.ScriptRunnerModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:189)
- impact (called by): [[function-cs-a60d77b9|Automator.Application.Automation.AutomationWorkflowEngine.RunAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:47)
- impact (called by): [[function-cs-ee81ce94|Automator.Application.Automation.AutomationWorkflowEngine.RunScheduledAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:134)

## Source anchors

- `src/Automator.Application/Automation/AutomationVariableService.cs#L13`
