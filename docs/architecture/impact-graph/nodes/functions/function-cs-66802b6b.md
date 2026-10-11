---
graph_id: "function-cs-66802b6b"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Infrastructure.Automation.InMemoryAutomationSecretManager.ValidateKey(string,string)

private static void ValidateKey(string profileId, string secretId)

## Connections

- impact (called by): [[function-cs-48b3b8a3|Automator.Infrastructure.Automation.InMemoryAutomationSecretManager.GetValueAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/InMemoryAutomationSecretManager.cs:45)
- impact (called by): [[function-cs-9d608b87|Automator.Infrastructure.Automation.InMemoryAutomationSecretManager.SetAsync(string,string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/InMemoryAutomationSecretManager.cs:19)
- impact (called by): [[function-cs-b2459f5d|Automator.Infrastructure.Automation.InMemoryAutomationSecretManager.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/InMemoryAutomationSecretManager.cs:37)
- impact (called by): [[function-cs-f86231d2|Automator.Infrastructure.Automation.InMemoryAutomationSecretManager.ExistsAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/InMemoryAutomationSecretManager.cs:30)

## Source anchors

- `src/Automator.Infrastructure/Automation/InMemoryAutomationSecretManager.cs#L50`
