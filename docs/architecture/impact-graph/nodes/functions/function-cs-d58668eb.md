---
graph_id: "function-cs-d58668eb"
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

# Automator.Application.Automation.AutomationSchedulerCoordinator.ReadCursorAsync(string,CancellationToken)

private Task ReadCursorAsync(string scheduleId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-65f618ff|Automator.Application.Automation.AutomationSchedulerCoordinator.AdvanceCursorAsync(string,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:469)
- impact (called by): [[function-cs-994ec76c|Automator.Application.Automation.AutomationSchedulerCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:76)
- impact (called by): [[function-cs-c0b222e1|Automator.Application.Automation.AutomationSchedulerCoordinator.ProcessOccurrenceAsync(AutomationScheduleDefinition,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:264)
- impact (called by): [[function-cs-d521a055|Automator.Application.Automation.AutomationSchedulerCoordinator.ProcessDueSchedulesAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:234)
- impact (called by): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:297)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L455`
