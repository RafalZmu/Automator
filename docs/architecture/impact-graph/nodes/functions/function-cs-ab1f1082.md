---
graph_id: "function-cs-ab1f1082"
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

# Automator.Services.NativeMethods.SetWindowLongPtr(nint,int,nint)

internal static extern nint SetWindowLongPtr(nint window, int index, nint value)

## Connections

- impact (called by): [[function-cs-bcf32019|Automator.MainWindow.ConfigureNativeWindow()]] (src/Automator.App/MainWindow.xaml.cs:82)
- impact (called by): [[function-cs-bcf32019|Automator.MainWindow.ConfigureNativeWindow()]] (src/Automator.App/MainWindow.xaml.cs:86)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L160`
