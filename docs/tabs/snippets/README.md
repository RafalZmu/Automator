# Snippets — slot 9

Snippets saves pieces of text you paste often, such as a tax number or a command like `/plan`. Each snippet has a name, the text to copy, and an optional shortcut of 1 to 32 letters or digits. Shortcuts are unique ignoring letter case; a snippet without one can only be copied with its button.

There are three ways to copy a snippet to the clipboard:

- Type its shortcut in the launch panel's Quick Actions search (the home surface shown when the launcher opens). A unique match runs automatically once typing settles, or press Enter. The text is copied and the launcher closes straight away, without switching to the Snippets tab, so you can paste into the app you came from. Matching is by name and shortcut like other actions, so a shortcut that is the start of another (for example `plan` and `plan2`) lists both until you choose one. Only snippets that have a shortcut appear here.
- Press the **Copy** button beside it. The button shows a check mark for a moment and the tab reports `Copied <name> to the clipboard.`
- Type its shortcut in this tab's action search and press Enter. The search lists the matching snippets as `shortcut · name`; Enter copies a unique match, and a shortcut that is the start of another one (for example `plan` and `plan2`) lists both so you can choose with the arrow keys.

The text is copied exactly as written, including line breaks and tabs. Windows stores line breaks on the clipboard as CRLF, so pasted text breaks lines the way Windows apps expect. A snippet is copied as it appears on screen, even before it has been saved; the **Save** button persists the list. Add snippets with **Add snippet**, reorder them with the arrows, and remove them with the bin button.

Snippets are stored as module preferences in plain text in `settings.json`, together with the other tab preferences. They are not encrypted, so keep passwords, API keys, and other secrets out of them. The launch panel and the tab search match only a snippet's name and shortcut, and notices never repeat its text. To let the launch panel copy without opening the tab, the host publishes each snippet that has a shortcut in the Snippets module state, with its text split into pieces of at most 2000 characters because state values are limited to 2048 characters. That state stays inside Automator and is not shown by agent access (MCP).

Limits: 100 snippets, 4000 characters of text and 128 characters of name each, and 64 KiB for the whole list. Text may contain line breaks and tabs but no other control characters.

## Code map

- `ui/modules/SnippetsView.tsx` and `SnippetsView.css` render the editor, the Copy buttons, and the tab commands that copy through the renderer's `navigator.clipboard`.
- `ui/contracts/snippets.ts` validates and reads the saved settings (`{ "snippets": [{ id, name, alias, value }] }`) and rebuilds the copyable snippets from the module state (`readSnippetShortcuts`).
- `ui/App.tsx` lists those snippets in the launch panel, copies one with the renderer clipboard, and closes the launcher without selecting a tab; `src/Automator.Backend/BackendServer.cs` merges the published entries into the Snippets module state.
- `src/Automator.Application/Automation/SnippetsModule.cs` registers the slot, defines the empty default settings, and declares no actions or capabilities, and builds the published shortcut entries (`CreateShortcutStateValues`). Settings are written and read through the shared module-settings route.
- `src/Automator.Application/Launcher/LauncherTabRegistry.cs` registers the module in slot 9; `ui/viewRegistry.tsx`, `ui/viewKinds.ts`, and `contracts/bundled-tab-view-kinds.json` declare the `snippets` view kind; `ui/App.tsx` supplies the tab icon.
- Tests: `tests/snippets-contract.test.mjs`, the Application specification listed in [TESTS.md](TESTS.md), and `tests/electron/tab-happy-path-visibility.test.cjs`.

## Test case steps

See [TESTS.md](TESTS.md) for the settings contract, module registration, and Electron save-and-copy coverage.
