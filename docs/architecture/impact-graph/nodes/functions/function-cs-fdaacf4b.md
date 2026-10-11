---
graph_id: "function-cs-fdaacf4b"
graph_layer: "function"
graph_kind: "method"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/method"
  - "impact-graph/area/windows"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Windows.WindowsSettingsStore.Load()

public SettingsLoadResult Load()

## Connections

- impact (called by): [[function-cs-0f944a05|Program.LegacySettingsUpgradeIsAtomic()]] (tests/Automator.Windows.Specs/Program.cs:140)
- impact (called by): [[function-cs-191b0921|Program.FailedModuleMigrationPreservesSource()]] (tests/Automator.Windows.Specs/Program.cs:167)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:961)

## Source anchors

- `src/Automator.Windows/WindowsSettingsStore.cs#L44`
