# Snippets test coverage

This file documents the snippet settings contract, module registration, and the Electron save-and-copy flow. Test titles match their node:test descriptions or registered specification names.

Run the focused contract cases with:

    node --experimental-strip-types --test tests/snippets-contract.test.mjs

Run the full contract suite with npm.cmd run test:contracts. The Application specification is included in npm.cmd run test:dotnet.

## Snippet settings contract — tests/snippets-contract.test.mjs

### parses snippets without changing their order or text

**Steps**

1. Parse settings containing two valid snippets.
2. Compare the parsed settings with the source.

**Expected result:** The order and text are preserved exactly.

### keeps line breaks and tabs in snippet text but rejects other control characters

**Steps**

1. Validate a snippet whose text contains CRLF and a tab.
2. Validate snippets whose text contains a bell, a NUL, or an escape character.

**Expected result:** Line breaks and tabs are accepted; any other control character is rejected.

### rejects blank snippet text and text over the length limit

**Steps**

1. Validate snippets with empty and whitespace-only text.
2. Validate text of exactly the maximum length and one character more.

**Expected result:** Blank text and over-long text are rejected; text at the limit is accepted.

### accepts letters and digits as a shortcut, allows none, and rejects other characters

**Steps**

1. Validate a snippet without a shortcut and one with letters and digits.
2. Validate shortcuts containing a slash, a space, a dash, and 33 characters.

**Expected result:** An empty or letters-and-digits shortcut is accepted; the other values are rejected.

### rejects a shortcut used by two snippets regardless of letter case but allows several without one

**Steps**

1. Validate two snippets whose shortcuts differ only by letter case.
2. Validate two snippets that both have no shortcut.

**Expected result:** The duplicate shortcut is rejected; several snippets without a shortcut are valid.

### requires a unique valid key and a name for each snippet

**Steps**

1. Validate snippets that share a key, use an invalid key, or have a blank name.

**Expected result:** Each case is rejected with a message naming the key or the name.

### rejects malformed settings, too many snippets, and an oversized payload

**Steps**

1. Parse settings whose snippets field is not an array and settings with no snippets field.
2. Validate more snippets than the limit.
3. Validate a list whose combined size exceeds 64 KiB.

**Expected result:** Malformed data throws a parse error; too many snippets and an oversized payload are reported as invalid.

### reads an invalid saved list as empty instead of exposing partial data

**Steps**

1. Read a saved list that contains one invalid snippet, an empty object, and undefined.

**Expected result:** Each read returns an empty list rather than a partly valid one.

## Application specification — tests/Automator.Application.Specs/Program.cs

### snippets module keeps its text in module settings and registers no actions

**Steps**

1. Inspect the module definition for slot, view kind, actions, and capabilities and read the default settings.
2. Write a snippet list through the module registry's settings route and read it back, including a line break.
3. Try to dispatch an action to the module.

**Expected result:** Snippets is registered in slot 9 as `snippets` with no actions or capabilities, defaults to an empty list, stores and returns the text unchanged, and rejects every action dispatch. The related registry case **tab registry publishes nine stable slots and their actions** checks that slot 9 is Snippets and that Codex is not registered.

## Electron happy path — tests/electron/tab-happy-path-visibility.test.cjs

### Snippets save and copy happy path keeps its buttons within visible bounds and fills the clipboard

**Steps**

1. Open Snippets in an isolated compact host and use checked Add snippet.
2. Enter a name, the shortcut plan, and two lines of text; check Save against viewport and clipping ancestors and save.
3. Clear the clipboard, use the checked Copy button, and read the Electron clipboard.
4. Clear the clipboard again, type the shortcut in the tab search, press Enter, and read the clipboard.
5. Read the module settings.

**Expected result:** The Copy and Save buttons are visible, inside their bounds, and uncovered. Both ways of copying put the saved text on the clipboard (with Windows line endings), no notice repeats the text, and the settings store the name, shortcut, and text.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.

## Shared numbered-tab icon coverage

The test **numbered tabs show an identifying icon in the launcher and Workspace** in tests/electron/tab-happy-path-visibility.test.cjs includes Snippets in slot 9; its steps are listed in the other tab test guides.
