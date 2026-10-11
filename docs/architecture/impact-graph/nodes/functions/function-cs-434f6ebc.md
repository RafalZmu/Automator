---
graph_id: "function-cs-434f6ebc"
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

# Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.SqliteNative.sqlite3_close_v2(nint)

internal static extern int sqlite3_close_v2(nint database)

## Connections

- impact (called by): [[function-cs-10a73fa4|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.OpenDatabase()]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs:116)
- impact (called by): [[function-cs-f046d1d3|Automator.Infrastructure.Automation.SqliteAutomationLibraryStore.Database.Dispose()]] (src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs:161)

## Source anchors

- `src/Automator.Infrastructure/Automation/SqliteAutomationLibraryStore.cs#L215`
