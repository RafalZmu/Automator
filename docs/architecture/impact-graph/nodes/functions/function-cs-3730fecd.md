---
graph_id: "function-cs-3730fecd"
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

# Automator.Application.Automation.IAutomationFocusSessionCoordinator.SaveSettingsAsync(AutomationFocusSettings,CancellationToken)

Task SaveSettingsAsync(AutomationFocusSettings settings, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-5700-78b6804d|lambda@tests/Automator.Focus.Specs/Program.cs:107]] (tests/Automator.Focus.Specs/Program.cs:107)
- impact (called by): [[function-5858-3eefbe54|lambda@tests/Automator.Focus.Specs/Program.cs:109]] (tests/Automator.Focus.Specs/Program.cs:109)
- impact (called by): [[function-cs-0fdb7fc3|Automator.Application.Automation.AutomationServicesContext.ContextFocusCoordinator.SaveSettingsAsync(AutomationFocusSettings,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:326)
- impact (called by): [[function-cs-1ea7ed37|Program.ControlsPersistStateAndHistory()]] (tests/Automator.Focus.Specs/Program.cs:65)
- impact (called by): [[function-cs-2bee94a2|Program.SettingsAreFrozenPerSession()]] (tests/Automator.Focus.Specs/Program.cs:48)
- impact (called by): [[function-cs-2bee94a2|Program.SettingsAreFrozenPerSession()]] (tests/Automator.Focus.Specs/Program.cs:52)
- impact (called by): [[function-cs-4b429c00|Program.WorkerFailureIsLoggedAndRetried()]] (tests/Automator.Focus.Specs/Program.cs:209)
- impact (called by): [[function-cs-a7692215|Program.DisposalStopsTheWorker()]] (tests/Automator.Focus.Specs/Program.cs:235)
- impact (called by): [[function-cs-ca2a4f0a|Program.NaturalBoundaryPersistsBeforeNotification()]] (tests/Automator.Focus.Specs/Program.cs:121)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:158)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:183)

## Source anchors

- `src/Automator.Application/Automation/AutomationFocusContracts.cs#L53`
