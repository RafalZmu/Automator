---
graph_id: "function-cs-5321db58"
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

# Automator.Application.Automation.AutomationWorkTimeCoordinator.CreateOperationToken(CancellationToken)

private CancellationTokenSource CreateOperationToken(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-01bf579e|Automator.Application.Automation.AutomationWorkTimeCoordinator.ResumePendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:205)
- impact (called by): [[function-cs-1bbf4541|Automator.Application.Automation.AutomationWorkTimeCoordinator.DiscardPendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:223)
- impact (called by): [[function-cs-29453081|Automator.Application.Automation.AutomationWorkTimeCoordinator.DeleteEntryAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:189)
- impact (called by): [[function-cs-6115d1f1|Automator.Application.Automation.AutomationWorkTimeCoordinator.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:73)
- impact (called by): [[function-cs-6569ec75|Automator.Application.Automation.AutomationWorkTimeCoordinator.StopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:104)
- impact (called by): [[function-cs-a57bd922|Automator.Application.Automation.AutomationWorkTimeCoordinator.UpdateEntryAsync(string,string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:169)
- impact (called by): [[function-cs-e97659e5|Automator.Application.Automation.AutomationWorkTimeCoordinator.SaveEntryAsync(string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:134)
- impact (called by): [[function-cs-f16d6b15|Automator.Application.Automation.AutomationWorkTimeCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:86)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L332`
