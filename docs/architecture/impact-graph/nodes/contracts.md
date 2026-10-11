---
graph_id: "contracts"
graph_layer: "module"
graph_kind: "contract"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/contract"
  - "impact-graph/area/contracts"
generated_by: "scripts/impact-graph.mjs"
---

# Renderer, IPC, and RPC contracts

Shared request/response shapes, tab view kinds, RPC methods, and validated IPC payloads.

## Connections

- impact (defines IPC/RPC request and response shapes): [[desktop|Electron desktop shell]] (contracts/rpc.ts; electron/preload.ts)
- impact (defines bridge and view-kind types consumed by the renderer): [[renderer|React renderer]] (contracts/bundled-tab-view-kinds.json; ui/viewKinds.ts; ui/types.d.ts)
- impact (defines protocol methods and backend request shapes): [[backend|JSON-RPC backend]] (contracts/rpc.ts; src/Automator.Protocol)
- contains (owns source file): [[file-activity-7d425ddd|activity.ts]]
- contains (owns source file): [[file-automation-http-ipc-8acc19d2|automationHttpIpc.ts]]
- contains (owns source file): [[file-rpc-01beae63|rpc.ts]]
- contains (owns source file): [[file-script-runner-6d48095d|scriptRunner.ts]]
- contains (owns source file): [[file-rpc-protocol-2dcf2b03|RpcProtocol.cs]]
- contains (owns source file): [[file-program-956792df|Program.cs]]
- contains (owns source file): [[file-api-d41afc7b|api.ts]]
- contains (owns source file): [[file-browser-c6c6d8f4|browser.ts]]
- contains (owns source file): [[file-scheduler-7a44da61|scheduler.ts]]
- contains (owns source file): [[file-variables-50abdb6f|variables.ts]]
- contains (owns source file): [[file-website-launcher-cf4392e4|websiteLauncher.ts]]
- contains (owns source file): [[file-workflows-96f11afe|workflows.ts]]

## Source anchors

- `contracts/rpc.ts`
- `contracts/automationHttpIpc.ts`
- `contracts/bundled-tab-view-kinds.json`
- `ui/types.d.ts`
