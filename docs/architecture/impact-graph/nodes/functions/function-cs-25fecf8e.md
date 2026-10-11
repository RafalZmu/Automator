---
graph_id: "function-cs-25fecf8e"
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

# Automator.Windows.Interop.WindowsNativeMethods.IsIconic(nint)

internal static extern bool IsIconic(nint window)

## Connections

- impact (called by): [[function-cs-8737a8ad|Automator.Windows.WindowsApplicationActivator.RestoreAndMaximizeAsync(nint,string,CancellationToken)]] (src/Automator.Windows/WindowsApplicationActivator.cs:107)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L88`
