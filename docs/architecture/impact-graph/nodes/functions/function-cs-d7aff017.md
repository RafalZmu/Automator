---
graph_id: "function-cs-d7aff017"
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

# Automator.Services.NativeMethods.SetWindowSubclass(nint,SubclassProc,nuint,nuint)

internal static extern bool SetWindowSubclass(nint window, SubclassProc callback, nuint subclassId, nuint referenceData)

## Connections

- impact (called by): [[function-cs-0b9625f2|Automator.Services.TrayIconService.TrayIconService(nint,Action,Action,Action,Action)]] (src/Automator.App/Services/TrayIconService.cs:31)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L203`
