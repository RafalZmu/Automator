---
graph_id: "function-cs-3c671cc4"
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

# Automator.Services.NativeMethods.ShowWindowAsync(nint,int)

internal static extern bool ShowWindowAsync(nint window, int command)

## Connections

- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:106)
- impact (called by): [[function-cs-3da4f18c|Automator.Services.WindowsLauncherService.WaitAndMaximizeAsync(string,Process,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:121)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:47)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:56)
- impact (called by): [[function-cs-701adf3d|Automator.Services.WindowsLauncherService.ActivateAsync(AppBinding,CancellationToken)]] (src/Automator.App/Services/WindowsLauncherService.cs:57)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L138`
