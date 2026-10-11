---
graph_id: "function-cs-0547277c"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/work-time"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# CoordinatorHarness.CreateAsync(IAutomationLibraryStore,IAutomationNotificationService,TimeProvider,IApplicationLog?)

public static Task CreateAsync(IAutomationLibraryStore store, IAutomationNotificationService notifications, TimeProvider timeProvider, IApplicationLog log)

## Connections

- impact (called by): [[function-cs-106b844c|Program.SettingsRespectHostBounds()]] (tests/Automator.Focus.Specs/Program.cs:103)
- impact (called by): [[function-cs-1ea7ed37|Program.ControlsPersistStateAndHistory()]] (tests/Automator.Focus.Specs/Program.cs:63)
- impact (called by): [[function-cs-2bee94a2|Program.SettingsAreFrozenPerSession()]] (tests/Automator.Focus.Specs/Program.cs:43)
- impact (called by): [[function-cs-4b429c00|Program.WorkerFailureIsLoggedAndRetried()]] (tests/Automator.Focus.Specs/Program.cs:207)
- impact (called by): [[function-cs-a7692215|Program.DisposalStopsTheWorker()]] (tests/Automator.Focus.Specs/Program.cs:233)
- impact (called by): [[function-cs-ca2a4f0a|Program.NaturalBoundaryPersistsBeforeNotification()]] (tests/Automator.Focus.Specs/Program.cs:119)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:157)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:163)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:173)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:182)
- impact (called by): [[function-cs-da54e0e7|Program.StartupRecoveryIsExplicit()]] (tests/Automator.Focus.Specs/Program.cs:188)

## Source anchors

- `tests/Automator.Focus.Specs/Program.cs#L345`
