---
graph_id: "function-cs-92774ec4"
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

# Automator.Windows.Interop.WindowsNativeMethods.ShowWindowAsync(nint,int)

internal static extern bool ShowWindowAsync(nint window, int command)

## Connections

- impact (called by): [[function-cs-1f3283a4|Automator.Windows.WindowsApplicationActivator.ActivateAsync(AppBinding,string?,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:44)
- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:107)
- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:108)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L92`
