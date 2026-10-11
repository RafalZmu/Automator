---
graph_id: "function-cs-202e73d7"
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

# Automator.Application.Automation.IAutomationLibraryStore.UpsertAsync(AutomationLibraryRecord,CancellationToken)

Task UpsertAsync(AutomationLibraryRecord record, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-0898c152|Automator.Application.Automation.AutomationServicesContext.ContextLibrary.UpsertAsync(string,string,int,JsonElement,CancellationToken)]] (src/Automator.Application/Automation/AutomationServicesContext.cs:194)
- impact (called by): [[function-cs-0bc112f6|Automator.Application.Automation.AutomationWorkTimeCoordinator.WriteEntryAsync(AutomationWorkTimeEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:262)
- impact (called by): [[function-cs-1362de04|Automator.Application.Automation.AutomationWorkTimeCoordinator.PersistStateAsync(PersistedActive?,AutomationWorkTimeEntry?,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:258)
- impact (called by): [[function-cs-2d99adb2|Automator.Application.Automation.AutomationVariableService.ReadAndMigrateAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs:104)
- impact (called by): [[function-cs-5211ef65|Automator.Application.Automation.AutomationWorkflowEngine.PersistHistoryAsync(AutomationWorkflowProfile,AutomationWorkflowRunResult,DateTimeOffset,DateTimeOffset,AutomationExecutionOrigin,string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkflowEngine.cs:232)
- impact (called by): [[function-cs-586d18f3|Automator.Application.Automation.AutomationFocusSessionCoordinator.WriteRecordAsync(string,string,T,CancellationToken)]] (src/Automator.Application/Automation/AutomationFocusSessionCoordinator.cs:416)
- impact (called by): [[function-cs-5fe4fafe|Automator.Application.Automation.AutomationVariableService.SaveAsync(IReadOnlyDictionary<string, JsonElement>,CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs:111)
- impact (called by): [[function-cs-665cd8f0|Automator.Application.Automation.AutomationRunActivityService.RecordAsync(AutomationRunActivityEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationRunActivityService.cs:46)
- impact (called by): [[function-cs-bfa77545|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ImportAsync(ReadOnlyMemory<byte>,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:139)
- impact (called by): [[function-cs-ca4e628e|Automator.Application.Automation.AutomationSchedulerCoordinator.WriteRecordAsync(string,string,T,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:489)

## Source anchors

- `src/Automator.Application/Automation/AutomationLibraryStore.cs#L19`
