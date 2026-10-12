# Launcher contributor guide

Read the root AGENTS.md first. This guide covers the slot 1 app launcher and its boundary with global action search.

## Preserve the launcher contract

- Keep app aliases separate from tab commands. App aliases remain letters-only and are resolved by the launcher; global commands are registered through the tab command registry.
- Keep the numbered slots 1–9 available for tab selection. Global Action Search is a home surface, not another tab.
- The panel opener, native keyboard hook, activation, and foreground restoration belong to the desktop host/backend. Do not replace them with renderer-only keyboard listeners.
- Preserve Tab 1 catalog behavior, custom EXE/shortcut binding, app-row shortcut removal, and launch/restore/minimize semantics.
- Keep launcher and Workspace window state separate where the host contract says it is per-window.
- Keep the compact launcher fixed at its 760×800 preferred size; preserve work-area clamping for smaller displays.
- Keep the topbar, tab selector, command bar, and footer outside the active tab view's vertical scrolling area. Active view roots must remain vertically scrollable.
- Add regression coverage for alias matching, app activation, and command routing when changing these flows.
- Add regression coverage for preferred panel bounds, fixed-size behavior, and active-tab scrolling when changing launcher geometry.

## Code and checks

Main UI: `ui/App.tsx`, `ui/viewRegistry.tsx`, and `ui/commands/QuickActionsHome.tsx`.
Desktop lifecycle and native focus: `electron/main.ts`; backend launcher behavior: `src/Automator.Application/Launcher` and `src/Automator.Windows`.
Tests: `tests/electron/catalog-alias.test.cjs`, `tests/electron/native-hotkey.test.cjs`, and `tests/electron/ui-flow.test.cjs`.

Update this guide and the sibling README when a user-visible launcher rule or source boundary changes.
