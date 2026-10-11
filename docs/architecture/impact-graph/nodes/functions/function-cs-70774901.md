---
graph_id: "function-cs-70774901"
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

# Automator.Windows.Interop.WindowsNativeMethods.GetWindowThreadProcessId(nint,uint)

internal static extern uint GetWindowThreadProcessId(nint window, uint processId)

## Connections

- impact (called by): [[function-8721-89a19ab9|lambda@src/Automator.Windows/WindowsApplicationActivator.cs:156]] (src/Automator.Windows/WindowsApplicationActivator.cs:159)
- impact (called by): [[function-cs-476a7400|Automator.Windows.WindowsAppCatalog.CaptureUnderCursor()]] (src/Automator.Windows/WindowsAppCatalog.cs:39)
- impact (called by): [[function-cs-cd23d773|Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)]] (src/Automator.Windows/WindowsForegroundWindow.cs:24)
- impact (called by): [[function-cs-cd23d773|Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)]] (src/Automator.Windows/WindowsForegroundWindow.cs:32)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L84`
