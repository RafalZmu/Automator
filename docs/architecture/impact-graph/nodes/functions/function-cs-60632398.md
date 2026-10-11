---
graph_id: "function-cs-60632398"
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

# Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ValidateKey(string,string)

private static void ValidateKey(string profileId, string secretId)

## Connections

- impact (called by): [[function-cs-0fc9808a|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ExistsAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:96)
- impact (called by): [[function-cs-0fe6e3ae|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:116)
- impact (called by): [[function-cs-870707eb|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.GetValueAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:135)
- impact (called by): [[function-cs-e12005bf|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.SetAsync(string,string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:33)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L164`
