---
graph_id: "http"
graph_layer: "module"
graph_kind: "adapter"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/adapter"
  - "impact-graph/area/infrastructure"
generated_by: "scripts/impact-graph.mjs"
---

# HTTP service and network policy

Sends profile requests while enforcing the configured network policy.

## Connections

- impact (API profiles use the shared HTTP client): [[api|API]] (src/Automator.Application/Automation/ApiModule.cs; src/Automator.Infrastructure/Automation/SharedHttpService.cs)
- contains (owns source file): [[file-shared-http-service-6a990934|SharedHttpService.cs]]

## Source anchors

- `src/Automator.Infrastructure/Automation/SharedHttpService.cs`
- `src/Automator.Infrastructure/Automation/HostAddressResolver.cs`
