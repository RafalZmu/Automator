# Shared Automation Services Design

Date: 2026-10-02
Status: implemented; targeted verification passed; full package verification awaits independent review.

Implementation status (2026-10-03): Tasks 1–5 are implemented and their targeted specs, Electron bridge tests, RPC contract tests, and TypeScript typecheck pass. The full workspace verification, Windows package build, and packaged lifecycle run remain pending.

## Purpose

Let current and future Automator tabs reuse host capabilities through a stable typed context. The first shared capabilities are keyboard input and HTTP requests. Each capability has one host-owned implementation, a policy boundary, and adapters for the .NET backend and React renderer.

The goal is to avoid per-tab native hooks, HTTP client configuration, protocol plumbing, and inconsistent error handling. New service types should fit the same capability registration and lifecycle model without exposing arbitrary process or OS access to a tab.

## Current state

- `Automator.Core` holds portable rules and the small `IAutomationTab` identity contract. The interface does not yet provide services or lifecycle operations.
- `Automator.Application` owns launcher state and a registry of tab metadata. `ILauncherTabModuleProvider` returns metadata and initial serializable state; it does not receive a service context.
- `Automator.Windows.KeyboardHookService` owns one low-level keyboard hook and message-pump thread. It currently emits only launcher-specific opener, navigation, and hotkey-recording events. Arbitrary key events are passed through and are not published as a tab service.
- `Automator.Backend.BackendServer` constructs the keyboard hook directly and handles a fixed set of JSON-RPC commands. The renderer uses an allowlisted Electron preload bridge and has no general automation-service facade.
- Slots 2–9 are reserved. External plugin loading and script execution remain deferred.

## Decision

Use one **host-owned, typed automation service context** with capability declaration and lifecycle scoping.

The recommended alternatives considered were:

1. **Typed host service context (recommended).** The host constructs shared implementations, grants typed services to a module from its descriptor, and exposes the same contracts through the backend protocol and typed React bridge. This centralizes resource ownership, focus rules, limits, cancellation, diagnostics, and future extension points.
2. **Per-tab service helpers.** Every module implements its own HTTP and keyboard plumbing. This has a small start-up cost but duplicates native resource ownership and security/error policy, and cannot safely give every tab its own global hook.
3. **Dynamic service bus.** Modules look up services by arbitrary string names and submit untyped JSON commands. It is flexible at first, but removes compile-time checks from a cross-process boundary and makes module authorization and versioning harder to audit.

The service context is a capability set, not a general service locator. A module declares the service IDs it needs; the registry validates those declarations, and the host binds the declared typed adapters when that module is active. Unknown capabilities fail registration with a clear diagnostic.

## Layering and ownership

- **Core:** portable capability identifiers and request/result/event DTOs. No Win32 handles, Electron types, network clients, or UI framework references.
- **Application:** module service requirements, typed service interfaces, active-module context, subscription lifecycle, capability policy, and serialization-safe dispatch contracts.
- **Windows:** adapts the existing single keyboard hook to the portable key-event source. It remains the only system keyboard hook.
- **Shared infrastructure:** owns one long-lived `SharedHttpService` facade and implements the typed HTTP service. The facade owns two long-lived .NET `HttpClient` instances and connection pools, partitioned by `AllowLocalNetwork`; `SocketsHttpHandler` pools by origin, so a public-only module must not reuse a socket opened under a local-network policy. Production handlers disable system proxy routing (`UseProxy = false`) and cookies (`UseCookies = false`). There are no per-tab clients, extra package dependencies, or unbounded connection lifetimes.
- **Backend:** composes services and dispatches authorized service operations over the existing versioned JSON-RPC process channel. Protocol reader/state publication remains responsive while an HTTP request is pending; HTTP work must not hold the serialized launcher-state command queue.
- **Electron main and preload:** preserve the existing allowlist boundary, validate renderer requests, forward only typed backend operations, and expose typed event subscriptions with unsubscribe functions. Renderer code never receives `ipcRenderer`, .NET process access, or raw backend stdin/stdout.
- **React tabs:** obtain a typed `AutomationServices` facade from the active tab context. A view uses that facade instead of implementing a native hook or choosing/configuring its own transport.

No external plugin loader is added. The first consumer of the API can be an in-repository module when a later tab is implemented.

## Capability contracts

### Keyboard input

One normalized `KeyInputEvent` describes a key-down or key-up, a layout-neutral key code, the Windows virtual-key value, modifier flags, repeat state, and a monotonically increasing sequence number. It contains no typed text, window title, or unrelated foreground-app data.

A tab explicitly subscribes to the key event stream while eligible. The host delivers events only when the launcher panel is visible, the panel owns native foreground focus, that tab is selected, and no native dialog or key-recording mode owns input. Visibility and focus come from host/native window lifecycle state; the renderer cannot assert them in a service request. Switching tabs, hiding the panel, losing native foreground, entering another view, restarting the backend, or disposing the context revokes the subscription. The service does not buffer events for inactive tabs. Host modifier tracking is physical keyboard state and is reset when the hook stops or resets, not on a tab focus change. The normalized key code uses stable names such as `KeyA`, `Digit1`, and `ShiftRight`, with the Windows virtual-key value retained for platform-specific consumers.

The backend sends `automation/keyboardAvailability({ moduleId, eligible, revision })` after changing the eligible delivery state and supports `automation/keyboardEligibility({ moduleId })` for a snapshot. The renderer installs availability and input listeners before requesting the snapshot, then ignores a snapshot whose revision is older than a notification already received. This lets a tab detach on blur and resubscribe after focus returns without missing a focus edge during startup.

The existing opener toggle, numeric tab navigation, catalog shortcut, and settings key recording keep their current precedence and behavior. The hook never consumes ordinary key events on behalf of a subscriber; it consumes only the existing launcher actions. A later need for a shortcut while another application is foregrounded must use a separate explicit chord-registration capability, not a stream of all keystrokes. That capability is not part of this service release.

Native callbacks perform bounded, non-blocking publication. They do not call React or wait on the protocol writer. Delivery to the active module is serialized separately from the global hook callback.

### HTTP requests

One shared HTTP service accepts a typed request containing an absolute URI, method, explicit headers, optional textual/JSON body, and cancellation. It returns the status code, response headers selected for exposure, content type, and response text. Non-2xx responses are ordinary HTTP results for the caller to inspect; transport, policy, timeout, cancellation, and size failures use normalized service errors.

Initial limits are:

- Allow only `http` and `https` schemes. Each module declares an exact normalized host allowlist; broad wildcard patterns are not supported in the first release. Private, loopback, link-local, and otherwise non-public IP targets (including targets reached through DNS) are denied unless that module explicitly declares local-network access. A local-network declaration does not bypass the module's host allowlist.
- Maximum request body: 1 MiB. Maximum response body: 4 MiB. Reject oversized bodies before passing them to a renderer.
- Default request timeout: 30 seconds, bounded by the caller's cancellation token. Cancel outstanding requests when the owning module context is revoked.
- Do not log request or response bodies, authorization headers, cookies, or secret values. Logs may include module ID, host, method, duration, status, and normalized error category.
- Do not persist credentials or introduce a secret store in this release. Callers may provide request-scoped headers; a future credential capability can own secure storage separately.

Automatic redirects are disabled. The service may follow at most five redirects itself, revalidating scheme, normalized host allowlist, and network-address policy at every hop; a hop to a host not declared by the module fails with a policy error. A response remains textual in this release; streaming, arbitrary binary transfer, WebSocket, and custom TCP services are future capability implementations.

## Module lifecycle and API flow

1. Each bundled module descriptor lists versioned capabilities (for example `keyboard.input` and `http.request`) and service-specific policy (for example allowed hosts).
2. The host validates the descriptor against the service registry and creates an `AutomationServices` context scoped to that module.
3. Selecting a module activates its context. React uses a typed facade backed by preload methods/events; a future .NET module can receive the corresponding typed interfaces directly.
4. The backend checks that each subscription/request module ID and capability match the currently selected tab and host-reported visible/focused state before dispatch. It rejects service work from an inactive tab, undeclared capability, old protocol version, malformed request, or oversized payload. Cleanup calls for a selected module with the declared capability may unsubscribe or cancel while the panel is unfocused; after context revocation they return idempotent cleanup results.
5. On tab change, non-launcher view, panel hide, focus loss, or shutdown, the context cancels pending work and revokes event subscriptions. Availability revisions tell a live facade to resubscribe on regain; a later module activation creates a fresh context.

Service-specific interfaces live in the Application contract layer; concrete services live in their owning adapter/infrastructure project. The context exposes named typed interfaces, not a `GetService(string)` method. Adding a new connection type means defining its DTO/interface, implementation, capability ID/policy, protocol and preload adapter, and tests once; consuming tabs then call the shared typed API.

## Compatibility

- Keep the current launcher tab, persisted settings schema version 1, startup registration, logging location, and JSON-RPC stdout discipline unchanged.
- Preserve the current one-hook design and all Right Ctrl, opener-recording, navigation, alias, focus, and tab-selection behavior.
- Increment the protocol/tab registry version when adding required fields or incompatible behavior. Optional notifications must be ignored safely by older renderer versions during a failed or partial startup.
- Keep the WinUI rollback project buildable; the new service abstractions must not add React/Electron references to Core or Application.

## Error handling and observability

Every service request has an ID and module ID. Input validation errors are returned before doing OS or network work. HTTP timeout, cancellation, disallowed host, unsupported scheme, body-size limit, and network failure are distinguishable to the UI but never include request credentials in logs. Keyboard subscriber failures are isolated from the hook; a bad tab callback is logged and that subscriber is detached without breaking launcher controls.

HTTP failures cross Electron IPC in a serializable `{ ok, result | error }` envelope because rejected `ipcMain.handle` errors retain only their message. The preload validates and returns the plain envelope; the React service facade constructs `AutomationServiceError` in the renderer realm so its category remains available to tab code.

Pending HTTP requests are canceled when their context is disposed and during backend shutdown. The HTTP client has explicit connect/request limits so disposal cannot leave the backend or UI waiting indefinitely. The hook remains installed for the host lifetime and publishes no key events when no eligible module subscription exists.

## Verification requirements

- Core/Application specs cover capability validation, active-tab routing, visible/focused gating, tab-switch and shutdown disposal, no delivery to inactive modules, and non-consumption of ordinary key events.
- Windows specs cover normalized key-event translation while preserving opener/navigation/recording behavior and the single-hook lifecycle.
- HTTP tests use a local fake `HttpMessageHandler`, not public network endpoints, and verify method/body/header mapping, non-2xx results, cancellation, timeout, request/response limits, and allowed/denied host policy.
- Shared C#/TypeScript protocol fixtures cover valid and malformed service requests, version mismatches, and service notifications. Electron bridge specs verify allowlisting, typed subscription/unsubscription, rejection when a module is inactive or lacks the capability, and typed HTTP error categories across a structured-clone IPC envelope.
- Run the workspace `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle`. Use `scripts/Restart-Automator.ps1` for desktop restarts and preserve production schema-1 settings during verification.
- Parent visual inspection remains a separate native-window check; renderer screenshots do not prove DWM Acrylic or native corners.

## Out of scope

- New slot-2–9 tab UI or changes to Tab 1 behavior.
- External C# or JavaScript plugin loading, Python/Bash execution, or arbitrary child-process access.
- Credential vault, OAuth flows, persistent secrets, WebSocket, raw TCP, browser automation, or general global keylogging.
- Dynamic service lookup by untyped string, per-tab keyboard hooks, or unrestricted direct network calls from the renderer.
