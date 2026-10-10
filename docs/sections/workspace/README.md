# Workspace window

Workspace is the larger window for editing complex profiles and reviewing module data. It hosts the same tabs and module services as the compact launcher, but keeps its own selected tab. It also provides Run Activity and notification entry points.

Each numbered tab displays a small icon beside its number to help identify the tab. The icon is decorative; the tab retains its accessible name.

The compact panel remains optimized for quick access. Workspace changes should reuse module views and host capabilities rather than fork feature behavior.

## Code map

- electron/main.ts creates and manages the launcher and Workspace windows.
- ui/App.tsx selects the active surface and composes the shared tab views.
- ui/viewRegistry.tsx maps module IDs to views.
- ui/automationServices.ts gives renderer modules the validated service boundary.
- tests/electron/ui-flow.test.cjs and other Electron integration specs cover window behavior.

Window visibility, focus, and per-window tab selection are host/UI shell concerns; module profile semantics remain owned by each tab.
