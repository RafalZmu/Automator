# Quick notes contributor guide

Read the root `AGENTS.md` first. Quick notes keeps short markdown notes in module preferences and renders them in the renderer. It has no backend actions and no host capability.

## Preserve the notes contract

- Keep each note as one record in the module's partition of the SQLite library (`notes` collection, record `{ id, title, body, created }`, schema version 1), reached only through the `listNotes`, `saveNote`, and `deleteNote` actions with the `storage.library` capability. The renderer never writes the library directly, and the list is ordered by creation time so editing a note keeps its place.
- Every add, edit, delete, and checkbox tick is saved at once through an action. The list on screen changes only after the action succeeded, so a failed save never shows data that was not stored.
- Render markdown to React elements from the tree in `ui/modules/notesMarkdown.ts`. Never use `dangerouslySetInnerHTML`, and keep links limited to `http` and `https` addresses.
- The status dot is derived from the body with `countCheckboxes`: red when any checkbox is unticked, green when every checkbox is ticked, no dot when the note has none. Checkbox-like lines inside fenced code never count and never toggle.
- Ticking a checkbox rewrites only that line's `[ ]`/`[x]` mark and keeps the rest of the body, including line endings, unchanged (`toggleCheckbox`).
- Keep titles required, at most 128 characters; text optional, at most 3000 characters with only line breaks and tabs as control characters; at most 50 notes. The limits keep the `listNotes` result under the host's 512 KiB structured-data limit even for three-byte characters, so raise them only together with a paged or split list. `ui/contracts/notes.ts` and `NotesModule` validate the same rules; keep both strict and tested.
- Notes are stored unencrypted in `library.db`. Do not present Quick notes as a place for secrets.
- Keep the module in slot 10 (the **0** key) with the stable `notes` ID, view kind, and and record schema version. Changing the record shape needs a reader that still accepts the old records. Notes are not yet part of the library export and import (`AutomationLibraryTransferService`); add them there deliberately, with record validation, if that is wanted.

## Code and checks

UI: `ui/modules/NotesView.tsx`, `NotesView.css`, `ui/modules/notesMarkdown.ts`; contract: `ui/contracts/notes.ts`.
Module and registry: `src/Automator.Application/Automation/NotesModule.cs` (validation, storage, actions) and `src/Automator.Application/Launcher/LauncherTabRegistry.cs`.
Tests: `tests/notes-contract.test.mjs`, the Application specifications, and `tests/electron/tab-happy-path-visibility.test.cjs`.

Update this guide, the sibling README and TESTS, and the root tab index when markdown support, validation limits, storage, or the slot change.
