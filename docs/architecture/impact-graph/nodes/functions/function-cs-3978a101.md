---
graph_id: "function-cs-3978a101"
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

# Automator.Services.NativeMethods.RemoveWindowSubclass(nint,SubclassProc,nuint)

internal static extern bool RemoveWindowSubclass(nint window, SubclassProc callback, nuint subclassId)

## Connections

- impact (called by): [[function-cs-e0917e34|Automator.Services.TrayIconService.Dispose()]] (src/Automator.App/Services/TrayIconService.cs:149)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L205`
