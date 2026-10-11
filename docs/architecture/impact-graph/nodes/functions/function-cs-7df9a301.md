---
graph_id: "function-cs-7df9a301"
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

# Automator.Windows.FileApplicationLog.Write(ApplicationLogLevel,string,string,Exception?,IReadOnlyDictionary<string, object?>?)

public void Write(ApplicationLogLevel level, string eventName, string message, Exception exception, IReadOnlyDictionary properties)

## Connections

- impact (called by): [[function-3712-772ffd01|lambda@src/Automator.Backend/BackendServer.cs:77]] (src/Automator.Backend/BackendServer.cs:78)
- impact (called by): [[function-75700-56feccfc|lambda@src/Automator.Backend/BackendServer.cs:1392]] (src/Automator.Backend/BackendServer.cs:1403)
- impact (called by): [[function-81138-5b822b22|lambda@src/Automator.Backend/BackendServer.cs:1510]] (src/Automator.Backend/BackendServer.cs:1524)
- impact (called by): [[function-86276-05faa1cf|lambda@src/Automator.Backend/BackendServer.cs:1609]] (src/Automator.Backend/BackendServer.cs:1618)
- impact (called by): [[function-cs-263256f4|Automator.Backend.BackendServer.MonitorHostProcessAsync(Process)]] (src/Automator.Backend/BackendServer.cs:1710)
- impact (called by): [[function-cs-263256f4|Automator.Backend.BackendServer.MonitorHostProcessAsync(Process)]] (src/Automator.Backend/BackendServer.cs:1717)
- impact (called by): [[function-cs-2d3ba82d|Automator.Backend.BackendServer.AcquirePanelForeground()]] (src/Automator.Backend/BackendServer.cs:1100)
- impact (called by): [[function-cs-30f73aac|Automator.Backend.BackendServer.ImportSettingsAsync(string)]] (src/Automator.Backend/BackendServer.cs:1291)
- impact (called by): [[function-cs-3fb84b3a|Automator.Backend.BackendServer.RestorePriorForeground(ForegroundCheckParams)]] (src/Automator.Backend/BackendServer.cs:1134)
- impact (called by): [[function-cs-42dc29b3|Automator.Backend.BackendServer.PublishGlobalVariablesChangedSafelyAsync()]] (src/Automator.Backend/BackendServer.cs:549)
- impact (called by): [[function-cs-54e43092|Automator.Windows.FileApplicationLog.FileApplicationLog(string,string,string,string)]] (src/Automator.Windows/FileApplicationLog.cs:31)
- impact (called by): [[function-cs-57b6127e|Automator.Backend.BackendServer.ExportBackupAsync(string)]] (src/Automator.Backend/BackendServer.cs:1344)
- impact (called by): [[backend-handle-core|Automator.Backend.BackendServer.HandleLineCoreAsync(string)]] (src/Automator.Backend/BackendServer.cs:173)
- impact (called by): [[function-cs-6fc2cb71|Automator.Backend.BackendServer.ObserveServiceRequestAsync(long,Task)]] (src/Automator.Backend/BackendServer.cs:145)
- impact (called by): [[function-cs-73dd72bc|Automator.Backend.BackendServer.DisposeAsync()]] (src/Automator.Backend/BackendServer.cs:1759)
- impact (called by): [[function-cs-73dd72bc|Automator.Backend.BackendServer.DisposeAsync()]] (src/Automator.Backend/BackendServer.cs:1772)
- impact (called by): [[backend-run|Automator.Backend.BackendServer.RunAsync()]] (src/Automator.Backend/BackendServer.cs:102)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:952)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:971)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:983)
- impact (called by): [[function-cs-8ad53cb2|Automator.Backend.BackendServer.InitializeAsync(InitializeParams)]] (src/Automator.Backend/BackendServer.cs:998)
- impact (called by): [[function-cs-a1f7cecb|Automator.Backend.BackendServer.RecordRunActivitySafelyAsync(AutomationRunActivityEntry)]] (src/Automator.Backend/BackendServer.cs:525)
- impact (called by): [[function-cs-d3c24835|Automator.Backend.BackendServer.ObserveScheduledActivationAsync(Task)]] (src/Automator.Backend/BackendServer.cs:1476)
- impact (called by): [[function-cs-d4ecb388|Automator.Backend.BackendServer.ObserveActivationAsync(Task,string)]] (src/Automator.Backend/BackendServer.cs:1565)
- impact (called by): [[function-cs-eb9f861d|Automator.Backend.BackendServer.OpenPanelAsync(bool)]] (src/Automator.Backend/BackendServer.cs:1431)
- impact (called by): [[function-cs-f2b7250b|Automator.Backend.BackendServer.PublishActivityChangedSafelyAsync()]] (src/Automator.Backend/BackendServer.cs:539)

## Source anchors

- `src/Automator.Windows/FileApplicationLog.cs#L37`
