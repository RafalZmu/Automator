---
graph_id: "function-cs-e17a0de1"
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

# Automator.Windows.Interop.WindowsNativeMethods.CallNextHookEx(nint,int,nint,nint)

internal static extern nint CallNextHookEx(nint hook, int code, nint message, nint data)

## Connections

- impact (called by): [[function-cs-325c624c|Automator.Windows.KeyboardHookService.HookCallback(int,nint,nint)]] (src/Automator.Windows/KeyboardHookService.cs:166)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L63`
