---
graph_id: "function-cs-6855ddf8"
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

# Automator.Windows.Interop.WindowsNativeMethods.GetModuleHandle(string?)

internal static extern nint GetModuleHandle(string moduleName)

## Connections

- impact (called by): [[function-cs-1d77c74b|Automator.Windows.KeyboardHookService.HookThread()]] (src/Automator.Windows/KeyboardHookService.cs:112)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L77`
