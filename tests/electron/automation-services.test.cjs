const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');

async function loadAuthorization() {
  return import(pathToFileURL(path.join(workspace, 'electron', 'automationServiceAuthorization.ts')).href);
}

function state({ selectedTab = 1, visible = true, capabilities = [] } = {}) {
  return {
    selectedTab,
    visible,
    tabs: [
      { slot: 1, id: 'launcher', capabilities },
      { slot: 2, id: 'reserved-2', capabilities: [] },
      { slot: 3, id: 'api', capabilities: [{ id: 'http.request', version: 1 }] },
    ],
  };
}

function deferred() {
  let resolve;
  let reject;
  const promise = new Promise((resolvePromise, rejectPromise) => { resolve = resolvePromise; reject = rejectPromise; });
  return { promise, resolve, reject };
}

async function loadFacade() {
  return import(pathToFileURL(path.join(workspace, 'ui', 'automationServices.ts')).href);
}

async function waitFor(predicate) {
  const deadline = Date.now() + 2_000;
  while (!predicate()) {
    if (Date.now() >= deadline) throw new Error('Timed out waiting for automation service transition.');
    await new Promise((resolve) => setTimeout(resolve, 1));
  }
}

test('typed service IPC requires the main frame and the selected module grant', async () => {
  const { authorizeAutomationServiceCall } = await loadAuthorization();
  const granted = state({ capabilities: [{ id: 'keyboard.input', version: 1 }] });
  assert.equal(authorizeAutomationServiceCall(true, granted, 'launcher', 'keyboard.input', false), 'forward');
  assert.throws(() => authorizeAutomationServiceCall(false, granted, 'launcher', 'keyboard.input', false), /main frame/i);
  assert.throws(() => authorizeAutomationServiceCall(true, granted, 'reserved-2', 'keyboard.input', false), /active module/i);
  assert.throws(() => authorizeAutomationServiceCall(true, granted, 'launcher', 'http.request', false), /capability/i);
});

test('cleanup IPC is allowed for a granted selected module while hidden and becomes a no-op after tab revocation', async () => {
  const { authorizeAutomationServiceCall } = await loadAuthorization();
  const hiddenGranted = state({ visible: false, capabilities: [{ id: 'http.request', version: 1 }] });
  assert.equal(authorizeAutomationServiceCall(true, hiddenGranted, 'launcher', 'http.request', true), 'forward');
  assert.equal(authorizeAutomationServiceCall(true, state({ selectedTab: 2 }), 'launcher', 'http.request', true), 'noop');
  assert.throws(() => authorizeAutomationServiceCall(true, hiddenGranted, 'launcher', 'keyboard.input', true), /capability/i);
});

test('workspace service calls use their own tab and cannot subscribe to global keyboard input', async () => {
  const { authorizeAutomationServiceCall } = await loadAuthorization();
  const backendState = state({ selectedTab: 1, capabilities: [{ id: 'keyboard.input', version: 1 }] });
  const workspace = { role: 'workspace', selectedTab: 3 };
  assert.equal(authorizeAutomationServiceCall(true, backendState, 'api', 'http.request', false, workspace), 'forward');
  assert.throws(() => authorizeAutomationServiceCall(true, backendState, 'launcher', 'keyboard.input', false, {
    role: 'workspace', selectedTab: 1,
  }), /workspace.*keyboard/i);
  assert.throws(() => authorizeAutomationServiceCall(true, backendState, 'launcher', 'http.request', false, {
    role: 'workspace', selectedTab: 3,
  }), /active module/i);
});

test('keyboard facade installs listeners before its eligibility snapshot and reconciles revisions', async () => {
  const { createAutomationServices } = await loadFacade();
  const snapshot = deferred();
  const calls = [];
  let availability;
  let input;
  const bridge = {
    getAutomationKeyboardEligibility: () => { calls.push('snapshot'); return snapshot.promise; },
    onAutomationKeyboardAvailability: (listener) => { calls.push('availability-listener'); availability = listener; return () => {}; },
    onAutomationKeyboardInput: (listener) => { calls.push('input-listener'); input = listener; return () => {}; },
    subscribeAutomationKeyboard: async (moduleId) => calls.push(`subscribe:${moduleId}`),
    unsubscribeAutomationKeyboard: async (moduleId) => calls.push(`unsubscribe:${moduleId}`),
    automationHttpRequest: async () => { throw new Error('unused'); },
    cancelAutomationHttpRequest: async () => ({ canceled: false }),
  };
  const services = createAutomationServices('launcher', bridge, ['keyboard.input']);
  assert.deepEqual(calls.slice(0, 3), ['availability-listener', 'input-listener', 'snapshot']);
  const events = [];
  const unsubscribe = services.keyboard.subscribe((event) => events.push(event));
  availability({ moduleId: 'launcher', eligible: true, revision: 5 });
  await waitFor(() => calls.filter((call) => call.startsWith('subscribe:')).length === 1);
  snapshot.resolve({ eligible: false, revision: 4 });
  await new Promise((resolve) => setTimeout(resolve, 0));
  assert.equal(calls.filter((call) => call.startsWith('subscribe:')).length, 1);
  availability({ moduleId: 'launcher', eligible: 'false', revision: 6 });
  availability({ moduleId: 'another', eligible: true, revision: 6 });
  input({ moduleId: 'launcher', event: { sequence: 1, code: 'KeyA' } });
  assert.deepEqual(events, [{ sequence: 1, code: 'KeyA' }]);
  availability({ moduleId: 'launcher', eligible: false, revision: 6 });
  input({ moduleId: 'launcher', event: { sequence: 2, code: 'KeyB' } });
  assert.deepEqual(events, [{ sequence: 1, code: 'KeyA' }]);
  availability({ moduleId: 'launcher', eligible: true, revision: 7 });
  await waitFor(() => calls.filter((call) => call.startsWith('subscribe:')).length === 2);
  const event = { sequence: 3, code: 'KeyC' };
  input({ moduleId: 'launcher', event });
  assert.deepEqual(events, [{ sequence: 1, code: 'KeyA' }, event]);
  unsubscribe();
  await waitFor(() => calls.includes('unsubscribe:launcher'));
  await services.dispose();
});

test('HTTP abort maps to cancel for the generated module request ID', async () => {
  const { createAutomationServices } = await loadFacade();
  const started = deferred();
  const result = deferred();
  const calls = [];
  const bridge = {
    getAutomationKeyboardEligibility: async () => ({ eligible: false, revision: 0 }),
    onAutomationKeyboardAvailability: () => () => {},
    onAutomationKeyboardInput: () => () => {},
    subscribeAutomationKeyboard: async () => ({ subscribed: false }),
    unsubscribeAutomationKeyboard: async () => ({ unsubscribed: true }),
    automationHttpRequest: (moduleId, requestId) => { calls.push(['request', moduleId, requestId]); started.resolve(); return result.promise; },
    cancelAutomationHttpRequest: async (moduleId, requestId) => { calls.push(['cancel', moduleId, requestId]); return { canceled: true }; },
  };
  const services = createAutomationServices('launcher', bridge, ['http.request']);
  const controller = new AbortController();
  const pending = services.http.request({ uri: 'https://example.test', method: 'GET', headers: {}, body: null }, { signal: controller.signal });
  await started.promise;
  controller.abort();
  await waitFor(() => calls.some(([kind]) => kind === 'cancel'));
  result.resolve({ ok: false, error: { category: 'canceled', message: 'canceled' } });
  await assert.rejects(pending, (error) => error.category === 'canceled' && /canceled/.test(error.message));
  assert.deepEqual(calls[1], ['cancel', 'launcher', calls[0][2]]);
  await services.dispose();
});

test('module actions use declared versions and cancellation stays inside the module scope', async () => {
  const { createAutomationServices } = await loadFacade();
  const calls = [];
  const result = deferred();
  const bridge = {
    dispatchModuleAction: (request) => { calls.push(['dispatch', request]); return result.promise; },
    cancelAutomationModuleAction: async (moduleId, requestId) => { calls.push(['cancel', moduleId, requestId]); return { canceled: true }; },
  };
  const tab = { contractVersion: 1, actions: [{ id: 'runProfile', version: 1 }] };
  const services = createAutomationServices('script-runner', bridge, [], tab);
  const controller = new AbortController();
  const pending = services.modules.dispatch('runProfile', { id: 'daily' }, { signal: controller.signal });
  controller.abort();
  await waitFor(() => calls.some(([kind]) => kind === 'cancel'));
  assert.equal(calls[0][1].moduleId, 'script-runner');
  assert.equal(calls[0][1].actionVersion, 1);
  assert.ok(calls[0][1].requestId);
  assert.deepEqual(calls[1].slice(0, 2), ['cancel', 'script-runner']);
  result.resolve({ contractVersion: 1, status: 'success', message: 'done', data: {}, actions: [] });
  await pending;
  await assert.rejects(services.modules.dispatch('unregisteredAction', {}), /not declared/i);
  await services.dispose();
});

test('HTTP service error categories survive the main-to-renderer IPC boundary', async () => {
  const { createAutomationHttpIpcFailure } = await import(pathToFileURL(path.join(workspace, 'contracts', 'automationHttpIpc.ts')).href);
  const backendNetworkError = Object.assign(new Error('The target network is not allowed.'), {
    data: { category: 'networkNotAllowed' },
  });
  const backendHostError = Object.assign(new Error('The target host is not allowed.'), {
    data: { category: 'hostNotAllowed' },
  });

  const networkEnvelope = structuredClone(createAutomationHttpIpcFailure(backendNetworkError));
  const hostEnvelope = structuredClone(createAutomationHttpIpcFailure(backendHostError));
  const responses = [networkEnvelope, hostEnvelope];
  const { AutomationServiceError, createAutomationServices } = await loadFacade();
  const services = createAutomationServices('launcher', {
    getAutomationKeyboardEligibility: async () => ({ eligible: false, revision: 0 }),
    onAutomationKeyboardAvailability: () => () => {},
    onAutomationKeyboardInput: () => () => {},
    subscribeAutomationKeyboard: async () => ({ subscribed: false }),
    unsubscribeAutomationKeyboard: async () => ({ unsubscribed: true }),
    automationHttpRequest: async () => responses.shift(),
    cancelAutomationHttpRequest: async () => ({ canceled: false }),
  }, ['http.request']);
  const request = { uri: 'https://example.test', method: 'GET', headers: {}, body: null };

  await assert.rejects(services.http.request(request), (error) =>
    error instanceof AutomationServiceError && error.category === 'networkNotAllowed'
      && error.message === 'The target network is not allowed.');
  await assert.rejects(services.http.request(request), (error) =>
    error instanceof AutomationServiceError && error.category === 'hostNotAllowed'
      && error.message === 'The target host is not allowed.');
  await services.dispose();
});

test('browser preview keeps keyboard inert and rejects HTTP without networking', async () => {
  const { createBrowserPreviewBridge } = await import(pathToFileURL(path.join(workspace, 'ui', 'bridge.ts')).href);
  const { backendUiStateSchema } = await import(pathToFileURL(path.join(workspace, 'contracts', 'rpc.ts')).href);
  const preview = createBrowserPreviewBridge();
  const initial = await preview.getInitialState();
  assert.equal(backendUiStateSchema.safeParse(initial.state).success, true);
  assert.equal(initial.state.tabRegistryVersion, 3);
  assert.ok(initial.state.tabs.every((tab) => Array.isArray(tab.capabilities)));
  assert.deepEqual(await preview.getAutomationKeyboardEligibility('launcher'), { eligible: false, revision: 0 });
  assert.deepEqual(await preview.subscribeAutomationKeyboard('launcher'), { subscribed: false });
  const { createAutomationServices } = await loadFacade();
  const services = createAutomationServices('launcher', preview, ['http.request']);
  await assert.rejects(services.http.request({
    uri: 'https://example.test', method: 'GET', headers: {}, body: null,
  }), (error) => error.category === 'transportFailure' && /unavailable in browser preview/.test(error.message));
  await services.dispose();
});
