---
graph_id: "function-cs-58621022"
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

# Automator.Core.Launcher.AliasValidator.Validate(string?,IEnumerable<string>)

public static AliasValidationError? Validate(string alias, IEnumerable existingAliases)

## Connections

- impact (called by): [[function-cs-0fedf070|Automator.Backend.BackendServer.SaveAliasAsync(SaveAliasParams)]] (src/Automator.Backend/BackendServer.cs:1171)
- impact (called by): [[function-cs-6f3ab710|Automator.Protocol.RpcParameterValidator.Validate(RpcRequest)]] (src/Automator.Protocol/RpcProtocol.cs:242)
- impact (called by): [[function-cs-82cd01e5|Automator.Application.Launcher.LauncherSession.SaveAlias(string)]] (src/Automator.Application/Launcher/LauncherSession.cs:253)
- impact (called by): [[function-cs-94d4a3b9|Program.AliasesValidate()]] (tests/Automator.Core.Specs/Program.cs:77)
- impact (called by): [[function-cs-94d4a3b9|Program.AliasesValidate()]] (tests/Automator.Core.Specs/Program.cs:78)
- impact (called by): [[function-cs-94d4a3b9|Program.AliasesValidate()]] (tests/Automator.Core.Specs/Program.cs:79)
- impact (called by): [[function-cs-94d4a3b9|Program.AliasesValidate()]] (tests/Automator.Core.Specs/Program.cs:80)
- impact (called by): [[function-cs-fdc443c3|Automator.Core.Configuration.SettingsSerializer.Validate(LauncherSettings)]] (src/Automator.Core/Configuration/LauncherSettings.cs:146)

## Source anchors

- `src/Automator.Core/Launcher/LauncherDomain.cs#L75`
