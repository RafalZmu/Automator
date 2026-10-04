# Shared Automation Services Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Give current and future Automator tabs one typed, host-owned service context for keyboard input and HTTP requests.

**Architecture:** Put portable event and HTTP DTOs in `Automator.Core`, typed capability contracts and per-module scoping in `Automator.Application`, the single-hook keyboard adapter in `Automator.Windows`, and the shared `HttpClient` adapter in a new reusable `Automator.Infrastructure` project. Route authorized service calls through the .NET backend, existing JSON-RPC process, Electron main/preload allowlists, and a typed React tab facade.

**Tech Stack:** .NET 10 and C#; BCL `HttpClient`; TypeScript 7, Zod, Electron 44, and React 19; existing console-based .NET specifications and Node test runner.

**Spec:** `docs/superpowers/specs/2026-10-02-shared-automation-services-design.md`

## Progress ledger (2026-10-03)

- Tasks 1–5 are implemented. Their targeted .NET specs, RPC fixtures, Electron bridge tests, and TypeScript typecheck pass.
- HTTP failures use a serializable IPC envelope. The preload returns plain validated data; the React facade constructs typed errors in the renderer realm, preserving the normalized category.
- Task 6 is pending independent review and execution of `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle`.

## Global Constraints

- Keyboard events are delivered only while the panel is visible, owns native foreground focus, the requesting tab is active, and no native dialog or key-recording mode owns input.
- Keep one low-level keyboard hook; do not publish typed text or unrelated foreground-app information; do not consume ordinary key input for subscribers.
- Allow only `http` and `https`; use exact normalized module host allowlists, reject non-public targets unless the module explicitly declares local-network access, and revalidate up to five redirects.
- Limit request bodies to 1 MiB, response bodies to 4 MiB, connect attempts to 10 seconds, and requests to 30 seconds by default; cancel work when its module context is revoked.
- Never log request/response bodies, authorization headers, cookies, or secret values; do not persist credentials.
- Ensure JSON-RPC request/response line limits include the maximum HTTP payload after JSON escaping; service payload limits remain 1 MiB request and 4 MiB response.
- Keep Core and Application independent of Win32, Electron, React, and UI frameworks. Keep direct renderer networking and external plugin loading out of scope.
- Keep credential vaults, OAuth, streaming/binary HTTP, WebSocket, raw TCP, browser automation, and general global keylogging out of scope.
- Preserve Tab 1 behavior, settings schema 1, startup registration, logging location, JSON-RPC stdout discipline, and the WinUI rollback build.
- Use test-first development for behavior changes. Run `npm.cmd run verify`, `npm.cmd run package:win`, and `npm.cmd run test:packaged-lifecycle` after implementation.
- Restart desktop builds only through `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Restart-Automator.ps1 -Show`; use isolated test data and preserve production settings.
- This workspace has no Git worktree and must not be initialized for this task; omit commits and review the changed files directly.

## Review Focus

- Hidden panel, wrong active tab, native dialog, or lost native foreground must revoke keyboard delivery — pin with Application lifecycle and Windows adapter tests in Tasks 1 and 3.
- Key-up, repeat, modifier, and system-key translation must remain stable without changing opener/navigation/recording behavior — pin with normalizer and single-hook tests in Task 3.
- Mixed-case/trailing-dot hostnames, IP literals, and DNS-resolved private/loopback/link-local addresses must follow the exact host policy — pin with injected resolver tests in Task 2.
- Redirects to undeclared hosts, unsafe schemes, and redirect loops must fail with normalized policy errors — pin with fake-handler tests in Task 2.
- Cancellation, over-limit request/response bodies, and HTTP work concurrent with launcher commands must not leak secrets or block state handling — pin with bounded-body/log assertions in Tasks 2 and 4.

---

### Task 1: Define portable capabilities and scoped module services

**Files:**
- Create: `src/Automator.Core/Automation/AutomationCapability.cs`
- Create: `src/Automator.Core/Automation/KeyInputEvent.cs`
- Create: `src/Automator.Core/Automation/AutomationHttpContracts.cs`
- Create: `src/Automator.Application/Automation/AutomationServicesContext.cs`
- Create: `src/Automator.Application/Automation/AutomationCapabilityRegistry.cs`
- Create: `src/Automator.Application/Automation/AutomationKeyboardEventHub.cs`
- Create: `src/Automator.Application/Automation/AutomationRequestExecutor.cs`
- Modify: `src/Automator.Application/Launcher/LauncherTabRegistry.cs`
- Modify: `tests/Automator.Application.Specs/Program.cs`

**Interfaces:**
- Core publishes `AutomationCapabilityRequirement(string Id, int Version)`, capability IDs `keyboard.input` and `http.request`, `KeyInputEvent(long Sequence, string Code, int VirtualKey, bool IsDown, bool IsRepeat, AutomationKeyModifiers Modifiers)`, and transport-neutral `AutomationHttpRequest` / `AutomationHttpResult` records.
- The HTTP DTOs are `AutomationHttpRequest(string Uri, string Method, IReadOnlyDictionary<string, string> Headers, string? Body)` and `AutomationHttpResult(int StatusCode, IReadOnlyDictionary<string, string> Headers, string ContentType, string BodyText)`; normalized failures use `AutomationServiceErrorCategory` (`UnsupportedScheme`, `HostNotAllowed`, `NetworkNotAllowed`, `RequestTooLarge`, `ResponseTooLarge`, `TimedOut`, `Canceled`, `TransportFailure`, `InvalidResponse`) plus a credential-free message.
- Application publishes `AutomationModuleDescriptor` with module ID, declared capability requirements, and optional host-only `AutomationHttpPolicy` (`AllowedHosts`, `AllowLocalNetwork`). `IAutomationKeyboardInput.SubscribeAsync(Func<KeyInputEvent, ValueTask> receiver, CancellationToken)` returns an `IAsyncDisposable`; `IAutomationHttpClient.SendAsync(AutomationHttpRequest, CancellationToken)` returns `Task<AutomationHttpResult>`.
- `AutomationServicesContext` exposes named nullable `KeyboardInput` and `Http` interfaces only when granted; `AutomationCapabilityRegistry.CreateContext(...)` rejects unknown IDs and unsupported versions.
- `AutomationKeyboardEventHub.SetDeliveryContext(KeyboardDeliveryState)`, `Publish(KeyInputEvent)`, and `SubscribeAsync(moduleId, receiver, cancellationToken)` own subscriptions and gate delivery on trusted host state. `KeyboardDeliveryState` carries module ID, visible, native-foreground, renderer-focused, native-dialog, and key-recording flags. `AutomationRequestExecutor.ExecuteAsync<T>(string moduleId, string requestId, CancellationToken contextToken, Func<CancellationToken, Task<T>> operation)` tracks concurrent service work separately from `SerializedCommandQueue`, with `Cancel(string moduleId, string requestId)`, `CancelModule(string moduleId)`, and `CancelAll()` lifecycle operations.
- Extend `LauncherTabMetadata` with declared capability ID/version entries. Keep host allowlists and local-network grants inside the backend-owned descriptor; never serialize them to renderer state. Existing tabs declare no services in this release. Increment the tab registry version to 2.

- [x] **Step 1: Add failing Application specs** for unknown capability rejection, unsupported version rejection, only granted typed services being available, keyboard delivery gated by active/visible/focused/no-dialog/no-recording state, subscriber cleanup on revocation/overflow, request/module cancellation, and a blocked service task not blocking a launcher command queued in `SerializedCommandQueue`.
- [x] **Step 2: Run** `dotnet run --project tests/Automator.Application.Specs/Automator.Application.Specs.csproj --no-restore`; expect the new checks to fail because the descriptor/context API is absent.
- [x] **Step 3: Implement** the Core DTOs and Application contracts/context/registry, event hub, and independent request executor in the files above. Make context disposal idempotent and cancel its lifetime token before disposing subscriptions.
- [x] **Step 4: Extend** launcher metadata and registry specs to assert version 2 and an empty capability list for the existing launcher and reserved slots.
- [x] **Step 5: Re-run** the Application specs; expect all checks to pass.

### Task 2: Add the reusable shared HTTP infrastructure service

**Files:**
- Create: `src/Automator.Infrastructure/Automator.Infrastructure.csproj`
- Create: `src/Automator.Infrastructure/Automation/SharedHttpService.cs`
- Create: `src/Automator.Infrastructure/Automation/HostAddressResolver.cs`
- Create: `tests/Automator.Infrastructure.Specs/Automator.Infrastructure.Specs.csproj`
- Create: `tests/Automator.Infrastructure.Specs/Program.cs`
- Modify: `Automator.sln`
- Modify: `src/Automator.Backend/Automator.Backend.csproj`
- Modify: `package.json`

**Interfaces:**
- `SharedHttpService` is one host-owned facade over two long-lived `HttpClient` instances and connection pools partitioned by `AllowLocalNetwork`. `SocketsHttpHandler` pools by origin, so a public-only module cannot share an origin pool with a local-network-enabled module. Production handlers set `UseProxy = false` and `UseCookies = false`; module-scoped service adapters share the facade.
- `IHostAddressResolver.ResolveAsync(string host, CancellationToken)` returns `Task<IReadOnlyList<IPAddress>>` for production DNS policy checks and deterministic fake-address tests without public network calls. Configure the shared production handler with a 10-second connect timeout and a 30-second request timeout.
- Construct `SharedHttpService(HttpClient publicNetworkClient, HttpClient localNetworkClient, IHostAddressResolver resolver, IApplicationLog log)` once in the backend. Configure each production handler with automatic redirects disabled and a 10-second connection timeout. Direct connections keep the validated destination policy at the socket boundary instead of delegating routing to a system proxy.
- Each scoped adapter receives an `AutomationHttpPolicy` containing exact normalized hostnames and `AllowLocalNetwork`; the shared handler validates the resolved destination before connecting and preserves the URI hostname for TLS and the HTTP Host value. Unsupported schemes, disallowed hosts/addresses, size limits, timeout, cancellation, invalid text responses, and network failures map to normalized `AutomationServiceErrorCategory` values.
- Update `test:dotnet` to run the new infrastructure specs.

- [x] **Step 1: Add failing Infrastructure specs** for method/header/body mapping, non-2xx responses, exact host matching, unsafe schemes, request body limit, bounded response reading, timeout/cancellation, five-redirect cap, cross-host redirect rejection, DNS address policy, and redacted logs.
- [x] **Step 2: Run** `dotnet run --project tests/Automator.Infrastructure.Specs/Automator.Infrastructure.Specs.csproj`; expect build failure because the new project and API do not exist.
- [x] **Step 3: Implement** an injected-handler-friendly service that disables automatic redirects, checks every hop, bounds bytes while streaming, returns only textual content, applies the 30-second default timeout, and never logs body or credential data.
- [x] **Step 4: Re-run** the Infrastructure specs; expect all fake-handler and fake-resolver checks to pass without internet access.
- [x] **Step 5: Add** the Infrastructure project to the solution and backend references, and add its spec runner to `package.json`'s `test:dotnet` command.

### Task 3: Publish scoped normalized key events from the existing hook

**Files:**
- Create: `src/Automator.Windows/KeyboardInputNormalizer.cs`
- Modify: `src/Automator.Windows/KeyboardHookService.cs`
- Modify: `tests/Automator.Windows.Specs/Program.cs`

**Interfaces:**
- `KeyboardInputNormalizer.Normalize(uint virtualKey, uint scanCode, uint flags, bool isDown, bool isRepeat, long sequence)` converts low-level data into the Core `KeyInputEvent` stable-code/modifier model.
- The existing `KeyboardHookService` remains the sole low-level hook owner and exposes a non-blocking source for Application subscribers. Subscriber queues are bounded at 256 events; on overflow the host logs and detaches that subscriber rather than blocking the native hook callback. Physical modifier tracking is cleared on hook reset/stop, not on a tab focus change.
- Input delivery is enabled only for the active service context while the panel is visible, native foreground, renderer-focused, and not in a native dialog or recording mode. Context/tab/window changes revoke delivery; ordinary key input remains unconsumed.

- [x] **Step 1: Add failing Windows specs** for `KeyA`/`Digit1`/left-right modifier normalization, key-down/up, repeat, system-key messages, and unchanged opener/navigation/recording decisions. Application gating, revocation, and overflow behavior is pinned in Task 1's event-hub specs.
- [x] **Step 2: Run** `dotnet run --project tests/Automator.Windows.Specs/Automator.Windows.Specs.csproj --no-restore`; expect the new normalizer/subscription checks to fail.
- [x] **Step 3: Implement** normalization and bounded, asynchronous subscriber delivery using the existing hook callback; isolate subscriber failures and log them without interrupting launcher actions.
- [x] **Step 4: Re-run** Windows specs; expect all event and existing hook behavior checks to pass.

### Task 4: Wire service lifecycle and protocol through the backend

**Files:**
- Modify: `src/Automator.Protocol/RpcProtocol.cs`
- Modify: `src/Automator.Backend/BackendServer.cs`
- Modify: `tests/Automator.Protocol.Specs/Program.cs`
- Modify: `tests/Automator.Application.Specs/Program.cs` only if Task 1's request-executor checks require backend lifecycle cases

**Interfaces:**
- Add typed JSON-RPC methods `automation/keyboardEligibility({ moduleId })`, `automation/keyboardSubscribe({ moduleId })`, `automation/keyboardUnsubscribe({ moduleId })`, `automation/httpRequest({ moduleId, requestId, request })`, and `automation/httpCancel({ moduleId, requestId })`, with strict request/result DTO validation.
- Add `automation/keyboardInput({ moduleId, event })` and revisioned `automation/keyboardAvailability({ moduleId, eligible, revision })` notifications. Bump the protocol version to 2 because capability metadata and service contracts are now required protocol fields.
- Set bounded JSON-RPC request and response line limits high enough for escaped 1 MiB request and 4 MiB response bodies; add a fixture with control characters to prove the request and response stay inside those limits.
- Compose the shared HTTP client and capability registry once per backend lifetime. Resolve authorization from the registered module descriptor and backend-owned active-tab/window context; never trust a renderer-provided focus assertion.
- Route long-running HTTP requests outside `SerializedCommandQueue`; keep JSON-RPC writes serialized by the existing stdout lock. Track per-module cancellation and cancel subscriptions/requests on tab change, non-launcher views, panel hide, context disposal, and backend shutdown. Allow unsubscribe/cancel for the selected granted module without the focus gate, and return idempotent cleanup results after context revocation.

- [x] **Step 1: Add failing C# and TypeScript shared fixtures** for valid subscribe/request/cancel operations plus malformed payload, inactive module, undeclared capability, and protocol-version mismatch cases.
- [x] **Step 2: Run** `dotnet run --project tests/Automator.Protocol.Specs/Automator.Protocol.Specs.csproj --no-restore` and `node --experimental-strip-types --test tests/rpc-contracts.test.mjs`; expect new fixtures to fail validation.
- [x] **Step 3: Implement** C# method constants/DTOs/validators and backend composition, authorization, request cancellation, notification forwarding, and concurrent service dispatch.
- [x] **Step 4: Extend** the backend/Application lifecycle checks so tab switch, non-launcher view, hidden panel, loss of native foreground, and backend shutdown revoke keyboard subscriptions and cancel pending HTTP work through the Task 1 event hub/request executor. Verify cleanup calls remain authorized for the selected granted module while unfocused and become idempotent after revocation. Publish revisioned keyboard availability and support a snapshot query for focus regain. The request-executor spec proves a blocked service task does not block a launcher command.
- [x] **Step 5: Run** the Protocol and Application specs again; expect valid requests to round-trip, invalid requests to be rejected, and lifecycle checks to pass.

### Task 5: Expose the typed service facade through Electron and React

**Files:**
- Modify: `contracts/rpc.ts`
- Modify: `contracts/rpc-contract-fixtures.json`
- Modify: `electron/main.ts`
- Modify: `electron/preload.ts`
- Modify: `ui/types.d.ts`
- Create: `ui/automationServices.ts`
- Modify: `ui/viewRegistry.tsx`
- Modify: `ui/App.tsx`
- Modify: `ui/bridge.ts`
- Modify: `tests/rpc-contracts.test.mjs`
- Modify: `tests/electron/backend-process.test.cjs`
- Create: `tests/electron/automation-services.test.cjs`

**Interfaces:**
- `AutomatorBridge` exposes only typed service operations and listener registrations; preload owns all channel names and returns unsubscribe functions. HTTP requests return a serializable `{ ok, result | error }` envelope; preload validates and returns plain data, and the React facade creates `AutomationServiceError` in the renderer realm because thrown errors do not preserve custom properties across Electron IPC/contextBridge boundaries. Do not expose `ipcRenderer` or a generic channel/method invocation API.
- Add bridge operations `getAutomationKeyboardEligibility(moduleId)`, `subscribeAutomationKeyboard(moduleId)`, `unsubscribeAutomationKeyboard(moduleId)`, `automationHttpRequest(moduleId, requestId, request)`, `cancelAutomationHttpRequest(moduleId, requestId)`, and availability/input listeners that return unsubscribe functions.
- `createAutomationServices(moduleId, bridge, capabilities)` returns named `keyboard.subscribe(handler)` and `http.request(request, { signal? })` methods for a tab. It creates request IDs, maps `AbortSignal` to `automation/httpCancel`, and binds the module ID; backend authorization remains authoritative. The React owner creates/disposes it in an effect, installs availability/input listeners before the eligibility snapshot, and uses revisions to ignore stale snapshots during focus transitions.
- `viewRegistry.tsx` requires the typed service facade in active module props, and App holds module view rendering until the keyed effect installs it. Browser preview provides no-op keyboard subscription and a clear unsupported-HTTP error without direct networking.
- Zod validates service request/result/notification DTOs and capability metadata; C# and TypeScript consume the same protocol fixtures.

- [x] **Step 1: Add failing bridge tests** for main-frame allowlisting, active-module checks, typed request forwarding, keyboard listener unsubscribe, abort-to-cancel mapping, malformed notification rejection, preview behavior, and normalized HTTP error categories through the serializable envelope.
- [x] **Step 2: Run** `node --experimental-strip-types --test tests/rpc-contracts.test.mjs` and the new Electron service test; expect failures because service methods/facade are absent.
- [x] **Step 3: Implement** the bridge, preload, main-process validation, typed React facade, and module prop wiring without changing visible Tab 1 UI. Main returns HTTP failures as structured-clone-safe envelopes; the renderer facade reconstructs typed errors after preload/contextBridge transfer.
- [x] **Step 4: Re-run** the RPC fixture and Electron service tests; expect allowed requests to route, disallowed requests to reject, and all subscriptions to detach cleanly.
- [x] **Step 5: Run** `npm.cmd run typecheck`; expect no TypeScript contract or renderer errors.

### Task 6: Verify the complete package and runtime lifecycle

**Files:**
- Modify only files required by verification failures; do not change production settings or add service UI.

- [ ] **Step 1: Run** `npm.cmd run verify`; require typecheck, renderer/host/backend build, contract tests, Electron tests, and all .NET specs to exit successfully.
- [ ] **Step 2: Run** `npm.cmd run package:win`; require a complete Windows x64 package at `artifacts\electron-dist\win-unpacked\Automator.exe`.
- [ ] **Step 3: Run** `npm.cmd run test:packaged-lifecycle`; require the packaged single-instance and isolated-backend lifecycle checks to pass.
- [ ] **Step 4: Inspect** the final changed files against the design spec and confirm no production schema-1 settings or visible Tab 1 behavior changed.

## Plan Decisions to Review

- Use a separate `Automator.Infrastructure` project so shared HTTP and future reusable connection adapters do not become tab-owned code or pollute portable Core/Application contracts.
- Future connections such as Bluetooth can add their own versioned typed capability, portable contract, host/platform adapter, policy, and renderer bridge while reusing module scoping and lifecycle; no Bluetooth implementation is included here.
- Keep all current tabs' capability grants empty. The reusable APIs and test fixtures ship first; a future tab explicitly declares the keyboard/HTTP capability it consumes.
- Bound each keyboard subscriber queue at 256 events and detach a subscriber on overflow. This protects the native hook callback; the cost if too small is that a slow subscriber must resubscribe after a burst.
- Bump the protocol version to 2 and tab registry version to 2 because capability metadata becomes required and both ends ship together.
