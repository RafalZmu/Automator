# Keyboard shortcuts contributor guide

Read the root AGENTS.md first. Keyboard shortcuts are a shared surface: they reach every tab, the compact launcher, and the workspace.

- Declare a tab shortcut with `shortcut` on the tab command. Do not add a view-local window listener: the registry, the tab action search, Quick Actions, and the reference panel all read that one field.
- Use Ctrl/Alt chords or function keys. A printable key without a modifier is allowed only when the guard in `ui/commands/keyboardShortcuts.ts` keeps it out of text controls.
- Never bind system-wide hotkeys for these actions. The opener key is the only global key, and it belongs to the native host.
- Shortcuts never start a command that needs confirmation. Keep destructive or externally visible actions in the tab action search or behind their inline confirmation.
- Escape backs out one layer at a time in the workspace. In the compact launcher the native key layer owns Escape and digits in launcher and catalog modes, so do not make renderer behavior depend on those keys reaching the renderer there. Changing Escape semantics means changing `escapeOneLayer` in `ui/App.tsx` and, for launcher modes, `NavigationKeyPolicy` in the same change.
- Every new shortcut must appear in its tab README and be visible in the GUI (the command row, a button tooltip, or the reference panel).
- Keep `ui/commands/keyboardShortcuts.ts` free of React and DOM globals so `tests/command-matching.test.mjs` can import it under Node.
