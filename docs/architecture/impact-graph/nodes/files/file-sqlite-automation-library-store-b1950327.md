---
graph_id: "file-sqlite-automation-library-store-b1950327"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/library"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# SqliteAutomationLibraryStore.cs

Source file src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs.

## Connections

- contains (declares function): [[function-cs-2f40f831|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteAutomationLibraryStore(string)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L22)
- contains (declares function): [[function-cs-43e6c72b|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.ListAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L29)
- contains (declares function): [[function-1506-df5de3bc|lambda@src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs:31]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L31)
- contains (declares function): [[function-cs-da2d9dd0|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.GetAsync(string,string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L44)
- contains (declares function): [[function-2493-d10a9043|lambda@src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs:46]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L46)
- contains (declares function): [[function-cs-4e3d154c|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.UpsertAsync(AutomationLibraryRecord,CancellationToken)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L56)
- contains (declares function): [[function-3820-17ad5407|lambda@src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs:67]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L67)
- contains (declares function): [[function-cs-6fcbabfc|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.DeleteAsync(string,string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L81)
- contains (declares function): [[function-4860-b5c0eeb6|lambda@src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs:83]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L83)
- contains (declares function): [[function-cs-fad3be29|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.RunAsync(Func<T>,CancellationToken)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L94)
- contains (declares function): [[function-cs-10a73fa4|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.OpenDatabase()]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L107)
- contains (declares function): [[function-cs-73b2cd24|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.EnsureSchema(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L123)
- contains (declares function): [[function-cs-9306535c|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.ReadRecord(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L134)
- contains (declares function): [[function-cs-1436b465|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.BindKeys(Statement,string,string,string)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L143)
- contains (declares function): [[function-cs-e7c8d26b|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.ValidateKeys(string,string,string)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L150)
- contains (declares function): [[function-cs-d4f73748|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Database.Prepare(string)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L160)
- contains (declares function): [[function-cs-f046d1d3|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Database.Dispose()]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L161)
- contains (declares function): [[function-cs-4ac8a0fa|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Statement.Statement(nint,string)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L166)
- contains (declares function): [[function-cs-636a3b82|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Statement.Step()]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L174)
- contains (declares function): [[function-cs-076ae07f|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Statement.ExpectDone()]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L182)
- contains (declares function): [[function-cs-a55f0bd4|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Statement.BindText(int,string)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L187)
- contains (declares function): [[function-cs-67822f24|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Statement.BindInt(int,int)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L194)
- contains (declares function): [[function-cs-f0b53aee|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Statement.Dispose()]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L200)
- contains (declares function): [[function-cs-3fba76af|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_open_v2(string,nint,int,nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L213)
- contains (declares function): [[function-cs-434f6ebc|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_close_v2(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L215)
- contains (declares function): [[function-cs-2bc4e400|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_prepare_v2(nint,string,int,nint,nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L217)
- contains (declares function): [[function-cs-fde35088|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_step(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L219)
- contains (declares function): [[function-cs-4e3ff731|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_finalize(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L221)
- contains (declares function): [[function-cs-52cb5472|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_bind_text(nint,int,byte[],int,nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L223)
- contains (declares function): [[function-cs-b65b1af6|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_bind_int(nint,int,int)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L225)
- contains (declares function): [[function-cs-b824fac5|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_column_text(nint,int)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L227)
- contains (declares function): [[function-cs-9b206217|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_column_bytes(nint,int)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L229)
- contains (declares function): [[function-cs-37396177|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_column_int(nint,int)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L231)
- contains (declares function): [[function-cs-4b1f5713|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_changes(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L233)
- contains (declares function): [[function-cs-cc3a84de|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_busy_timeout(nint,int)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L235)
- contains (declares function): [[function-cs-9f312b8b|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_errmsg(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L237)
- contains (declares function): [[function-cs-b557a69b|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_exec(nint,string,nint,nint,nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L239)
- contains (declares function): [[function-cs-943abe14|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_free(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L241)
- contains (declares function): [[function-cs-02a878b6|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.ErrorMessage(nint)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L244)
- contains (declares function): [[function-cs-ad85fc83|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.ColumnText(nint,int)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L246)
- contains (declares function): [[function-cs-786c8a8f|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.Execute(nint,string)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L256)

## Source anchors

- `src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs`
