---
graph_id: "function-cs-fa8403d9"
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

# Automator.Application.Automation.IAutomationWorkTimeCoordinator.GetSnapshotAsync(CancellationToken)

Task GetSnapshotAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-5ae3f6f0|Program.ActiveWorkTimeSurvivesRestart()]] (tests/Automator.Focus.Specs/Program.cs:336)
- impact (called by): [[function-cs-cb8fbfdc|Automator.Application.Automation.FocusSessionsModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/FocusSessionsModule.cs:77)
- impact (called by): [[function-cs-f0e7da14|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:365)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeContracts.cs#L25`
