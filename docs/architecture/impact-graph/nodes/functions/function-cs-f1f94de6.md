---
graph_id: "function-cs-f1f94de6"
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

# Automator.Services.NativeMethods.GetWindowThreadProcessId(nint,uint)

internal static extern uint GetWindowThreadProcessId(nint window, uint processId)

## Connections

- impact (called by): [[function-7838-49316956|lambda@src/Automator.App/Services/WindowsLauncherService.cs:158]] (src/Automator.App/Services/WindowsLauncherService.cs:161)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:475)
- impact (called by): [[function-cs-2d1aa4b1|Automator.Services.AppCatalogService.CaptureUnderCursor()]] (src/Automator.App/Services/AppCatalogService.cs:37)
- impact (called by): [[function-cs-dff82375|Automator.App.ActivateExistingInstance()]] (src/Automator.App/App.xaml.cs:149)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L132`
