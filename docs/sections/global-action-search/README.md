# Global Action Search

Global Action Search appears as the default home surface when the compact launcher opens. It searches actions registered by tabs; it is not an additional tab and does not replace the Tab 1 app-alias search. A single unambiguous match runs automatically after the search settles. Multiple matches remain visible for selection; a tab-specific action switches to its tab before running.

Website Launcher aliases are also available here with a trailing `w` (for example, `docsw`). The suffix keeps website shortcuts distinguishable from app aliases with the same letters. Saved website shortcuts are loaded from their module settings at startup and refreshed after edits.

Ctrl+K opens Quick Actions from the compact launcher and focuses the active tab's action search in the workspace. Results show the chord of any command that declares one; a shortcut never starts a command that needs confirmation, so those results stay in the list for an explicit choice. See [Keyboard shortcuts](../keyboard-shortcuts/README.md).

Saved Script Runner profiles, API profiles and Workflows are searchable as "Run <name>" before their tab has been opened. The backend lists their names from the library at startup, after every save/delete and after a backup import, and publishes them in each module's `savedItems` state value. Choosing one opens the tab and runs the tab's own live command after the same confirmation the tab would ask for, so these entries never run automatically on a unique match.

## Code map

- ui/commands/TabCommandRegistry.tsx gathers commands registered by visible tab modules and resolves a live command.
- ui/commands/QuickActionsHome.tsx renders the cross-tab action list and search input.
- ui/commands/commandMatching.ts handles matching and unique-result behavior.
- ui/commands/TabCommandBar.tsx provides the corresponding per-tab command search.
- ui/App.tsx routes selected actions, turns published `savedItems` into search entries (`readSavedItemActions`), and preserves the app alias path.
- src/Automator.Backend/BackendServer.cs (`RefreshSavedItemActionsAsync`) publishes the saved item names.
- Tests: tests/command-matching.test.mjs and Electron renderer/UI-flow tests.

Add commands next to the feature that owns them so labels, availability, confirmations, and execution stay in sync.
