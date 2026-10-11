---
graph_id: "function-cs-429c4826"
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

# Automator.Application.Automation.AutomationFocusSessionCoordinator.PersistSessionAsync(AutomationFocusSessionSnapshot,DateTimeOffset,CancellationToken)

private Task PersistSessionAsync(AutomationFocusSessionSnapshot session, DateTimeOffset startedUtc, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-2100e5d1|Automator.Application.Automation.AutomationFocusSessionCoordinator.ResumeAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:184)
- impact (called by): [[function-cs-2de78eba|Automator.Application.Automation.AutomationFocusSessionCoordinator.PauseAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:159)
- impact (called by): [[function-cs-3a25a0b0|Automator.Application.Automation.AutomationFocusSessionCoordinator.SkipAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:211)
- impact (called by): [[function-cs-5b735c19|Automator.Application.Automation.AutomationFocusSessionCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:134)
- impact (called by): [[function-cs-77b3ad9f|Automator.Application.Automation.AutomationFocusSessionCoordinator.ProcessDuePhaseAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:312)
- impact (called by): [[function-cs-87fdc7c1|Automator.Application.Automation.AutomationFocusSessionCoordinator.RecoverCurrentSessionAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:384)

## Source anchors

- `src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs#L408`
