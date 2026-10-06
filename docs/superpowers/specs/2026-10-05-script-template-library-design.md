# Script Template Library Design

## Goal

Let users discover trusted, ready-made scripts, fill in typed inputs, and install them as ordinary Script Runner profiles without writing or pasting script code. Provide reusable file and directory path fields with Browse, text paste, and Explorer drag-and-drop support.

## Current foundations

- Script Runner profiles are stored in the shared automation library by `ScriptRunnerModule` and contain an absolute script path, interpreter, ordered arguments, working directory, output mode, and timeout.
- Profile arguments are passed individually to the process API. Keep this contract; do not concatenate shell commands.
- Workflows and Scheduler refer to saved profiles, so installed templates must become normal profiles and preserve current scheduled/workflow behavior.
- Script files are not embedded in profile records. Settings transfer can warn about missing local paths, so installation must place a template's script asset in a stable user-data location.
- Existing native file and folder pickers use a restricted Electron preload bridge. There is no shared path-field component or Explorer drop support yet.
- Script execution runs as the signed-in user and is not sandboxed. Library entries must be treated as executable code and shown with a clear trust notice.

## User experience

Add a searchable Library view within Script Runner, separate from the user's installed profile list. Search matches template name, description, and tags. A template detail view shows its purpose, interpreter, requirements, inputs, and the source/release version. Installing a template copies its packaged script asset to Automator's per-user data directory and creates a normal editable saved profile. Reinstalling an already installed template updates neither the user's profile nor script automatically; offer an explicit update path only in a later version.

When the user chooses a template, render its declared inputs as a form. Required inputs and defaults are validated before running. On Run, map the form values into the template's declared argument positions and dispatch a transient argument override to `runProfile`; do not persist the entered run values into the saved profile. The installed profile remains usable by Workflows and Scheduler with its normal saved arguments. A template run shows the same bounded result and cancellation behavior as a regular script run.

For file and directory values, use shared `PathField` controls with editable text, Browse, and drop affordances. A text path can be pasted directly. Drop accepts exactly one filesystem item and resolves its OS path through a narrow preload method backed by Electron `webUtils.getPathForFile`; it must not read file contents. Reuse the control for Script Runner's script path and working directory, and use it for template file inputs. Keep existing caller/tab authorization and native-dialog ownership checks. Future tabs can adopt this shared control when they add path fields.

## Template and parameter model

Templates are bundled, versioned catalog data and script assets for the initial release; no remote marketplace, user-authored template importer, or arbitrary plugin loading is included. Each catalog entry declares a stable ID/version, display name, description, tags, interpreter, script asset, working-directory policy, output mode, timeout, and ordered parameters. A parameter declares a stable key, display label/help, type (`text`, `file`, `directory`, `boolean`, or constrained choice), required/default behavior, and an argument mapping. File/directory arguments remain one process argument even when their paths contain spaces.

Validate catalog data before display and validate all run input again in the application layer. Renderer input can select a known template/profile and provide values only; it cannot supply script text, executable paths, arbitrary argument templates, or broaden capabilities. Use existing argument count and size limits. Do not interpolate commands or perform shell quoting. On install, create the script asset with a safe, stable name under the app's per-user data directory; never overwrite a user-modified asset silently. Record the originating template ID and version as profile metadata where the current persisted profile schema safely supports optional fields, preserving older records.

## Firebird example

The first example template is “Firebird 3 database backup and ZIP”. Its form should ask for the `.fdb` database file and output backup/ZIP destinations, and identify the required Firebird `gbak` executable or installation. Include a username input defaulting to `SYSDBA` and a masked password input defaulting to `masterkey`, as requested for the initial version. The password is a transient run value: do not copy it into the installed profile, script asset, persisted template defaults, or logs. The script must fail with a clear message when the database, tool, or output path is invalid; it must not delete or replace an existing backup without explicit behavior and confirmation. It should create a Firebird backup first, then ZIP that backup, and report success only after both steps complete. The Firebird credentials will be passed to `gbak` for that run; the user accepts that a local process may inspect the command line while it is running. Keep credentials out of diagnostic output and clear transient renderer state when the run completes or is canceled.

## Persistence and transfer

Installed profiles continue to use the existing shared automation profile collection and remain editable/deletable. Bundled catalog definitions are app resources, not settings data, and are not exported. Profile argument values and persisted template defaults must never contain credentials; the `SYSDBA`/`masterkey` values are transient UI defaults only. Because template scripts are stored locally, export/import behavior must clearly report if a referenced installed asset is missing; do not silently drop the profile. Preserve legacy profile loading and existing workflow/schedule references.

## Security and errors

- Show the existing trusted-code warning for template runs and identify the source/version.
- Do not run code on template selection or installation; the user must deliberately choose Run.
- Do not log arguments, file contents, stdout/stderr, or secret values. The default Firebird password is included only in the transient template form and process invocation, never in stored profiles or settings. Command-line visibility to local processes during execution is an accepted limitation for this initial template.
- Reject malformed template data, unknown parameter types, duplicate IDs/keys, excessive lengths, missing required inputs, and invalid argument mappings before dispatch.
- A dropped path is user-provided data, not permission to read the file. Validate the path at the execution boundary and show a recoverable missing/invalid path error.
- Keep script output bounded, cancellation, timeout, and process cleanup as implemented by Script Runner.

## Initial scope and follow-up

Initial scope is the bundled searchable catalog, install-as-profile flow, typed parameter form, transient parameterized run, shared file/directory path field, and one Firebird backup/ZIP template using `SYSDBA`/`masterkey` as transient defaults. Defer remote catalogs, community imports, updating installed assets, persisted credentials, generic cross-tab form schemas, and converting every existing path input in every tab. The shared path control is designed for reuse, and later tabs can migrate their fields as focused changes.

## Acceptance checks

1. Search finds a bundled template by name, description, and tags; installing it creates one normal saved profile and safely stages its script asset.
2. Editing/running the installed profile, and invoking it from existing Workflows/Scheduler flows, retains current behavior.
3. A template form enforces required/type constraints and maps a file path containing spaces to one process argument without shell interpolation.
4. Template input values are transient; running does not overwrite the installed profile's saved defaults.
5. Browse, direct text paste, and dropping one Explorer file populate the path field. Multiple dropped files and unsupported payloads are rejected with a clear message.
6. Renderer cannot choose arbitrary catalog asset paths, executable paths, native dialog options, or unregistered templates.
7. Legacy profiles load and missing installed script assets produce a repairable error.
8. Firebird backup failure prevents ZIP creation; successful backup is zipped, existing output is handled explicitly, the form defaults to `SYSDBA` / `masterkey`, and credentials are absent from persisted data and logs.

## Implementation boundaries

- Script Runner renderer: searchable library, template detail/form, shared path field, install and transient run interaction.
- Script Runner contracts and application module: catalog metadata, parameter validation, install metadata, argument override validation, and profile compatibility.
- Electron preload/main: narrowly scoped dropped-file path extraction and safe native picker reuse, preserving caller authorization.
- Bundled template resources and per-user installation: validated, versioned scripts and stable installed paths.
- Script Runner and Electron/Application specifications: contract validation, serialization compatibility, no-shell argument preservation, path-field interactions, authorization, and failure cases.
- Script Runner `AGENTS.md` and `README.md`: document installed templates, parameter behavior, path input, and safety constraints with any behavior change.
