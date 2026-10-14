# Browser Automation — slot 4

The tab has two related surfaces:

- **Playwright Test Explorer** discovers tests in the managed project or a selected existing Playwright project. Sections are test files; Automator-managed tags can group tests across files. Run one test, all tests in a section, or a tag group and inspect combined outcomes.
- **Browser profiles** use an Automator-managed isolated browser session for saved navigation and interaction actions. A profile stores its start URL and host grant.

When the configured managed project is first created, Automator creates a tests directory and an example spec file. For an existing project, choose its project root; Automator does not install or change that project's dependencies.

Runs started from the Playwright Test Explorer are listed in Run Activity without a saved profile identifier.

## Running state and run history

A run belongs to the window, not to the Browser Automation view. Start a test, section or tag group (from the tab or from the compact window's action search), close the window and reopen it: the run keeps going, a **Running** banner with elapsed time and Cancel appears at the top of the tab, and a pill with the target shows in the header on every screen (click it to jump back). Only one run is active per window; a run started by another window or an agent shows as running too (without Cancel) until it ends.

Every finished run is saved to the Automator library database (`browser-automation/run-history`): target, kind, state (Passed, Failed, Timed out, Canceled, Error), start time, duration, exit code, the passed/failed/skipped summary and up to 256 per-test outcomes. The newest 200 are kept across restarts and listed under **Run history**; select an entry to read its outcomes, or use **Clear history**. Raw runner output is never stored. Run Activity keeps its own metadata-only entry for each run.

## Code map

- ui/modules/BrowserAutomationView.tsx renders project selection, discovered files/tests, tag editing, grouped runs, the running banner and the saved run history.
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

MCP clients can list browser profiles and Playwright tests and run one profile action or a test selection through `list_browser_automations` and `run_browser_automation`, using the same `runAction` and `runTests` actions and allowed-host policy as this tab. See [Agent access](../../sections/agent-access/README.md).
