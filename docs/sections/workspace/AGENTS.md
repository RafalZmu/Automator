# Workspace window contributor guide

Read the root AGENTS.md first. Workspace is the larger editing/review window over the same modules, not a second backend or a new tab.

- Keep the launcher and Workspace as separate window surfaces while sharing validated module services through the preload/RPC bridge.
- Workspace tab selection is local to that window; do not accidentally mutate compact launcher selection unless an explicit navigation action requires it.
- Keep each active numbered tab paired with its purpose icon in both windows, keep slot 9's Snippets clipboard icon and slot 10's Quick notes notebook icon, and preserve the existing accessible tab name (`<title>, tab <key>` on row one, `<title>, tab <key> of row <n>` on later rows). Icons follow the tab (its slot), not its position.
- Tabs are shown in rows of nine selected with keys 1–9; Up/Down switch the focused row. The compact launcher's row lives in the backend (`LauncherSession`, `launcher/shiftTabRow`) because its global keys need it; Workspace keeps its own row in the renderer. The tab order (`LauncherSettings.TabOrder`, `launcher/setTabOrder`) is shared by both windows. Slot is a tab's stable identity; keep module state, settings and authorization keyed by slot/ID, never by position. The launcher (slot 1) stays pinned at position 1.
- Keep window sizing, placement, activation, and lifecycle in electron/main.ts. Renderer events must not make the window follow the pointer or drift while controls receive focus.
- Avoid duplicating module business logic in Workspace. Reuse the same tab view registry and service interfaces; only change layout or per-surface affordances.
- Run or add Electron integration coverage for Workspace open/hide, tab isolation, focus, and activity navigation when changing host or window behavior.

Code: electron/main.ts, ui/App.tsx, ui/viewRegistry.tsx, and ui/automationServices.ts.

File Explorer launch requests are subscribed in the shared App shell before renderer readiness and retained until Script Runner mounts. The host delivers them to the compact launcher; keep this explicit navigation separate from Workspace's local tab selection. Module forms and execution stay in Script Runner.
