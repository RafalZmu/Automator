---
graph_id: "function-cs-179e2b87"
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

# Automator.Application.Launcher.LauncherSession.EnterMode(LauncherMode)

public void EnterMode(LauncherMode mode)

## Connections

- impact (called by): [[function-cs-00fc632e|Automator.Application.Launcher.LauncherModeCoordinator.SetMode(LauncherMode)]] (src/Automator.Application/Launcher/LauncherModeCoordinator.cs:13)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:163)
- impact (called by): [[function-cs-481685e2|Program.GlobalTabKeysRequireLauncherContext()]] (tests/Automator.Application.Specs/Program.cs:110)
- impact (called by): [[function-cs-481685e2|Program.GlobalTabKeysRequireLauncherContext()]] (tests/Automator.Application.Specs/Program.cs:113)
- impact (called by): [[function-cs-71105874|Program.AliasMatchingIsContextual()]] (tests/Automator.Application.Specs/Program.cs:249)
- impact (called by): [[function-cs-f79b54b9|Program.NavigationKeyPolicyRequiresFocusedUnmodifiedLauncher()]] (tests/Automator.Application.Specs/Program.cs:154)

## Source anchors

- `src/Automator.Application/Launcher/LauncherSession.cs#L215`
