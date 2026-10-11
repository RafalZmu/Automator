---
graph_id: "function-cs-77d4e1c5"
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

# Automator.Application.Automation.ScriptRunnerTemplateCatalog.Get(string)

public static ScriptRunnerTemplateDescriptor Get(string id)

## Connections

- impact (called by): [[function-cs-2424efe2|Automator.Application.Automation.ScriptRunnerTemplateInstaller.MatchesInstalledAsset(ScriptRunnerProfile)]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:84)
- impact (called by): [[function-cs-32fa0481|Automator.Application.Automation.ScriptRunnerSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:38)
- impact (called by): [[function-cs-4af81470|ScriptRunnerTemplateSpecs.RunAsync()]] (tests/Automator.Application.Specs/ScriptRunnerTemplateSpecs.cs:9)
- impact (called by): [[function-cs-594088fd|Automator.Application.Automation.ScriptRunnerModule.RunProfileAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:176)
- impact (called by): [[function-cs-654dae28|Automator.Application.Automation.ScriptRunnerTemplateInstaller.InstallAsync(string,JsonElement,IAutomationLibrary,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs:25)

## Source anchors

- `src/Automator.Application/Automation/ScriptRunnerTemplateCatalog.cs#L56`
