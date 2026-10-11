---
graph_id: "function-cs-c38c8bc7"
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

# Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)

private Task RunScheduleAsync(AutomationScheduleDefinition schedule, DateTimeOffset occurrenceUtc, bool isOccurrence, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-31f458d9|Automator.Application.Automation.AutomationSchedulerCoordinator.RunNowAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:201)
- impact (called by): [[function-cs-c0b222e1|Automator.Application.Automation.AutomationSchedulerCoordinator.ProcessOccurrenceAsync(AutomationScheduleDefinition,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:266)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L269`
