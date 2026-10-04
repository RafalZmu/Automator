# Bundled automation tabs

Automator tabs are compiled into the .NET backend. The backend owns slot registration, action versions, capability checks, settings migrations, and action dispatch. React renders only the `kind` declared by a registered tab. External assembly loading and renderer-supplied code are not supported.

## Register a tab

Add one `IAutomationModule` provider to `LauncherTabRegistry.CreateProviders`. Its `AutomationModuleDefinition` is the source for the backend runtime registry and the metadata sent to React, so slot, ID, title, view kind, versions, actions, and capabilities do not need a second backend registration. Slot 1 remains the app launcher; slot 2 is Script Runner; slots 3–9 remain reserved.

Module IDs use lowercase stable names such as `report-viewer`. Action IDs use stable names such as `refresh` or `open-report`. Increase an action version when its input or behavior changes incompatibly. Increase `ContractVersion` only when the shared module contract changes. Slots remain numbered 1–9.

Tab 1's existing command routes are marked with `LegacyCommand`. They stay on their current RPC methods and cannot be dispatched through `automation/moduleAction`. New module actions leave `LegacyCommand` unset; the backend then checks the active tab, contract and action versions, and declared capabilities before calling `ExecuteAsync`.

## Add a view

Add the view kind to `contracts/bundled-tab-view-kinds.json` and `ui/viewKinds.ts`, then implement it in `ui/viewRegistry.tsx`. The TypeScript registry is typed against the shared kind union, while Application specs check all production module definitions against the shared JSON contract. A runtime state with an unrecognized kind uses the safe reserved view.

The renderer dispatches through the module-scoped `AutomationServices.modules` facade. It accepts only actions declared by the active tab, supplies their registered versions, creates a request ID, and exposes abort signals. The preload validates the action and cancellation requests; the Electron main process and JSON-RPC backend validate them again before dispatch. Follow-up actions returned in `AutomationResult` contain versioned JSON payloads; the active view passes those values back through the same facade.

## Actions and shared services

`ExecuteAsync` receives JSON action input, the module's JSON preferences, an `AutomationServicesContext`, and a cancellation token. The context contains only host-granted typed services. Add a capability to the module definition before using it, and list it in each action that requires it. Existing keyboard, HTTP, library-storage, and process-execution adapters remain responsible for their own policy and lifecycle checks. The process service launches direct executable paths with individual arguments, bounds stdout/stderr, and kills the process tree on timeout, cancellation, or tab deactivation.

Keep action data and results as JSON values. The host limits action input and follow-up payloads to 64 KiB, result data to 512 KiB, and follow-up actions to 32. Return concise display text and an explicit `AutomationStatus`. Throw for failed operations; cancellation is propagated when the active tab or backend lifetime ends.

## Module preferences

Store preferences as `ModuleSettingsEntry` values: module ID, module schema version, and opaque JSON. Return a fresh default from `CreateDefaultSettings`. When preferences evolve, implement a deterministic migration from older versions directly to the module's current `SettingsVersion`.

The settings serializer validates payload sizes and duplicate IDs, upgrades schema-1 launcher settings while preserving their fields, and keeps entries for unknown modules during load, save, import, and export. A failed known-module migration marks the settings invalid and does not replace the source file. Larger reusable records belong in the versioned SQLite library. Its `module_id` partition is bound by `AutomationServicesContext.Library`, so a module cannot name another module's partition.

## Script Runner (slot 2)

Profiles are persisted in the `script-runner/profiles` SQLite collection. Each profile selects Python or Bash, an interpreter executable, script path, working directory, argument values, output mode, and a 1–3600 second timeout. The service invokes the interpreter directly without shell interpolation. Script output is returned only as a bounded action result for display in the Script Runner view; it is not written to application logs. These are trusted scripts and run with the user's Windows permissions.

## Adding capabilities

Do not add a direct service locator. Add a typed capability ID and adapter to `AutomationCapabilityRegistry`, define the service interface in Application, and authorize it against the active module and host policy. Keep operating-system access in Windows/Infrastructure adapters and expose only the narrow interface required by a module. Long-running jobs and external plugins need a separate design before they are introduced.
