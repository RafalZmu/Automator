---
graph_id: "function-cs-1ef04643"
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

# Automator.Infrastructure.Automation.WindowsCredentialSecretManager.CredDeleteW(string,uint,uint)

private static extern bool CredDeleteW(string targetName, uint type, uint flags)

## Connections

- impact (called by): [[function-cs-aa14ac0f|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.DeleteCredential(string)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:244)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L276`
