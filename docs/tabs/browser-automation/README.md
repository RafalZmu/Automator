# Browser Automation — slot 4

The tab has two related surfaces:

- **Playwright Test Explorer** discovers tests in the managed project or a selected existing Playwright project. Sections are test files; Automator-managed tags can group tests across files. Run one test, all tests in a section, or a tag group and inspect combined outcomes.
- **Browser profiles** use an Automator-managed isolated browser session for saved navigation and interaction actions. A profile stores its start URL and host grant.

When the configured managed project is first created, Automator creates a tests directory and an example spec file. For an existing project, choose its project root; Automator does not install or change that project's dependencies.

Runs started from the Playwright Test Explorer are listed in Run Activity without a saved profile identifier.

## Running state and run history

A run belongs to the window, not to the Browser Automation view. Start a test, section or tag group (from the tab or from the compact window's action search), close the window and reopen it: the run keeps going, a **Running** banner with elapsed time and Cancel appears at the top of the tab, and a pill with the target shows in the header on every screen (click it to jump back). The backend starts these runs outside the compact window's shared context, so hiding the window or changing tabs does not cancel them (Cancel, quitting Automator and the timeout still do). Tests are discovered when the tab opens for any selected project, and **Run history** is always shown, with an empty-state line until the first run. Only one run is active per window; a run started by another window or an agent shows as running too (without Cancel) until it ends.

Every finished run is saved to the Automator library database (`browser-automation/run-history`): target, kind, state (Passed, Failed, Timed out, Canceled, Error), start time, duration, exit code, a count of passed, flaky, failed, timed-out, skipped and interrupted tests, and up to 256 per-test outcomes. The newest 200 are kept across restarts. **Run history** is hidden by default. In the compact window it opens as a panel on the left: the window widens to the left by the panel width (about 220 ms ease, clamped to the screen's work area) so the existing content stays where it was, and narrows again when the panel is closed or you leave the tab. In the workspace window it stays an expandable section at the bottom of the tab. Whether it is open is remembered on this computer. Each entry shows its counts, such as `2 passed · 1 failed · 2 timed out`. Select an entry to open its results, or use **Clear history**.

In a run's results, tests that did not pass are listed first, each with its status, its file and failing line, the step that failed (for example `expect.toBeVisible` or `locator.click`), its attempt count, and a reason:

- **Test timed out**: the whole test passed its time limit.
- **Hook or fixture timed out**: a `beforeEach`, `beforeAll`, or fixture passed its time limit.
- **Expect timed out**: a retrying expect (such as `toBeVisible`, `toHaveText`, or `expect.poll`) kept failing until its timeout.
- **Expect failed**: an expect compared a value that did not match.
- **Action timed out**: a page action, such as a click, waited past its timeout.
- **Runner limit reached**: Automator stopped the run at its time limit while this test was still running.
- **Error** or **Interrupted**: any other failure, or a run that ended before the test finished.

A test that passed only after a failed attempt is **Flaky**, and its error is from the attempt that failed. Passed and skipped tests are collapsed under one line. The error text is shown first; the code around the failure is one click away. Reasons and steps come from the run events that Automator's Playwright reporter writes; when those are unavailable, the reasons come from Playwright's JSON report, which carries less detail. The stored error excerpt is 500 characters and the code excerpt 900. They may contain values from the page or your test. Raw runner output is never stored. Run Activity keeps its own metadata-only entry for each run.

## Code map

- Side panel: `ui/modules/BrowserAutomationView.tsx` uses the shared `ui/components/SideHistoryPanel.tsx` (also used by Script Runner and Scheduler) to portal the history into `#side-panel-slot` (rendered by `ui/App.tsx`, sized from the window width in `ui/styles.css`) and calls `bridge.setSidePanel`; `electron/main.ts` animates the window bounds using `computeSidePanelBounds` and `interpolateBounds` in `electron/platform.ts` (tests: tests/host-platform.test.mjs).
- ui/modules/BrowserAutomationView.tsx renders project selection, discovered files/tests, tag editing, grouped runs, the running banner and the saved run history.
- src/Automator.Infrastructure/Automation/playwright-run-reporter.cjs is the Playwright reporter that writes each run's per-test events; tests/playwright-run-reporter.test.cjs covers it.
- ui/modules/browserRunStore.ts owns the active run for the window (memory only); tests/browser-run-store.test.mjs covers it. `listRunHistory` and `deleteRunHistory` are the backend actions behind the history; tests/Automator.Application.Specs/BrowserRunHistorySpecs.cs covers them, and tests/electron/browser-run-history.test.cjs checks the running banner across tabs, saving, restart persistence and clearing against the real host.
- contracts/browserAutomation.ts parses explorer state and validates renderer inputs.
- src/Automator.Application/Automation/BrowserAutomationModule.cs and PlaywrightTestExplorer.cs own module actions, discovery, section creation, tags, and run requests.
- src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs, PlaywrightAutomationBrowserService.cs, and browser-worker.cjs own the isolated browser runtime.
- Tests: tests/browser-automation-contract.test.mjs and Browser Infrastructure/Application spec projects.
- `PlaywrightTaskSavedProfileHandler.cs` validates and invokes saved Codex Playwright profiles from Workflows; `PlaywrightTestExplorer.cs` owns managed source creation, containment, and execution.

Test files are maintained as normal Playwright code. Automator owns display and execution; test behavior belongs in the project files.

The Codex task builder can create a generated Playwright task in the configured Automator-managed project's `tests/codex` folder. It saves a `playwright-task` profile with the managed project identity, relative spec path, named JSON inputs, declared scope/effects, and approval revision. Workflows can call this profile and map JSON values into its inputs. A changed source, scope, or declared effects must be reviewed and reapproved before execution. Generated test code runs with the signed-in Windows user's permissions; the displayed scope is consent context, not a sandbox. Existing selected projects and their dependencies are not rewritten by this feature. See [Codex](../codex/README.md) for generation and approval details.

## Test case steps

See [TESTS.md](TESTS.md) for the profile, explorer, preview, and managed-project test steps.

## Agent access

Only profiles with the **AI can use** checkbox ticked (`aiAccess`, off by default, toggled in the profile list on this tab) are visible to MCP clients. MCP clients can list those browser profiles and Playwright tests and run one profile action or a test selection through `list_browser_automations` and `run_browser_automation`, using the same `runAction` and `runTests` actions and allowed-host policy as this tab. See [Agent access](../../sections/agent-access/README.md).
