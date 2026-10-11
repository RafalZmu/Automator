---
graph_id: "function-cs-c332c613"
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

# Automator.Services.NativeMethods.SetForegroundWindow(nint)

internal static extern bool SetForegroundWindow(nint window)

## Connections

- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:507)
- impact (called by): [[function-cs-03b76bea|Automator.MainWindow.ActivatePanelWindow(string)]] (src/Automator.App/MainWindow.xaml.cs:515)
- impact (called by): [[function-cs-08bd04dd|Automator.Services.TrayIconService.ShowMenu()]] (src/Automator.App/Services/TrayIconService.cs:118)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:107)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:122)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:58)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L142`
