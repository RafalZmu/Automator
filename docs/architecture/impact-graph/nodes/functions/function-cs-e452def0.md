---
graph_id: "function-cs-e452def0"
graph_layer: "function"
graph_kind: "constructor"
tags:
  - "impact-graph"
  - "impact-graph/function"
  - "impact-graph/kind/constructor"
  - "impact-graph/area/backend-context"
  - "impact-graph/language/csharp"
generated_by: "scripts/impact-graph.mjs"
---

# Automator.Core.Launcher.AppBinding.AppBinding(string,string,string,string,string)

public AppBinding(string id, string name, string targetPath, string alias, string arguments)

## Connections

- impact (called by): [[function-66565-a3fa439d|lambda@src/Automator.Backend/BackendServer.cs:1219]] (src/Automator.Backend/BackendServer.cs:1219)
- impact (called by): [[function-cs-28cfba6a|Automator.Windows.WindowsAppCatalog.AddEntry(IDictionary<string, AppBinding>,string,string,string)]] (src/Automator.Windows/WindowsAppCatalog.cs:126)
- impact (called by): [[function-cs-2b186808|Program.AliasesResolve()]] (tests/Automator.Core.Specs/Program.cs:62)
- impact (called by): [[function-cs-2b186808|Program.AliasesResolve()]] (tests/Automator.Core.Specs/Program.cs:63)
- impact (called by): [[function-cs-2b186808|Program.AliasesResolve()]] (tests/Automator.Core.Specs/Program.cs:71)
- impact (called by): [[function-cs-2d1aa4b1|Automator.Services.AppCatalogService.CaptureUnderCursor()]] (src/Automator.App/Services/AppCatalogService.cs:53)
- impact (called by): [[function-cs-2f802681|Program.ConfigurationRoundTrips()]] (tests/Automator.Core.Specs/Program.cs:98)
- impact (called by): [[function-cs-31abfc19|Automator.Windows.WindowsAppCatalog.FromCustomPath(string)]] (src/Automator.Windows/WindowsAppCatalog.cs:72)
- impact (called by): [[function-cs-4143c88b|Program.InvalidBindingsRejected()]] (tests/Automator.Core.Specs/Program.cs:142)
- impact (called by): [[function-cs-4143c88b|Program.InvalidBindingsRejected()]] (tests/Automator.Core.Specs/Program.cs:143)
- impact (called by): [[function-cs-4143c88b|Program.InvalidBindingsRejected()]] (tests/Automator.Core.Specs/Program.cs:150)
- impact (called by): [[function-cs-41d1ba5a|Automator.Windows.WindowsAppCatalog.AddEntryStatic(IDictionary<string, AppBinding>,string,string,string)]] (src/Automator.Windows/WindowsAppCatalog.cs:150)
- impact (called by): [[function-cs-476a7400|Automator.Windows.WindowsAppCatalog.CaptureUnderCursor()]] (src/Automator.Windows/WindowsAppCatalog.cs:52)
- impact (called by): [[function-cs-69689ea0|Automator.Services.AppCatalogService.FromCustomPath(string)]] (src/Automator.App/Services/AppCatalogService.cs:70)
- impact (called by): [[function-cs-9da412e9|Automator.Services.AppCatalogService.AddEntry(IDictionary<string, AppBinding>,string,string,string)]] (src/Automator.App/Services/AppCatalogService.cs:153)
- impact (called by): [[function-cs-e1dceae3|Automator.Backend.BackendServer.RelinkAsync(RelinkParams)]] (src/Automator.Backend/BackendServer.cs:1357)

## Source anchors

- `src/Automator.Core/Launcher/LauncherDomain.cs#L13`
