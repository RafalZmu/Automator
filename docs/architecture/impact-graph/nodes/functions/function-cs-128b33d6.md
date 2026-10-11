---
graph_id: "function-cs-128b33d6"
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

# Automator.Application.Automation.AutomationSchedulerCoordinator.SafelyProcessOccurrenceAsync(AutomationScheduleDefinition,DateTimeOffset,CancellationToken)

private Task SafelyProcessOccurrenceAsync(AutomationScheduleDefinition schedule, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-557bf15d|Automator.Application.Automation.AutomationSchedulerCoordinator.QueueOccurrence(AutomationScheduleDefinition,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:247)
- impact (called by): [[function-cs-994ec76c|Automator.Application.Automation.AutomationSchedulerCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:94)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L253`
