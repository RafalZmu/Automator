---
graph_id: "function-cs-428d35bd"
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

# Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ReadCredential(string)

private static byte[] ReadCredential(string target)

## Connections

- impact (called by): [[function-cs-0fc9808a|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ExistsAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:104)
- impact (called by): [[function-cs-0fc9808a|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ExistsAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:97)
- impact (called by): [[function-cs-0fe6e3ae|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:118)
- impact (called by): [[function-cs-870707eb|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.GetValueAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:137)
- impact (called by): [[function-cs-870707eb|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.GetValueAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:146)
- impact (called by): [[function-cs-e12005bf|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.SetAsync(string,string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs:45)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L222`
