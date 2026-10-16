const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');

const workspace = path.resolve(__dirname, '../..');
const powershell = 'C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe';

async function launchHost(name) {
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'electron-mcp', name, randomUUID());
  await fs.mkdir(dataDirectory, { recursive: true });
  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: path.join(dataDirectory, 'backend'),
      AUTOMATOR_BROWSER_NODE_PATH: process.execPath,
      AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_BUILD_ID: `${name}-${process.pid}`,
    },
    timeout: 60_000,
  });
  const page = await app.firstWindow();
  page.setDefaultTimeout(15_000);
  await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible', timeout: 60_000 });
  return { app, page, dataDirectory };
}

/** The renderer never sees the token; the test reads it from the clipboard the way a user would. */
async function connect({ app, page }) {
  const status = await page.evaluate(() => window.automator.getMcpStatus());
  assert.equal(status.state, 'listening', 'the server starts with the app');
  assert.equal(await page.evaluate(() => window.automator.copyMcpCommand()), true);
  const command = await app.evaluate(({ clipboard }) => clipboard.readText());
  const token = /Bearer (\S+)"$/.exec(command)?.[1];
  assert.ok(token, 'the copied command carries the bearer token');
  assert.equal(JSON.stringify(status).includes(token), false, 'the status never exposes the token');
  const call = async (method, params, headers = { authorization: `Bearer ${token}` }) => {
    const response = await fetch(`http://127.0.0.1:${status.port}/mcp`, {
      method: 'POST', headers: { 'content-type': 'application/json', ...headers },
      body: JSON.stringify({ jsonrpc: '2.0', id: 1, method, params }),
    });
    return { status: response.status, body: response.status === 200 ? await response.json() : null };
  };
  const tool = async (name, args = {}) => {
    const { body } = await call('tools/call', { name, arguments: args });
    const text = body.result.content[0].text;
    return { isError: body.result.isError === true, payload: JSON.parse(text) };
  };
  return { status, token, call, tool };
}

async function moduleAction(page, moduleId, actionId, input) {
  const result = await page.evaluate(async ({ moduleId, actionId, input }) => {
    const { state } = await window.automator.getInitialState();
    const tab = state.tabs.find((candidate) => candidate.id === moduleId);
    const action = tab.actions.find((candidate) => candidate.id === actionId);
    await window.automator.selectTab(tab.slot);
    const reply = await window.automator.dispatchModuleAction({
      requestId: crypto.randomUUID(), contractVersion: tab.contractVersion, moduleId, actionId, actionVersion: action.version, input,
    });
    await window.automator.selectTab(1);
    return reply;
  }, { moduleId, actionId, input });
  assert.notEqual(result.status, 'error', result.message);
  return result;
}

test('the MCP server starts with the app, authenticates callers and lists the tools', async () => {
  const host = await launchHost('startup');
  try {
    const { call, status } = await connect(host);
    assert.match(status.url, /^http:\/\/127\.0\.0\.1:\d+\/mcp$/);
    assert.equal((await call('ping', {}, {})).status, 401);
    assert.equal((await call('ping', {}, { authorization: 'Bearer nope' })).status, 401);
    const listed = (await call('tools/list', {})).body.result.tools.map((tool) => tool.name);
    for (const name of ['list_scripts', 'run_script', 'get_run', 'cancel_run', 'run_browser_automation', 'get_running_timers', 'register_time'])
      assert.ok(listed.includes(name), name);
  } finally { await host.app.close(); }
});

test('Work Time tools work while another tab is selected and match what the Work Time module stores', async () => {
  const host = await launchHost('work-time');
  try {
    const { tool } = await connect(host);
    assert.equal((await tool('get_running_timers')).payload.running.length, 0);

    await moduleAction(host.page, 'focus-sessions', 'start', { description: 'Agent visible timer' });
    const running = (await tool('get_running_timers')).payload;
    assert.equal(running.running.length, 1);
    assert.equal(running.running[0].description, 'Agent visible timer');
    await moduleAction(host.page, 'focus-sessions', 'pause', { id: running.running[0].id });
    assert.equal((await tool('get_running_timers')).payload.paused.length, 1);

    const registered = await tool('register_time', { description: 'Written by agent', minutes: 15, tags: ['agent'] });
    assert.equal(registered.isError, false, JSON.stringify(registered.payload));
    assert.equal(registered.payload.registered.durationMilliseconds, 15 * 60_000);

    const snapshot = await moduleAction(host.page, 'focus-sessions', 'getSnapshot', {});
    const entry = snapshot.data.history.find((candidate) => candidate.id === registered.payload.registered.id);
    assert.ok(entry, 'the entry is in the Work Time history');
    assert.equal(entry.description, 'Written by agent');
    assert.deepEqual(entry.tags, ['agent']);
    assert.equal(entry.durationMilliseconds, 15 * 60_000);

    const future = await tool('register_time', { description: 'Too late', minutes: 5, endedAt: new Date(Date.now() + 3_600_000).toISOString() });
    assert.equal(future.isError, true);
  } finally { await host.app.close(); }
});

test('scripts are listed with typed inputs, run with values, polled and canceled', async () => {
  const host = await launchHost('scripts');
  try {
    const greet = path.join(host.dataDirectory, 'greet.ps1');
    const slow = path.join(host.dataDirectory, 'slow.ps1');
    await fs.writeFile(greet, '[Console]::WriteLine("hello " + $args[0])');
    await fs.writeFile(slow, 'Start-Sleep -Seconds 60');
    const base = { interpreter: 'powershell', interpreterPath: powershell, workingDirectory: host.dataDirectory, outputMode: 'text', aiAccess: true };
    await moduleAction(host.page, 'script-runner', 'saveProfile', {
      ...base, id: 'greet', name: 'Greet', scriptPath: greet, arguments: [], timeoutSeconds: 30,
      parameters: [{ key: 'who', label: 'Who', type: 'text', required: true, argumentIndex: 0, description: 'Person to greet', example: 'Ada' }],
    });
    await moduleAction(host.page, 'script-runner', 'saveProfile', { ...base, id: 'slow', name: 'Slow', scriptPath: slow, arguments: [], timeoutSeconds: 120 });
    await moduleAction(host.page, 'script-runner', 'saveProfile', { ...base, aiAccess: false, id: 'private', name: 'Private', scriptPath: greet, arguments: [], timeoutSeconds: 30 });

    const { tool } = await connect(host);
    const listed = (await tool('list_scripts')).payload.scripts;
    assert.deepEqual(listed.map((script) => script.id).sort(), ['greet', 'slow']);
    const detail = (await tool('get_script', { id: 'greet' })).payload;
    assert.equal(detail.inputs[0].hint, 'Person to greet');
    assert.equal(detail.inputs[0].example, 'Ada');
    assert.equal((await tool('get_script', { id: 'private' })).isError, true, 'a script without AI access is not visible');
    assert.equal((await tool('run_script', { id: 'private' })).isError, true, 'a script without AI access is refused');

    const missing = await tool('run_script', { id: 'greet' });
    assert.equal(missing.payload.state, 'failed', 'a required input is enforced by the backend');

    const ran = (await tool('run_script', { id: 'greet', values: { who: 'Grace' }, waitSeconds: 30 })).payload;
    assert.equal(ran.state, 'success', JSON.stringify(ran));
    assert.match(ran.result.stdout, /hello Grace/);

    const started = (await tool('run_script', { id: 'slow', waitSeconds: 0 })).payload;
    assert.equal(started.state, 'running');
    assert.equal((await tool('get_run', { runId: started.runId })).payload.state, 'running');
    assert.equal((await tool('cancel_run', { runId: started.runId })).payload.canceled, true);
    const finished = (await tool('get_run', { runId: started.runId, waitSeconds: 20 })).payload;
    assert.equal(finished.state, 'canceled');
    const runs = (await tool('list_runs')).payload.runs;
    assert.equal(runs.length, 3);
  } finally { await host.app.close(); }
});

test('the Settings switch stops and restarts the server, and rotating the token revokes the old one', async () => {
  const host = await launchHost('switch');
  try {
    const first = await connect(host);
    assert.equal((await first.call('ping', {})).status, 200);

    const off = await host.page.evaluate(() => window.automator.setMcpEnabled(false));
    assert.equal(off.state, 'off');
    await assert.rejects(first.call('ping', {}));

    const on = await host.page.evaluate(() => window.automator.setMcpEnabled(true));
    assert.equal(on.state, 'listening');
    const second = await connect(host);
    assert.equal((await second.call('ping', {})).status, 200);

    await host.page.evaluate(() => window.automator.rotateMcpToken());
    assert.equal((await second.call('ping', {})).status, 401, 'the old token stops working immediately');
    const third = await connect(host);
    assert.notEqual(third.token, second.token);
    assert.equal((await third.call('ping', {})).status, 200);
  } finally { await host.app.close(); }
});
