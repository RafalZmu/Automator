---
graph_id: "app"
graph_layer: "module"
graph_kind: "root"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/root"
  - "impact-graph/area/application"
generated_by: "scripts/impact-graph.mjs"
---

# Automator application

The desktop automation application: Electron shell, React surfaces, compiled .NET modules, and host services.

## Connections

- contains (desktop host): [[desktop|Electron desktop shell]]
- contains (user interface): [[renderer|React renderer]]
- contains (surface 1): [[launcher|Launcher]]
- contains (surface 2): [[script|Script Runner]]
- contains (surface 3): [[api|API]]
- contains (surface 4): [[browser|Browser Automation]]
- contains (surface 5): [[workflows|Workflows]]
- contains (surface 6): [[scheduler|Scheduler]]
- contains (surface 7): [[work-time|Work Time]]
- contains (surface 8): [[website|Website Launcher]]
- contains (shared surface): [[search|Global Action Search]]
- contains (shared surface): [[options|Options and Variables]]
- contains (shared surface): [[activity|Run Activity and Notifications]]
- contains (backend process): [[backend|JSON-RPC backend]]
- contains (renderer action facade): [[renderer-services|Renderer action facade]]
- contains (backend capability context): [[backend-context|Backend capability context]]
- contains (shared contracts): [[contracts|Renderer, IPC, and RPC contracts]]
- contains (compiled surface map): [[tab-registry|Tab and view registries]]
- contains (platform adapters): [[windows|Windows platform adapters]]
- contains (persistence adapter): [[library|Automation library store]]

## Source anchors

- `README.md`
