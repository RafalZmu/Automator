---
graph_id: "function-cs-b9b4c1a2"
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

# Automator.Windows.Interop.WindowsNativeMethods.GetAsyncKeyState(int)

internal static extern short GetAsyncKeyState(int virtualKey)

## Connections

- impact (called by): [[function-cs-6e417b8c|Automator.Windows.KeyboardHookService.IsKeyDown(uint)]] (src/Automator.Windows/KeyboardHookService.cs:300)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L82`
