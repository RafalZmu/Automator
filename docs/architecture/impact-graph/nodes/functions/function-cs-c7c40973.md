---
graph_id: "function-cs-c7c40973"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/scheduler-coordinator"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationSchedulerCoordinator.ReadScheduleAsync(string,CancellationToken)

private Task ReadScheduleAsync(string id, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0eb771b0|Automator.Application.Automation.AutomationSchedulerCoordinator.SetEnabledAsync(string,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:183)
- impact (called by): [[function-cs-31f458d9|Automator.Application.Automation.AutomationSchedulerCoordinator.RunNowAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:199)
- impact (called by): [[function-cs-bd5641b2|Automator.Application.Automation.AutomationSchedulerCoordinator.SaveAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:144)
- impact (called by): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:291)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L416`
