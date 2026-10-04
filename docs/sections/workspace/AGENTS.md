# Workspace window contributor guide

Read the root AGENTS.md first. Workspace is the larger editing/review window over the same modules, not a second backend or a new tab.

- Keep the launcher and Workspace as separate window surfaces while sharing validated module services through the preload/RPC bridge.
- Workspace tab selection is local to that window; do not accidentally mutate compact launcher selection unless an explicit navigation action requires it.
- Keep window sizing, placement, activation, and lifecycle in electron/main.ts. Renderer events must not make the window follow the pointer or drift while controls receive focus.
- Avoid duplicating module business logic in Workspace. Reuse the same tab view registry and service interfaces; only change layout or per-surface affordances.
- Run or add Electron integration coverage for Workspace open/hide, tab isolation, focus, and activity navigation when changing host or window behavior.

Code: electron/main.ts, ui/App.tsx, ui/viewRegistry.tsx, and ui/automationServices.ts.
