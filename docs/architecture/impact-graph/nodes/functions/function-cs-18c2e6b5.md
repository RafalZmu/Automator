---
graph_id: "function-cs-18c2e6b5"
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

# Automator.Services.NativeMethods.Shell_NotifyIcon(uint,NotifyIconData)

internal static extern bool Shell_NotifyIcon(uint message, NotifyIconData data)

## Connections

- impact (called by): [[function-cs-0b9625f2|Automator.Services.TrayIconService.TrayIconService(nint,Action,Action,Action,Action)]] (src/Automator.App/Services/TrayIconService.cs:51)
- impact (called by): [[function-cs-e0917e34|Automator.Services.TrayIconService.Dispose()]] (src/Automator.App/Services/TrayIconService.cs:144)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L210`
