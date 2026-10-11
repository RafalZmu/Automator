---
graph_id: "function-cs-227cb77a"
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

# Automator.Windows.Interop.WindowsNativeMethods.IsWindowVisible(nint)

internal static extern bool IsWindowVisible(nint window)

## Connections

- impact (called by): [[function-8721-89a19ab9|lambda@src/Automator.Windows/WindowsApplicationActivator.cs:156]] (src/Automator.Windows/WindowsApplicationActivator.cs:158)
- impact (called by): [[function-cs-1f3283a4|Automator.Windows.WindowsApplicationActivator.ActivateAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:85)
- impact (called by): [[function-cs-cd23d773|Automator.Windows.WindowsForegroundWindow.TryBringToForeground(nint,int)]] (src/Automator.Windows/WindowsForegroundWindow.cs:25)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L86`
