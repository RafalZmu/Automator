---
graph_id: "function-cs-6f0a5723"
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

# Automator.Services.NativeMethods.SetWindowRgn(nint,nint,bool)

internal static extern bool SetWindowRgn(nint window, nint region, bool redraw)

## Connections

- impact (called by): [[function-cs-ae4b309e|Automator.MainWindow.ConfigureNativeCorners()]] (src/Automator.App/MainWindow.xaml.cs:154)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L171`
