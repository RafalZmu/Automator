---
graph_id: "function-cs-07c9613e"
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

# Automator.Application.Launcher.HotkeyRecordingPolicy.ShouldCapture(LauncherNavigationState,bool,bool,bool,bool,bool)

public static bool ShouldCapture(LauncherNavigationState state, bool windowContextVisible, bool rendererFocused, bool nativeDialogActive, bool nativeForeground, bool contextModeMatches)

## Connections

- impact (called by): [[function-86276-05faa1cf|lambda@src/Automator.Backend/BackendServer.cs:1609]] (src/Automator.Backend/BackendServer.cs:1615)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:165)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:166)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:167)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:168)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:169)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:170)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:171)
- impact (called by): [[function-cs-1e9c4477|Program.RecordingRequiresVisibleFocusedContext()]] (tests/Automator.Application.Specs/Program.cs:172)
- impact (called by): [[function-cs-3b137fa2|Automator.Windows.KeyboardHookService.ProcessKey(int,KeyboardHookData)]] (src/Automator.Windows/KeyboardHookService.cs:216)

## Source anchors

- `src/Automator.Application/Launcher/HotkeyRecordingPolicy.cs#L8`
