# Tab happy-path control visibility

## Goal

Add Electron end-to-end coverage that catches missing, hidden, or viewport-clipped buttons required to complete the documented happy path for every current user-facing tab.

## Scope

The covered tabs are API, Browser Automation, Launcher, Scheduler, Script Runner, Website Launcher, Work Time, and Workflows. Reuse the repository's Electron/Playwright integration harness and existing test conventions. For each happy-path state, identify its required action buttons, assert that each control is displayed and within its visible content area before using it, and complete the flow where the existing test fixtures support it.

Update each affected `docs/tabs/<tab>/TESTS.md` entry in the existing test-case format, with numbered steps and an expected result. Keep assertions tied to accessible button names and actual rendered UI. Do not add product behavior unless the new coverage demonstrates a current regression that prevents the requested flow.

## Acceptance criteria

- Every current documented tab has at least one E2E happy-path visibility case or is explicitly documented as not having a supported E2E fixture, with its existing focused coverage identified.
- Visibility checks verify viewport/content-area placement, not only Playwright's CSS visibility state or click actionability.
- Tests do not rely on auto-click scrolling to make an off-screen control pass.
- Test descriptions and `TESTS.md` case names/steps describe the same scenarios.
- `npm.cmd run verify` passes against the current workspace.

## Follow-up skill

After the test/docs change is verified, create a discoverable user-level skill that prompts future contributors to update matching per-tab test documentation whenever E2E tests, test names, or coverage change. Validate it and forward-test its guidance before reporting completion.
