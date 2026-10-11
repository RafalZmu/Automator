---
graph_id: "file-automation-work-time-coordinator-34a4d8a0"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/worktime-coordinator"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# AutomationWorkTimeCoordinator.cs

Source file src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs.

## Connections

- contains (declares function): [[function-cs-2a7ed3d6|Automator.Application.Automation.AutomationWorkTimeCoordinator.AutomationWorkTimeCoordinator(IAutomationLibraryStore,TimeProvider?)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L30)
- contains (declares function): [[function-cs-20febace|Automator.Application.Automation.AutomationWorkTimeCoordinator.InitializeAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L36)
- contains (declares function): [[function-2919-6b9afadd|lambda@src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:59]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L59)
- contains (declares function): [[function-cs-6115d1f1|Automator.Application.Automation.AutomationWorkTimeCoordinator.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L71)
- contains (declares function): [[function-cs-f16d6b15|Automator.Application.Automation.AutomationWorkTimeCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L84)
- contains (declares function): [[function-cs-6569ec75|Automator.Application.Automation.AutomationWorkTimeCoordinator.StopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L102)
- contains (declares function): [[function-cs-e97659e5|Automator.Application.Automation.AutomationWorkTimeCoordinator.SaveEntryAsync(string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L121)
- contains (declares function): [[function-6969-c3b7675e|lambda@src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:143]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L143)
- contains (declares function): [[function-7060-cf7cfb45|lambda@src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:145]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L145)
- contains (declares function): [[function-cs-a57bd922|Automator.Application.Automation.AutomationWorkTimeCoordinator.UpdateEntryAsync(string,string,IReadOnlyList<string>,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L153)
- contains (declares function): [[function-8385-e7107d2e|lambda@src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:173]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L173)
- contains (declares function): [[function-cs-29453081|Automator.Application.Automation.AutomationWorkTimeCoordinator.DeleteEntryAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L184)
- contains (declares function): [[function-9456-1f771327|lambda@src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:193]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L193)
- contains (declares function): [[function-cs-01bf579e|Automator.Application.Automation.AutomationWorkTimeCoordinator.ResumePendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L203)
- contains (declares function): [[function-cs-1bbf4541|Automator.Application.Automation.AutomationWorkTimeCoordinator.DiscardPendingAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L221)
- contains (declares function): [[function-cs-39e9c68d|Automator.Application.Automation.AutomationWorkTimeCoordinator.DisposeAsync()]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L235)
- contains (declares function): [[function-cs-5e268321|Automator.Application.Automation.AutomationWorkTimeCoordinator.Snapshot(DateTimeOffset)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L245)
- contains (declares function): [[function-cs-97f87a5c|Automator.Application.Automation.AutomationWorkTimeCoordinator.ToActiveSnapshot(PersistedActive,DateTimeOffset)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L248)
- contains (declares function): [[function-cs-fb6c494c|Automator.Application.Automation.AutomationWorkTimeCoordinator.ElapsedMilliseconds(PersistedActive,DateTimeOffset)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L251)
- contains (declares function): [[function-cs-1362de04|Automator.Application.Automation.AutomationWorkTimeCoordinator.PersistStateAsync(PersistedActive?,AutomationWorkTimeEntry?,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L257)
- contains (declares function): [[function-cs-0bc112f6|Automator.Application.Automation.AutomationWorkTimeCoordinator.WriteEntryAsync(AutomationWorkTimeEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L261)
- contains (declares function): [[function-cs-58974c42|Automator.Application.Automation.AutomationWorkTimeCoordinator.PruneHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L265)
- contains (declares function): [[function-cs-1f5ddc16|Automator.Application.Automation.AutomationWorkTimeCoordinator.NormalizeTags(IReadOnlyList<string>)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L275)
- contains (declares function): [[function-cs-6cd8c172|Automator.Application.Automation.AutomationWorkTimeCoordinator.ValidateState(PersistedActive?,AutomationWorkTimeEntry?)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L292)
- contains (declares function): [[function-cs-d5fea14e|Automator.Application.Automation.AutomationWorkTimeCoordinator.ValidateEntry(AutomationWorkTimeEntry,bool)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L305)
- contains (declares function): [[function-15639-f8fb70bb|lambda@src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs:309]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L309)
- contains (declares function): [[function-cs-536ec5d3|Automator.Application.Automation.AutomationWorkTimeCoordinator.Deserialize(AutomationLibraryRecord,string)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L317)
- contains (declares function): [[function-cs-5321db58|Automator.Application.Automation.AutomationWorkTimeCoordinator.CreateOperationToken(CancellationToken)]] (src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs#L332)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1734)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1736)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:46)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:934)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:935)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:947)

## Source anchors

- `src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs`
