---
graph_id: "function-cs-12f58257"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/windows"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Windows.Interop.WindowsNativeMethods.UnhookWindowsHookEx(nint)

internal static extern bool UnhookWindowsHookEx(nint hook)

## Connections

- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:144)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L61`
