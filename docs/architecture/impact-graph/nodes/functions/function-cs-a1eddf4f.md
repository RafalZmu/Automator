---
graph_id: "function-cs-a1eddf4f"
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

# Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ParseManifest(byte[])

private static SecretManifest ParseManifest(byte[] bytes)

## Connections

- impact (called by): [[function-cs-0fc9808a|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ExistsAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:101)
- impact (called by): [[function-cs-0fe6e3ae|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:123)
- impact (called by): [[function-cs-870707eb|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.GetValueAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:141)
- impact (called by): [[function-cs-e12005bf|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.SetAsync(string,string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:49)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L173`
