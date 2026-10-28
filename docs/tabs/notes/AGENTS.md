# Quick notes contributor guide

Read the root `AGENTS.md` first. Quick notes keeps short markdown notes in module preferences and renders them in the renderer. It has no backend actions and no host capability.

## Preserve the notes contract

- Keep the note list in module settings (`{ "notes": [{ "id", "title", "body" }] }`, settings version 1) and read and write it only through the module settings bridge. The module declares no actions or capabilities.
- Every add, edit, delete, and checkbox tick is written immediately. The list on screen changes only after the write succeeds, so a failed save never shows data that was not stored.
- Render markdown to React elements from the tree in `ui/modules/notesMarkdown.ts`. Never use `dangerouslySetInnerHTML`, and keep links limited to `http` and `https` addresses.
- The status dot is derived from the body with `countCheckboxes`: red when any checkbox is unticked, green when every checkbox is ticked, no dot when the note has none. Checkbox-like lines inside fenced code never count and never toggle.
- Ticking a checkbox rewrites only that line's `[ ]`/`[x]` mark and keeps the rest of the body, including line endings, unchanged (`toggleCheckbox`).
- Keep titles required, at most 128 characters; bodies optional, at most 8000 characters with only line breaks and tabs as control characters; at most 100 notes and 64 KiB in total. The contract in `ui/contracts/notes.ts` is the validation, so keep it strict and unit-tested.
- Notes are plain JSON in `settings.json` and are not encrypted. Do not present Quick notes as a place for secrets.
- Keep the module in slot 10 (the **0** key) with the stable `notes` ID, view kind, and settings version. Changing the settings shape requires a migration in `NotesModule.MigrateSettings`.

## Code and checks

UI: `ui/modules/NotesView.tsx`, `NotesView.css`, `ui/modules/notesMarkdown.ts`; contract: `ui/contracts/notes.ts`.
Module and registry: `src/Automator.Application/Automation/NotesModule.cs` and `src/Automator.Application/Launcher/LauncherTabRegistry.cs`.
Tests: `tests/notes-contract.test.mjs`, the Application specifications, and `tests/electron/tab-happy-path-visibility.test.cjs`.

Update this guide, the sibling README and TESTS, and the root tab index when markdown support, validation limits, storage, or the slot change.
