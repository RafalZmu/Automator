---
graph_id: "function-cs-17ec5b3b"
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

# Automator.Application.Automation.AutomationScheduleRecurrenceCalculator.LatestOccurrenceUtc(AutomationScheduleDefinition,DateTimeOffset,TimeZoneInfo?)

public static DateTimeOffset? LatestOccurrenceUtc(AutomationScheduleDefinition schedule, DateTimeOffset atOrBeforeUtc, TimeZoneInfo localTimeZone)

## Connections

- impact (called by): [[function-cs-05739c70|Program.CalculatesRecurrences()]] (tests/Automator.Scheduler.Specs/Program.cs:43)
- impact (called by): [[function-cs-994ec76c|Automator.Application.Automation.AutomationSchedulerCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:74)
- impact (called by): [[function-cs-d0669245|Automator.Application.Automation.AutomationSchedulerCoordinator.RebaselineCursorAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:476)
- impact (called by): [[function-cs-d521a055|Automator.Application.Automation.AutomationSchedulerCoordinator.ProcessDueSchedulesAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:232)
- impact (called by): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:296)

## Source anchors

- `src/Automator.Application/Automation/AutomationScheduleRecurrenceCalculator.cs#L42`
