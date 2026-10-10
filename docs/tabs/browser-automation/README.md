# Browser Automation — slot 4

The tab has two related surfaces:

- **Playwright Test Explorer** discovers tests in the managed project or a selected existing Playwright project. Sections are test files; Automator-managed tags can group tests across files. Run one test, all tests in a section, or a tag group and inspect combined outcomes.
- **Browser profiles** use an Automator-managed isolated browser session for saved navigation and interaction actions. A profile stores its start URL and host grant.

When the configured managed project is first created, Automator creates a tests directory and an example spec file. For an existing project, choose its project root; Automator does not install or change that project's dependencies.

## Code map

- ui/modules/BrowserAutomationView.tsx renders project selection, discovered files/tests, tag editing, grouped runs, and browser profiles.
- contracts/browserAutomation.ts parses explorer state and validates renderer inputs.
- src/Automator.Application/Automation/BrowserAutomationModule.cs and PlaywrightTestExplorer.cs own module actions, discovery, section creation, tags, and run requests.
- src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs, PlaywrightAutomationBrowserService.cs, and browser-worker.cjs own the isolated browser runtime.
- Tests: tests/browser-automation-contract.test.mjs and Browser Infrastructure/Application spec projects.
- `PlaywrightTaskSavedProfileHandler.cs` validates and invokes saved Codex Playwright profiles from Workflows; `PlaywrightTestExplorer.cs` owns managed source creation, containment, and execution.

Test files are maintained as normal Playwright code. Automator owns display and execution; test behavior belongs in the project files.

The Codex task builder can create a generated Playwright task in the configured Automator-managed project's `tests/codex` folder. It saves a `playwright-task` profile with the managed project identity, relative spec path, named JSON inputs, declared scope/effects, and approval revision. Workflows can call this profile and map JSON values into its inputs. A changed source, scope, or declared effects must be reviewed and reapproved before execution. Generated test code runs with the signed-in Windows user's permissions; the displayed scope is consent context, not a sandbox. Existing selected projects and their dependencies are not rewritten by this feature. See [Codex](../codex/README.md) for generation and approval details.

## Test case steps

See [TESTS.md](TESTS.md) for the profile, explorer, preview, and managed-project test steps.
