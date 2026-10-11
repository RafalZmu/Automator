---
graph_id: "function-cs-d5fea14e"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/worktime-coordinator"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.AutomationWorkTimeCoordinator.ValidateEntry(AutomationWorkTimeEntry,bool)

private static void ValidateEntry(AutomationWorkTimeEntry entry, bool requireDescription)

## Connections

- impact (called by): [[function-cs-20febace|Automator.Application.Automation.AutomationWorkTimeCoordinator.InitializeAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:62)
- impact (called by): [[function-cs-6cd8c172|Automator.Application.Automation.AutomationWorkTimeCoordinator.ValidateState(PersistedActive?,AutomationWorkTimeEntry?)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:302)
- impact (called by): [[function-cs-a57bd922|Automator.Application.Automation.AutomationWorkTimeCoordinator.UpdateEntryAsync(string,string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:176)
- impact (called by): [[function-cs-e97659e5|Automator.Application.Automation.AutomationWorkTimeCoordinator.SaveEntryAsync(string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:140)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L305`
