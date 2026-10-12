# Browser Automation test coverage

This file documents Browser Automation profile and Playwright Test Explorer contracts, plus Electron checks for the preview bridge and managed project. The test titles match their node:test descriptions.

Run focused contracts with node --experimental-strip-types --test tests/browser-automation-contract.test.mjs. Run Electron cases after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/automation-services.test.cjs or ui-flow.test.cjs. Application and Browser Infrastructure specifications are included in npm.cmd run test:dotnet.

## Browser profile and explorer contracts — tests/browser-automation-contract.test.mjs

### browser profile validation requires its start host in an exact allowlist

**Steps**

1. Validate a profile whose start URL host matches an exact host allowlist.
2. Change the URL to a host outside that allowlist and validate again.

**Expected result:** A matching host is accepted; an unlisted start host is rejected.

### browser actions reject unsupported, oversized, malformed, and out-of-policy inputs

**Steps**

1. Validate a supported browser action and its input.
2. Repeat with an unsupported action, malformed parameters, oversized values, and a URL outside the saved host policy.

**Expected result:** Only bounded, well-formed actions allowed by the saved host policy are accepted.

### browser result decoding accepts bounded known fields and ignores malformed links

**Steps**

1. Decode a result containing a URL, title, text, and both valid and malformed links.
2. Provide an oversized current URL and a malformed profile record.

**Expected result:** Known bounded fields and valid links are retained; malformed links, oversized results, and invalid profiles are ignored or rejected safely.

### Playwright list parsing groups exact project, file, location, and title identities

**Steps**

1. Parse a Playwright list containing the same test under Chromium and Firefox projects plus a second test file.
2. Inspect project, file, line, nested title path, list entry, and generated identity.

**Expected result:** Each project/file/location/title combination is parsed, and the same test under two projects receives distinct identities.

### Playwright section path validation allows test files under the project and rejects traversal

**Steps**

1. Validate a project-relative spec path.
2. Try parent-directory traversal, a text file, an absolute path, and an unsupported test extension.

**Expected result:** A supported in-project test path is returned; traversal and unsupported paths are rejected.

## Electron and host integration tests

### browser preview keeps keyboard inert and rejects HTTP without networking

**Source:** tests/electron/automation-services.test.cjs

**Steps**

1. Create the browser-preview bridge and validate its initial tab state.
2. Ask for keyboard eligibility and attempt a keyboard subscription.
3. Issue an HTTP request through the preview service facade.

**Expected result:** Preview state satisfies the shared contract, keyboard input remains unavailable, and HTTP fails with a transport error without making a network request.

### Browser Automation opens an Automator-managed Playwright project with an example test

**Source:** tests/electron/ui-flow.test.cjs

**Steps**

1. Launch an isolated app and open Workspace.
2. Select Browser Automation and wait for the Playwright Test Explorer.
3. Inspect the configured project folder and read the generated example spec.

**Expected result:** The managed project is created in isolated test data and includes the Automator example test in tests/example.spec.ts.

## Electron happy-path visibility — tests/electron/tab-happy-path-visibility.test.cjs

### Browser Automation section happy path keeps create and tag buttons within visible bounds

**Steps**

1. Open Workspace through its checked button, select Browser Automation, and wait for the managed example test.
2. Check Browse and Use folder; check Refresh tests before refreshing discovery.
3. Check New section before opening its form, enter an isolated spec path and test name, check Cancel and Create file, then create the section.
4. Check the section/test Run controls, open checked Edit tags, enter a tag, check Cancel tag editing and Save tags, then save.
5. Verify the rendered tag and the generated spec file in isolated data.

**Expected result:** Required buttons are CSS-visible, fully inside viewport and clipping ancestor bounds, and uncovered before use. User wheel scrolling may reveal controls before assertion; click auto-scrolling cannot satisfy it. Section creation and tag saving work. Browser/test execution and native folder selection are not performed by this case.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.

## Additional backend coverage

Module discovery, project selection, tag handling, safe section-file operations, and browser execution are covered by Application and Browser Infrastructure specification projects. Run them with npm.cmd run test:dotnet.

Codex-generated Playwright containment, declared-input validation, source-hash checks, and reapproval are covered by the Codex Application specifications listed in [Codex TESTS](../codex/TESTS.md). Workflow input forwarding is supported by the saved-profile handler and Workflow mappings; scheduling uses the existing saved-Workflow target.

## Shared numbered-tab icon coverage — tests/electron/tab-happy-path-visibility.test.cjs

### numbered tabs show an identifying icon in the launcher and Workspace

**Source:** tests/electron/tab-happy-path-visibility.test.cjs

**Steps**

1. Launch the isolated compact host and locate slots 1–8 by their exact accessible tab names; verify each contains one visible decorative icon with dimensions of at least 12 by 12 pixels.
2. Open Workspace and verify slots 2–8 each contain one visible decorative icon with dimensions of at least 12 by 12 pixels.

**Expected result:** The numbered tabs retain their accessible names and show one visible icon in both windows.
