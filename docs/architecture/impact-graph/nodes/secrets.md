---
graph_id: "secrets"
graph_layer: "module"
graph_kind: "adapter"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/adapter"
  - "impact-graph/area/infrastructure"
generated_by: "scripts/impact-graph.mjs"
---

# Secret manager

Resolves named secrets from Windows Credential Manager or the isolated test implementation.

## Connections

- impact (named profile secrets are resolved by the HTTP path): [[http|HTTP service and network policy]] (src/Automator.Infrastructure/Automation/SharedHttpService.cs; src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs)
- contains (owns source file): [[file-windows-credential-secret-manager-a514aabe|WindowsCredentialSecretManager.cs]]

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs`
