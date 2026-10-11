---
graph_id: "file-windows-credential-secret-manager-a514aabe"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/secrets"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# WindowsCredentialSecretManager.cs

Source file src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs.

## Connections

- contains (declares function): [[function-cs-51e40d10|Automator.Infrastructure.Automation.IAutomationSecretValueReader.GetValueAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L13)
- contains (declares function): [[function-cs-e12005bf|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.SetAsync(string,string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L30)
- contains (declares function): [[function-cs-0fc9808a|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ExistsAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L93)
- contains (declares function): [[function-cs-0fe6e3ae|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.DeleteAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L113)
- contains (declares function): [[function-cs-870707eb|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.GetValueAsync(string,string,CancellationToken)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L132)
- contains (declares function): [[function-cs-60632398|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ValidateKey(string,string)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L164)
- contains (declares function): [[function-cs-ce69fbff|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.Target(string,string)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L170)
- contains (declares function): [[function-cs-52e25e5a|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ChunkTarget(string,string,int)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L171)
- contains (declares function): [[function-cs-a1eddf4f|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ParseManifest(byte[])]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L173)
- contains (declares function): [[function-cs-17b2e78c|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.TryDeleteChunks(string,string,int)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L185)
- contains (declares function): [[function-cs-25312868|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.WriteCredential(string,byte[])]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L194)
- contains (declares function): [[function-cs-428d35bd|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.ReadCredential(string)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L222)
- contains (declares function): [[function-cs-aa14ac0f|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.DeleteCredential(string)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L242)
- contains (declares function): [[function-cs-b614e59a|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.CredWriteW(NativeCredential,uint)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L268)
- contains (declares function): [[function-cs-1bad25dd|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.CredReadW(string,uint,uint,nint)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L272)
- contains (declares function): [[function-cs-1ef04643|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.CredDeleteW(string,uint,uint)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L276)
- contains (declares function): [[function-cs-076d1808|Automator.Infrastructure.Automation.WindowsCredentialSecretManager.CredFree(nint)]] (src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs#L280)
- impact (type-reference): [[file-automation-api-profile-runner-5fd1dcea|AutomationApiProfileRunner.cs]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:13)
- impact (type-reference): [[file-automation-api-profile-runner-5fd1dcea|AutomationApiProfileRunner.cs]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:42)
- impact (type-reference): [[file-in-memory-automation-secret-manager-3a9cf48b|InMemoryAutomationSecretManager.cs]] (src/Automator.Infrastructure/Automation/InMemoryAutomationSecretManager.cs:9)

## Source anchors

- `src/Automator.Infrastructure/Automation/WindowsCredentialSecretManager.cs`
