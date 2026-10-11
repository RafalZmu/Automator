---
graph_id: "function-cs-85cd89e3"
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

# Automator.Services.NativeMethods.CallNextHookEx(nint,int,nint,nint)

internal static extern nint CallNextHookEx(nint hook, int code, nint message, nint data)

## Connections

- impact (called by): [[function-cs-3a9d9de5|Automator.Services.KeyboardHookService.HookCallback(int,nint,nint)]] (src/Automator.App/Services/KeyboardHookService.cs:163)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L95`
