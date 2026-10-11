---
graph_id: "function-cs-a18fd8cc"
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

# Automator.Services.NativeMethods.PostMessage(nint,uint,nuint,nint)

internal static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam)

## Connections

- impact (called by): [[function-cs-08bd04dd|Automator.Services.TrayIconService.ShowMenu()]] (src/Automator.App/Services/TrayIconService.cs:120)
- impact (called by): [[function-cs-dff82375|Automator.App.ActivateExistingInstance()]] (src/Automator.App/App.xaml.cs:156)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L222`
