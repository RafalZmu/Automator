const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');
const modelUrl = pathToFileURL(path.join(workspace, 'ui', 'modules', 'focusSessionsViewModel.ts')).href;

test('focus view derives a running countdown from the host deadline and a paused one from the saved remainder', async () => {
  const { remainingFocusMilliseconds } = await import(modelUrl);
  const now = Date.parse('2026-10-03T12:00:00.000Z');
  assert.equal(remainingFocusMilliseconds({ state: 'running', phaseEndsAtUtc: '2026-10-03T12:05:00.000Z', remainingMilliseconds: null }, now), 300_000);
  assert.equal(remainingFocusMilliseconds({ state: 'paused', phaseEndsAtUtc: null, remainingMilliseconds: 91_250 }, now), 91_250);
  assert.equal(remainingFocusMilliseconds({ state: 'idle', phaseEndsAtUtc: null, remainingMilliseconds: null }, now), 0);
});

test('focus view formats countdowns without showing a completed phase as negative time', async () => {
  const { formatFocusClock } = await import(modelUrl);
  assert.equal(formatFocusClock(1), '00:01');
  assert.equal(formatFocusClock(0), '00:00');
  assert.equal(formatFocusClock(-25), '00:00');
  assert.equal(formatFocusClock(3_600_000), '1:00:00');
});

test('focus view accepts only a complete host snapshot with supported settings and session states', async () => {
  const { readFocusSnapshot } = await import(modelUrl);
  const valid = {
    settings: { focusMinutes: 50, breakMinutes: 10 },
    session: {
      state: 'running', sessionId: 'focus-1', phase: 'focus', phaseEndsAtUtc: '2026-10-03T12:25:00Z',
      remainingMilliseconds: null, completedFocusPhases: 0, settings: { focusMinutes: 25, breakMinutes: 5 },
    },
    history: [],
  };
  const parsed = readFocusSnapshot(valid);
  assert.equal(parsed?.session.sessionId, 'focus-1');
  assert.equal(parsed?.settings.focusMinutes, 50, 'the editor must read next-session defaults');
  assert.equal(parsed?.session.settings.focusMinutes, 25, 'the timer phase retains its frozen session duration');
  assert.equal(readFocusSnapshot({ ...valid, session: { ...valid.session, state: 'unknown' } }), null);
  assert.equal(readFocusSnapshot({ ...valid, session: { ...valid.session, settings: { focusMinutes: 0, breakMinutes: 5 } } }), null);
});
