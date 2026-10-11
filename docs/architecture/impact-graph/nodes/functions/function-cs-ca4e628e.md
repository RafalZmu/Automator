---
graph_id: "function-cs-ca4e628e"
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

# Automator.Application.Automation.AutomationSchedulerCoordinator.WriteRecordAsync(string,string,T,CancellationToken)

private Task WriteRecordAsync(string collection, string id, T value, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0eb771b0|Automator.Application.Automation.AutomationSchedulerCoordinator.SetEnabledAsync(string,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:186)
- impact (called by): [[function-cs-880c7664|Automator.Application.Automation.AutomationSchedulerCoordinator.RecoverClaimsAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:397)
- impact (called by): [[function-cs-b12a8229|Automator.Application.Automation.AutomationSchedulerCoordinator.CompleteRunAsync(AutomationScheduleHistoryEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:365)
- impact (called by): [[function-cs-bd5641b2|Automator.Application.Automation.AutomationSchedulerCoordinator.SaveAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:151)
- impact (called by): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:312)
- impact (called by): [[function-cs-efeeb43d|Automator.Application.Automation.AutomationSchedulerCoordinator.WriteCursorAsync(string,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:465)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L488`
