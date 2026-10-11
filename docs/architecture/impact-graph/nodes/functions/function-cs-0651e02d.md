---
graph_id: "function-cs-0651e02d"
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

# Automator.Services.NativeMethods.PostThreadMessage(uint,uint,nuint,nint)

internal static extern bool PostThreadMessage(uint threadId, uint message, nuint wParam, nint lParam)

## Connections

- impact (called by): [[function-cs-4d4949d5|Automator.Services.KeyboardHookService.Dispose()]] (src/Automator.App/Services/KeyboardHookService.cs:235)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L107`
