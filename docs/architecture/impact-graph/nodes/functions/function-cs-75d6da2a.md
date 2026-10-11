---
graph_id: "function-cs-75d6da2a"
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

# Automator.Application.Automation.IAutomationSchedulerCoordinator.RunNowAsync(string,CancellationToken)

Task RunNowAsync(string scheduleId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-df454d4b|Automator.Application.Automation.SchedulerModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/SchedulerModule.cs:63)
- impact (called by): [[function-cs-e322f9ea|Automator.Application.Automation.AutomationServicesContext.ContextSchedulerCoordinator.RunNowAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:311)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerContracts.cs#L68`
