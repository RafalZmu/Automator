# Quick notes test coverage

This file documents the note contract, markdown handling, module registration, and the Electron add-tick-edit-delete flow. Test titles match their node:test descriptions or registered specification names.

Run the focused contract cases with:

    node --experimental-strip-types --test tests/notes-contract.test.mjs

Run the full contract suite with npm.cmd run test:contracts. The Application and Core specifications are included in npm.cmd run test:dotnet.

## Notes contract — tests/notes-contract.test.mjs

### accepts a note with an empty body and keeps line breaks and tabs

**Steps**

1. Validate a note, a note with an empty body, and a note with line breaks and tabs.

**Expected result:** All are valid.

### rejects control characters other than line breaks and tabs in the text

**Steps**

1. Validate text containing bell, NUL, and escape characters.

**Expected result:** Each is rejected with a text error.

### requires a title of at most 128 characters without control characters

**Steps**

1. Validate a 128-character title, then blank, 129-character, and control-character titles.

**Expected result:** The 128-character title is accepted; the others are rejected with a title error.

### limits the text length

**Steps**

1. Validate text of exactly 3000 characters, then 3001.

**Expected result:** 3000 is accepted; 3001 is rejected.

### requires a valid key

**Steps**

1. Validate keys that are empty, contain a space or capitals, are 65 characters long, are not text, or are missing.

**Expected result:** Each is rejected with a key error.

### reads the saved notes in order

**Steps**

1. Read a `listNotes` result with two notes.

**Expected result:** Both notes come back in the same order.

### skips invalid and repeated notes without hiding the others

**Steps**

1. Read a list with a blank title, non-objects, and a repeated key, then a non-array and undefined.

**Expected result:** Only the valid, first-seen notes are kept; malformed results read as empty.

### never reads more notes than the limit

**Steps**

1. Read a list of 55 valid notes.

**Expected result:** Only 50 are kept.

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

- `notes module saves, lists, and deletes validated notes in the module library` — keeps notes in the `notes` collection, trims titles, keeps order when a note is edited, rejects invalid notes and a 51st note, and deletes notes.
- `notes module lists a full library within the structured result limit` — 50 notes of the maximum size of three-byte characters list without exceeding the 512 KiB result limit.
- `tab registry publishes ten stable slots and their actions` — Quick notes is the tenth tab.
- `global navigation consumes only focused unmodified launcher keys` — the 0 key selects a tab.
- `tab keys select slots 1 through 10` — the 0 key maps to slot 10.

## Electron — tests/electron/tab-happy-path-visibility.test.cjs

### Quick notes add, tick, edit, and delete happy path persists notes and shows the unticked dot

**Steps**

1. Open Quick notes, add a note with one unticked and one ticked checkbox, and save it.
2. Check the rendered heading and the unticked dot, then tick the last checkbox.
3. Collapse and expand the note, edit its title, then delete it.

**Expected result:** The dot turns green after the tick, each change is present in the notes the module lists, and the list ends empty.
