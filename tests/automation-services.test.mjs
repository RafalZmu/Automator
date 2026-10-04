import assert from 'node:assert/strict';
import { test } from 'node:test';
import { createAutomationServices } from '../ui/automationServices.ts';

function deferred() {
  let resolve;
  let reject;
  const promise = new Promise((resolvePromise, rejectPromise) => {
    resolve = resolvePromise;
    reject = rejectPromise;
  });
  return { promise, resolve, reject };
}

async function waitFor(predicate) {
  const deadline = Date.now() + 2_000;
  while (!predicate()) {
    if (Date.now() >= deadline) throw new Error('Timed out waiting for keyboard service transition.');
    await new Promise((resolve) => setTimeout(resolve, 1));
  }
}

test('keyboard service re-subscribes after revoke and regain and ignores stale snapshots', async () => {
  const snapshot = deferred();
  const calls = [];
  let availabilityListener;
  let inputListener;
  const bridge = {
    getAutomationKeyboardEligibility: async (moduleId) => {
      calls.push(['snapshot', moduleId]);
      return snapshot.promise;
    },
    onAutomationKeyboardAvailability: (listener) => {
      calls.push(['availability-listener']);
      availabilityListener = listener;
      return () => calls.push(['availability-unsubscribe']);
    },
    onAutomationKeyboardInput: (listener) => {
      calls.push(['input-listener']);
      inputListener = listener;
      return () => calls.push(['input-unsubscribe']);
    },
    subscribeAutomationKeyboard: async (moduleId) => calls.push(['subscribe', moduleId]),
    unsubscribeAutomationKeyboard: async (moduleId) => calls.push(['unsubscribe', moduleId]),
    automationHttpRequest: async () => { throw new Error('not used'); },
    cancelAutomationHttpRequest: async () => ({ canceled: false }),
  };

  const services = createAutomationServices('module-2', bridge, ['keyboard.input']);
  assert.deepEqual(calls.slice(0, 3), [
    ['availability-listener'],
    ['input-listener'],
    ['snapshot', 'module-2'],
  ]);

  const received = [];
  const unsubscribe = services.keyboard.subscribe((event) => received.push(event));
  availabilityListener({ moduleId: 'module-2', eligible: true, revision: 5 });
  await waitFor(() => calls.filter(([name]) => name === 'subscribe').length === 1);
  snapshot.resolve({ eligible: false, revision: 4 });
  await new Promise((resolve) => setTimeout(resolve, 0));
  assert.equal(calls.filter(([name]) => name === 'subscribe').length, 1);

  availabilityListener({ moduleId: 'module-2', eligible: 'false', revision: 6 });
  availabilityListener({ moduleId: 'other-module', eligible: true, revision: 6 });
  inputListener({ moduleId: 'module-2', event: { sequence: 9, code: 'KeyA' } });
  assert.deepEqual(received, [{ sequence: 9, code: 'KeyA' }]);

  availabilityListener({ moduleId: 'module-2', eligible: false, revision: 6 });
  inputListener({ moduleId: 'module-2', event: { sequence: 10, code: 'KeyA' } });
  assert.deepEqual(received, [{ sequence: 9, code: 'KeyA' }]);

  availabilityListener({ moduleId: 'module-2', eligible: true, revision: 7 });
  await waitFor(() => calls.filter(([name]) => name === 'subscribe').length === 2);
  const event = { sequence: 11, code: 'KeyB' };
  inputListener({ moduleId: 'module-2', event });
  assert.deepEqual(received, [{ sequence: 9, code: 'KeyA' }, event]);

  unsubscribe();
  await waitFor(() => calls.filter(([name]) => name === 'unsubscribe').length === 1);
  await services.dispose();
  assert.ok(calls.some(([name]) => name === 'availability-unsubscribe'));
  assert.ok(calls.some(([name]) => name === 'input-unsubscribe'));
});

test('HTTP AbortSignal cancels the module-scoped request and disposal detaches listeners', async () => {
  const requestStarted = deferred();
  const requestResult = deferred();
  const calls = [];
  const bridge = {
    getAutomationKeyboardEligibility: async () => ({ eligible: false, revision: 0 }),
    onAutomationKeyboardAvailability: () => () => {},
    onAutomationKeyboardInput: () => () => {},
    subscribeAutomationKeyboard: async () => {},
    unsubscribeAutomationKeyboard: async () => {},
    automationHttpRequest: (moduleId, requestId, request) => {
      calls.push(['request', moduleId, requestId, request]);
      requestStarted.resolve();
      return requestResult.promise;
    },
    cancelAutomationHttpRequest: async (moduleId, requestId) => {
      calls.push(['cancel', moduleId, requestId]);
      return { canceled: true };
    },
  };
  const services = createAutomationServices('module-http', bridge, ['http.request']);
  const controller = new AbortController();
  const pending = services.http.request({ uri: 'https://example.test', method: 'GET', headers: {}, body: null }, { signal: controller.signal });
  await requestStarted.promise;
  controller.abort();
  await waitFor(() => calls.some(([name]) => name === 'cancel'));
  requestResult.reject(Object.assign(new Error('request cancelled'), { category: 'canceled' }));
  await assert.rejects(pending, /request cancelled/);
  const requestCall = calls.find(([name]) => name === 'request');
  assert.deepEqual(calls.find(([name]) => name === 'cancel'), ['cancel', 'module-http', requestCall[2]]);
  assert.equal(requestCall[3].method, 'GET');
  await services.dispose();
});

test('Work Time CSV saving is exposed only to the Work Time module and forwards the file content', async () => {
  const calls = [];
  const bridge = {
    saveWorkTimeCsv: async (csv) => { calls.push(csv); return true; },
  };
  const services = createAutomationServices('focus-sessions', bridge);
  assert.equal(await services.files.saveWorkTimeCsv('id,duration\n1,1000'), true);
  assert.deepEqual(calls, ['id,duration\n1,1000']);
  await services.dispose();

  const scriptRunner = createAutomationServices('script-runner', bridge);
  await assert.rejects(() => scriptRunner.files.saveWorkTimeCsv('secret'), /only available to Work Time/);
  await scriptRunner.dispose();
});
