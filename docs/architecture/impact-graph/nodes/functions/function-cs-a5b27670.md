---
graph_id: "function-cs-a5b27670"
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

# Automator.Services.NativeMethods.AppendMenu(nint,uint,nuint,string)

internal static extern bool AppendMenu(nint menu, uint flags, nuint id, string text)

## Connections

- impact (called by): [[function-cs-08bd04dd|Automator.Services.TrayIconService.ShowMenu()]] (src/Automator.App/Services/TrayIconService.cs:114)
- impact (called by): [[function-cs-08bd04dd|Automator.Services.TrayIconService.ShowMenu()]] (src/Automator.App/Services/TrayIconService.cs:115)
- impact (called by): [[function-cs-08bd04dd|Automator.Services.TrayIconService.ShowMenu()]] (src/Automator.App/Services/TrayIconService.cs:116)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L216`
