---
graph_id: "function-cs-1362de04"
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

# Automator.Application.Automation.AutomationWorkTimeCoordinator.PersistStateAsync(PersistedActive?,AutomationWorkTimeEntry?,CancellationToken)

private Task PersistStateAsync(PersistedActive active, AutomationWorkTimeEntry pending, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-01bf579e|Automator.Application.Automation.AutomationWorkTimeCoordinator.ResumePendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:213)
- impact (called by): [[function-cs-1bbf4541|Automator.Application.Automation.AutomationWorkTimeCoordinator.DiscardPendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:228)
- impact (called by): [[function-cs-6569ec75|Automator.Application.Automation.AutomationWorkTimeCoordinator.StopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:113)
- impact (called by): [[function-cs-e97659e5|Automator.Application.Automation.AutomationWorkTimeCoordinator.SaveEntryAsync(string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:142)
- impact (called by): [[function-cs-f16d6b15|Automator.Application.Automation.AutomationWorkTimeCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:95)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L257`
