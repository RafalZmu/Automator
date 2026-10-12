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
- Playwright test runs have no saved profile and are recorded in Run Activity with a null profile ID; keep that field explicit in the activity RPC response.
- Codex-generated Playwright tasks are stored as `playwright-task` profiles under the managed project's `tests/codex` directory. Keep their relative paths contained in that project, preserve create-new behavior, validate their declared JSON inputs, and reject a source hash that no longer matches its approval. They run through this host-owned Explorer path; do not rewrite tests in a user-selected project or change project dependencies.
- Generated-task scope is displayed for review and consent, not enforced as an OS boundary. A changed saved source or declared scope/effects requires review and reapproval before a Workflow invokes it.
- Recording (`recordTest`): runs `codegen --target=playwright-test --output <temp file>` through the host process service with the project's own Playwright CLI, an optional http(s) start URL as a separate argument (never concatenated), a 30-minute limit, and deletes the temp file. It returns code only; nothing is saved by it.
- `installBrowsers` runs only `<project cli> install chromium` with fixed arguments and a 15-minute limit. Failure details (`error`, page snapshot from the `error-context` attachment) are bounded, ANSI-stripped, and read only when the attachment path is inside the project root; the managed-project config seed must never replace an existing config.
- AI authoring (`proposeTest` / `applyTest`, implemented in `PlaywrightTestExplorer.Authoring.cs`): the renderer pastes a recording and notes; the host asks the selected local CLI (Codex or Claude, via `IAuthoringCliClient` and the Codex task schema with `format: playwright`) for the complete file, and returns it with the previous source and its hash. Proposing never writes. `applyTest` writes only the source the user reviewed: new files use create-new, edits require the unchanged base hash, and both use the same project-root containment, reparse-point, `.spec.ts`/`.test.ts` and size checks as section creation. Keep project context sent to the CLI bounded and read-only (Playwright config, up to eight auth/fixture/setup/helper files and one sample test under `tests`/`e2e`/`test`, never `.env` or non-script files); the CLI never executes anything and cannot add dependencies. Recordings and notes are treated as data in the prompt, and prompts and sources are not logged.
- Browser-action profiles must continue using the isolated host browser session and their saved host grant. Do not introduce unbounded navigation or bypass host network policy.

## Code and checks

UI: ui/modules/BrowserAutomationView.tsx; contracts: contracts/browserAutomation.ts.
Module and Test Explorer: src/Automator.Application/Automation/BrowserAutomationModule.cs and PlaywrightTestExplorer.cs.
Browser runtime: src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs, PlaywrightAutomationBrowserService.cs, and browser-worker.cjs.
Tests: tests/browser-automation-contract.test.mjs and Browser Infrastructure/Application specs.

Update this guide when supported test-file patterns, managed-project behavior, tags, or browser grants change.

## Keyboard

- `create-section` is bound to Ctrl+N through the commands list. Keep its disabled rules (project chosen, not busy) in sync with the button.
