import assert from 'node:assert/strict';
import { test } from 'node:test';
import {
  activeScriptRun, cancelScriptRun, clearFinishedScriptRuns, getScriptRuns, recordExternalScriptRun, startScriptRun,
} from '../ui/modules/scriptRunStore.ts';

const output = (overrides = {}) => ({ profileId: 'report', exitCode: 0, timedOut: false, stdout: 'done', stderr: '', stdoutTruncated: false,
  stderrTruncated: false, durationMilliseconds: 12, structuredOutput: null, parseFailure: false, ...overrides });
const success = (data) => ({ contractVersion: 1, status: 'success', message: 'ok', data, actions: [] });
const target = (bridge) => ({ bridge, moduleId: 'script-runner', contractVersion: 1, action: { id: 'runProfile', version: 1 } });

test('a run is visible as running until the backend answers, and its output joins the history', async () => {
  clearFinishedScriptRuns();
  let finish;
  const bridge = { dispatchModuleAction: () => new Promise((resolve) => { finish = resolve; }), cancelAutomationModuleAction: async () => ({ canceled: true }) };
  const pending = startScriptRun(target(bridge), { id: 'report', name: 'Report' }, undefined);
  assert.equal(activeScriptRun()?.profileName, 'Report');
  await assert.rejects(startScriptRun(target(bridge), { id: 'other', name: 'Other' }, undefined), /already running/);
  finish(success(output()));
  const record = await pending;
  assert.equal(record.state, 'success');
  assert.equal(activeScriptRun(), undefined);
  assert.equal(getScriptRuns()[0].output.stdout, 'done');
});

test('failed, timed out and canceled runs are classified', async () => {
  clearFinishedScriptRuns();
  const run = (result) => startScriptRun(target({ dispatchModuleAction: async () => result, cancelAutomationModuleAction: async () => ({}) }), { id: 'report', name: 'Report' }, undefined);
  assert.equal((await run(success(output({ exitCode: 3 })))).state, 'failed');
  assert.equal((await run(success(output({ timedOut: true, exitCode: null })))).state, 'timedOut');
  let reject;
  const bridge = { dispatchModuleAction: () => new Promise((_, fail) => { reject = fail; }), cancelAutomationModuleAction: async () => { reject(new Error('aborted')); return {}; } };
  const pending = startScriptRun(target(bridge), { id: 'report', name: 'Report' }, undefined);
  await cancelScriptRun(activeScriptRun().id);
  assert.equal((await pending).state, 'canceled');
});

test('history is bounded, keeps newest first and external runs are recorded', () => {
  clearFinishedScriptRuns();
  for (let index = 0; index < 35; index++) recordExternalScriptRun({ id: `p${index}`, name: `P${index}` }, { status: 'success', message: '', data: output({ profileId: `p${index}` }) });
  const runs = getScriptRuns();
  assert.equal(runs.length, 30);
  assert.equal(runs[0].profileId, 'p34');
  clearFinishedScriptRuns();
  assert.equal(getScriptRuns().length, 0);
});
