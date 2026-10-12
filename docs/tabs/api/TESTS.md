# API tab test coverage

This file documents the API tab's request-profile contracts and HTTP service boundary checks. Test titles match the corresponding node:test descriptions. Each case includes the inputs or UI/host actions and the result asserted by the test.

Run the focused profile cases from the repository root with:

    node --experimental-strip-types --test tests/api-view.test.mjs

Run the contract suite with npm.cmd run test:contracts. Electron service-boundary cases are run with npm.cmd run test:electron after npm.cmd run build:desktop. Backend Application and Infrastructure specifications are included in npm.cmd run test:dotnet.

## Profile and request contracts — tests/api-view.test.mjs

### API profile reader keeps only the persisted safe profile fields

**Steps**

1. Provide one valid profile containing an extra secretValue field and one malformed profile.
2. Read the profiles through readApiProfiles.
3. Check the accepted profile against the allowed persisted fields and serialize the result.

**Expected result:** The valid profile is retained without the extra secret field; the malformed profile is dropped and the secret value does not appear in serialized output.

### API profiles derive host policy from the URL and retain profile-local default input

**Steps**

1. Add a JSON default input to a valid profile and validate it.
2. Read it alongside legacy allowedHosts metadata.
3. Read a profile containing an older default-input value.

**Expected result:** URL-based host policy is accepted, legacy host metadata is not retained, and each profile's default JSON input remains available.

### Chrome Copy as cURL parses a Bash request into an unsent profile draft and separates credential headers

**Steps**

1. Parse a Bash-style cURL request with a URL, PUT method, ordinary Accept header, Authorization and Cookie headers, and a JSON body.
2. Inspect the draft fields, including ordinary headers, secure-header names and values, and default input.
3. Try importing a body with an embedded access token.

**Expected result:** The request becomes an unsent draft with the method, URL, and JSON input intact. Credential headers are separated from ordinary headers, and token-bearing input is rejected.

### Chrome Copy as cURL parser accepts Windows cmd quoting and rejects shell substitutions

**Steps**

1. Parse a Windows cmd-quoted cURL request containing a JSON body.
2. Try a request containing a Bash command substitution.
3. Try a request containing an environment-variable substitution.

**Expected result:** Supported Windows quoting produces a POST draft with JSON input; shell substitutions are rejected.

### API request body substitutes input as raw JSON instead of quoted text

**Steps**

1. Build a body template containing the {{input}} placeholder and a typed JSON object.
2. Build a static body without input.
3. Build a request with no template and with or without input.

**Expected result:** Input is inserted as raw JSON, static bodies remain unchanged, and a missing template falls back to serialized input or null.

### API profile validation accepts URL-only host scope and keeps authentication out of ordinary headers

**Steps**

1. Validate a profile whose URL defines the request host.
2. Validate a profile with an explicit wildcard allowed-host entry.
3. Put an Authorization value in ordinary headers and validate again.

**Expected result:** URL-only and explicit host scopes pass; authentication values in ordinary headers are rejected as secret headers.

### API response decoder returns structured JSON and preserves malformed JSON as text

**Steps**

1. Decode valid JSON, malformed JSON, plain text, and the JSON literal null.
2. Inspect body text, structured output, structured-output availability, and parse-failure state.

**Expected result:** Valid JSON is returned as structured output; malformed JSON remains visible as text with a parse-failure flag; text mode preserves text; JSON null is still a successfully parsed structured result.

## HTTP capability and transport checks

### module action requests and structured results enforce version and payload limits

**Source:** tests/rpc-contracts.test.mjs

**Steps**

1. Validate a versioned module-action request and a workspace context with a valid tab.
2. Try a missing request ID, invalid action version, oversized payload, and out-of-range tab.
3. Validate cancellation requests and structured results with unique and duplicate follow-up actions.

**Expected result:** Valid versioned requests and results pass; malformed, oversized, duplicate, and out-of-range values are rejected.

### module settings writes enforce active module identifiers, schema versions and payload limits

**Source:** tests/rpc-contracts.test.mjs

**Steps**

1. Validate a settings update with a valid module ID, contract version, settings version, and value.
2. Try an invalid version, module ID, and oversized value.
3. Validate settings read and write result envelopes.

**Expected result:** Supported identifiers, versions, and bounded payloads pass; invalid or oversized settings are rejected.

### module settings reads and writes require the active version and preserve all other saved settings

**Source:** tests/electron/ui-flow.test.cjs

**Steps**

1. Launch an isolated host with existing launcher preferences, an app binding, and an unknown module setting.
2. Read and write settings for the active module, then try an inactive module and a stale settings version.
3. Read the updated setting and inspect the persisted settings file and recoverable backup.

**Expected result:** Only the active module and current settings version can read or write; the new value persists while unrelated preferences, bindings, and unknown module settings remain intact.

### shared C# and TypeScript request fixtures agree on valid and invalid parameters

**Source:** tests/rpc-contracts.test.mjs

**Steps**

1. Read the shared valid-request fixtures and validate each request.
2. Validate each invalid-request fixture and compare its error code with the expected code.

**Expected result:** TypeScript RPC validation agrees with the shared fixture contract used by the C# host.

### authorization fixtures are well-formed but fail active-module and capability checks

**Source:** tests/rpc-contracts.test.mjs

**Steps**

1. Validate each authorization fixture as a well-formed request.
2. Compare its module ID with the active module and its requested capability with the declared capabilities.

**Expected result:** Request shape validation succeeds, while the expected authorization result follows the active-module and capability grant.

### control-character HTTP payloads fit escaped JSON-RPC request and response line caps

**Source:** tests/rpc-contracts.test.mjs

**Steps**

1. Fill a request body to the supported size using control characters that expand when escaped.
2. Validate and serialize the HTTP request as JSON-RPC.
3. Add one more control character and validate again.

**Expected result:** The maximum supported body fits the encoded request line; the oversized body is rejected.

### cleanup IPC is allowed for a granted selected module while hidden and becomes a no-op after tab revocation

**Source:** tests/electron/automation-services.test.cjs

**Steps**

1. Authorize an HTTP cleanup call while the granted module is selected but its window is hidden.
2. Change the selected tab and issue the same cleanup request.
3. Try cleanup with a capability that is not granted.

**Expected result:** A granted selected module may clean up while hidden; after tab revocation cleanup becomes a no-op; undeclared capabilities are rejected.

### workspace service calls use their own tab and cannot subscribe to global keyboard input

**Source:** tests/electron/automation-services.test.cjs

**Steps**

1. Create a Workspace context whose selected tab is API while the compact launcher has a different selected tab.
2. Authorize an API HTTP call through Workspace.
3. Try a launcher keyboard subscription and an HTTP call for a module not selected in Workspace.

**Expected result:** Workspace API service authorization uses Workspace's own active tab; keyboard input is unavailable to Workspace and inactive modules are rejected.

### typed service IPC requires the main frame and the selected module grant

**Source:** tests/electron/automation-services.test.cjs

**Steps**

1. Create an active module state with a granted service capability.
2. Authorize a matching call from the main frame.
3. Repeat with a subframe, an inactive module, and an undeclared capability.

**Expected result:** Only the main-frame call for the selected module and declared capability is forwarded.

### HTTP AbortSignal cancels the module-scoped request and disposal detaches listeners

**Source:** tests/automation-services.test.mjs

**Steps**

1. Start an HTTP service request through a fake bridge that holds the response open.
2. Abort its AbortController and wait for a cancel request carrying the original module and request IDs.
3. Reject the pending request as canceled and dispose the service facade.

**Expected result:** The canceled error reaches the caller, cancellation remains in the original module scope, and the facade is disposed after the request ends.

### HTTP abort maps to cancel for the generated module request ID

**Source:** tests/electron/automation-services.test.cjs

**Steps**

1. Start an HTTP request through the renderer-facing automation service and record the module and request IDs sent over the bridge.
2. Abort the request while the fake host call is pending.
3. Complete the fake host request with a cancellation result.

**Expected result:** The main-to-renderer service boundary sends cancellation for the matching request ID and returns a canceled service result.

### HTTP service error categories survive the main-to-renderer IPC boundary

**Source:** tests/electron/automation-services.test.cjs

**Steps**

1. Convert backend network-policy and host-policy failures into IPC envelopes.
2. Return each envelope through a fake HTTP bridge request.
3. Inspect the rejected renderer-side errors.

**Expected result:** The renderer receives typed service errors with the original networkNotAllowed and hostNotAllowed categories and messages.

## Electron happy-path visibility — tests/electron/tab-happy-path-visibility.test.cjs

### API profile happy path keeps create save and edit buttons within visible bounds

**Steps**

1. Open API in an isolated compact host and check Create API profile and New profile.
2. Open the editor, check Close editor, and enter a name, key, and HTTPS URL without credentials.
3. Use user wheel scrolling where needed; check Cancel and Save profile against viewport and clipping ancestors before saving.
4. Check Run and Edit on the saved row, rename through the editor, save, reopen, and inspect the saved name.

**Expected result:** Required buttons are CSS-visible, fully inside viewport and ancestor content bounds, and uncovered before use; click auto-scrolling cannot satisfy the check. The edited profile is saved. No HTTP request or secret-store write occurs; Run is checked only for reachability.

Run after npm.cmd run build:desktop with node --experimental-strip-types --test --test-concurrency=1 tests/electron/tab-happy-path-visibility.test.cjs.

## Coverage note

The Electron suite verifies UI profile saving plus the HTTP capability and IPC boundary. API profile contracts verify request construction, parsing, validation, and response decoding. There is no live external-endpoint or credential fixture in the new UI case; request execution and secret-store behavior retain Application and Infrastructure specification coverage.

## Shared numbered-tab icon coverage — tests/electron/tab-happy-path-visibility.test.cjs

### numbered tabs show an identifying icon in the launcher and Workspace

**Source:** tests/electron/tab-happy-path-visibility.test.cjs

**Steps**

1. Launch the isolated compact host and locate slots 1–8 by their exact accessible tab names; verify each contains one visible decorative icon with dimensions of at least 12 by 12 pixels.
2. Open Workspace and verify slots 2–8 each contain one visible decorative icon with dimensions of at least 12 by 12 pixels.

**Expected result:** The numbered tabs retain their accessible names and show one visible icon in both windows.
