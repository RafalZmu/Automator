---
graph_id: "function-cs-51e40d10"
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

# Automator.Infrastructure.Automation.IAutomationSecretValueReader.GetValueAsync(string,string,CancellationToken)

Task GetValueAsync(string profileId, string secretId, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-cs-2ed359b5|Automator.Infrastructure.Automation.AutomationApiProfileRunner.RunProfileAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:42)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L13`
