# Global Action Search

Global Action Search appears as the default home surface when the compact launcher opens. It searches actions registered by tabs; it is not an additional tab and does not replace the Tab 1 app-alias search. A single unambiguous match runs automatically after the search settles. Multiple matches remain visible for selection; a tab-specific action switches to its tab before running.

Website Launcher aliases are also available here with a trailing `w` (for example, `docsw`). The suffix keeps website shortcuts distinguishable from app aliases with the same letters. Saved website shortcuts are loaded from their module settings at startup and refreshed after edits.

## Code map

- ui/commands/TabCommandRegistry.tsx gathers commands registered by visible tab modules and resolves a live command.
- ui/commands/QuickActionsHome.tsx renders the cross-tab action list and search input.
- ui/commands/commandMatching.ts handles matching and unique-result behavior.
- ui/commands/TabCommandBar.tsx provides the corresponding per-tab command search.
- ui/App.tsx routes selected actions and preserves the app alias path.
- Tests: tests/command-matching.test.mjs and Electron renderer/UI-flow tests.

Add commands next to the feature that owns them so labels, availability, confirmations, and execution stay in sync.
