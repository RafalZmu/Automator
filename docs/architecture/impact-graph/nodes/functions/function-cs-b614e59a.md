---
graph_id: "function-cs-b614e59a"
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

# Automator.Infrastructure.Automation.WindowsCredentialSecretManager.CredWriteW(NativeCredential,uint)

private static extern bool CredWriteW(NativeCredential credential, uint flags)

## Connections

- impact (called by): [[function-cs-25312868|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.WriteCredential(string,byte[])]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:211)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L268`
