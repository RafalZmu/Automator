const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');
async function loadContextRegistry() {
  return import(pathToFileURL(path.join(workspace, 'electron', 'windowContextRegistry.ts')).href);
}

async function loadHostContextInjection() {
  return import(pathToFileURL(path.join(workspace, 'electron', 'hostWindowContext.ts')).href);
}

test('host-owned BrowserWindow contexts preserve independent roles and active tabs', async () => {
  const { WindowContextRegistry } = await loadContextRegistry();
  const contexts = new WindowContextRegistry();
  const launcherContents = {};
  const workspaceContents = {};
  contexts.register(launcherContents, 'launcher');
  contexts.register(workspaceContents, 'workspace');
  contexts.setSelectedTab(launcherContents, 1);
  contexts.setSelectedTab(workspaceContents, 5);

  assert.deepEqual(contexts.get(launcherContents), { role: 'launcher', selectedTab: 1 });
  assert.deepEqual(contexts.get(workspaceContents), { role: 'workspace', selectedTab: 5 });
  assert.equal(contexts.get({}), null);
  assert.throws(() => contexts.setSelectedTab(workspaceContents, 10), /tab slot/i);
});

test('window context disposal removes only the owning renderer', async () => {
  const { WindowContextRegistry } = await loadContextRegistry();
  const contexts = new WindowContextRegistry();
  const launcherContents = {};
  const workspaceContents = {};
  const disposeLauncher = contexts.register(launcherContents, 'launcher');
  contexts.register(workspaceContents, 'workspace');
  disposeLauncher();
  assert.equal(contexts.get(launcherContents), null);
  assert.deepEqual(contexts.get(workspaceContents), { role: 'workspace', selectedTab: 1 });
});

test('window context disposal remains valid after the local tab changes', async () => {
  const { WindowContextRegistry } = await loadContextRegistry();
  const contexts = new WindowContextRegistry();
  const renderer = {};
  const dispose = contexts.register(renderer, 'workspace');
  contexts.setSelectedTab(renderer, 5);
  dispose();
  assert.equal(contexts.get(renderer), null);
});

test('workspace state projection isolates launcher visibility, tab selection and transient input state', async () => {
  const { projectStateForWindow } = await loadContextRegistry();
  const state = {
    visible: false, selectedTab: 1, query: 'foo', mode: 'catalog', aliasEditCandidate: { id: 'app' },
    error: 'launcher error', busy: true, matchKind: 'Exact', matchedBindingId: 'app',
  };
  assert.deepEqual(projectStateForWindow(state, { role: 'workspace', selectedTab: 5 }), {
    ...state, visible: true, selectedTab: 5, query: '', mode: 'launcher', aliasEditCandidate: null,
    error: null, busy: false, matchKind: 'None', matchedBindingId: null,
  });
  assert.equal(projectStateForWindow(state, { role: 'launcher', selectedTab: 1 }), state);
});

test('host context injection overwrites renderer claims', async () => {
  const { injectHostWindowContext } = await loadHostContextInjection();
  const request = injectHostWindowContext({
    moduleId: 'api',
    hostWindowContext: { role: 'launcher', selectedTab: 1 },
  }, { role: 'workspace', selectedTab: 3 });
  assert.deepEqual(request, { moduleId: 'api', hostWindowContext: { role: 'workspace', selectedTab: 3 } });
});
