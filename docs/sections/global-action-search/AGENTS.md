# Global Action Search contributor guide

Read the root AGENTS.md first. Global Action Search is the compact launcher's home surface, not a numbered tab or a separate module.

- Register tab-owned actions using useRegisterTabCommands from ui/commands/TabCommandRegistry.tsx; use stable IDs, concise labels, and useful search keywords.
- Keep each action's execution inside its owning tab/module service. The global surface may resolve and route commands, but must not call host APIs around the validated bridge.
- Preserve unique-match behavior and present choices for ambiguous matches. Disabled actions must stay non-runnable.
- Add confirmation prompts for actions with destructive or externally visible effects; do not auto-run a command merely because its name is similar.
- Preserve app aliases as a separate Tab 1 flow. Do not repurpose digit keys or add a new numbered slot for search.
- Add command matching and renderer integration tests when changing ranking, routing, focus, or execution timing.

Code: ui/commands/QuickActionsHome.tsx, TabCommandBar.tsx, TabCommandRegistry.tsx, and commandMatching.ts; composition and routing live in ui/App.tsx.
