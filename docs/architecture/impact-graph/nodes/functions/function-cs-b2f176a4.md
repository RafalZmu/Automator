---
graph_id: "function-cs-b2f176a4"
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

# Automator.Application.Automation.IAutomationSchedulerCoordinator.SetEnabledAsync(string,bool,CancellationToken)

Task SetEnabledAsync(string scheduleId, bool enabled, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-df454d4b|Automator.Application.Automation.SchedulerModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/SchedulerModule.cs:60)
- impact (called by): [[function-cs-fd9cd7e0|Automator.Application.Automation.AutomationServicesContext.ContextSchedulerCoordinator.SetEnabledAsync(string,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:305)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerContracts.cs#L67`
