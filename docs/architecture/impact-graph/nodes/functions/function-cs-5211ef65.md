---
graph_id: "function-cs-5211ef65"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/workflow-engine"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationWorkflowEngine.PersistHistoryAsync(AutomationWorkflowProfile,AutomationWorkflowRunResult,DateTimeOffset,DateTimeOffset,AutomationExecutionOrigin,string,CancellationToken)

private Task PersistHistoryAsync(AutomationWorkflowProfile workflow, AutomationWorkflowRunResult result, DateTimeOffset startedUtc, DateTimeOffset finishedUtc, AutomationExecutionOrigin origin, string correlationId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-a60d77b9|Automator.Application.Automation.AutomationWorkflowEngine.RunAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:111)
- impact (called by): [[function-cs-a60d77b9|Automator.Application.Automation.AutomationWorkflowEngine.RunAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:119)
- impact (called by): [[function-cs-ee81ce94|Automator.Application.Automation.AutomationWorkflowEngine.RunScheduledAsync(string,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:178)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkflowEngine.cs#L205`
