---
graph_id: "function-cs-907005b5"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/windows"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Windows.WindowsAppCatalog.NormalizePath(string)

private static string NormalizePath(string path)

## Connections

- impact (called by): [[function-cs-1b3326a5|Automator.Windows.WindowsAppCatalog.CreateId(string,string)]] (src/Automator.Windows/WindowsAppCatalog.cs:204)
- impact (called by): [[function-cs-28cfba6a|Automator.Windows.WindowsAppCatalog.AddEntry(IDictionary<string, AppBinding>,string,string,string)]] (src/Automator.Windows/WindowsAppCatalog.cs:123)
- impact (called by): [[function-cs-2fee09ce|Automator.Windows.WindowsAppCatalog.ShouldExcludeProcess(uint)]] (src/Automator.Windows/WindowsAppCatalog.cs:184)
- impact (called by): [[function-cs-31abfc19|Automator.Windows.WindowsAppCatalog.FromCustomPath(string)]] (src/Automator.Windows/WindowsAppCatalog.cs:65)
- impact (called by): [[function-cs-31abfc19|Automator.Windows.WindowsAppCatalog.FromCustomPath(string)]] (src/Automator.Windows/WindowsAppCatalog.cs:72)
- impact (called by): [[function-cs-41d1ba5a|Automator.Windows.WindowsAppCatalog.AddEntryStatic(IDictionary<string, AppBinding>,string,string,string)]] (src/Automator.Windows/WindowsAppCatalog.cs:147)
- impact (called by): [[function-cs-476a7400|Automator.Windows.WindowsAppCatalog.CaptureUnderCursor()]] (src/Automator.Windows/WindowsAppCatalog.cs:51)
- impact (called by): [[function-cs-7ece56ff|Automator.Windows.WindowsAppCatalog.ShouldExcludeTarget(string)]] (src/Automator.Windows/WindowsAppCatalog.cs:193)
- impact (called by): [[function-cs-7ece56ff|Automator.Windows.WindowsAppCatalog.ShouldExcludeTarget(string)]] (src/Automator.Windows/WindowsAppCatalog.cs:194)
- impact (called by): [[function-cs-a0c76d14|Automator.Windows.WindowsAppCatalog.WindowsAppCatalog(int,string,IApplicationLog?)]] (src/Automator.Windows/WindowsAppCatalog.cs:21)

## Source anchors

- `src/Automator.Windows/WindowsAppCatalog.cs#L196`
