---
graph_id: "file-automation-scheduler-coordinator-5f0f022a"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/scheduler-coordinator"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# AutomationSchedulerCoordinator.cs

Source file src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs.

## Connections

- contains (declares function): [[function-cs-6836b1dc|Automator.Application.Automation.AutomationSchedulerCoordinator.AutomationSchedulerCoordinator(IAutomationLibraryStore,IAutomationScheduledWorkflowExecutor,TimeProvider?,IApplicationLog?,AutomationSavedProfileExecutor?,Func<Task>?)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L44)
- contains (declares function): [[function-cs-994ec76c|Automator.Application.Automation.AutomationSchedulerCoordinator.StartAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L60)
- contains (declares function): [[function-3660-68265368|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:72]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L72)
- contains (declares function): [[function-5163-4a0fda42|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:97]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L97)
- contains (declares function): [[function-cs-ecdcd86d|Automator.Application.Automation.AutomationSchedulerCoordinator.GetSnapshotAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L103)
- contains (declares function): [[function-6222-e9081526|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:115]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L115)
- contains (declares function): [[function-cs-bd5641b2|Automator.Application.Automation.AutomationSchedulerCoordinator.SaveAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L122)
- contains (declares function): [[function-7683-47edc14f|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:138]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L138)
- contains (declares function): [[function-cs-38f8b515|Automator.Application.Automation.AutomationSchedulerCoordinator.DeleteAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L157)
- contains (declares function): [[function-cs-0eb771b0|Automator.Application.Automation.AutomationSchedulerCoordinator.SetEnabledAsync(string,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L174)
- contains (declares function): [[function-cs-31f458d9|Automator.Application.Automation.AutomationSchedulerCoordinator.RunNowAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L193)
- contains (declares function): [[function-cs-4bf12468|Automator.Application.Automation.AutomationSchedulerCoordinator.WorkerLoopAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L204)
- contains (declares function): [[function-cs-d521a055|Automator.Application.Automation.AutomationSchedulerCoordinator.ProcessDueSchedulesAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L223)
- contains (declares function): [[function-12898-068a77d7|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:227]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L227)
- contains (declares function): [[function-cs-557bf15d|Automator.Application.Automation.AutomationSchedulerCoordinator.QueueOccurrence(AutomationScheduleDefinition,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L244)
- contains (declares function): [[function-14221-69a6afdb|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:249]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L249)
- contains (declares function): [[function-cs-128b33d6|Automator.Application.Automation.AutomationSchedulerCoordinator.SafelyProcessOccurrenceAsync(AutomationScheduleDefinition,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L253)
- contains (declares function): [[function-cs-c0b222e1|Automator.Application.Automation.AutomationSchedulerCoordinator.ProcessOccurrenceAsync(AutomationScheduleDefinition,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L261)
- contains (declares function): [[function-cs-c38c8bc7|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L269)
- contains (declares function): [[function-cs-db1673b5|Automator.Application.Automation.AutomationSchedulerCoordinator.RunScheduleCoreAsync(AutomationScheduleDefinition,DateTimeOffset,bool,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L283)
- contains (declares function): [[function-cs-b12a8229|Automator.Application.Automation.AutomationSchedulerCoordinator.CompleteRunAsync(AutomationScheduleHistoryEntry,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L363)
- contains (declares function): [[function-cs-27b1ab97|Automator.Application.Automation.AutomationSchedulerCoordinator.NotifyHistoryChangedAsync()]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L371)
- contains (declares function): [[function-cs-880c7664|Automator.Application.Automation.AutomationSchedulerCoordinator.RecoverClaimsAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L378)
- contains (declares function): [[function-cs-45bdfb2a|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadSchedulesAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L408)
- contains (declares function): [[function-24229-96369a04|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:412]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L412)
- contains (declares function): [[function-24322-84f2b137|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:413]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L413)
- contains (declares function): [[function-cs-c7c40973|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadScheduleAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L416)
- contains (declares function): [[function-cs-b1ebc8ea|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadValidSchedule(AutomationLibraryRecord)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L422)
- contains (declares function): [[function-cs-a34abae5|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L429)
- contains (declares function): [[function-25436-b2ad0ab0|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:432]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L432)
- contains (declares function): [[function-25556-82029018|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:433]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L433)
- contains (declares function): [[function-25659-537e800e|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:434]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L434)
- contains (declares function): [[function-cs-029e97be|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadWorkflowCatalogAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L437)
- contains (declares function): [[function-26737-a5e9ec07|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:449]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L449)
- contains (declares function): [[function-26907-1c382007|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:452]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L452)
- contains (declares function): [[function-cs-d58668eb|Automator.Application.Automation.AutomationSchedulerCoordinator.ReadCursorAsync(string,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L455)
- contains (declares function): [[function-cs-efeeb43d|Automator.Application.Automation.AutomationSchedulerCoordinator.WriteCursorAsync(string,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L464)
- contains (declares function): [[function-cs-65f618ff|Automator.Application.Automation.AutomationSchedulerCoordinator.AdvanceCursorAsync(string,DateTimeOffset,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L467)
- contains (declares function): [[function-cs-d0669245|Automator.Application.Automation.AutomationSchedulerCoordinator.RebaselineCursorAsync(AutomationScheduleDefinition,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L474)
- contains (declares function): [[function-cs-afeef435|Automator.Application.Automation.AutomationSchedulerCoordinator.PruneHistoryAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L481)
- contains (declares function): [[function-29034-9b4414da|lambda@src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs:484]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L484)
- contains (declares function): [[function-cs-ca4e628e|Automator.Application.Automation.AutomationSchedulerCoordinator.WriteRecordAsync(string,string,T,CancellationToken)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L488)
- contains (declares function): [[function-cs-92cc13a5|Automator.Application.Automation.AutomationSchedulerCoordinator.TryDeserialize(AutomationLibraryRecord,T?)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L492)
- contains (declares function): [[function-cs-1c267b65|Automator.Application.Automation.AutomationSchedulerCoordinator.SkippedOccurrence(string,string)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L502)
- contains (declares function): [[function-cs-bcc3d92b|Automator.Application.Automation.AutomationSchedulerCoordinator.CreateJsonOptions()]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L505)
- contains (declares function): [[function-cs-d3e5432a|Automator.Application.Automation.AutomationSchedulerCoordinator.BuildHistory(string,string,DateTimeOffset,DateTimeOffset,AutomationExecutionSummary)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L513)
- contains (declares function): [[function-cs-954f1c3c|Automator.Application.Automation.AutomationSchedulerCoordinator.OccurrenceId(string,DateTimeOffset)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L525)
- contains (declares function): [[function-cs-5287f020|Automator.Application.Automation.AutomationSchedulerCoordinator.ValidateId(string)]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L528)
- contains (declares function): [[function-cs-dad9d9a7|Automator.Application.Automation.AutomationSchedulerCoordinator.EnsureStarted()]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L533)
- contains (declares function): [[function-cs-02334ad9|Automator.Application.Automation.AutomationSchedulerCoordinator.DisposeAsync()]] (src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs#L541)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1729)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:1731)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:45)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:930)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:936)
- impact (type-reference): [[file-backend-server-c9515ea3|BackendServer.cs]] (src/Automator.Backend/BackendServer.cs:946)

## Source anchors

- `src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs`
