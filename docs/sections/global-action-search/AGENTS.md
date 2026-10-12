# Global Action Search contributor guide

Read the root AGENTS.md first. Global Action Search is the compact launcher's home surface, not a numbered tab or a separate module.

- Register tab-owned actions using useRegisterTabCommands from ui/commands/TabCommandRegistry.tsx; use stable IDs, concise labels, and useful search keywords.
- Keep each action's execution inside its owning tab/module service. The global surface may resolve and route commands, but must not call host APIs around the validated bridge.
- Preserve unique-match behavior and present choices for ambiguous matches. Disabled actions must stay non-runnable.
- Add confirmation prompts for actions with destructive or externally visible effects; do not auto-run a command merely because its name is similar.
- Preserve app aliases as a separate Tab 1 flow. Do not repurpose digit keys or add a new numbered slot for search.
- Website Launcher aliases are exposed as `<alias>w`; keep the suffix and route the resulting command through the Website Launcher tab's live module command.
- Add command matching and renderer integration tests when changing ranking, routing, focus, or execution timing.

Code: ui/commands/QuickActionsHome.tsx, TabCommandBar.tsx, TabCommandRegistry.tsx, and commandMatching.ts; composition and routing live in ui/App.tsx.

- Saved-item search entries use the exact command ids the tab views register (`run-script:<id>`, `api:run:<id>`, `workflow:run:<id>`) and always carry a confirmation prompt. When adding another saved-item kind, add its source to `SavedItemSources` in BackendServer.cs and its id/prompt to `savedItemRunActions` in ui/App.tsx; keep ids identical to the view's command so the live command replaces the entry.
- Show a command's chord in its result row by setting `shortcut` on the registered command. The chord must match the tab's README and the shortcut table in `docs/sections/keyboard-shortcuts/README.md`.
