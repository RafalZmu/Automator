# Keyboard shortcuts

Automator works from the keyboard as well as the mouse. This section is the shared rulebook: the global keys, how Escape backs out, which chords tab commands declare, and where the interface shows them. Each tab documents its own chords in its README.

## Global keys

| Keys | Action | Where |
| --- | --- | --- |
| Ctrl+K | Find an app, tab, or action. The compact launcher opens Quick Actions; the workspace focuses the active tab's action search. | Launcher and workspace |
| Ctrl+/ or F1 | Show or hide the keyboard reference panel. | Launcher and workspace |
| Ctrl+, | Open Settings. | Launcher |
| Ctrl+Tab, Ctrl+Shift+Tab | Next or previous tab. The workspace skips the Launcher tab. | Launcher and workspace |
| 1–9 | Select a tab. Digits type normally inside text fields. | Launcher and workspace |
| / | Add an application from the Launcher tab. | Launcher |
| ↑ and ↓ | Move between the app search and the application list. | Launcher |
| Enter | Open the first app match when the search has text and no exact alias. In catalog search it starts alias editing for the first match. | Launcher |
| Esc | Compact launcher: closes the panel; the native key layer owns Escape there. Settings: hides the launcher, as before. Alias editing: cancels it. Workspace: closes the reference panel, the notification or activity view, then leaves a text field, then hides the window. | Both |

Inside Settings, Ctrl+Enter saves the changes. Inside the Quick Actions list, Enter runs the highlighted action.

## Tab command shortcuts

A tab command declares its chord in `shortcut` (for example `Ctrl+N`, `Ctrl+S`, `Ctrl+Enter`, or `S`). The window-level handler in `ui/App.tsx` runs it only while its tab is active, the command is enabled, and it needs no confirmation. Commands that ask for confirmation, such as deleting or running a saved profile, are never started by a shortcut; they stay in the tab action search where the confirmation is shown.

Current chords: Ctrl+N creates the tab's item (script profile, API profile, schedule, workflow, website shortcut, Playwright test section); Ctrl+S saves the open editor; Ctrl+Enter generates a Codex task; S starts or pauses the Work Time timer.

## Typing safety

- Printable keys without Ctrl or Alt (letters, digits, `/`) never fire inside text fields, selects, or content-editable regions, so typing is never intercepted.
- Ctrl and Alt chords and function keys work everywhere, because they do not type characters. Editing shortcuts such as Ctrl+C, Ctrl+V, and Ctrl+Z are never bound.
- Shortcuts are window-local. Automator registers no system-wide hotkey for them; the opener key is still the only global key, and it belongs to the native host.
- Tab command shortcuts pause while the reference panel or Quick Actions is showing, and while Settings is open.

## Where the interface shows shortcuts

- Command rows in the tab action search and in Quick Actions show their chord.
- Buttons with a shortcut show it in their tooltip (for example Settings, New profile, Save profile).
- The footer shows Ctrl+/ for the reference panel.
- The reference panel lists the global keys, the launcher keys, and the active tab's shortcuts.

## Code map

- `ui/commands/keyboardShortcuts.ts` parses chords, matches events, applies the unmodified-key guard, and defines the global key table and reference rows.
- `ui/commands/KeyboardShortcutsPanel.tsx` renders the reference panel in place of the active content.
- `ui/commands/TabCommandRegistry.tsx` carries `shortcut` through registration and provides `useTabShortcutResolver`.
- `ui/App.tsx` owns the window-level handler (`handleShortcutKeyDown`), the layered Escape (`escapeOneLayer`), tab cycling, and the launcher app-list keys.
- `ui/commands/TabCommandBar.tsx` and `ui/commands/QuickActionsHome.tsx` display shortcuts and handle their own arrow, Enter, and Escape keys.

## Tests

- `tests/command-matching.test.mjs` covers chord parsing, exact modifiers, the text-field guard, surface routing, and the confirmation rule.
- `tests/electron/keyboard-shortcuts.test.cjs` covers the launcher key layer, Script Runner Ctrl+N and Ctrl+S, workspace Ctrl+K with layered Escape, and catalog arrow navigation.
