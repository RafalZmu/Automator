---
graph_id: "function-cs-873fc862"
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

# Automator.Core.Configuration.SettingsSerializer.FindMissingPaths(LauncherSettings,Func<string, bool>)

public static IReadOnlyList FindMissingPaths(LauncherSettings settings, Func pathExists)

## Connections

- impact (called by): [[function-cs-2f802681|Program.ConfigurationRoundTrips()]] (tests/Automator.Core.Specs/Program.cs:107)
- impact (called by): [[function-cs-2f802681|Program.ConfigurationRoundTrips()]] (tests/Automator.Core.Specs/Program.cs:108)
- impact (called by): [[function-cs-8b971eca|Automator.Backend.BackendServer.GetState()]] (src/Automator.Backend/BackendServer.cs:1011)
- impact (called by): [[function-cs-e30a99b8|Automator.Services.SettingsStore.FindMissingPaths(LauncherSettings)]] (src/Automator.App/Services/SettingsStore.cs:92)

## Source anchors

- `src/Automator.Core/Configuration/LauncherSettings.cs#L84`
