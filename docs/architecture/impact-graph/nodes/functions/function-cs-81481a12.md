---
graph_id: "function-cs-81481a12"
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

# Automator.Windows.WindowsApplicationActivator.NormalizePath(string)

private static string NormalizePath(string path)

## Connections

- impact (called by): [[function-8721-89a19ab9|lambda@src/Automator.Windows/WindowsApplicationActivator.cs:156]] (src/Automator.Windows/WindowsApplicationActivator.cs:165)
- impact (called by): [[function-cs-6ca5d076|Automator.Windows.WindowsApplicationActivator.ShouldExcludeProcess(uint)]] (src/Automator.Windows/WindowsApplicationActivator.cs:185)
- impact (called by): [[function-cs-76d50e96|Automator.Windows.WindowsApplicationActivator.WindowsApplicationActivator(ForegroundWindowMonitor,int,string,IApplicationLog?)]] (src/Automator.Windows/WindowsApplicationActivator.cs:22)
- impact (called by): [[function-cs-8ba9c460|Automator.Windows.WindowsApplicationActivator.FindWindows(string)]] (src/Automator.Windows/WindowsApplicationActivator.cs:154)

## Source anchors

- `src/Automator.Windows/WindowsApplicationActivator.cs#L203`
