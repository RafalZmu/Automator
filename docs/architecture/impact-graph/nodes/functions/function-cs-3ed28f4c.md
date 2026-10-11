---
graph_id: "function-cs-3ed28f4c"
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

# Automator.Windows.Interop.WindowsNativeMethods.EnumWindows(EnumWindowsProc,nint)

internal static extern bool EnumWindows(EnumWindowsProc callback, nint data)

## Connections

- impact (called by): [[function-cs-8ba9c460|Automator.Windows.WindowsApplicationActivator.FindWindows(string)]] (src/Automator.Windows/WindowsApplicationActivator.cs:156)

## Source anchors

- `src/Automator.Windows/Interop/WindowsNativeMethods.cs#L100`
