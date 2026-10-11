---
graph_id: "file-program-03ef3c69"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/scheduler"
  - "impact-graph/language/csharp"
  - "impact-graph/test"
generated_by: "scripts/impact-graph.mjs"
---

# Program.cs

Source file tests/Automator.Scheduler.Specs/Program.cs.

## Connections

- contains (declares function): [[function-cs-4b629b78|Program.Json(string)]] (tests/Automator.Scheduler.Specs/Program.cs#L27)
- contains (declares function): [[function-cs-05739c70|Program.CalculatesRecurrences()]] (tests/Automator.Scheduler.Specs/Program.cs#L28)
- contains (declares function): [[function-cs-f4f2ab89|Program.CoordinatorPersistsAndSerializesRuns()]] (tests/Automator.Scheduler.Specs/Program.cs#L47)
- contains (declares function): [[function-5185-a0ad75af|lambda@tests/Automator.Scheduler.Specs/Program.cs:60]] (tests/Automator.Scheduler.Specs/Program.cs#L60)
- contains (declares function): [[function-cs-e55db0ee|Program.RunsSavedScriptProfile()]] (tests/Automator.Scheduler.Specs/Program.cs#L73)
- contains (declares function): [[function-cs-d8aa3ef0|Program.MigratesLegacyWorkflowSchedule()]] (tests/Automator.Scheduler.Specs/Program.cs#L93)
- contains (declares function): [[function-cs-3f1e92dd|Program.SeedWorkflow(TestLibraryStore,string)]] (tests/Automator.Scheduler.Specs/Program.cs#L105)
- contains (declares function): [[function-cs-eef7e43e|Program.IntervalSchedule(string,string,bool,bool)]] (tests/Automator.Scheduler.Specs/Program.cs#L107)
- contains (declares function): [[function-cs-644066e3|Program.SeedSchedule(TestLibraryStore,AutomationScheduleDefinition)]] (tests/Automator.Scheduler.Specs/Program.cs#L110)
- contains (declares function): [[function-cs-fb2f8495|Program.SeedCursor(TestLibraryStore,string,DateTimeOffset)]] (tests/Automator.Scheduler.Specs/Program.cs#L113)
- contains (declares function): [[function-cs-4d98c25f|Program.Cursor(TestLibraryStore,string)]] (tests/Automator.Scheduler.Specs/Program.cs#L116)
- contains (declares function): [[function-cs-fb1e7f56|Program.Eventually(Func<bool>)]] (tests/Automator.Scheduler.Specs/Program.cs#L120)
- contains (declares function): [[function-cs-082c2db5|Program.StartupCanRetry()]] (tests/Automator.Scheduler.Specs/Program.cs#L124)
- contains (declares function): [[function-10191-6f4a665e|lambda@tests/Automator.Scheduler.Specs/Program.cs:129]] (tests/Automator.Scheduler.Specs/Program.cs#L129)
- contains (declares function): [[function-10331-2749614a|lambda@tests/Automator.Scheduler.Specs/Program.cs:131]] (tests/Automator.Scheduler.Specs/Program.cs#L131)
- contains (declares function): [[function-10421-07bedc77|lambda@tests/Automator.Scheduler.Specs/Program.cs:132]] (tests/Automator.Scheduler.Specs/Program.cs#L132)
- contains (declares function): [[function-cs-3e893542|Program.RecoveryPreservesCursor()]] (tests/Automator.Scheduler.Specs/Program.cs#L139)
- contains (declares function): [[function-12135-ce162f7a|lambda@tests/Automator.Scheduler.Specs/Program.cs:153]] (tests/Automator.Scheduler.Specs/Program.cs#L153)
- contains (declares function): [[function-cs-74f78698|Program.CatchupRunsOnce()]] (tests/Automator.Scheduler.Specs/Program.cs#L156)
- contains (declares function): [[function-13002-75ff4e84|lambda@tests/Automator.Scheduler.Specs/Program.cs:163]] (tests/Automator.Scheduler.Specs/Program.cs#L163)
- contains (declares function): [[function-13056-a3bf45c4|lambda@tests/Automator.Scheduler.Specs/Program.cs:164]] (tests/Automator.Scheduler.Specs/Program.cs#L164)
- contains (declares function): [[function-cs-9d12dd19|Program.UpdatesRebaselineCursor()]] (tests/Automator.Scheduler.Specs/Program.cs#L172)
- contains (declares function): [[function-cs-028ec883|Program.WorkerSurvivesFailureAndSlowRuns()]] (tests/Automator.Scheduler.Specs/Program.cs#L185)
- contains (declares function): [[function-15313-4bc5b26f|lambda@tests/Automator.Scheduler.Specs/Program.cs:194]] (tests/Automator.Scheduler.Specs/Program.cs#L194)
- contains (declares function): [[function-15487-f87d0d55|lambda@tests/Automator.Scheduler.Specs/Program.cs:195]] (tests/Automator.Scheduler.Specs/Program.cs#L195)
- contains (declares function): [[function-15582-8dde9330|lambda@tests/Automator.Scheduler.Specs/Program.cs:196]] (tests/Automator.Scheduler.Specs/Program.cs#L196)
- contains (declares function): [[function-15690-0799b9d9|lambda@tests/Automator.Scheduler.Specs/Program.cs:197]] (tests/Automator.Scheduler.Specs/Program.cs#L197)
- contains (declares function): [[function-cs-3586d040|Program.ShutdownCancelsManualRun()]] (tests/Automator.Scheduler.Specs/Program.cs#L200)
- contains (declares function): [[function-16410-0ff05dae|lambda@tests/Automator.Scheduler.Specs/Program.cs:206]] (tests/Automator.Scheduler.Specs/Program.cs#L206)
- contains (declares function): [[function-cs-e5c7e3ca|Program.ClaimFailureDoesNotReplay()]] (tests/Automator.Scheduler.Specs/Program.cs#L210)
- contains (declares function): [[function-17300-f6ffcf02|lambda@tests/Automator.Scheduler.Specs/Program.cs:217]] (tests/Automator.Scheduler.Specs/Program.cs#L217)
- contains (declares function): [[function-17385-58536d2b|lambda@tests/Automator.Scheduler.Specs/Program.cs:218]] (tests/Automator.Scheduler.Specs/Program.cs#L218)
- contains (declares function): [[function-17664-3af52bbc|lambda@tests/Automator.Scheduler.Specs/Program.cs:220]] (tests/Automator.Scheduler.Specs/Program.cs#L220)
- contains (declares function): [[function-17718-28341ed4|lambda@tests/Automator.Scheduler.Specs/Program.cs:221]] (tests/Automator.Scheduler.Specs/Program.cs#L221)
- contains (declares function): [[function-18105-2b74a809|lambda@tests/Automator.Scheduler.Specs/Program.cs:225]] (tests/Automator.Scheduler.Specs/Program.cs#L225)
- contains (declares function): [[function-18189-511c1aae|lambda@tests/Automator.Scheduler.Specs/Program.cs:227]] (tests/Automator.Scheduler.Specs/Program.cs#L227)
- contains (declares function): [[function-18279-6cdbbee4|lambda@tests/Automator.Scheduler.Specs/Program.cs:228]] (tests/Automator.Scheduler.Specs/Program.cs#L228)
- contains (declares function): [[function-cs-26149e53|Program.ThrowsAsync(Func<Task>)]] (tests/Automator.Scheduler.Specs/Program.cs#L230)
- contains (declares function): [[function-cs-dccd5e4b|Program.Equal(T,T)]] (tests/Automator.Scheduler.Specs/Program.cs#L234)
- contains (declares function): [[function-18868-b050413e|lambda@tests/Automator.Scheduler.Specs/Program.cs:235]] (tests/Automator.Scheduler.Specs/Program.cs#L235)
- contains (declares function): [[function-cs-645589c3|Program.Context(FakeCoordinator)]] (tests/Automator.Scheduler.Specs/Program.cs#L235)
- contains (declares function): [[function-cs-cc528a68|Program.ControlsHostCoordinator()]] (tests/Automator.Scheduler.Specs/Program.cs#L238)
- contains (declares function): [[function-cs-dd304ddc|Program.RejectsInvalidRecurrence()]] (tests/Automator.Scheduler.Specs/Program.cs#L250)
- contains (declares function): [[function-cs-b9cd4b3f|Program.ReadsSafeSnapshot()]] (tests/Automator.Scheduler.Specs/Program.cs#L259)
- contains (declares function): [[function-cs-e4fe6157|Program.HandlesFailureAndCancellation()]] (tests/Automator.Scheduler.Specs/Program.cs#L266)
- contains (declares function): [[function-cs-86585fb8|FakeCoordinator.GetSnapshotAsync(CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L276)
- contains (declares function): [[function-cs-871bfe0f|FakeCoordinator.SaveAsync(AutomationScheduleDefinition,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L277)
- contains (declares function): [[function-cs-9208c6f8|FakeCoordinator.DeleteAsync(string,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L278)
- contains (declares function): [[function-cs-819c11fc|FakeCoordinator.SetEnabledAsync(string,bool,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L279)
- contains (declares function): [[function-cs-e208f062|FakeCoordinator.RunNowAsync(string,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L280)
- contains (declares function): [[function-24089-1e48964c|lambda@tests/Automator.Scheduler.Specs/Program.cs:286]] (tests/Automator.Scheduler.Specs/Program.cs#L286)
- contains (declares function): [[function-cs-b69982f4|TestLibraryStore.Count(string,string)]] (tests/Automator.Scheduler.Specs/Program.cs#L286)
- contains (declares function): [[function-cs-ac7cb18e|TestLibraryStore.ListAsync(string,string,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L287)
- contains (declares function): [[function-24552-208c1cc1|lambda@tests/Automator.Scheduler.Specs/Program.cs:290]] (tests/Automator.Scheduler.Specs/Program.cs#L290)
- contains (declares function): [[function-cs-e29144b9|TestLibraryStore.GetAsync(string,string,string,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L293)
- contains (declares function): [[function-cs-f4aef918|TestLibraryStore.UpsertAsync(AutomationLibraryRecord,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L296)
- contains (declares function): [[function-cs-f26bb43c|TestLibraryStore.DeleteAsync(string,string,string,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L302)
- contains (declares function): [[function-cs-4e200fbc|TestTimeProvider.GetUtcNow()]] (tests/Automator.Scheduler.Specs/Program.cs#L308)
- contains (declares function): [[function-cs-273375c9|TestTimeProvider.Advance(TimeSpan)]] (tests/Automator.Scheduler.Specs/Program.cs#L309)
- contains (declares function): [[function-cs-e42403e8|SavedScriptHandler.ListProfilesAsync(CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L315)
- contains (declares function): [[function-cs-931e0dba|SavedScriptHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L317)
- contains (declares function): [[function-cs-3cb8d480|BlockingWorkflowExecutor.RunScheduledAsync(string,string,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L329)
- contains (declares function): [[function-cs-ad1ca3de|CountingWorkflowExecutor.RunScheduledAsync(string,string,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L335)
- contains (declares function): [[function-cs-670cdae1|SelectivelyBlockingExecutor.RunScheduledAsync(string,string,CancellationToken)]] (tests/Automator.Scheduler.Specs/Program.cs#L343)
- contains (declares function): [[function-cs-9953b768|RecordingSchedulerLog.Write(ApplicationLogLevel,string,string,Exception?,IReadOnlyDictionary<string, object?>?)]] (tests/Automator.Scheduler.Specs/Program.cs#L351)

## Source anchors

- `tests/Automator.Scheduler.Specs/Program.cs`
