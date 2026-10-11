---
graph_id: "function-cs-81e2b7e8"
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

# Automator.Services.NativeMethods.DefSubclassProc(nint,uint,nuint,nint)

internal static extern nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam)

## Connections

- impact (called by): [[function-cs-fe6e446c|Automator.Services.TrayIconService.WindowProc(nint,uint,nuint,nint,nuint,nuint)]] (src/Automator.App/Services/TrayIconService.cs:99)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L207`
