---
graph_id: "function-cs-f94efe25"
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

# Automator.Core.Configuration.SettingsSerializer.Deserialize(string)

public static LauncherSettings Deserialize(string json)

## Connections

- impact (called by): [[function-5144-9f6c5997|lambda@tests/Automator.Core.Specs/Program.cs:113]] (tests/Automator.Core.Specs/Program.cs:113)
- impact (called by): [[function-6385-db940a51|lambda@tests/Automator.Core.Specs/Program.cs:135]] (tests/Automator.Core.Specs/Program.cs:135)
- impact (called by): [[function-7266-9b546b7e|lambda@tests/Automator.Core.Specs/Program.cs:157]] (tests/Automator.Core.Specs/Program.cs:157)
- impact (called by): [[function-7359-b554d71f|lambda@tests/Automator.Core.Specs/Program.cs:158]] (tests/Automator.Core.Specs/Program.cs:158)
- impact (called by): [[function-cs-18c37380|Program.UnknownModuleSettingsRoundTrip()]] (tests/Automator.Core.Specs/Program.cs:204)
- impact (called by): [[function-cs-2f802681|Program.ConfigurationRoundTrips()]] (tests/Automator.Core.Specs/Program.cs:102)
- impact (called by): [[function-cs-30f73aac|Automator.Backend.BackendServer.ImportSettingsAsync(string)]] (src/Automator.Backend/BackendServer.cs:1265)
- impact (called by): [[function-cs-b4327623|Automator.Windows.WindowsSettingsStore.Import(string)]] (src/Automator.Windows/WindowsSettingsStore.cs:100)
- impact (called by): [[function-cs-d1682377|Automator.Services.SettingsStore.Load()]] (src/Automator.App/Services/SettingsStore.cs:21)
- impact (called by): [[function-cs-f9444825|Program.SchemaOneSettingsMigrate()]] (tests/Automator.Core.Specs/Program.cs:184)
- impact (called by): [[function-cs-fdaacf4b|Automator.Windows.WindowsSettingsStore.Load()]] (src/Automator.Windows/WindowsSettingsStore.cs:56)
- impact (called by): [[function-cs-fe85137b|Automator.Services.SettingsStore.Import(string)]] (src/Automator.App/Services/SettingsStore.cs:75)

## Source anchors

- `src/Automator.Core/Configuration/LauncherSettings.cs#L41`
