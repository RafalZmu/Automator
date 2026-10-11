---
graph_id: "function-cs-16387e34"
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

# Automator.Services.NativeMethods.TrackPopupMenu(nint,uint,int,int,int,nint,nint)

internal static extern uint TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint window, nint rectangle)

## Connections

- impact (called by): [[function-cs-08bd04dd|Automator.Services.TrayIconService.ShowMenu()]] (src/Automator.App/Services/TrayIconService.cs:119)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L218`
