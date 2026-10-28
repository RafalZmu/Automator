# Quick notes — slot 10

Quick notes keeps short notes written in markdown. Press **0** (or click tab 10) to open it. The tab lists one row per note: the title on the left, then a status dot and an expand arrow on the right. Click a row to expand it and see the rendered markdown; click it again to collapse it.

The dot reflects the note's markdown checkboxes (`- [ ] task`, `- [x] done`): red means at least one checkbox is unticked, green means every checkbox is ticked, and no dot means the note has no checkboxes. Checkboxes in an expanded note can be ticked and unticked directly; the change is saved at once.

- **Add note** opens an editor for a title and the markdown text. **Save note** stores it and expands it.
- **Edit** on an expanded note reopens the editor; **Delete** removes the note.

Supported markdown: headings, paragraphs, block quotes, horizontal rules, fenced code, bullet, numbered and task lists, `**bold**`, `*italic*`, `~~strikethrough~~`, `` `code` `` and `[text](https://…)` links. Other markup is shown as plain text.

Notes are stored in plain text in Automator's SQLite library (`library.db`), one record per note, so they survive restarts and are saved the moment you change them. They are not encrypted, so keep passwords and other secrets out of them. They are not yet included in the library export. Limits: 50 notes, 128 characters of title and 3000 characters of text each. Text may contain line breaks and tabs but no other control characters.

## Code map

- `ui/modules/NotesView.tsx` and `NotesView.css` render the list, editor, dots, and checkboxes, and save every change through the `saveNote` and `deleteNote` module actions.
- `ui/modules/notesMarkdown.ts` parses markdown into a tree, counts and toggles checkboxes.
- `ui/contracts/notes.ts` validates a note and reads the `listNotes` result.
- `src/Automator.Application/Automation/NotesModule.cs` registers slot 10, declares the `storage.library` capability, and implements the `listNotes`, `saveNote`, and `deleteNote` actions over the module's library partition.
- `src/Automator.Application/Launcher/LauncherTabRegistry.cs` registers the module; `ui/viewRegistry.tsx`, `ui/viewKinds.ts`, and `contracts/bundled-tab-view-kinds.json` declare the `notes` view kind; `ui/App.tsx` supplies the tab icon and the **0** key.
- Tests: `tests/notes-contract.test.mjs`, the Application specification listed in [TESTS.md](TESTS.md), and `tests/electron/tab-happy-path-visibility.test.cjs`.

## Test case steps

See [TESTS.md](TESTS.md) for the note and markdown contract, module registration, and Electron coverage.
