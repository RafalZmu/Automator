# Launcher and shell test coverage

This file documents the Launcher tab and the desktop-shell end-to-end cases assigned to it. Test titles are copied from their automated test descriptions. Each case has steps and the behavior asserted by the test.

Run a focused contract test with node --experimental-strip-types --test TEST_FILE. Run an Electron case after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 TEST_FILE. The full contract, Electron, and .NET suites are included in npm.cmd run verify.

## Launcher and host Electron tests

### Electron shows the minimal isolated launcher and focuses search on startup

**Source:** tests/electron/minimal-window.test.cjs

**Steps**

1. Launch Electron with test mode, an isolated tray host, and a unique build ID.
2. Wait for the search field, ready indicator, and initial keyboard focus.
3. Inspect the preload bridge, renderer Node access, native window focus, and foreground window handle.

**Expected result:** The isolated launcher becomes ready and foreground-focused, search receives focus, the preload bridge is available, Node globals are absent from the renderer, and no renderer errors are recorded.

### real backend catalog discovery saves a selected app alias to isolated schema-2 settings

**Source:** tests/electron/catalog-alias.test.cjs

**Steps**

1. Start the app with isolated settings and open the application catalog.
2. Search the real Windows catalog for Windows PowerShell and select the discovered executable.
3. Save the letters-only alias whp, then inspect backend state and the persisted settings file.
4. Remove the binding, confirm the executable still exists, and search the catalog again.

**Expected result:** The binding is saved with the discovered ID and path in schema-2 settings; removal deletes only the binding, survives in state, and leaves the executable untouched.

### real Right Ctrl and Escape input reaches the global hook from an owned foreground app

**Source:** tests/electron/native-hotkey.test.cjs

**Steps**

1. Start an owned foreground fixture window and launch Automator hidden with isolated test data.
2. Send real synthetic Right Ctrl input; verify the launcher opens, focuses search, and records the previous foreground window.
3. Move the pointer and verify the open panel does not move; send number keys and a letter through the global hook.
4. Send Escape and verify the app closes while the fixture receives no launcher input.

**Expected result:** Native input routes to Automator only while appropriate; tab selection, query input, panel placement, foreground restoration, and Escape behavior match the launcher contract.

### settings and tab transitions preserve focus in the isolated desktop host

**Source:** tests/electron/ui-flow.test.cjs

**Steps**

1. Launch an isolated host, select Launcher, and verify the app search focus and initial view.
2. Attempt a module action for an inactive tab.
3. Change the theme, save it, and wait for the visible launcher transition to settle.
4. Select reserved tab 9 and return to Launcher, then close with Escape.

**Expected result:** Inactive module actions are rejected, the selected theme is applied, tab changes preserve the expected focus, and Escape hides the launcher and native window.

### compact launcher uses the larger fixed size and remains non-resizable

**Source:** tests/electron/ui-flow.test.cjs

**Steps**

1. Launch the isolated compact host and wait for its home search.
2. Read the native window content bounds and the selected display work area.
3. Compare dimensions and inspect the native resizable flag.

**Expected result:** The launcher uses the preferred 760×800 size, shrinks to the display work area when necessary, and remains non-resizable.

### active tab content scrolls independently from the launcher chrome

**Source:** tests/electron/ui-flow.test.cjs

**Steps**

1. Open Script Runner in the compact launcher.
2. Add temporary tall content to its active view and inspect its vertical overflow.
3. Scroll the active view to the bottom and compare the topbar, tabs, command bar, and footer positions.

**Expected result:** The active view can scroll vertically while the surrounding launcher chrome remains fixed.

### backend retry restores a visible launcher after the child process exits

**Source:** tests/electron/ui-flow.test.cjs

**Steps**

1. Start the isolated host and capture the backend process ID.
2. Terminate the backend and wait for the restart prompt.
3. Choose Retry and wait for the backend and launcher to become ready again.

**Expected result:** Retry starts a ready backend, restores the visible launcher, and returns focus to Global Action Search.

### Workspace opens as an independent large window with local tabs and hide behavior

**Source:** tests/electron/ui-flow.test.cjs

**Steps**

1. Open Workspace from the compact launcher and verify its initial role and selected tab.
2. Select API in Workspace and compare both window contexts, dimensions, and visibility.
3. Hide Workspace and inspect the two native windows.

**Expected result:** Workspace has its own resizable large window and tab state; selecting API there does not change Launcher; hiding Workspace leaves Launcher and the shared host available.

### Launcher alias happy path keeps catalog and editor buttons within visible bounds

**Source:** tests/electron/tab-happy-path-visibility.test.cjs

**Steps**

1. Open the application catalog in an isolated host and search for Windows PowerShell.
2. Check the catalog selection button against the viewport and clipping ancestors before selecting it.
3. Check both editor cancellation buttons, enter an alias, and check Save alias before saving.
4. Check the saved Launch and Edit buttons, edit the alias, save it, and inspect isolated backend settings.

**Expected result:** Each required button is CSS-visible, fully inside the viewport and ancestor content bounds, and uncovered before use. User wheel scrolling may reveal a control before the assertion; click auto-scrolling cannot satisfy it. The edited alias persists. The test does not launch Windows PowerShell.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.

## Global Action Search command contracts

### command search matches every query word against a command label or keywords

**Source:** tests/command-matching.test.mjs

**Steps**

1. Search for a pair of words present in a command label and keywords.
2. Search for a pair that matches the Playwright test action.

**Expected result:** Every query word must match the same command's label or keywords.

### command search ignores case and diacritics and returns no results for an unknown phrase

**Source:** tests/command-matching.test.mjs

**Steps**

1. Search for a French command label with uppercase, unaccented input.
2. Search for words that match no command.

**Expected result:** Case and diacritics are ignored; unknown phrases produce no results.

### a command executes from Enter only when the query has one unique match

**Source:** tests/command-matching.test.mjs

**Steps**

1. Resolve a query matching multiple commands.
2. Resolve a query matching exactly one command.
3. Resolve a query with no matches.

**Expected result:** Resolution is ambiguous, unique, or none according to the match count; only a unique command is eligible to execute from Enter.

### delayed automatic execution is limited to unique enabled actions without confirmation prompts

**Source:** tests/command-matching.test.mjs

**Steps**

1. Check an enabled command with no confirmation prompt.
2. Check a disabled command, a command requiring confirmation, and no command.

**Expected result:** Only a unique enabled action without a confirmation prompt may run automatically.

## Platform and renderer contracts

### Windows 10 keeps transparent outer corners with a solid CSS surface

**Source:** tests/host-platform.test.mjs

**Steps**

1. Resolve the window-surface options for Windows 10.
2. Inspect transparency and CSS surface settings.

**Expected result:** The surface uses the solid CSS fallback while preserving transparent outer corners.

### Windows 11 21H2 uses an opaque solid surface with native corners

**Source:** tests/host-platform.test.mjs

**Steps**

1. Resolve surface options for Windows 11 build 22000.
2. Inspect transparency, background, and corner settings.

**Expected result:** The window uses an opaque solid surface with native corners.

### Windows 11 22H2 enables native Acrylic without a transparent window

**Source:** tests/host-platform.test.mjs

**Steps**

1. Resolve surface options for Windows 11 build 22621.
2. Inspect the native Acrylic and transparency settings.

**Expected result:** Native Acrylic is enabled without making the window transparent.

### panel placement clamps both its origin and dimensions to small work areas

**Source:** tests/host-platform.test.mjs

**Steps**

1. Place the preferred panel against a 420×300 work area at a negative screen origin.
2. Inspect the computed position and dimensions.

**Expected result:** Both the panel origin and dimensions remain inside the smaller work area.

### panel placement remains inside a negative-origin secondary monitor

**Source:** tests/host-platform.test.mjs

**Steps**

1. Place the panel near a pointer on a secondary display whose work area begins at a negative x coordinate.
2. Inspect the panel bounds.

**Expected result:** Preferred dimensions are retained when space allows, and all bounds remain inside that display.

### Electron development URL runs Vite React preamble under the development-only CSP

**Source:** tests/electron/dev-renderer.test.cjs

**Steps**

1. Launch the development renderer.
2. Wait for the Vite React preamble and inspect the active security policy.

**Expected result:** The development renderer loads without console errors under the development-only Content Security Policy.

### saved dark theme is applied before the native window is shown

**Source:** tests/electron/native-theme.test.cjs

**Steps**

1. Start the app with a saved dark theme in isolated settings.
2. Observe the page and native window as the host becomes visible.

**Expected result:** The dark theme is present before the native window is shown, avoiding a light-theme flash.

## Shared host and window-context checks

### keyboard service re-subscribes after revoke and regain and ignores stale snapshots

**Source:** tests/automation-services.test.mjs

**Steps**

1. Create the keyboard service and confirm it subscribes to availability and input events before requesting its eligibility snapshot.
2. Deliver a newer eligible event before resolving the older snapshot.
3. Revoke eligibility, send an input, restore eligibility with a newer revision, and unsubscribe.

**Expected result:** The stale snapshot is ignored, input is accepted only while eligible, regain causes a new subscription, and disposal detaches listeners.

### keyboard facade installs listeners before its eligibility snapshot and reconciles revisions

**Source:** tests/electron/automation-services.test.cjs

**Steps**

1. Create a launcher-scoped keyboard facade with a pending eligibility snapshot.
2. Deliver a newer eligibility event before resolving the older snapshot.
3. Revoke and regain eligibility, send events from the selected and another module, then unsubscribe.

**Expected result:** Listeners are installed before the snapshot, stale revisions do not change the subscription, unrelated modules are ignored, and input stops while ineligible.

### keyboard input notifications validate their versioned event payload

**Source:** tests/rpc-contracts.test.mjs

**Steps**

1. Validate a keyboard notification with a valid module ID, sequence, key, and modifier data.
2. Change the sequence to a negative value and validate again.

**Expected result:** The valid event passes the notification schema; the malformed event is rejected.

### keyboard availability notification supports revoke and regain

**Source:** tests/rpc-contracts.test.mjs

**Steps**

1. Validate the shared keyboard availability notification and an eligible result with a positive revision.
2. Try a negative revision and a string value for the eligible flag.

**Expected result:** Valid availability updates pass; invalid revisions and flag types are rejected.

### host-owned BrowserWindow contexts preserve independent roles and active tabs

**Source:** tests/electron/window-context.test.cjs

**Steps**

1. Register launcher and Workspace renderer contexts.
2. Set different selected tabs for each context and request each context.
3. Request an unregistered renderer and try an invalid tab slot.

**Expected result:** Each renderer retains its own host-owned role and tab; unknown renderers and invalid slots are rejected.

### window context disposal removes only the owning renderer

**Source:** tests/electron/window-context.test.cjs

**Steps**

1. Register launcher and Workspace contexts.
2. Dispose the launcher registration.
3. Read both renderer contexts.

**Expected result:** The launcher registration is removed and the Workspace registration is unchanged.

### window context disposal remains valid after the local tab changes

**Source:** tests/electron/window-context.test.cjs

**Steps**

1. Register a Workspace renderer and change its local selected tab.
2. Dispose its context and read the registry.

**Expected result:** Disposal succeeds and the renderer context is removed after tab changes.

### workspace state projection isolates launcher visibility, tab selection and transient input state

**Source:** tests/electron/window-context.test.cjs

**Steps**

1. Create launcher state containing an open catalog, query, error, and alias-edit candidate.
2. Project that state into Workspace and Launcher contexts.
3. Compare each projected result with the source state.

**Expected result:** Workspace is visible on its own selected tab with transient launcher state cleared; Launcher retains the original state.

### host context injection overwrites renderer claims

**Source:** tests/electron/window-context.test.cjs

**Steps**

1. Submit a module request claiming it came from Launcher.
2. Inject the host-owned Workspace context.

**Expected result:** The request carries the host context rather than the renderer-supplied claim.

## Backend-process integration checks

### binding removal persists, preserves custom files, prevents alias launch, and survives reload

**Source:** tests/electron/backend-process.test.cjs

**Steps**

1. Start the real backend with an isolated custom app binding and executable file.
2. Force settings persistence to fail and try removing the binding.
3. Restore the settings file, remove the binding successfully, and query state and disk.
4. Attempt to launch the removed binding, then restart the backend.

**Expected result:** Failed removal leaves state unchanged; successful removal persists, does not delete the executable, prevents later launch, and remains removed after reload.

### versioned backups transfer module profiles and return path repair warnings

**Source:** tests/electron/backend-process.test.cjs

**Steps**

1. Save a Script Runner profile with paths that do not exist and export a versioned backup.
2. Import the backup into a second isolated backend.
3. Inspect import warnings and list the imported profile.

**Expected result:** Module profile data is transferred, missing paths produce repair warnings, and the profile is available after import.

### backend keeps stdout as JSON-RPC, serializes stateChanged params, and exits on stdin EOF

**Source:** tests/electron/backend-process.test.cjs

**Steps**

1. Start the backend and send a launcher query update.
2. Inspect the returned state, the stateChanged notification, and backend protocol logs.
3. Stop the host connection and inspect process exit and shutdown logs.

**Expected result:** Standard output remains valid JSON-RPC, notification data is serialized under params, and the backend shuts down cleanly when stdin closes.

### backend exits and cleans up when its host process dies

**Source:** tests/electron/backend-process.test.cjs

**Steps**

1. Start a host fixture and attach an isolated backend process to it.
2. Terminate the host fixture.
3. Wait for backend exit and inspect its logs.

**Expected result:** The backend detects unexpected host exit, stops, and records host-exit and shutdown events.

### backend process parser rejects JSON null and malformed response envelopes without throwing

**Source:** tests/electron/backend-process.test.cjs

**Steps**

1. Start a fake backend that responds with JSON null, an invalid JSON-RPC version, or conflicting result and error fields.
2. Send a normal backend request for each malformed response.
3. Observe the request rejection and process failure event.

**Expected result:** Each malformed response is reported as a protocol failure without an uncaught parser exception.

### packaged desktop host enforces one instance before starting another backend

**Source:** tests/electron/packaged-single-instance.package.cjs

**Steps**

1. Start the packaged app with isolated settings and wait for the primary host and backend ready events.
2. Start a second app process with a different build ID.
3. Inspect the second process exit, duplicate-launch event, and backend-ready events.
4. Stop the packaged test host through the workspace helper.

**Expected result:** The duplicate host exits cleanly before starting another backend, the primary host remains ready, and exactly one backend starts.

Run this packaged lifecycle case separately with npm.cmd run test:packaged-lifecycle after creating the Windows x64 package.

## Shared numbered-tab icon coverage — tests/electron/tab-happy-path-visibility.test.cjs

### numbered tabs show an identifying icon in the launcher and Workspace

**Source:** tests/electron/tab-happy-path-visibility.test.cjs

**Steps**

1. Launch the isolated compact host and locate slots 1–8 by their exact accessible tab names; verify each contains one visible decorative icon with dimensions of at least 12 by 12 pixels.
2. Open Workspace and verify slots 2–8 each contain one visible decorative icon with dimensions of at least 12 by 12 pixels.

**Expected result:** The numbered tabs retain their accessible names and show one visible icon in both windows.
