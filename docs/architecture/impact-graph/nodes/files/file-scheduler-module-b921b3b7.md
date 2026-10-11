---
graph_id: "file-scheduler-module-b921b3b7"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/scheduler"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# SchedulerModule.cs

Source file src/Automator.Application/Automation/SchedulerModule.cs.

## Connections

- contains (declares function): [[function-cs-5af88b7a|Automator.Application.Automation.SchedulerModule.CreateInitialState()]] (src/Automator.Application/Automation/SchedulerModule.cs#L23)
- contains (declares function): [[function-cs-5463a1a6|Automator.Application.Automation.SchedulerModule.CreateDefaultSettings()]] (src/Automator.Application/Automation/SchedulerModule.cs#L25)
- contains (declares function): [[function-cs-22d0b572|Automator.Application.Automation.SchedulerModule.MigrateSettings(int,JsonElement)]] (src/Automator.Application/Automation/SchedulerModule.cs#L26)
- contains (declares function): [[function-cs-df454d4b|Automator.Application.Automation.SchedulerModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/SchedulerModule.cs#L29)
- contains (declares function): [[function-cs-c57e8cc9|Automator.Application.Automation.SchedulerModule.NormalizeAndValidate(AutomationScheduleDefinition)]] (src/Automator.Application/Automation/SchedulerModule.cs#L74)
- contains (declares function): [[function-7407-b5f91b17|lambda@src/Automator.Application/Automation/SchedulerModule.cs:98]] (src/Automator.Application/Automation/SchedulerModule.cs#L98)
- contains (declares function): [[function-cs-5e08eefa|Automator.Application.Automation.SchedulerModule.ReadId(JsonElement)]] (src/Automator.Application/Automation/SchedulerModule.cs#L113)
- contains (declares function): [[function-cs-2453e07c|Automator.Application.Automation.SchedulerModule.Result(AutomationStatus,string,object)]] (src/Automator.Application/Automation/SchedulerModule.cs#L120)
- contains (declares function): [[function-cs-2d460acd|Automator.Application.Automation.SchedulerModule.CreateJsonOptions()]] (src/Automator.Application/Automation/SchedulerModule.cs#L122)
- impact (type-reference): [[file-automation-schedule-recurrence-calculator-1ae5caaa|AutomationScheduleRecurrenceCalculator.cs]] (src/Automator.Application/Automation/AutomationScheduleRecurrenceCalculator.cs:11)
- impact (type-reference): [[file-automation-schedule-recurrence-calculator-1ae5caaa|AutomationScheduleRecurrenceCalculator.cs]] (src/Automator.Application/Automation/AutomationScheduleRecurrenceCalculator.cs:47)
- impact (type-reference): [[file-automation-scheduler-coordinator-5f0f022a|AutomationSchedulerCoordinator.cs]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:125)
- impact (type-reference): [[file-automation-scheduler-coordinator-5f0f022a|AutomationSchedulerCoordinator.cs]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:425)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:99)

## Source anchors

- `src/Automator.Application/Automation/SchedulerModule.cs`
