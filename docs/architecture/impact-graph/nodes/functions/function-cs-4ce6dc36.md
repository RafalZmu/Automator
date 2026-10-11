---
graph_id: "function-cs-4ce6dc36"
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

# Automator.Windows.Interop.WindowsNativeMethods.GetWindowText(nint,StringBuilder,int)

internal static extern int GetWindowText(nint window, StringBuilder text, int maximumCount)

## Connections

- impact (called by): [[function-cs-476a7400|Automator.Windows.WindowsAppCatalog.CaptureUnderCursor()]] (src/Automator.Windows/WindowsAppCatalog.cs:48)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L108`
