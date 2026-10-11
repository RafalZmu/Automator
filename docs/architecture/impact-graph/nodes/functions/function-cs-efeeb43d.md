---
graph_id: "function-cs-efeeb43d"
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

# Automator.Application.Automation.AutomationSchedulerCoordinator.WriteCursorAsync(string,DateTimeOffset,CancellationToken)

private Task WriteCursorAsync(string scheduleId, DateTimeOffset occurrenceUtc, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-65f618ff|Automator.Application.Automation.AutomationSchedulerCoordinator.AdvanceCursorAsync(string,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:471)
- impact (called by): [[function-cs-994ec76c|Automator.Application.Automation.AutomationSchedulerCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:84)
- impact (called by): [[function-cs-d0669245|Automator.Application.Automation.AutomationSchedulerCoordinator.RebaselineCursorAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:477)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L464`
