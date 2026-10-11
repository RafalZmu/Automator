---
graph_id: "function-cs-2cdc215a"
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

# Automator.Application.Automation.AutomationVariableService.Validate(IReadOnlyDictionary<string, JsonElement>)

public static void Validate(IReadOnlyDictionary values)

## Connections

- impact (called by): [[function-2838-415389da|lambda@tests/Automator.Variables.Specs/Program.cs:32]] (tests/Automator.Variables.Specs/Program.cs:32)
- impact (called by): [[function-2990-aabe1135|lambda@tests/Automator.Variables.Specs/Program.cs:33]] (tests/Automator.Variables.Specs/Program.cs:33)
- impact (called by): [[function-cs-19609f5b|Automator.Infrastructure.Automation.AutomationLibraryTransferService.NormalizeRecord(AutomationLibraryRecord)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:230)
- impact (called by): [[function-cs-2d99adb2|Automator.Application.Automation.AutomationVariableService.ReadAndMigrateAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs:75)
- impact (called by): [[function-cs-2d99adb2|Automator.Application.Automation.AutomationVariableService.ReadAndMigrateAsync(CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs:92)
- impact (called by): [[function-cs-bfa77545|Automator.Infrastructure.Automation.AutomationLibraryTransferService.ImportAsync(ReadOnlyMemory<byte>,CancellationToken)]] (src/Automator.Infrastructure/Automation/AutomationLibraryTransferService.cs:128)
- impact (called by): [[function-cs-cd26a675|Automator.Application.Automation.AutomationVariableService.SetAsync(IReadOnlyDictionary<string, JsonElement>,CancellationToken)]] (src/Automator.Application/Automation/AutomationVariableService.cs:35)

## Source anchors

- `src/Automator.Application/Automation/AutomationVariableService.cs#L45`
