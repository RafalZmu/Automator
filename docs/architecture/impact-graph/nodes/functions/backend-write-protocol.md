---
graph_id: "backend-write-protocol"
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

# Automator.Backend.BackendServer.WriteProtocolAsync(object)

private Task WriteProtocolAsync(object value)

## Connections

- impact (request handling writes the serialized result): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] (src/Automator.Backend/BackendServer.cs#L158-L159)
- impact (the host parses protocol lines emitted by backend stdout): [[backend-consume-stdout|BackendProcess.consumeStdout]] (src/Automator.Backend/BackendServer.cs#L1648-L1654; electron/backendProcess.ts#L49)
- impact (called by): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] (src/Automator.Backend/BackendServer.cs:159)
- impact (called by): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] (src/Automator.Backend/BackendServer.cs:163)
- impact (called by): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] (src/Automator.Backend/BackendServer.cs:169)
- impact (called by): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] (src/Automator.Backend/BackendServer.cs:177)
- impact (called by): [[function-cs-8e0d0e01|Automator.Backend.BackendServer.SendNotificationAsync(string,object)]] (src/Automator.Backend/BackendServer.cs:1646)

## Source anchors

- `src/Automator.Backend/BackendServer.cs#L1648`
