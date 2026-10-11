---
graph_id: "function-cs-c280ac39"
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

# Automator.Services.NativeMethods.GetForegroundWindow()

internal static extern nint GetForegroundWindow()

## Connections

- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:465)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:472)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:512)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:516)
- impact (called by): [[function-cs-103bed2e|Automator.MainWindow.OnWindowActivated(object,WindowActivatedEventArgs)]] (src/Automator.App/MainWindow.xaml.cs:429)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:36)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L130`
