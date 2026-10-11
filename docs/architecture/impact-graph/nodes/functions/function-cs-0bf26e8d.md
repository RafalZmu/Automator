---
graph_id: "function-cs-0bf26e8d"
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

# Automator.Services.NativeMethods.SetWindowPos(nint,nint,int,int,int,int,uint)

internal static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags)

## Connections

- impact (called by): [[function-cs-5fe9fc70|Automator.MainWindow.PositionNearPointer()]] (src/Automator.App/MainWindow.xaml.cs:389)
- impact (called by): [[function-cs-bcf32019|Automator.MainWindow.ConfigureNativeWindow()]] (src/Automator.App/MainWindow.xaml.cs:87)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L169`
