---
graph_id: "function-cs-962cbb73"
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

# Automator.Services.AppLogger.WriteCore(string,string,string,Exception?,IReadOnlyDictionary<string, object?>?)

private static void WriteCore(string level, string eventName, string message, Exception exception, IReadOnlyDictionary properties)

## Connections

- impact (called by): [[function-cs-2def1775|Automator.Services.AppLogger.Write(string,string,string,Exception?,IReadOnlyDictionary<string, object?>?)]] (src/Automator.App/Services/AppLogger.cs:88)
- impact (called by): [[function-cs-8f22c07c|Automator.Services.AppLogger.Initialize()]] (src/Automator.App/Services/AppLogger.cs:59)

## Source anchors

- `src/Automator.App/Services/AppLogger.cs#L91`
