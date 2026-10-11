---
graph_id: "function-cs-3742d375"
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

# Automator.Core.Launcher.HotkeyKeyNameValidator.IsSupported(string?)

public static bool IsSupported(string name)

## Connections

- impact (called by): [[function-cs-1fe9a2ae|Automator.Core.Launcher.HotkeyVirtualKeyMapper.ToVirtualKey(string)]] (src/Automator.Core/Launcher/HotkeyVirtualKeyMapper.cs:8)
- impact (called by): [[function-cs-3b137fa2|Automator.Windows.KeyboardHookService.ProcessKey(int,KeyboardHookData)]] (src/Automator.Windows/KeyboardHookService.cs:221)
- impact (called by): [[function-cs-68bab4ef|Automator.Windows.KeyboardHookService.KeyboardHookService(string,Func<LauncherNavigationState>,Func<LauncherWindowContext>,IApplicationLog?)]] (src/Automator.Windows/KeyboardHookService.cs:46)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:360)
- impact (called by): [[function-cs-d639e3f3|Automator.Windows.KeyboardHookService.SetOpener(string)]] (src/Automator.Windows/KeyboardHookService.cs:78)
- impact (called by): [[function-cs-fdc443c3|Automator.Core.Configuration.SettingsSerializer.Validate(LauncherSettings)]] (src/Automator.Core/Configuration/LauncherSettings.cs:107)

## Source anchors

- `src/Automator.Core/Launcher/HotkeyKeyNameValidator.cs#L16`
