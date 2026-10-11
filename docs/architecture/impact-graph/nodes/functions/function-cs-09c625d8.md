---
graph_id: "function-cs-09c625d8"
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

# Automator.Windows.Interop.WindowsNativeMethods.DwmSetWindowAttribute(nint,uint,uint,uint)

internal static extern int DwmSetWindowAttribute(nint window, uint attribute, uint value, uint valueSize)

## Connections

- impact (called by): [[function-cs-bbf0aede|Automator.Windows.NativeChromeController.Apply(nint)]] (src/Automator.Windows/NativeChromeController.cs:15)
- impact (called by): [[function-cs-bbf0aede|Automator.Windows.NativeChromeController.Apply(nint)]] (src/Automator.Windows/NativeChromeController.cs:18)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L115`
