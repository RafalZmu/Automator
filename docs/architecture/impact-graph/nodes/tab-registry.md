---
graph_id: "tab-registry"
graph_layer: "module"
graph_kind: "registry"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/registry"
  - "impact-graph/area/runtime"
generated_by: "scripts/impact-graph.mjs"
---

# Tab and view registries

Maps registered tab metadata and view kinds to the renderer's compiled views.

## Connections

- impact (the app composes registered view kinds): [[renderer|React renderer]] (ui/App.tsx; ui/viewRegistry.tsx)
- impact (registered tab/view mapping includes launcher surfaces): [[launcher|Launcher]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs; ui/viewRegistry.tsx)
- impact (registered tab/view mapping includes Script Runner): [[script|Script Runner]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs; ui/viewRegistry.tsx)
- impact (registered tab/view mapping includes API): [[api|API]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs; ui/viewRegistry.tsx)
- impact (registered tab/view mapping includes Browser Automation): [[browser|Browser Automation]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs; ui/viewRegistry.tsx)
- impact (registered tab/view mapping includes Workflows): [[workflows|Workflows]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs; ui/viewRegistry.tsx)
- impact (registered tab/view mapping includes Scheduler): [[scheduler|Scheduler]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs; ui/viewRegistry.tsx)
- impact (registered tab/view mapping includes Work Time): [[work-time|Work Time]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs; ui/viewRegistry.tsx)
- impact (registered tab/view mapping includes Website Launcher): [[website|Website Launcher]] (src/Automator.Application/Launcher/LauncherTabRegistry.cs; ui/viewRegistry.tsx)
- contains (owns source file): [[file-launcher-tab-registry-41824cf8|LauncherTabRegistry.cs]]
- contains (owns source file): [[file-view-kinds-97702f6b|viewKinds.ts]]
- contains (owns source file): [[file-view-registry-2cb1168b|viewRegistry.tsx]]

## Source anchors

- `src/Automator.Application/Launcher/LauncherTabRegistry.cs`
- `ui/viewRegistry.tsx`
- `ui/viewKinds.ts`
