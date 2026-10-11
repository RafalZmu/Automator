---
graph_id: "function-cs-438d43d4"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/desktop"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Services.NativeMethods.DwmSetWindowAttribute(nint,uint,uint,uint)

internal static extern int DwmSetWindowAttribute(nint window, uint attribute, uint value, uint valueSize)

## Connections

- impact (called by): [[function-cs-ae4b309e|Automator.MainWindow.ConfigureNativeCorners()]] (src/Automator.App/MainWindow.xaml.cs:100)
- impact (called by): [[function-cs-ae4b309e|Automator.MainWindow.ConfigureNativeCorners()]] (src/Automator.App/MainWindow.xaml.cs:110)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L181`
