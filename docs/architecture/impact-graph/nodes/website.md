---
graph_id: "website"
graph_layer: "module"
graph_kind: "feature"
tags:
  - "impact-graph"
  - "impact-graph/module"
  - "impact-graph/kind/feature"
  - "impact-graph/area/website"
generated_by: "scripts/impact-graph.mjs"
---

# Website Launcher

Manages website aliases and launches their configured destinations.

## Connections

- impact (the renderer consumes website alias actions and launch results): [[website-view|WebsiteLauncherView]] (src/Automator.Application/Automation/WebsiteLauncherModule.cs; ui/modules/WebsiteLauncherView.tsx)
- contains (owns source file): [[file-website-launcher-module-395a7694|WebsiteLauncherModule.cs]]
- contains (owns source file): [[file-website-launcher-view-4aee31ea|WebsiteLauncherView.tsx]]

## Source anchors

- `src/Automator.Application/Automation/WebsiteLauncherModule.cs`
- `src/Automator.Windows/WindowsAutomationWebsiteLauncher.cs`
- `ui/modules/WebsiteLauncherView.tsx`
