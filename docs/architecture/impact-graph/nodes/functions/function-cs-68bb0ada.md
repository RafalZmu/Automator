---
graph_id: "function-cs-68bb0ada"
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

# Automator.Windows.Interop.WindowsNativeMethods.AttachThreadInput(uint,uint,bool)

internal static extern bool AttachThreadInput(uint attachingThreadId, uint attachedThreadId, bool attach)

## Connections

- impact (called by): [[function-cs-cd23d773|Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)]] (src/Automator.Windows/WindowsForegroundWindow.cs:42)
- impact (called by): [[function-cs-cd23d773|Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)]] (src/Automator.Windows/WindowsForegroundWindow.cs:53)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L98`
