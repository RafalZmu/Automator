---
graph_id: "function-cs-fd3a8b9c"
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

# Automator.Application.Automation.IAutomationLibraryStore.ListAsync(string,string,CancellationToken)

Task ListAsync(string moduleId, string collection, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-029e97be|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadWorkflowCatalogAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:439)
- impact (called by): [[function-cs-138b9d54|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ListProfilesAsync(CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:17)
- impact (called by): [[function-cs-18e55154|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ExportAsync(CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:64)
- impact (called by): [[function-cs-20febace|Automator.Application.Automation.AutomationWorkTimeCoordinator.InitializeAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:57)
- impact (called by): [[function-cs-2c794b86|Automator.Application.Automation.AutomationFocusSessionCoordinator.LoadHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:348)
- impact (called by): [[function-cs-2d99adb2|Automator.Application.Automation.AutomationVariableService.ReadAndMigrateAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs:77)
- impact (called by): [[function-cs-45bdfb2a|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadSchedulesAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:410)
- impact (called by): [[function-cs-4954a330|Automator.Application.Automation.AutomationServicesContext.ContextLibrary.ListAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:182)
- impact (called by): [[function-cs-5211ef65|Automator.Application.Automation.AutomationWorkflowEngine.PersistHistoryAsync(AutomationWorkflowProfile,AutomationWorkflowRunResult,DateTimeOffset,DateTimeOffset,AutomationExecutionOrigin,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:235)
- impact (called by): [[function-cs-665cd8f0|Automator.Application.Automation.AutomationRunActivityService.RecordAsync(AutomationRunActivityEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationRunActivityService.cs:48)
- impact (called by): [[function-cs-880c7664|Automator.Application.Automation.AutomationSchedulerCoordinator.RecoverClaimsAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:380)
- impact (called by): [[function-cs-a34abae5|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:431)
- impact (called by): [[function-cs-afeef435|Automator.Application.Automation.AutomationSchedulerCoordinator.PruneHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:483)
- impact (called by): [[function-cs-c222d00b|Automator.Application.Automation.ApiSavedProfileHandler.ListProfilesAsync(CancellationToken)]] (src/Automator.Application/Automation/ApiModule.cs:559)
- impact (called by): [[function-cs-ce68fef5|Automator.Application.Automation.AutomationRunActivityService.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationRunActivityService.cs:58)
- impact (called by): [[function-cs-ce68fef5|Automator.Application.Automation.AutomationRunActivityService.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationRunActivityService.cs:69)
- impact (called by): [[function-cs-ce68fef5|Automator.Application.Automation.AutomationRunActivityService.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationRunActivityService.cs:86)
- impact (called by): [[function-cs-f49349d2|Automator.Application.Automation.BrowserSavedProfileHandler.ListProfilesAsync(CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:505)

## Source anchors

- `src/Automator.Application/Automation/AutomationLibraryStore.cs#L17`
