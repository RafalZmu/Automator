# Cross-module productivity features implementation plan

## Goal

Implement the approved productivity improvements in the existing Electron/React renderer and .NET backend while retaining the numbered module slots and the current global opener. The opener presents an unnumbered Quick Actions home view; it does not create a new tab and it does not add a new key binding.

## Existing behavior to preserve

- Slots 1–9 remain unchanged; slot 1 stays the app launcher.
- The configured opener toggles the small window, and opening returns to slot 1.
- Per-module action matching, confirmation requirements, settings import/export, secure API credentials, and module-scoped storage remain intact.
- Playwright already has project/file/test discovery and app-managed tag runs; workflows already have ordered steps and initial JSON; scheduler already targets scripts and workflows; AutomationResult already carries follow-up actions.
- Script/API/browser outputs remain transient. Cross-module run history stores metadata only.

## Workstreams

1. **Quick Actions home:** Add a non-tab default home surface to the opener. Show available app aliases and safe module actions across all registered modules. Typing filters the list; after a short settle delay, exactly one unguarded match runs. Multiple results remain selectable with arrow keys/Enter; confirmation-required actions always wait for explicit confirmation. Preserve slots and make the current per-tab search behavior available from the large workspace as appropriate.
2. **Global variables:** Add a typed global variable collection in Options, accepting JSON values (including arrays/objects), with validation, edit/delete, and import/export. Add nonintrusive variable hints/autocomplete to API request fields, scripts, workflows, and browser configuration only where interpolation is supported. Remove workflow-local variable authoring and migrate saved variables to `workflow.<workflow-id>.<key>` names; no global name silently shadows a credential or system value.
3. **Run activity and result surfaces:** Record bounded metadata for script, API, browser, workflow, and scheduled runs (module/action/profile, timestamps, status, duration). Never persist raw outputs, response bodies, secret values, or script text. Add a searchable activity view in the full workspace, a structured transient result viewer that understands text/JSON/workflow steps and follow-up actions, and an in-app notification center for summaries that navigates to the relevant run/module.
4. **Module refinements:** Retain Playwright's existing file sections/tags and improve discoverability/group execution only where gaps remain. Refine ordered workflow step editing and make global variables visible as hints. Add scheduler agenda/calendar presentation for script and workflow schedules without changing recurrence semantics.
5. **Script follow-up actions:** Surface AutomationResult actions next to structured script results; require a deliberate click to run each action. No result action auto-runs.

## Shared interface decisions

- Keep all host calls behind the existing validated preload/RPC bridge.
- Define versioned, bounded contracts before wiring UI. Use one host-owned variable service and one host-owned metadata-history service rather than exposing another module's library partition.
- Add global services through typed facades/capabilities; do not let renderer modules address other module storage directly.
- Preserve current host notifications and extend them with typed in-app events; notification payloads contain summaries and navigation IDs only.
- Keep module-specific results transient in renderer memory. Persist only explicitly approved metadata.

## Execution order

1. Inspect existing implementations and document the shared contracts and action auto-run safety rules.
2. Implement/test global services and UI components in isolated workstreams; announce cross-module API changes before dependent edits.
3. Integrate the Quick Actions home and route actions across inactive modules without mounting every module view.
4. Complete workspace activity/result/notification surfaces and module refinements.
5. Review cross-module security, accessibility, data migration, and action confirmation behavior.
6. Run `npm.cmd run verify`, package with `npm.cmd run package:win`, run `npm.cmd run test:packaged-lifecycle`, then restart through `scripts/Restart-Automator.ps1 -Show`.

## Acceptance checks

- Opening uses the configured opener, defaults to the unnumbered Quick Actions view, and leaves tabs 1–9 unchanged.
- Quick Actions lists actions across modules, filters as text is entered, auto-runs only one safe unique match after settling, and never bypasses confirmation.
- Global variables round-trip through backup/import, migrate legacy workflow values without collisions, and remain distinguishable from secrets.
- Activity metadata, transient structured output, follow-up actions, and in-app notifications work across supported run types without storing sensitive/raw output.
- Scheduler shows upcoming script/workflow items in agenda/calendar view; existing Playwright tag groups and workflow ordering keep working.
- Repository verification, Windows package, packaged lifecycle, and helper-managed app restart succeed.

## Workspace constraint

The supplied workspace currently has no `.git` metadata, so the work cannot use Git worktrees or commit-based isolation. Keep ownership boundaries narrow, run focused tests per workstream, and review the final combined diff via filesystem/test evidence.
