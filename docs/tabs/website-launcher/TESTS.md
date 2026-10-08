# Website Launcher test coverage

This file documents website shortcut settings, aliases, command matching, and launch-order contracts. Test titles match their node:test descriptions.

Run the focused contract cases with:

    node --experimental-strip-types --test tests/website-launcher-contract.test.mjs tests/command-matching.test.mjs

Run the full contract suite with npm.cmd run test:contracts. Application and Windows adapter specifications are included in npm.cmd run test:dotnet.

## Website shortcut contracts — tests/website-launcher-contract.test.mjs

### parses grouped websites without changing their launch order

**Steps**

1. Parse settings containing one shortcut row with two ordered browser groups and one website in each group.
2. Compare the parsed settings with the source row.

**Expected result:** Group and website order is preserved exactly.

### allows an alias that may also belong to an app, but rejects duplicate website aliases

**Steps**

1. Validate one shortcut using the alias docs.
2. Add a second shortcut row using the same alias and validate again.

**Expected result:** A website alias may overlap an application alias, but it must be unique among website rows.

### accepts only absolute HTTP and HTTPS website URLs

**Steps**

1. Validate valid website settings with file, javascript, credential-bearing HTTPS, and malformed URL variants.
2. Inspect validation errors for each rejected address.

**Expected result:** Only absolute HTTP or HTTPS URLs without embedded credentials are accepted.

### rejects missing groups, empty groups, and malformed row data

**Steps**

1. Validate a shortcut with no groups.
2. Validate a group with no websites.
3. Parse settings whose rows field is not an array.

**Expected result:** Empty structures are invalid and malformed top-level data throws a parse error.

### reads only safe shortcut labels and aliases from backend tab metadata

**Steps**

1. Parse backend metadata with a valid row whose alias uses uppercase letters.
2. Parse metadata containing one valid row and one row with an invalid ID.
3. Parse malformed JSON.

**Expected result:** Valid metadata is reduced to safe ID, name, and normalized lowercase alias values; invalid rows and malformed JSON are ignored.

## Command search contract — tests/command-matching.test.mjs

### website aliases work directly in their tab and use a trailing w to disambiguate global search

**Steps**

1. Match the website shortcut by its docs alias within Website Launcher.
2. Add an application command with the same alias and search globally using docsw.

**Expected result:** The tab accepts docs directly and the trailing w selects the website command when the app alias overlaps.

## End-to-end coverage note

The Electron visibility case below edits and saves a Website Launcher row without opening its URLs. URL launch behavior and command routing remain covered by the contracts and Application/Windows specification projects; native external-browser execution is not part of the isolated Electron fixture.

## Electron happy-path visibility — tests/electron/tab-happy-path-visibility.test.cjs

### Website Launcher save happy path keeps shortcut group and website buttons within visible bounds

**Steps**

1. Open Website Launcher in an isolated compact host and use checked Add shortcut.
2. Enter a name, alias, site name, and HTTPS URL in its first browser group.
3. Check Add website to this window and Add browser window; check Save against viewport and clipping ancestors before saving.
4. Check Launch for reachability, rename the shortcut, save, and check the renamed Launch button.
5. Read module settings and verify the edited name and saved URL.

**Expected result:** Required buttons are CSS-visible, fully inside viewport and ancestor content bounds, and uncovered before use. User wheel scrolling may reveal controls before assertion; click auto-scrolling cannot satisfy it. The edited shortcut persists. URL launch and additional group/site creation are not performed by this case.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.
