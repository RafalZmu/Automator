---
graph_id: "function-cs-076d1808"
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

# Automator.Infrastructure.Automation.WindowsCredentialSecretManager.CredFree(nint)

private static extern void CredFree(nint buffer)

## Connections

- impact (called by): [[function-cs-428d35bd|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ReadCredential(string)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:239)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L280`
