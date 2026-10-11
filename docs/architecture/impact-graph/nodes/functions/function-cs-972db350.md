---
graph_id: "function-cs-972db350"
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

# Automator.Application.Automation.IAutomationWorkTimeCoordinator.DiscardPendingAsync(CancellationToken)

Task DiscardPendingAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-c67974de|Automator.Application.Automation.AutomationServicesContext.ContextWorkTimeCoordinator.DiscardPendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:407)
- impact (called by): [[function-cs-cb8fbfdc|Automator.Application.Automation.FocusSessionsModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/FocusSessionsModule.cs:84)
- impact (called by): [[function-cs-e707ba34|Program.WorkTimePendingCanResumeAndDiscard()]] (tests/Automator.Focus.Specs/Program.cs:288)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeContracts.cs#L32`
