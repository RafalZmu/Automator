# Browser Automation contributor guide

Read the root AGENTS.md first. This tab contains both saved browser-action profiles and the Playwright Test Explorer.

## Playwright test authoring and maintenance

- Keep discovery, metadata, file creation, tag persistence, and test invocation host-owned in PlaywrightTestExplorer.cs; the renderer only dispatches validated module actions.
- The managed project is selected/configured by the host and is seeded with tests/example.spec.ts. Do not make an arbitrary test-folder path a mandatory user setting. Users may select an existing Playwright project.
- Each Explorer section is a Playwright test file. Add tests using the project's installed @playwright/test APIs and conventional .spec.ts, .test.ts, .spec.js, or .test.js files that the discovery implementation accepts.
- Do not install, upgrade, or rewrite dependencies in a user-selected project. If its Playwright runner is missing, show the existing setup error and let the user/agent maintain that project explicitly.
- Store Automator-managed tags as metadata keyed to the test/project identity; do not modify test source to add tags. Keep stale-tag cleanup explicit.
- Preserve the project-root containment and reparse-point checks when creating or editing section files.
- Run individual tests, a section file, or a tag group through the host process service. Never build a shell command by concatenating project paths or test names.
- Codex-generated Playwright tasks are stored as `playwright-task` profiles under the managed project's `tests/codex` directory. Keep their relative paths contained in that project, preserve create-new behavior, validate their declared JSON inputs, and reject a source hash that no longer matches its approval. They run through this host-owned Explorer path; do not rewrite tests in a user-selected project or change project dependencies.
- Generated-task scope is displayed for review and consent, not enforced as an OS boundary. A changed saved source or declared scope/effects requires review and reapproval before a Workflow invokes it.
- Browser-action profiles must continue using the isolated host browser session and their saved host grant. Do not introduce unbounded navigation or bypass host network policy.

## Code and checks

UI: ui/modules/BrowserAutomationView.tsx; contracts: contracts/browserAutomation.ts.
Module and Test Explorer: src/Automator.Application/Automation/BrowserAutomationModule.cs and PlaywrightTestExplorer.cs.
Browser runtime: src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs, PlaywrightAutomationBrowserService.cs, and browser-worker.cjs.
Tests: tests/browser-automation-contract.test.mjs and Browser Infrastructure/Application specs.

Update this guide when supported test-file patterns, managed-project behavior, tags, or browser grants change.
