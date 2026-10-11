---
graph_id: "function-cs-2def1775"
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

# Automator.Services.AppLogger.Write(string,string,string,Exception?,IReadOnlyDictionary<string, object?>?)

private static void Write(string level, string eventName, string message, Exception exception, IReadOnlyDictionary properties)

## Connections

- impact (called by): [[function-cs-2c73d5ab|Automator.Services.AppLogger.Warning(string,string,Exception?,IReadOnlyDictionary<string, object?>?)]] (src/Automator.App/Services/AppLogger.cs:74)
- impact (called by): [[function-cs-6de22692|Automator.Services.AppLogger.Critical(string,string,Exception?,IReadOnlyDictionary<string, object?>?)]] (src/Automator.App/Services/AppLogger.cs:82)
- impact (called by): [[function-cs-8a8d3172|Automator.Services.AppLogger.Error(string,string,Exception?,IReadOnlyDictionary<string, object?>?)]] (src/Automator.App/Services/AppLogger.cs:78)
- impact (called by): [[function-cs-cd03b137|Automator.Services.AppLogger.Info(string,string,IReadOnlyDictionary<string, object?>?)]] (src/Automator.App/Services/AppLogger.cs:70)

## Source anchors

- `src/Automator.App/Services/AppLogger.cs#L84`
