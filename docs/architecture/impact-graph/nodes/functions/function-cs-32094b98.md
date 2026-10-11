---
graph_id: "function-cs-32094b98"
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

# Automator.Application.Automation.AutomationVariableInterpolation.Expand(string,IReadOnlyDictionary<string, JsonElement>)

public static string Expand(string text, IReadOnlyDictionary values)

## Connections

- impact (called by): [[function-2352-54590ea3|lambda@src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:44]] (src/Automator.Application/Automation/ScriptRunnerSavedProfileHandler.cs:44)
- impact (called by): [[function-2741-8deeb379|lambda@tests/Automator.Variables.Specs/Program.cs:31]] (tests/Automator.Variables.Specs/Program.cs:31)
- impact (called by): [[function-3148-813effd1|lambda@tests/Automator.Variables.Specs/Program.cs:34]] (tests/Automator.Variables.Specs/Program.cs:34)
- impact (called by): [[function-9845-ba99f72e|lambda@src/Automator.Application/Automation/ScriptRunnerModule.cs:190]] (src/Automator.Application/Automation/ScriptRunnerModule.cs:190)
- impact (called by): [[function-cs-2ed359b5|Automator.Infrastructure.Automation.AutomationApiProfileRunner.RunProfileAsync(string,JsonElement?,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationApiProfileRunner.cs:61)
- impact (called by): [[function-cs-3bc99e91|Automator.Application.Automation.BrowserSavedProfileHandler.ExecuteAsync(string,JsonElement?,AutomationExecutionMetadata,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:534)
- impact (called by): [[function-cs-580ac465|Automator.Application.Automation.BrowserAutomationModule.RunActionAsync(JsonElement,AutomationServicesContext,CancellationToken)]] (src/Automator.Application/Automation/BrowserAutomationModule.cs:206)

## Source anchors

- `src/Automator.Application/Automation/AutomationVariableService.cs#L117`
