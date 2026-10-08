# Tab Happy-Path Visibility Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Verify that required happy-path buttons across Automator's tabs are visible and reachable in the current UI, and keep the per-tab test documentation synchronized.

**Architecture:** Extend the existing Electron/Playwright test harness with viewport-aware assertions on accessible button names and representative happy paths. Update the matching tab `TESTS.md` files in their current case/steps/expected-result format. After verification, add a workspace skill that makes the test-to-documentation update an explicit part of future test changes.

**Tech Stack:** Node.js `node:test`, Playwright Electron, Markdown, Agent Skills `SKILL.md`.

**Spec:** [2026-10-08-tab-happy-path-visibility.md](../specs/2026-10-08-tab-happy-path-visibility.md)

## Global Constraints

- Preserve the existing tab-specific test conventions and accessible names.
- Test viewport placement directly; Playwright clicks may not auto-scroll an off-screen button into a passing state.
- Keep each tab's test documentation aligned with the actual test title, steps, and assertions.
- Do not change product code; narrowly correct an outdated test expectation when it conflicts with the current documented UI flow, and bound any unbounded test wait discovered during full verification.
- Preserve all pre-existing workspace modifications; do not stage or commit them.

## Review Focus

- Buttons in a vertically overflowing tab view must be reachable through that view's user scroll behavior.
- A button hidden by CSS or rendered outside the active content viewport must fail the new assertion.
- Modal/editor action buttons must be checked in the state where the happy path needs them.
- Tabs without an E2E fixture must not be represented as having end-to-end visibility coverage.
- Documentation must retain the repository's numbered test-step and expected-result format.

---

### Task 1: Add tab happy-path visibility coverage and document it

**Files:**
- Create or modify: `tests/electron/` happy-path visibility test(s), following existing Electron test helpers.
- Modify: `docs/tabs/api/TESTS.md`
- Modify: `docs/tabs/browser-automation/TESTS.md`
- Modify: `docs/tabs/launcher/TESTS.md`
- Modify: `docs/tabs/scheduler/TESTS.md`
- Modify: `docs/tabs/script-runner/TESTS.md`
- Modify: `docs/tabs/website-launcher/TESTS.md`
- Modify: `docs/tabs/work-time/TESTS.md`
- Modify: `docs/tabs/workflows/TESTS.md`

- [x] Inventory existing tab navigation, fixtures, and happy-path button names from the test harness and each tab's README/AGENTS guide.
- [x] Add the failing viewport-aware E2E assertions before changing any product code; run the focused Electron test and capture the expected failure.
- [x] Complete supported happy paths and check every required button in its use state without relying on click auto-scroll.
- [x] Update each tab's test documentation with the matching test title, numbered steps, and expected result; explicitly identify any tab without an E2E fixture.
- [x] Run the focused Electron visibility test, then run `npm.cmd run verify` and record results.
- [x] Review only the task's changed test/docs files against this brief; preserve unrelated dirty files.

### Task 2: Create and forward-test the test/documentation sync skill

**Files:**
- Create: `$CODEX_HOME/skills/keeping-tab-tests-and-docs-in-sync/SKILL.md` (or `~/.codex/skills/...` when `CODEX_HOME` is unset).

- [ ] Before writing the skill, run an independent task scenario without it and record whether the matching `TESTS.md` update is omitted or incomplete.
- [ ] Write concise workspace-specific guidance for maintaining test case names, numbered steps, expected results, and source/test references together.
- [ ] Validate the skill structure and run an independent follow-up scenario with the skill instructions.
- [ ] Check the skill against the current eight per-tab `TESTS.md` files and this task's actual test names.
