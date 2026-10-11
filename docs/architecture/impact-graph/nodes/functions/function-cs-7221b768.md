---
graph_id: "function-cs-7221b768"
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

# Automator.Services.NativeMethods.UnhookWindowsHookEx(nint)

internal static extern bool UnhookWindowsHookEx(nint hook)

## Connections

- impact (called by): [[function-cs-bee7c0b4|Automator.Services.KeyboardHookService.HookThread()]] (src/Automator.App/Services/KeyboardHookService.cs:130)

## Source anchors

- `src/Automator.App/Services/NativeMethods.cs#L93`
