---
graph_id: "backend"
graph_layer: "module"
graph_kind: "runtime"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/runtime"
  - "impact-graph/area/runtime"
generated_by: "scripts/impact-graph.mjs"
---

# JSON-RPC backend

Reads JSON-RPC requests, validates and dispatches them, writes results, and publishes notifications.

## Connections

- contains (stdin loop): [[backend-run|Automator.Backend.BackendServer.RunAsync()]]
- contains (line handling): [[backend-handle-line|Automator.Backend.BackendServer.HandleLineAsync(string)]]
- contains (request dispatch core): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]]
- contains (RPC route): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]]
- contains (module action route): [[backend-execute-module|Automator.Backend.BackendServer.ExecuteModuleActionAsync(RpcRequest)]]
- contains (stdout response writer): [[backend-write-protocol|Automator.Backend.BackendServer.WriteProtocolAsync(object)]]
- contains (owns source file): [[file-backend-server-c9515ea3|BackendServer.cs]]
- contains (owns source file): [[file-backend-ui-state-8ff0ced1|BackendUiState.cs]]
- contains (owns source file): [[file-program-835ecfee|Program.cs]]

## Source anchors

- `src/Automator.Backend/BackendServer.cs`
- `src/Automator.Protocol`
