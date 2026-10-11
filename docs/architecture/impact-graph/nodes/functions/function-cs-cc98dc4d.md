---
graph_id: "function-cs-cc98dc4d"
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

# Automator.Services.NativeMethods.ShowWindow(nint,int)

internal static extern bool ShowWindow(nint window, int command)

## Connections

- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:470)
- impact (called by): [[function-cs-0bea5c60|Automator.MainWindow.HidePanel(string)]] (src/Automator.App/MainWindow.xaml.cs:356)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L140`
