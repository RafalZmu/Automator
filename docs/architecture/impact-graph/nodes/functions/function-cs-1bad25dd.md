---
graph_id: "function-cs-1bad25dd"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/secrets"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Infrastructure.Automation.WindowsCredentialSecretManager.CredReadW(string,uint,uint,nint)

private static extern bool CredReadW(string targetName, uint type, uint flags, nint credential)

## Connections

- impact (called by): [[function-cs-428d35bd|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ReadCredential(string)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:224)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L272`
