---
graph_id: "function-cs-14a3e462"
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

# Automator.Application.Automation.IAutomationFocusSessionCoordinator.StartAsync(CancellationToken)

Task StartAsync(CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-1ea7ed37|Program.ControlsPersistStateAndHistory()]] (tests/Automator.Focus.Specs/Program.cs:66)
- impact (called by): [[function-cs-2bee94a2|Program.SettingsAreFrozenPerSession()]] (tests/Automator.Focus.Specs/Program.cs:49)
- impact (called by): [[function-cs-4b429c00|Program.WorkerFailureIsLoggedAndRetried()]] (tests/Automator.Focus.Specs/Program.cs:210)
- impact (called by): [[function-cs-58f8d0dc|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:332)
- impact (called by): [[function-cs-a7692215|Program.DisposalStopsTheWorker()]] (tests/Automator.Focus.Specs/Program.cs:236)
- impact (called by): [[function-cs-ca2a4f0a|Program.NaturalBoundaryPersistsBeforeNotification()]] (tests/Automator.Focus.Specs/Program.cs:122)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:159)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:184)

## Source anchors

- `src/Automator.Application/Automation/AutomationFocusContracts.cs#L54`
