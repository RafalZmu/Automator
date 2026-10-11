---
graph_id: "function-cs-5e713aaa"
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

# Automator.Windows.WindowsIconCache.GetDataUrl(string)

public string GetDataUrl(string targetPath)

## Connections

- impact (called by): [[function-cs-32661b94|Program.InvalidIconPathIsIgnored()]] (tests/Automator.Windows.Specs/Program.cs:125)
- impact (called by): [[function-cs-5628eb1c|Automator.Backend.BackendServer.ToBindingState(AppBinding)]] (src/Automator.Backend/BackendServer.cs:1074)

## Source anchors

- `src/Automator.Windows/WindowsIconCache.cs#L18`
