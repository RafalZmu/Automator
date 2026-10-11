---
graph_id: "backend-handle-line"
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

# Automator.Backend.BackendServer.HandleLineAsync(string)

private Task HandleLineAsync(string line)

## Connections

- impact (the stdin loop hands each request line to HandleLineAsync): [[backend-run|Automator.Backend.BackendServer.RunAsync()]] (src/Automator.Backend/BackendServer.cs#L88-L92)
- impact (called by): [[backend-run|Automator.Backend.BackendServer.RunAsync()]] (src/Automator.Backend/BackendServer.cs:91)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L112`
