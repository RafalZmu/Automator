# Quick notes — slot 10

Quick notes keeps short notes written in markdown. Press **0** (or click tab 10) to open it. The tab lists one row per note: the title on the left, then a status dot and an expand arrow on the right. Click a row to expand it and see the rendered markdown; click it again to collapse it.

The dot reflects the note's markdown checkboxes (`- [ ] task`, `- [x] done`): red means at least one checkbox is unticked, green means every checkbox is ticked, and no dot means the note has no checkboxes. Checkboxes in an expanded note can be ticked and unticked directly; the change is saved at once.

- **Add note** opens an editor for a title and the markdown text. **Save note** stores it and expands it.
- **Edit** on an expanded note reopens the editor; **Delete** removes the note.

Supported markdown: headings, paragraphs, block quotes, horizontal rules, fenced code, bullet, numbered and task lists, `**bold**`, `*italic*`, `~~strikethrough~~`, `` `code` `` and `[text](https://…)` links. Other markup is shown as plain text.

Notes are stored as module preferences in plain text in `settings.json`, with the other tab preferences. They are not encrypted, so keep passwords and other secrets out of them. Limits: 100 notes, 128 characters of title and 8000 characters of text each, and 64 KiB for all notes. Text may contain line breaks and tabs but no other control characters.

## Code map

- `ui/modules/NotesView.tsx` and `NotesView.css` render the list, editor, dots, and checkboxes, and save every change through the module settings bridge.
- `ui/modules/notesMarkdown.ts` parses markdown into a tree, counts and toggles checkboxes.
- `ui/contracts/notes.ts` validates and reads the saved settings.
- `src/Automator.Application/Automation/NotesModule.cs` registers slot 10, defines the empty default settings, and declares no actions or capabilities. Settings are written and read through the shared module-settings route.
- `src/Automator.Application/Launcher/LauncherTabRegistry.cs` registers the module; `ui/viewRegistry.tsx`, `ui/viewKinds.ts`, and `contracts/bundled-tab-view-kinds.json` declare the `notes` view kind; `ui/App.tsx` supplies the tab icon and the **0** key.
- Tests: `tests/notes-contract.test.mjs`, the Application specification listed in [TESTS.md](TESTS.md), and `tests/electron/tab-happy-path-visibility.test.cjs`.

## Test case steps

See [TESTS.md](TESTS.md) for the settings and markdown contract, module registration, and Electron coverage.
