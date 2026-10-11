---
graph_id: "function-cs-52cb5472"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/library"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_bind_text(nint,int,byte[],int,nint)

internal static extern int sqlite3_bind_text(nint statement, int index, byte[] value, int byteCount, nint destructor)

## Connections

- impact (called by): [[function-cs-a55f0bd4|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Statement.BindText(int,string)]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs:190)

## Source anchors

- `src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L223`
