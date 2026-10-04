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

Test files are maintained as normal Playwright code. Automator owns display and execution; test behavior belongs in the project files.
