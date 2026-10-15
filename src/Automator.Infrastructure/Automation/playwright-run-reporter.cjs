'use strict';

// Playwright reporter for Browser Automation runs (see PlaywrightTestExplorer.cs).
// It appends one JSON line when each test attempt starts and one when it finishes to the file named by
// AUTOMATOR_PLAYWRIGHT_RUN_EVENTS. Automator reads that file after the run, including after the runner
// limit stops it, so a test that was still running can be named. Standard output stays reserved for the
// JSON report, so this reporter never prints.

const fs = require('node:fs');

const MAXIMUM_MESSAGE_CHARACTERS = 4096;
const MAXIMUM_SNIPPET_CHARACTERS = 4096;

class AutomatorRunReporter {
  printsToStdio() {
    return false;
  }

  onTestBegin(test, result) {
    write({ event: 'begin', ...identify(test, result) });
  }

  onTestEnd(test, result) {
    const failed = result.status !== 'passed' && result.status !== 'skipped';
    write({
      event: 'end',
      ...identify(test, result),
      status: result.status,
      timeoutMs: test.timeout,
      durationMs: result.duration,
      ...(failed ? { failure: describeFailure(result) } : {}),
    });
  }
}

function write(record) {
  const file = process.env.AUTOMATOR_PLAYWRIGHT_RUN_EVENTS;
  if (!file) return;
  try {
    fs.appendFileSync(file, `${JSON.stringify(record)}\n`, 'utf8');
  } catch {
    // Events are best effort; a failed write must not change the run.
  }
}

function identify(test, result) {
  const location = test.location ?? {};
  return {
    testId: test.id,
    file: location.file ?? '',
    line: location.line ?? 0,
    column: location.column ?? 0,
    title: test.title,
    project: test.parent?.project?.()?.name ?? '',
    retry: result.retry,
  };
}

// The step that failed deepest in the tree is the most specific place: a click inside a hook, for example,
// rather than the hook itself. Expect and action steps are only present here, not in Playwright's JSON report.
function deepestFailingStep(steps, depth = 0) {
  let deepest;
  for (const step of steps ?? []) {
    const nested = deepestFailingStep(step.steps, depth + 1);
    const candidate = nested ?? (step.error ? { step, depth } : undefined);
    if (candidate && (!deepest || candidate.depth > deepest.depth)) deepest = candidate;
  }
  return deepest;
}

function describeFailure(result) {
  const error = result.errors?.[0] ?? result.error;
  const found = deepestFailingStep(result.steps);
  const step = found?.step;
  const location = step?.location ?? error?.location;
  return {
    message: clip(error?.message ?? step?.error?.message, MAXIMUM_MESSAGE_CHARACTERS),
    snippet: clip(error?.snippet ?? step?.error?.snippet, MAXIMUM_SNIPPET_CHARACTERS),
    step: step?.title,
    stepCategory: step?.category,
    line: location?.line,
    column: location?.column,
  };
}

function clip(value, maximum) {
  return typeof value === 'string' ? value.slice(0, maximum) : undefined;
}

module.exports = AutomatorRunReporter;
