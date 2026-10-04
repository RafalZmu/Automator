import assert from 'node:assert/strict';
import { test } from 'node:test';
import { runActivitySnapshotSchema, activitySummary, filterActivity } from '../contracts/activity.ts';
import { clearTransientResults, getTransientResult, rememberTransientResult, supportedFollowUps } from '../ui/activity/transientResults.ts';

const entry = { id: 'run-1', moduleId: 'api', actionId: 'runProfile', profileId: 'request',
  startedUtc: '2026-10-04T12:00:00Z', finishedUtc: '2026-10-04T12:00:01Z', status: 'success', durationMilliseconds: 1000, origin: 'manual' };
test('activity contract rejects persisted bodies, unbounded metadata and invalid timestamps', () => {
  assert.equal(runActivitySnapshotSchema.safeParse({ contractVersion: 1, entries: [entry] }).success, true);
  for (const replacement of [{ ...entry, body: 'private' }, { ...entry, id: 'x'.repeat(129) }, { ...entry, durationMilliseconds: -1 }, { ...entry, finishedUtc: 'yesterday' }])
    assert.equal(runActivitySnapshotSchema.safeParse({ contractVersion: 1, entries: [replacement] }).success, false);
  assert.equal(runActivitySnapshotSchema.safeParse({ contractVersion: 1, entries: Array(301).fill(entry) }).success, false);
});
test('notifications use fixed status summaries and activity search matches all typed words', () => {
  assert.equal(activitySummary(entry), 'API completed.');
  assert.equal(activitySummary({ ...entry, status: 'error' }), 'API failed.');
  assert.equal(filterActivity([entry], 'api request success').length, 1);
  assert.equal(filterActivity([entry], 'api failed').length, 0);
});
test('transient outputs stay bounded in memory and returned actions require registered version', () => {
  clearTransientResults();
  const result = { contractVersion: 1, status: 'success', message: 'private message', data: { body: 'private output' }, actions: [
    { id: 'repeatAction', label: 'Again', version: 1, payload: {} },
    { id: 'deleteEverything', label: 'Bad', version: 1, payload: {} },
  ] };
  rememberTransientResult('save', 'api', 'saveProfile', result);
  assert.equal(getTransientResult('save'), undefined);
  for (let n = 0; n < 21; n++) rememberTransientResult(`run-${n}`, 'browser-automation', 'runAction', result);
  assert.equal(getTransientResult('run-0'), undefined);
  assert.equal(getTransientResult('run-20').result.data.body, 'private output');
  assert.deepEqual(supportedFollowUps(result, [{ id: 'repeatAction', version: 1 }]).map((action) => action.id), ['repeatAction']);
  assert.deepEqual(supportedFollowUps(result, [{ id: 'repeatAction', version: 2 }]), []);
  assert.deepEqual(supportedFollowUps(result, [{ id: 'repeatAction', version: 1, command: 'legacy' }]), []);
  clearTransientResults();
  assert.equal(getTransientResult('run-20'), undefined);
});
