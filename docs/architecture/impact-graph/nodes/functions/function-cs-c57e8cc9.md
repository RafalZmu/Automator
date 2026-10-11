---
graph_id: "function-cs-c57e8cc9"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/scheduler"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.SchedulerModule.NormalizeAndValidate(AutomationScheduleDefinition)

public static AutomationScheduleDefinition NormalizeAndValidate(AutomationScheduleDefinition schedule)

## Connections

- impact (called by): [[function-cs-00e91da4|Automator.Application.Automation.AutomationScheduleRecurrenceCalculator.NextOccurrenceUtc(AutomationScheduleDefinition,DateTimeOffset,TimeZoneInfo?)]] (src/Automator.Application/Automation/AutomationScheduleRecurrenceCalculator.cs:11)
- impact (called by): [[function-cs-17ec5b3b|Automator.Application.Automation.AutomationScheduleRecurrenceCalculator.LatestOccurrenceUtc(AutomationScheduleDefinition,DateTimeOffset,TimeZoneInfo?)]] (src/Automator.Application/Automation/AutomationScheduleRecurrenceCalculator.cs:47)
- impact (called by): [[function-cs-19609f5b|Automator.Infrastructure.Automation.AutomationLibraryTransferService.NormalizeRecord(AutomationLibraryRecord)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:224)
- impact (called by): [[function-cs-b1ebc8ea|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadValidSchedule(AutomationLibraryRecord)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:425)
- impact (called by): [[function-cs-bd5641b2|Automator.Application.Automation.AutomationSchedulerCoordinator.SaveAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:125)
- impact (called by): [[function-cs-df454d4b|Automator.Application.Automation.SchedulerModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/SchedulerModule.cs:48)
- impact (called by): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs:55)

## Source anchors

- `src/Automator.Application/Automation/SchedulerModule.cs#L74`
