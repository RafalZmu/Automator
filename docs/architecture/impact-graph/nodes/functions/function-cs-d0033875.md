---
graph_id: "function-cs-d0033875"
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

# Automator.Application.Automation.AutomationFocusSessionCoordinator.CreateHistoryEntry(string,DateTimeOffset,DateTimeOffset,int,AutomationFocusSessionState)

private static AutomationFocusSessionHistoryEntry CreateHistoryEntry(string id, DateTimeOffset startedUtc, DateTimeOffset finishedUtc, int completedFocusPhases, AutomationFocusSessionState finalState)

## Connections

- impact (called by): [[function-cs-0f26fb22|Automator.Application.Automation.AutomationFocusSessionCoordinator.EndAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:230)
- impact (called by): [[function-cs-87fdc7c1|Automator.Application.Automation.AutomationFocusSessionCoordinator.RecoverCurrentSessionAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:375)

## Source anchors

- `src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs#L481`
