import assert from 'node:assert/strict';
import { test } from 'node:test';
import { cancelBrowserRun, getBrowserRunSnapshot, startBrowserRun } from '../ui/modules/browserRunStore.ts';

const success = (message = 'ok') => ({ contractVersion: 1, status: 'success', message, data: {}, actions: [] });
const target = (bridge) => ({ bridge, moduleId: 'browser-automation', contractVersion: 1, action: { id: 'runTests', version: 1 } });

test('a run stays active until the backend answers, independent of any view', async () => {
  let finish;
  let request;
  const bridge = { dispatchModuleAction: (value) => { request = value; return new Promise((resolve) => { finish = resolve; }); }, cancelAutomationModuleAction: async () => ({}) };
  const pending = startBrowserRun(target(bridge), 'file', 'tests/a.spec.ts', { file: 'tests/a.spec.ts' });
  assert.equal(getBrowserRunSnapshot().active?.target, 'tests/a.spec.ts');
  assert.deepEqual(request.input, { kind: 'file', file: 'tests/a.spec.ts' });
  await assert.rejects(startBrowserRun(target(bridge), 'tag', 'tag smoke', { tag: 'smoke' }), /already in progress/);
  finish(success('done'));
  const finished = await pending;
  assert.equal(finished.state, 'finished');
  assert.equal(getBrowserRunSnapshot().active, null);
  assert.equal(getBrowserRunSnapshot().last?.message, 'done');
});

test('backend errors and cancellation are reported and free the slot', async () => {
  const failing = { dispatchModuleAction: async () => ({ ...success('Playwright failed.'), status: 'error' }), cancelAutomationModuleAction: async () => ({}) };
  assert.equal((await startBrowserRun(target(failing), 'test', 'one', { testId: 'x' })).state, 'error');
  let reject;
  const bridge = { dispatchModuleAction: () => new Promise((_, fail) => { reject = fail; }), cancelAutomationModuleAction: async () => { reject(new Error('aborted')); return {}; } };
  const pending = startBrowserRun(target(bridge), 'test', 'two', { testId: 'y' });
  await cancelBrowserRun();
  assert.equal((await pending).state, 'canceled');
  assert.equal(getBrowserRunSnapshot().active, null);
  const thrown = { dispatchModuleAction: async () => { throw new Error('backend gone'); }, cancelAutomationModuleAction: async () => ({}) };
  const record = await startBrowserRun(target(thrown), 'test', 'three', { testId: 'z' });
  assert.equal(record.state, 'error');
  assert.equal(record.message, 'backend gone');
});
