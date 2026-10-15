const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { test } = require('node:test');
const AutomatorRunReporter = require('../src/Automator.Infrastructure/Automation/playwright-run-reporter.cjs');

function withEventsFile(run) {
  const folder = fs.mkdtempSync(path.join(os.tmpdir(), 'automator-run-reporter-'));
  const events = path.join(folder, 'events.ndjson');
  const previous = process.env.AUTOMATOR_PLAYWRIGHT_RUN_EVENTS;
  process.env.AUTOMATOR_PLAYWRIGHT_RUN_EVENTS = events;
  try {
    return run(events);
  } finally {
    if (previous === undefined) delete process.env.AUTOMATOR_PLAYWRIGHT_RUN_EVENTS;
    else process.env.AUTOMATOR_PLAYWRIGHT_RUN_EVENTS = previous;
    fs.rmSync(folder, { recursive: true, force: true });
  }
}

function readLines(file) {
  return fs.readFileSync(file, 'utf8').trim().split('\n').map((line) => JSON.parse(line));
}

const spec = {
  id: 'hook-test',
  title: 'Hook timeout',
  location: { file: path.join(os.tmpdir(), 'managed', 'tests', 'example.spec.ts'), line: 4, column: 3 },
  timeout: 30000,
  parent: { project: () => ({ name: 'chromium' }) },
};

test('the run reporter writes begin and finish events, naming the deepest failing step', () => {
  withEventsFile((events) => {
    const reporter = new AutomatorRunReporter();
    assert.equal(reporter.printsToStdio(), false);
    reporter.onTestBegin(spec, { retry: 0 });
    reporter.onTestEnd(spec, {
      retry: 0,
      status: 'timedOut',
      duration: 1200,
      errors: [{ message: 'Test timeout of 30000ms exceeded while running "beforeEach" hook.', snippet: '> 9 | await page.click()' }],
      steps: [{
        category: 'hook',
        title: 'beforeEach hook',
        steps: [{ category: 'pw:api', title: 'locator.click', error: { message: 'waiting' }, location: { file: spec.location.file, line: 9, column: 5 }, steps: [] }],
      }],
    });
    const lines = readLines(events);
    assert.deepEqual(lines.map((line) => line.event), ['begin', 'end']);
    assert.equal(lines[0].testId, 'hook-test');
    assert.equal(lines[1].status, 'timedOut');
    assert.equal(lines[1].project, 'chromium');
    assert.equal(lines[1].timeoutMs, 30000);
    assert.equal(lines[1].failure.step, 'locator.click');
    assert.equal(lines[1].failure.stepCategory, 'pw:api');
    assert.equal(lines[1].failure.line, 9);
    assert.match(lines[1].failure.snippet, /page\.click/);
  });
});

test('the run reporter omits failure details for passed and skipped attempts', () => {
  withEventsFile((events) => {
    const reporter = new AutomatorRunReporter();
    reporter.onTestEnd(spec, { retry: 0, status: 'passed', duration: 5, errors: [], steps: [] });
    reporter.onTestEnd(spec, { retry: 0, status: 'skipped', duration: 0, errors: [], steps: [] });
    const lines = readLines(events);
    assert.equal(lines.length, 2);
    assert.equal(lines.some((line) => 'failure' in line), false);
  });
});

test('the run reporter does nothing without an events file and never throws', () => {
  const previous = process.env.AUTOMATOR_PLAYWRIGHT_RUN_EVENTS;
  delete process.env.AUTOMATOR_PLAYWRIGHT_RUN_EVENTS;
  try {
    const reporter = new AutomatorRunReporter();
    assert.doesNotThrow(() => reporter.onTestBegin(spec, { retry: 0 }));
  } finally {
    if (previous !== undefined) process.env.AUTOMATOR_PLAYWRIGHT_RUN_EVENTS = previous;
  }
});
