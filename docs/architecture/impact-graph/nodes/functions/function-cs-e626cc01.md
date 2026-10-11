---
graph_id: "function-cs-e626cc01"
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

# Automator.Application.Automation.IAutomationWorkTimeCoordinator.ResumePendingAsync(CancellationToken)

Task ResumePendingAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-92c7d9e3|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.ResumePendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:401)
- impact (called by): [[function-cs-cb8fbfdc|Automator.Application.Automation.FocusSessionsModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/FocusSessionsModule.cs:83)
- impact (called by): [[function-cs-e707ba34|Program.WorkTimePendingCanResumeAndDiscard()]] (tests/Automator.Focus.Specs/Program.cs:283)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeContracts.cs#L31`
