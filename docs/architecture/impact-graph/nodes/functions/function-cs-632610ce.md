---
graph_id: "function-cs-632610ce"
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

# Automator.Services.NativeMethods.CreateRoundRectRgn(int,int,int,int,int,int)

internal static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int ellipseWidth, int ellipseHeight)

## Connections

- impact (called by): [[function-cs-ae4b309e|Automator.MainWindow.ConfigureNativeCorners()]] (src/Automator.App/MainWindow.xaml.cs:137)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L177`
