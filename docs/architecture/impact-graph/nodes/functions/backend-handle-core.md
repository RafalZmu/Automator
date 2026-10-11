---
graph_id: "backend-handle-core"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Backend.BackendServer.HandleLineCoreAsync(string)

private Task HandleLineCoreAsync(string line)

## Connections

- impact (queued or direct line handling awaits the core request path): [[backend-handle-line|Automator.Backend.BackendServer.HandleLineAsync(string)]] (src/Automator.Backend/BackendServer.cs#L122-L136)
- impact (called by): [[function-6128-e1ed8c5a|lambda@src/Automator.Backend/BackendServer.cs:133]] (src/Automator.Backend/BackendServer.cs:135)
- impact (called by): [[backend-handle-line|Automator.Backend.BackendServer.HandleLineAsync(string)]] (src/Automator.Backend/BackendServer.cs:122)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L151`
