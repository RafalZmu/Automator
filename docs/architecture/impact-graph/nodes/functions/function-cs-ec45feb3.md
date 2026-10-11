---
graph_id: "function-cs-ec45feb3"
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

# Automator.Services.NativeMethods.GetWindowLongPtr(nint,int)

internal static extern nint GetWindowLongPtr(nint window, int index)

## Connections

- impact (called by): [[function-cs-bcf32019|Automator.MainWindow.ConfigureNativeWindow()]] (src/Automator.App/MainWindow.xaml.cs:80)
- impact (called by): [[function-cs-bcf32019|Automator.MainWindow.ConfigureNativeWindow()]] (src/Automator.App/MainWindow.xaml.cs:84)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L158`
