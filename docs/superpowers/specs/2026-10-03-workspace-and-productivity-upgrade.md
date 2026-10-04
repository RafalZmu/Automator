# Workspace and Productivity Upgrade

## Goal

Refine Automator's existing Electron/React and .NET architecture around the user's latest product decisions: API profiles own their input defaults; Browser Automation becomes a Playwright test explorer; workflows use saved variables; schedules can target scripts or workflows; the former Pomodoro tab becomes a work-time log; a separate large workspace supports complex editing; and each tab gets keyboard-oriented action search.

## Product behavior

### API (slot 3)

- Move request JSON input from a tab-global textbox into each saved profile. A profile can keep an optional example/default input; a run can override it for that request. No profile shares another profile's input.
- Remove the editable exact-host allowlist from the normal profile editor. A pasted URL is the request target. The host derives the only allowed host from that saved URL and keeps same-host redirect, DNS/private-address, size, timeout, cancellation, and secret-redaction protections. A redirect to another host fails with a clear message.
- Add a **Paste cURL** flow for Chrome's **Copy as cURL** output. Parse the command into an editable unsent draft (method, URL, headers, body); detect cookie/auth headers and route their values only to the credential-vault save flow. Never auto-send imported requests, log imported secrets, or include values in exports.

### Browser Automation (slot 4)

- Recast the tab as a Playwright Test Explorer. The user selects an existing project directory; this keeps agent-authored tests with the code they exercise. Persist the selected root locally and allow changing it.
- Render test files as sections and discovered tests within each file. Discover using the selected project's Playwright Test runner in `--list` mode; run a file, a test, or an app-managed tag group using Playwright test-list entries keyed to exact test locations. Do not rewrite agent-authored test sources to add Automator tags.
- Let the user create a section by creating a new `*.spec.ts`/`*.test.ts` file in the selected project, and create tests in that file from a small starter template. Preserve files and tests created externally by agents.
- Store app-managed tags in the Automator library keyed by normalized project root + relative file + test location/title. Tag group filters live inside this tab; they do not consume slots 8–9. Refreshing test discovery reconciles changed identities and reports stale tag entries for cleanup.
- Resolve the project's own `@playwright/test` runner and version. If it is missing, show setup guidance instead of installing dependencies or modifying the project silently. Execute only after explicit user action. Bound output and run duration, support cancellation/process-tree termination, keep output out of application logs, and display per-test outcome plus a safe summary. Tests are trusted project code with current-user permissions.

### Workflows (slot 5)

- Replace the ephemeral **Initial JSON input** field with a saved **Variables** editor on each workflow. Variables are key/value rows; each value is parsed as JSON, so arrays use square-bracket syntax, strings use quotes, and numbers/booleans/objects remain typed.
- Assemble saved variables into the root JSON object for each run. Preserve step input mappings from root variables and earlier step results. Keep current-run outputs in memory only and retain the existing safe, bounded run-history summary.

### Scheduler (slot 6)

- Let schedules target either a saved workflow or a saved Script Runner profile. Keep recurrence, local-time, missed-run, claim, shutdown, history, and concurrency policies the same.
- Use the host-owned saved-profile executor for scripts, with no arbitrary executable or command text in schedule records. Script schedules have no renderer input payload in this first iteration.

### Work Time (slot 7)

- Replace Pomodoro focus/break behavior in the UI with a work-time log. The user starts one stopwatch, stops it, then enters a required description and optional tags before saving the entry.
- Persist an active start timestamp so the elapsed time survives a tab switch and Automator restart. On stop, persist a pending completion before showing the description/tags dialog; allow save or resume tracking, and never silently lose the stopped interval.
- Show recent entries and total duration by day/tag. Preserve old focus records as legacy data, but do not present Pomodoro controls or add new focus sessions.

### Windows and command search

- Keep the small launcher as the right-side/quick-access surface with its hotkey, focus acquisition, and close behavior. Add a separate regular resizable **Workspace** window for complex module editing and larger result views. It has no global-hotkey focus-grab behavior; closing it does not quit the tray host or hide the launcher.
- Both surfaces share one backend process, library, credential vault, and job coordinators. Renderer identity and window role are host-derived. The workspace has its own selected tab and module authorization context; showing or changing tabs in one surface must not move, show, hide, or steal focus from the other.
- Add a reusable per-tab action search. Each module registers typed commands with labels, keywords, availability, and an action. Typing filters commands and `Enter` runs only the sole unambiguous match; ambiguous results require explicit selection. Commands that need values open/focus their editor. Keep destructive, network, script, and browser work behind explicit `Enter`/click; never execute while a user is merely typing.
- Keep existing tab 1 alias search/launch behavior unchanged.

## Host and persistence boundaries

- The .NET host remains owner of processes, Playwright Test invocation, HTTP/credentials, workflow/schedule execution, active work-time state, and durable records. React reaches these through versioned module actions and the validated IPC/RPC/preload bridge only.
- Project root and tags are local Automator metadata. Test source files and browser artifacts are not part of Automator backup exports. API cURL import exports only safe profile metadata and secret references.
- Schedule and workflow records use additive schema migration/default handling. Existing profile IDs and existing workflow/schedule history remain readable. Legacy focus data remains untouched.
- The command catalog is UI-scoped and cannot widen host capabilities. Host authorization is based on the trusted BrowserWindow role and its selected module, never a renderer-supplied role.

## Acceptance

- API requests use profile-specific JSON, paste/import Chrome cURL into an editable draft, never send it until requested, and retain host-derived request boundaries and credential protections.
- Browser tab can select a project, list files and tests, create a file/test, assign app-managed tags, run a file/exact test/tag group, cancel a run, and report missing runner/setup errors without modifying project dependencies.
- Workflow variables persist and feed step mappings with JSON types/arrays preserved.
- Scheduler can create/edit/run/pause/delete workflow and script schedules; legacy workflow schedules still load.
- Work Time start/stop/metadata/resume/history survive tab switches/restart without Pomodoro UI.
- Workspace and launcher remain independent in visibility, selected tab, focus, and module authorization while sharing backend services.
- Every tab exposes keyboard-searchable actions; only a sole match executes after explicit Enter.
- `npm.cmd run verify`, Windows packaging, and packaged lifecycle checks pass. Test runs keep production settings and startup registration untouched.

## Assumptions

- Browser Automation's user-selected existing project directory is the source of truth; there is no single Automator-managed test directory.
- App-managed tags are metadata only; test authors and agents do not need to add Playwright annotations to source.
- Browser module's prior saved browser-session profiles remain available to existing workflows until those workflows are migrated; the tab UI itself moves to test exploration.
- The command bar is consistent across slots 2–7; slot 1 keeps its existing alias-oriented command behavior.
