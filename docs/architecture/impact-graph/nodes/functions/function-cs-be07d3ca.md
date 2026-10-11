---
graph_id: "function-cs-be07d3ca"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/script"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Application.Automation.ScriptRunnerModule.Validate(ScriptRunnerProfile)

public static void Validate(ScriptRunnerProfile profile)

## Connections

- impact (called by): [[function-cs-19609f5b|Automator.Infrastructure.Automation.AutomationLibraryTransferService.NormalizeRecord(AutomationLibraryRecord)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:202)
- impact (called by): [[function-cs-32fa0481|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:45)
- impact (called by): [[function-cs-4af81470|ScriptRunnerTemplateSpecs.RunAsync()]] (tests/Automator.Application.Specs/ScriptRunnerTemplateSpecs.cs:46)
- impact (called by): [[function-cs-594088fd|Automator.Application.Automation.ScriptRunnerModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:169)
- impact (called by): [[function-cs-594088fd|Automator.Application.Automation.ScriptRunnerModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:191)
- impact (called by): [[function-cs-654dae28|Automator.Application.Automation.ScriptRunnerTemplateInstaller.InstallAsync(string,JsonElement,IAutomationLibrary,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:35)
- impact (called by): [[function-cs-654dae28|Automator.Application.Automation.ScriptRunnerTemplateInstaller.InstallAsync(string,JsonElement,IAutomationLibrary,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:64)
- impact (called by): [[function-cs-9a66600f|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ReadProfile(JsonElement)]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:93)
- impact (called by): [[function-cs-f7252106|Automator.Application.Automation.ScriptRunnerModule.SaveProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:143)

## Source anchors

- `src/Automator.Application/Automation/ScriptRunnerModule.cs#L267`
