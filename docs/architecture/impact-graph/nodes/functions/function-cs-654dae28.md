---
graph_id: "function-cs-654dae28"
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

# Automator.Application.Automation.ScriptRunnerTemplateInstaller.InstallAsync(string,JsonElement,IAutomationLibrary,CancellationToken)

public Task InstallAsync(string id, JsonElement settings, IAutomationLibrary library, CancellationToken cancellationToken)

## Connections

- impact (called by): [[function-8047-09c167ad|lambda@tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:81]] (tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:81)
- impact (called by): [[function-8494-0b500942|lambda@tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:85]] (tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:85)
- impact (called by): [[function-cs-75ed9ff8|ScriptRunnerTemplateInstallSpecs.RunAsync()]] (tests/Automator.Application.Specs/ScriptRunnerTemplateInstallSpecs.cs:74)
- impact (called by): [[function-cs-c8b971aa|Automator.Application.Automation.ScriptRunnerModule.InstallTemplateAsync(JsonElement,JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:133)

## Source anchors

- `src/Automator.Application/Automation/ScriptRunnerTemplateInstaller.cs#L22`
