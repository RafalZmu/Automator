---
graph_id: "function-cs-a4c1cf06"
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

# Automator.Windows.Interop.WindowsNativeMethods.IsWindow(nint)

internal static extern bool IsWindow(nint window)

## Connections

- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:106)
- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:112)
- impact (called by): [[function-cs-cd23d773|Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)]] (src/Automator.Windows/WindowsForegroundWindow.cs:21)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L90`
