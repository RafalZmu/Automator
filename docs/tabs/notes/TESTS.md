# Quick notes test coverage

This file documents the note settings contract, markdown handling, module registration, and the Electron add-tick-edit-delete flow. Test titles match their node:test descriptions or registered specification names.

Run the focused contract cases with:

    node --experimental-strip-types --test tests/notes-contract.test.mjs

Run the full contract suite with npm.cmd run test:contracts. The Application and Core specifications are included in npm.cmd run test:dotnet.

## Notes contract — tests/notes-contract.test.mjs

### parses notes without changing their order or text

**Steps**

1. Parse settings containing two valid notes.
2. Compare the parsed settings with the source.

**Expected result:** The order and text are preserved exactly.

### allows an empty body, line breaks and tabs, but no other control characters

**Steps**

1. Validate notes with an empty body and with line breaks and tabs.
2. Validate bodies containing bell, NUL, and escape characters.

**Expected result:** The first set is valid; the control characters are rejected.

### requires a title of at most 128 characters without control characters

**Steps**

1. Validate blank, over-long, and control-character titles.

**Expected result:** Each is rejected with a title error.

### limits the body length

**Steps**

1. Validate a body of exactly 8000 characters, then 8001.

**Expected result:** 8000 is accepted; 8001 is rejected.

### requires a unique valid key for each note

**Steps**

1. Validate two notes with the same key, and a key with a space.

**Expected result:** Both are rejected with a key error.

### rejects malformed settings, too many notes, and an oversized payload

**Steps**

1. Parse a non-array list and an object without notes.
2. Validate 101 notes and ten notes of the maximum body length.

**Expected result:** Parsing throws, and the list and size limits are reported.

### reads an invalid saved list as empty instead of exposing partial data

**Steps**

1. Read a list containing one invalid note, and an undefined value.

**Expected result:** Both read as an empty list.

### counts unticked checkboxes and ignores checkbox-like text in code and prose

**Steps**

1. Count checkboxes in bullet, star, plus, and numbered lists.
2. Count lines that only look like checkboxes, and checkboxes inside a code fence.

**Expected result:** Only real list-item checkboxes outside code are counted, and the unticked count drives the dot.

### toggles one checkbox line and keeps the rest of the note byte for byte

**Steps**

1. Toggle a task in a note with CRLF line endings, then toggle it back.
2. Toggle a non-checkbox line, a checkbox inside code, and out-of-range lines.

**Expected result:** Only the chosen `[ ]`/`[x]` mark changes; everything else is unchanged.

### renders blocks as a tree and records the line of each task

**Steps**

1. Parse a note with a heading, paragraph, task and plain lists, quote, rule, and code fence.

**Expected result:** Blocks come out in order and each task carries its body line.

### parses inline markup and only links http and https addresses

**Steps**

1. Parse bold, italic, strikethrough, code, an https link, a `javascript:` link, and text with HTML.

**Expected result:** Markup is parsed, the `javascript:` link and HTML stay plain text.

## Application and Core specifications

- `notes module keeps its markdown notes in module settings and registers no actions` — registers slot 10, default and saved settings round-trip, no action dispatch.
- `tab registry publishes ten stable slots and their actions` — Quick notes is the tenth tab.
- `global navigation consumes only focused unmodified launcher keys` — the 0 key selects a tab.
- `tab keys select slots 1 through 10` — the 0 key maps to slot 10.

## Electron — tests/electron/tab-happy-path-visibility.test.cjs

### Quick notes add, tick, edit, and delete happy path persists notes and shows the unticked dot

**Steps**

1. Open Quick notes, add a note with one unticked and one ticked checkbox, and save it.
2. Check the rendered heading and the unticked dot, then tick the last checkbox.
3. Collapse and expand the note, edit its title, then delete it.

**Expected result:** The dot turns green after the tick, each change is present in the saved module settings, and the list ends empty.
