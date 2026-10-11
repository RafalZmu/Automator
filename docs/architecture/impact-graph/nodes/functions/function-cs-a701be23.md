---
graph_id: "function-cs-a701be23"
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

# Automator.Windows.WindowsSettingsStore.Save(LauncherSettings,bool)

public void Save(LauncherSettings settings, bool replacingCorruptFile)

## Connections

- impact (called by): [[function-cs-0fedf070|Automator.Backend.BackendServer.SaveAliasAsync(SaveAliasParams)]] (src/Automator.Backend/BackendServer.cs:1180)
- impact (called by): [[function-cs-b9bf23d1|Automator.Backend.BackendServer.UpdateSettingsAsync(SettingsUpdateParams)]] (src/Automator.Backend/BackendServer.cs:1224)
- impact (called by): [[function-cs-bb2ddc62|Automator.Windows.WindowsSettingsStore.Import(LauncherSettings)]] (src/Automator.Windows/WindowsSettingsStore.cs:113)
- impact (called by): [[function-cs-e1dceae3|Automator.Backend.BackendServer.RelinkAsync(RelinkParams)]] (src/Automator.Backend/BackendServer.cs:1360)
- impact (called by): [[function-cs-e39d116e|Automator.Backend.BackendServer.RemoveBindingAsync(RemoveBindingParams)]] (src/Automator.Backend/BackendServer.cs:1203)
- impact (called by): [[function-cs-f3c0dc09|Automator.Backend.BackendServer.UpdateModuleSettingsCoreAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:587)

## Source anchors

- `src/Automator.Windows/WindowsSettingsStore.cs#L87`
