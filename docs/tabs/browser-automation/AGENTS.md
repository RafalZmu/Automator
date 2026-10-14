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
- Start Playwright runs through `startBrowserRun` in `ui/modules/browserRunStore.ts` and dispatch through the bridge, not the per-tab `services` object: `services` is disposed when the active tab changes and the view unmounts when the compact window reopens on its home surface. Never abort a run in an unmount cleanup.
- `runTests` (and Script Runner `runProfile`/`runAgain`) are "detached" in `BackendServer.IsDetachedRunAction`: they get their own module-services context and register with `survivesModuleCancel`, so `DeactivateServicesAsync` (panel hidden or tab changed) does not cancel them. Keep new long-running run actions on that list.
- Run history is host-owned. `PlaywrightTestExplorer.RunAsync` saves one `run-history` record per run (success, failed, timed out, canceled, error) after the process ends, bounded to 200 and 256 outcomes, and keeps an in-memory active-run list for `listRunHistory`. Persistence is best effort and must never change the run result. Store titles, files, lines, statuses, a classified failure `reason` and one ANSI-stripped error excerpt of at most 500 characters per failed test (`ReadFailure`/`ClassifyFailure`); never stdout/stderr, traces, screenshots or page content.
- Browser-action profiles must continue using the isolated host browser session and their saved host grant. Do not introduce unbounded navigation or bypass host network policy.

## Code and checks

UI: ui/modules/BrowserAutomationView.tsx; contracts: contracts/browserAutomation.ts.
Module and Test Explorer: src/Automator.Application/Automation/BrowserAutomationModule.cs and PlaywrightTestExplorer.cs.
Browser runtime: src/Automator.Infrastructure/Automation/BrowserAutomationProxy.cs, PlaywrightAutomationBrowserService.cs, and browser-worker.cjs.
Tests: tests/browser-automation-contract.test.mjs and Browser Infrastructure/Application specs.

Update this guide when supported test-file patterns, managed-project behavior, tags, or browser grants change.
