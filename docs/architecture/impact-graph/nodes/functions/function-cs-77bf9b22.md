---
graph_id: "function-cs-77bf9b22"
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

# Automator.Services.NativeMethods.GetCursorPos(Point)

internal static extern bool GetCursorPos(Point point)

## Connections

- impact (called by): [[function-cs-08bd04dd|Automator.Services.TrayIconService.ShowMenu()]] (src/Automator.App/Services/TrayIconService.cs:117)
- impact (called by): [[function-cs-2d1aa4b1|Automator.Services.AppCatalogService.CaptureUnderCursor()]] (src/Automator.App/Services/AppCatalogService.cs:24)
- impact (called by): [[function-cs-5fe9fc70|Automator.MainWindow.PositionNearPointer()]] (src/Automator.App/MainWindow.xaml.cs:381)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L124`
