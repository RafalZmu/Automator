---
graph_id: "file-workflow-module-465ef471"
graph_layer: "file"
graph_kind: "csharp"
tags:
  - "impact-graph"
  - "impact-graph/file"
  - "impact-graph/kind/csharp"
  - "impact-graph/area/workflows"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# WorkflowModule.cs

Source file src/Automator.Application/Automation/WorkflowModule.cs.

## Connections

- contains (declares function): [[function-cs-0da7b079|Automator.Application.Automation.WorkflowModule.CreateInitialState()]] (src/Automator.Application/Automation/WorkflowModule.cs#L58)
- contains (declares function): [[function-cs-1b3bbe04|Automator.Application.Automation.WorkflowModule.CreateDefaultSettings()]] (src/Automator.Application/Automation/WorkflowModule.cs#L61)
- contains (declares function): [[function-cs-f69cd148|Automator.Application.Automation.WorkflowModule.MigrateSettings(int,JsonElement)]] (src/Automator.Application/Automation/WorkflowModule.cs#L63)
- contains (declares function): [[function-cs-3d661bc6|Automator.Application.Automation.WorkflowModule.ExecuteAsync(string,JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs#L66)
- contains (declares function): [[function-cs-69732be8|Automator.Application.Automation.WorkflowModule.ListWorkflowsAsync(AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs#L94)
- contains (declares function): [[function-cs-71ed3a11|Automator.Application.Automation.WorkflowModule.GetWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs#L111)
- contains (declares function): [[function-cs-8734f347|Automator.Application.Automation.WorkflowModule.SaveWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs#L127)
- contains (declares function): [[function-cs-cb57835a|Automator.Application.Automation.WorkflowModule.ListSavedProfilesAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs#L141)
- contains (declares function): [[function-8518-ec1d6f57|lambda@src/Automator.Application/Automation/WorkflowModule.cs:153]] (src/Automator.Application/Automation/WorkflowModule.cs#L153)
- contains (declares function): [[function-cs-ec179b43|Automator.Application.Automation.WorkflowModule.DeleteWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs#L156)
- contains (declares function): [[function-cs-452bdcae|Automator.Application.Automation.WorkflowModule.RunWorkflowAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/WorkflowModule.cs#L165)
- contains (declares function): [[function-11482-aef65fcc|lambda@src/Automator.Application/Automation/WorkflowModule.cs:206]] (src/Automator.Application/Automation/WorkflowModule.cs#L206)
- contains (declares function): [[function-cs-537b6eb4|Automator.Application.Automation.WorkflowModule.CreateUiPayload(string,AutomationWorkflowRunResult)]] (src/Automator.Application/Automation/WorkflowModule.cs#L222)
- contains (declares function): [[function-12308-4c5cbd6b|lambda@src/Automator.Application/Automation/WorkflowModule.cs:225]] (src/Automator.Application/Automation/WorkflowModule.cs#L225)
- contains (declares function): [[function-cs-0cfa1354|Automator.Application.Automation.WorkflowModule.CreateSummaryOnlyPayload(string,AutomationWorkflowRunResult)]] (src/Automator.Application/Automation/WorkflowModule.cs#L230)
- contains (declares function): [[function-12735-5722464c|lambda@src/Automator.Application/Automation/WorkflowModule.cs:233]] (src/Automator.Application/Automation/WorkflowModule.cs#L233)
- contains (declares function): [[function-cs-d2a0e42a|Automator.Application.Automation.WorkflowModule.ValidateProfile(AutomationWorkflowProfile)]] (src/Automator.Application/Automation/WorkflowModule.cs#L240)
- contains (declares function): [[function-cs-5105a0f5|Automator.Application.Automation.WorkflowModule.Validate(AutomationWorkflowProfile)]] (src/Automator.Application/Automation/WorkflowModule.cs#L242)
- contains (declares function): [[function-17177-81b912c9|lambda@src/Automator.Application/Automation/WorkflowModule.cs:299]] (src/Automator.Application/Automation/WorkflowModule.cs#L299)
- contains (declares function): [[function-cs-4fc81695|Automator.Application.Automation.WorkflowModule.TryParseJsonPointer(string?,bool,string[])]] (src/Automator.Application/Automation/WorkflowModule.cs#L313)
- contains (declares function): [[function-cs-6d620169|Automator.Application.Automation.WorkflowModule.IsPrefix(IReadOnlyList<string>,IReadOnlyList<string>)]] (src/Automator.Application/Automation/WorkflowModule.cs#L344)
- contains (declares function): [[function-19104-ac5d4757|lambda@src/Automator.Application/Automation/WorkflowModule.cs:345]] (src/Automator.Application/Automation/WorkflowModule.cs#L345)
- contains (declares function): [[function-19157-ca21bc0f|lambda@src/Automator.Application/Automation/WorkflowModule.cs:345]] (src/Automator.Application/Automation/WorkflowModule.cs#L345)
- contains (declares function): [[function-cs-1ca72ebc|Automator.Application.Automation.WorkflowModule.DeserializeProfile(JsonElement)]] (src/Automator.Application/Automation/WorkflowModule.cs#L347)
- contains (declares function): [[function-cs-0e12831c|Automator.Application.Automation.WorkflowModule.NormalizeProfile(AutomationWorkflowProfile)]] (src/Automator.Application/Automation/WorkflowModule.cs#L368)
- contains (declares function): [[function-cs-e236e522|Automator.Application.Automation.WorkflowModule.EmptyVariables()]] (src/Automator.Application/Automation/WorkflowModule.cs#L371)
- contains (declares function): [[function-cs-5fb26a85|Automator.Application.Automation.WorkflowModule.ReadWorkflowId(JsonElement)]] (src/Automator.Application/Automation/WorkflowModule.cs#L374)
- contains (declares function): [[function-cs-c6d3b281|Automator.Application.Automation.WorkflowModule.ReadInitialInput(JsonElement)]] (src/Automator.Application/Automation/WorkflowModule.cs#L384)
- contains (declares function): [[function-cs-b6b28d3c|Automator.Application.Automation.WorkflowModule.Error(string)]] (src/Automator.Application/Automation/WorkflowModule.cs#L393)
- contains (declares function): [[function-cs-a00e9932|Automator.Application.Automation.WorkflowModule.Result(AutomationStatus,string,object)]] (src/Automator.Application/Automation/WorkflowModule.cs#L395)
- contains (declares function): [[function-cs-bc69eb2f|Automator.Application.Automation.WorkflowModule.CreateJsonOptions()]] (src/Automator.Application/Automation/WorkflowModule.cs#L402)
- impact (type-reference): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs:98)

## Source anchors

- `src/Automator.Application/Automation/WorkflowModule.cs`
