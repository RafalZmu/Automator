---
graph_id: "function-cs-0e9987d4"
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

# Automator.Application.Launcher.AliasLaunchScheduler.CancelPending()

public void CancelPending()

## Connections

- impact (called by): [[function-84600-faf3abc3|lambda@src/Automator.Backend/BackendServer.cs:1576]] (src/Automator.Backend/BackendServer.cs:1594)
- impact (called by): [[function-cs-00fc632e|Automator.Application.Launcher.LauncherModeCoordinator.SetMode(LauncherMode)]] (src/Automator.Application/Launcher/LauncherModeCoordinator.cs:12)
- impact (called by): [[function-cs-192e2a7c|Program.PendingAliasDelayCanBeCancelled()]] (tests/Automator.Application.Specs/Program.cs:1218)
- impact (called by): [[function-cs-30f73aac|Automator.Backend.BackendServer.ImportSettingsAsync(string)]] (src/Automator.Backend/BackendServer.cs:1287)
- impact (called by): [[function-cs-52c95cb3|Automator.Backend.BackendServer.StartTrackedActivationAsync(AppBinding,string?)]] (src/Automator.Backend/BackendServer.cs:1484)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:229)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:248)
- impact (called by): [[backend-dispatch|Automator.Backend.BackendServer.DispatchAsync(RpcRequest)]] (src/Automator.Backend/BackendServer.cs:275)
- impact (called by): [[function-cs-b15c1e38|Automator.Backend.BackendServer.SetQueryAsync(string)]] (src/Automator.Backend/BackendServer.cs:1153)
- impact (called by): [[function-cs-b314ad94|Program.QueryChangeDoesNotCancelActivation()]] (tests/Automator.Application.Specs/Program.cs:1236)
- impact (called by): [[function-cs-b9bf23d1|Automator.Backend.BackendServer.UpdateSettingsAsync(SettingsUpdateParams)]] (src/Automator.Backend/BackendServer.cs:1231)
- impact (called by): [[function-cs-bc9e1b17|Automator.Backend.BackendServer.HidePanelAsync()]] (src/Automator.Backend/BackendServer.cs:1440)
- impact (called by): [[function-cs-e327bb1a|Automator.Backend.BackendServer.BeginCatalogLoadingAsync()]] (src/Automator.Backend/BackendServer.cs:1372)
- impact (called by): [[function-cs-e39d116e|Automator.Backend.BackendServer.RemoveBindingAsync(RemoveBindingParams)]] (src/Automator.Backend/BackendServer.cs:1196)
- impact (called by): [[function-cs-eb9f861d|Automator.Backend.BackendServer.OpenPanelAsync(bool)]] (src/Automator.Backend/BackendServer.cs:1420)

## Source anchors

- `src/Automator.Application/Launcher/AliasLaunchScheduler.cs#L51`
