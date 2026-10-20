# Snippets contributor guide

Read the root `AGENTS.md` first. Snippets keeps named text in module preferences and copies it to the clipboard from the renderer. It has no backend actions and no host capability.

## Preserve the copy contract

- Keep the snippet list in module settings (`{ "snippets": [...] }`, settings version 1) and read and write it only through the module settings bridge. The module declares no actions or capabilities; do not add a clipboard capability or a backend action unless the renderer clipboard stops being sufficient.
- Copy with the renderer's `navigator.clipboard.writeText` in direct response to a click or Enter in the focused window, and surface a failure to the user instead of hiding it. Copy the text as it is on screen, saved or not.
- Never put a snippet's text in anything other than the clipboard and the module settings file. Tab commands, the command catalog that Global Action Search snapshots, notices, run activity, and logs must carry only the name and shortcut. Do not add the text to keywords or labels.
- The settings are plain JSON in `settings.json` and are not encrypted. Do not present Snippets as a place for secrets or add features that imply otherwise, and keep the documentation warning in place.
- Keep the shortcut optional, 1 to 32 letters or digits, unique ignoring case. Keep names required, text non-blank and at most 4000 characters with only line breaks and tabs as control characters, at most 100 snippets, and 64 KiB in total. The renderer contract in `ui/contracts/snippets.ts` is the validation; the host only stores opaque JSON, so keep that contract strict and keep it unit-tested.
- The tab uses the shared tab action search. A typed shortcut resolves like every other tab alias: only a single match runs, so shortcuts that start another shortcut list both. Do not change the shared matcher for Snippets alone.
- Keep the module in slot 9 with the stable `snippets` ID, view kind, and settings version. Changing the settings shape requires a migration in `SnippetsModule.MigrateSettings`.

## Code and checks

UI: `ui/modules/SnippetsView.tsx` and `SnippetsView.css`; contract: `ui/contracts/snippets.ts`.
Module and registry: `src/Automator.Application/Automation/SnippetsModule.cs` and `src/Automator.Application/Launcher/LauncherTabRegistry.cs`.
Tests: `tests/snippets-contract.test.mjs`, the Application specifications, and `tests/electron/tab-happy-path-visibility.test.cjs`.

Update this guide, the sibling README and TESTS, and the root tab index when copy behavior, validation limits, storage, or the slot change.
