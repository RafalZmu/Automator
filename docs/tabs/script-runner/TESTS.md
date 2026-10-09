# Script Runner test coverage

This file documents Script Runner profile, template, path-field, and Electron integration tests. Each heading preserves the test's existing node:test title; the steps summarize the fixture, action, and assertions.

Run a focused contract test with node --experimental-strip-types --test TEST_FILE. Run an Electron case after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 TEST_FILE. npm.cmd run test:contracts and npm.cmd run test:electron run their respective complete suites. Application specifications are included in npm.cmd run test:dotnet.

## Template contracts — tests/script-runner-template-contract.test.mjs

### Script template contract accepts a Firebird descriptor and validates transient values

**Steps**

1. Parse a Firebird template descriptor with required file, text, sensitive, boolean, and choice parameters.
2. Validate input containing database path, ZIP destination, username, password, and a selected compression option.
3. Try missing paths, invalid choices, attempts to supply scriptPath, and an oversized database path.

**Expected result:** Valid transient inputs are returned with defaults applied; malformed or unsafe inputs are rejected.

### Script template contract rejects duplicate IDs, keys, argument mappings, and malformed inputs

**Steps**

1. Parse duplicate template IDs.
2. For a single descriptor, try duplicate parameter keys, duplicate or excessive argument indexes, an unsupported type, an oversized label, a sensitive default, and a traversal asset ID.

**Expected result:** Each invalid catalog shape or unsafe descriptor is rejected.

## Template view-model tests — tests/script-template-ui.test.mjs

### library search matches name description and tags without selecting or executing

**Steps**

1. Search a Firebird template by name, description, tag, and multiple words.
2. Search for a phrase with no match.

**Expected result:** Matching returns the template without selecting or executing it; unknown text returns no results.

### Firebird defaults live in new forms only and secrets clear without mutating submitted values

**Steps**

1. Create initial values for a new Firebird template form.
2. Clear sensitive values and inspect both the cleared result and the original object.
3. Create initial values for a different template.

**Expected result:** New Firebird forms receive the expected defaults; sensitive values clear in the returned state without mutating the submitted values, and another template receives no Firebird password default.

### Firebird target database stays visible outside Details and its value survives template selection

**Steps**

1. Inspect the parameters assigned to the Details section.
2. Set the target database path on the current Firebird template values.
3. Reset the template form values.

**Expected result:** The database parameter is not in Details and its path survives template selection.

### catalog parser accepts the host JSON nulls used for optional parameter fields

**Steps**

1. Parse a template descriptor whose optional description, defaultValue, and options fields are JSON null.
2. Inspect the parsed file parameter.

**Expected result:** Host-produced nulls in optional fields are accepted and the parameter retains its file type.

### editing the installed script path clears its template origin

**Steps**

1. Update a profile with its original installed template path.
2. Update the same profile with a different script path.

**Expected result:** The original path preserves trusted template origin; an edited path clears it.

## Filesystem path field tests — tests/electron/path-field.test.cjs

### path drop accepts one File and resolves its path without reading contents

**Steps**

1. Provide one dropped File-like object to the path-drop model.
2. Resolve it through a fake host path resolver.
3. Inspect the resolved item and returned path.

**Expected result:** The resolver receives the File object and the model returns its local path without reading file contents.

### editable path input preserves pasted text and Browse applies the selected path

**Steps**

1. Apply a pasted path through the input-change model.
2. Apply a path returned by Browse.
3. Cancel a second Browse request.

**Expected result:** Pasted and selected paths are applied in order; canceling Browse leaves the current value unchanged.

### Script Runner uses file and directory PathFields with the matching picker kind

**Steps**

1. Read the Script Runner view source.
2. Inspect the script-path and working-directory PathField kinds.
3. Inspect the template file/directory parameter path and picker wiring.

**Expected result:** Script paths use file pickers, working directories use directory pickers, and template path parameters request the matching picker kind.

### Firebird Target database PathField is rendered on its library card before Details or installation

**Steps**

1. Read the Script Runner view source.
2. Find the Firebird card's target database PathField.
3. Inspect its value binding and change handler.

**Expected result:** The target database field is rendered directly on the library card and writes its value into template state before Details or installation.

### path drop rejects multiple files, unsupported payloads, and browser-preview paths accessibly

**Steps**

1. Drop multiple files, an unsupported File-like object, a file with no resolved local path, and a file whose resolver throws.
2. Inspect each returned error message.

**Expected result:** Invalid drops return clear accessible errors that explain one-item-at-a-time, unsupported paths, and the Browse-or-paste fallback.

### path field picker accepts only file or directory for active Script Runner and keeps dialog options host-owned

**Steps**

1. Request a file picker and a directory picker for active Script Runner.
2. Inspect the returned titles, dialog properties, and file filter.
3. Request an unsupported kind, another tab slot, and an inactive Script Runner.

**Expected result:** Only file and directory pickers are supported for the active Script Runner; dialog options remain host-owned.

## Electron profile tests — tests/electron/script-runner.test.cjs

### Script Runner remembers saved interpreter defaults and leaves saved profile paths as overrides

**Steps**

1. Start an isolated app and save a Python profile with a custom interpreter path.
2. Attempt to save a PowerShell profile with an invalid script extension, then correct it and save.
3. Save a Bash profile, close and relaunch the app using the same isolated data, and inspect each interpreter default.
4. Edit the first profile and compare its saved override with the shared per-interpreter defaults.

**Expected result:** Valid saved profiles update interpreter defaults; a rejected profile does not. Defaults survive restart, while each saved profile retains its own interpreter-path override.

### Script Runner file picker offers the PowerShell script extension

**Steps**

1. Check that PowerShell is a supported Script Runner interpreter.
2. Request its script file filter.

**Expected result:** The picker offers PowerShell scripts with the ps1 extension.

## Module action bridge test — tests/electron/automation-services.test.cjs

### module actions use declared versions and cancellation stays inside the module scope

**Steps**

1. Create a Script Runner service facade with one declared action and version.
2. Dispatch the declared action with an AbortSignal, then cancel it while the fake bridge response is pending.
3. Resolve the result and try dispatching an undeclared action.

**Expected result:** The request includes the declared version and module ID; cancellation uses the same module scope; undeclared actions are rejected.

## Electron happy-path visibility — tests/electron/tab-happy-path-visibility.test.cjs

### Script Runner profile happy path keeps create save and edit buttons within visible bounds

**Steps**

1. Open Script Runner in an isolated compact host and check New profile before opening the editor.
2. Create an isolated PowerShell script fixture and enter its interpreter, file, and working-directory paths.
3. Check Cancel and Save profile against the viewport and clipping ancestors, then save.
4. Check Run, Edit, and Close editor in their use states; edit the name, save, reopen, and inspect the saved name.

**Expected result:** Required buttons are CSS-visible, fully inside viewport and ancestor content bounds, and uncovered before use. User wheel scrolling reveals editor actions when needed; click auto-scrolling cannot satisfy the check. The renamed profile is saved. Run is checked for reachability but the script is not executed.

### Script Runner Library exposes a primary Add to Script Runner action on the Firebird card

**Steps**

1. Open Script Runner's Library in an isolated Electron host and locate the Firebird template card.
2. Check that Target database is visible, inside the viewport and its clipping ancestors, and unobscured; enter an absolute path under isolated test data.
3. Check Details and Add to Script Runner against their visible bounds; assert the Add action uses the primary button style, then add the profile.
4. Check Run template and confirm the database path remains; close the Library and find the saved profile in Script Runner.
5. Reopen the Library, check the card's Profile added state, open Details, and confirm Run template and the database path remain available.

**Expected result:** The card presents a visually distinct Add to Script Runner action beside Details. The target path stays available through installation and reopening Details for the saved profile. The saved profile appears in Script Runner, and the template is not run.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.

## Additional backend coverage

Script Runner installation, trusted template origin, execution, and persisted profile behavior also have Application specification coverage under tests/Automator.Application.Specs. Run the full suite with npm.cmd run test:dotnet.

## Explorer action UI model — tests/explorer-action-ui.test.mjs

### Explorer mapping extensions normalize without accepting malformed file patterns

**Steps**

1. Normalize mixed-case extensions separated by commas, semicolons, and whitespace, including duplicates.
2. Try empty input, wildcards, multiple dots, and path separators.

**Expected result:** `.fdb` and `.txt` are returned once each; malformed patterns are rejected.

### Explorer launch prepares transient arguments and never mutates the saved profile

**Steps**

1. Prepare a mapped launch containing a Unicode file path with spaces and tokens in two arguments.
2. Edit one prepared argument and inspect the original profile.

**Expected result:** File tokens resolve in a separate argument array; edits leave the saved tokens and arguments unchanged.

### Explorer launch reports stale mappings profiles extensions and invalid absolute paths

**Steps**

1. Prepare a launch with missing mapping, missing profile, a relative path, a mismatched extension, and a profile missing the file token.

**Expected result:** Each case gives a specific error before a form can run.

### Explorer templates prefill only the mapped input and create fresh typed values

**Steps**

1. Prepare a template action with a file input, password, and boolean default.
2. Map the password instead of the file input.

**Expected result:** Only the mapped file receives the selected path; fresh typed defaults are used, and a non-path mapping is rejected.

### Explorer registration warnings retain saved mappings and describe disabled hosts

**Steps**

1. Read registration notices for disabled, failed, and updated outcomes.

**Expected result:** Disabled and error details remain visible; successful registration produces no warning.

## Explorer action forms — tests/electron/explorer-action-ui.test.cjs

### Explorer mapping UI saves edits removes and clears mappings while registration stays disabled in test mode

**Steps**

1. Seed an isolated saved profile containing the file token, open Explorer actions, and add a mapping for mixed-case duplicate extensions.
2. Inspect persisted normalized extensions and disabled registration.
3. Rename and remove the mapping, add another, then remove all mappings.

**Expected result:** CRUD changes persist, duplicates normalize, delete/clear leave no mappings, and test mode keeps registration disabled.

### Explorer launch opens a transient Unicode file form and requires Run after argument edits

**Steps**

1. Launch a saved action through the existing-instance event with a Unicode file path containing spaces.
2. Inspect selected file and resolved arguments; edit and Cancel, then reopen.
3. Edit again and press Run on an isolated echo script; inspect output and saved profile arguments.
4. Remove the selected file and attempt a run, then launch a missing action.

**Expected result:** Opening and Cancel produce no output; Run receives one-run edits without saving them. Missing files and stale actions produce visible errors.

### Explorer template mapping prefills the typed file input and keeps credentials transient on cancel

**Steps**

1. Install the plain Firebird Backup template and map its database input to `.fdb` through the mapping form.
2. Launch it, inspect the selected file and credential defaults, change the password, and press Run with missing required destinations.
3. Fill a destination, Cancel, and reopen; inspect fresh values and saved profile arguments.
4. Cancel, delete its profile, and try the now-stale action.

**Expected result:** The file is prefilled, missing inputs prevent execution, credentials/destinations remain transient, and deleting the profile removes its mapping.

### Explorer registration errors remain visible beside persisted mappings

**Steps**

1. Persist a real isolated mapping.
2. Replace the host's list response with the saved mapping and a simulated registration error.
3. Open Explorer actions and inspect the warning and edit control.

**Expected result:** A registration failure is visible and the saved mapping remains available.

### cold Explorer launch retains a mapped Unicode file until the Script Runner form mounts

**Steps**

1. Save a profile and action in an isolated host, then close it.
2. Relaunch with the action ID and Unicode selected path as separate CLI arguments.
3. Inspect the prefilled form and Cancel.

**Expected result:** The request survives startup and mounting; no execution occurs before Run or on Cancel.

### Firebird Library card path labels address the input on each card

**Steps**

1. Open the Library with the plain Backup and Backup and ZIP cards present.
2. Find Target database by its accessible label inside each card.
3. Compare the input IDs and owning cards.

**Expected result:** Each label resolves exactly one input on its own card and IDs are unique between instances.

### stopping an Explorer template run clears sensitive inputs and preserves editable non-sensitive values

**Steps**

1. Install and map the Firebird Backup template, launch its Explorer form, and fill destinations, username, and a test password.
2. Simulate an in-flight module action whose cancellation rejects the pending request.
3. Press Run and Stop run, then inspect the still-open form.
4. Enter a replacement password and Cancel.

**Expected result:** Stop clears every sensitive template value, retains selected file and non-sensitive inputs, and leaves the form editable for another run.

## Explorer launch bridge — tests/electron/file-explorer-launch.test.cjs

### Explorer command parser preserves one Unicode path and rejects malformed commands

**Steps**

1. Parse the fixed action flag, action ID, separator, and one Unicode path.
2. Try missing arguments, unsafe IDs, relative/rooted paths, and additional selections.

**Expected result:** Only the constrained one-file command is accepted and its path is preserved.

### Explorer action IDs use the backend profile ID contract

**Steps**

1. Parse dotted and maximum-length action IDs through the CLI parser and typed request schema.
2. Try uppercase, punctuation prefixes, and excessive length.

**Expected result:** Both boundaries accept and reject the same ID contract.

### Explorer queue retains cold requests until ready and serializes existing-instance requests

**Steps**

1. Queue a request while delivery is not ready.
2. Queue another and drain when ready.

**Expected result:** Cold requests remain queued and are delivered in order.

### only stable packaged hosts advertise Explorer registration executable

**Steps**

1. Request the registration path from a stable packaged host.
2. Try development, test mode, portable, and temporary hosts.

**Expected result:** Only the stable package supplies the executable path.

### cold and second-instance Explorer launches open Script Runner and preserve the backend

**Steps**

1. Start an isolated cold host with an unmapped action and wait for its visible stale-action error in Script Runner.
2. Subscribe additional listeners and launch a second process with a Unicode path.
3. Unsubscribe a temporary listener twice, deliver another request, and inspect the retained listener and backend process ID.

**Expected result:** Cold delivery reaches the App subscriber, later requests preserve the path and running backend, and temporary unsubscription does not disable remaining listeners.

## Explorer mapping contract — tests/script-runner-template-contract.test.mjs

### Explorer mapping contract normalizes extensions and rejects unsafe or malformed mappings

**Steps**

1. Parse a mapping with duplicate mixed-case extensions and an optional file-parameter key.
2. Try malformed IDs, labels, extensions, parameter keys, and duplicate action IDs.

**Expected result:** Valid extensions normalize and unsafe mapping shapes are rejected.

Backend mapping validation, token/template requirements, file/extension revalidation, transient execution, deletion cleanup, and registry reconciliation are covered by Application and Windows specification projects. Registry reconciliation uses isolated test keys and checks unrelated-key/default-association preservation. Real File Explorer menus on Windows 10 and Windows 11 require manual packaged smoke checks; automated test-mode hosts never register production menu keys.
