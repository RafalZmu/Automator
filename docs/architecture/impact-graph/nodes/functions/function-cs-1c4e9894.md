---
graph_id: "function-cs-1c4e9894"
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

# Automator.Windows.Interop.WindowsNativeMethods.SetForegroundWindow(nint)

internal static extern bool SetForegroundWindow(nint window)

## Connections

- impact (called by): [[function-cs-cd23d773|Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)]] (src/Automator.Windows/WindowsForegroundWindow.cs:49)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L94`
