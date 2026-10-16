import assert from 'node:assert/strict';
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { randomUUID } from 'node:crypto';
import test from 'node:test';
import { McpToolError } from '../electron/mcp/backendBridge.ts';
import { loadMcpConfig, saveMcpConfig, claudeAddCommand } from '../electron/mcp/config.ts';
import { McpController } from '../electron/mcp/controller.ts';
import { RunRegistry } from '../electron/mcp/runRegistry.ts';
import { checkRequestAccess, handleMcpMessage, McpHttpServer } from '../electron/mcp/server.ts';
import { durationMinutes, MCP_TOOLS } from '../electron/mcp/tools.ts';

const TOKEN = 'a'.repeat(43);
const NOW = new Date('2026-10-14T12:00:00.000Z');
const ok = (data, message = 'ok') => ({ contractVersion: 1, status: 'success', message, data, actions: [] });
const failed = (message) => ({ contractVersion: 1, status: 'error', message, data: {}, actions: [] });

function fakeBackend(handlers = {}) {
  const calls = [];
  const cancels = [];
  return {
    calls, cancels,
    async call(moduleId, actionId, input, requestId) {
      calls.push({ moduleId, actionId, input, requestId });
      const handler = handlers[`${moduleId}/${actionId}`];
      if (!handler) throw new McpToolError(`No fake handler for ${moduleId}/${actionId}.`);
      return handler(input, requestId);
    },
    async cancel(moduleId, requestId) {
      cancels.push({ moduleId, requestId });
      return handlers.cancel ? handlers.cancel(moduleId, requestId) : true;
    },
  };
}

function setup(handlers) {
  const backend = fakeBackend(handlers);
  const runs = new RunRegistry((moduleId, requestId) => backend.cancel(moduleId, requestId));
  const logs = [];
  const options = { getToken: () => TOKEN, backend, runs, version: 'test', log: (event, message, properties) => logs.push({ event, message, properties }), now: () => NOW };
  const rpc = (method, params, id = 1) => handleMcpMessage({ jsonrpc: '2.0', id, method, params }, options);
  const tool = async (name, args = {}) => {
    const reply = await rpc('tools/call', { name, arguments: args });
    const text = reply.result.content[0].text;
    let payload;
    try { payload = JSON.parse(text); } catch { payload = text; }
    return { payload, isError: reply.result.isError === true };
  };
  return { backend, runs, logs, options, rpc, tool };
}

function isolatedDirectory(name) {
  const directory = path.resolve('artifacts/test-data/mcp-server', name, randomUUID());
  fs.mkdirSync(directory, { recursive: true });
  return directory;
}

const fakeProtector = {
  encrypt: (value) => Buffer.from(value, 'utf8').toString('base64'),
  decrypt: (value) => Buffer.from(value, 'base64').toString('utf8'),
};

function post(port, { path: requestPath = '/mcp', method = 'POST', headers = {}, body } = {}) {
  return new Promise((resolve, reject) => {
    const request = http.request({ host: '127.0.0.1', port, path: requestPath, method, headers: {
      host: `127.0.0.1:${port}`, 'content-type': 'application/json', ...headers } }, (response) => {
      const chunks = [];
      response.on('data', (chunk) => chunks.push(chunk));
      response.on('end', () => resolve({ status: response.statusCode, headers: response.headers, text: Buffer.concat(chunks).toString('utf8') }));
    });
    request.on('error', reject);
    request.end(body);
  });
}

test('access control requires loopback Host, a safe Origin and the bearer token', () => {
  const good = { host: '127.0.0.1:5000', authorization: `Bearer ${TOKEN}` };
  assert.deepEqual(checkRequestAccess(good, 5000, TOKEN), { ok: true });
  assert.deepEqual(checkRequestAccess({ ...good, host: 'localhost:5000' }, 5000, TOKEN), { ok: true });
  assert.deepEqual(checkRequestAccess({ ...good, host: 'evil.example:5000' }, 5000, TOKEN), { ok: false, status: 403, reason: 'host' });
  assert.deepEqual(checkRequestAccess({ ...good, host: '127.0.0.1:5001' }, 5000, TOKEN), { ok: false, status: 403, reason: 'host' });
  assert.deepEqual(checkRequestAccess({ ...good, origin: 'https://evil.example' }, 5000, TOKEN), { ok: false, status: 403, reason: 'origin' });
  assert.deepEqual(checkRequestAccess({ ...good, origin: 'http://localhost:5000' }, 5000, TOKEN), { ok: true });
  assert.equal(checkRequestAccess({ host: good.host }, 5000, TOKEN).reason, 'token');
  assert.equal(checkRequestAccess({ ...good, authorization: `Bearer ${'b'.repeat(43)}` }, 5000, TOKEN).status, 401);
  assert.equal(checkRequestAccess({ ...good, authorization: `Basic ${TOKEN}` }, 5000, TOKEN).status, 401);
});

test('initialize negotiates the protocol and advertises tools only', async () => {
  const { rpc } = setup();
  const supported = (await rpc('initialize', { protocolVersion: '2025-03-26' })).result;
  assert.equal(supported.protocolVersion, '2025-03-26');
  assert.deepEqual(supported.capabilities, { tools: { listChanged: false } });
  assert.equal(supported.serverInfo.name, 'automator');
  assert.equal((await rpc('initialize', { protocolVersion: '1999-01-01' })).result.protocolVersion, '2025-06-18');
  assert.deepEqual((await rpc('ping')).result, {});
  assert.equal((await rpc('resources/list')).error.code, -32601);
  assert.equal(await handleMcpMessage({ jsonrpc: '2.0', method: 'notifications/initialized' }, setup().options), null);
});

test('tools/list publishes object schemas for every tool', async () => {
  const { rpc } = setup();
  const { tools } = (await rpc('tools/list')).result;
  assert.deepEqual(tools.map((tool) => tool.name).sort(), [
    'cancel_run', 'get_run', 'get_running_timers', 'get_script', 'list_browser_automations', 'list_runs',
    'list_scripts', 'register_time', 'run_browser_automation', 'run_script',
  ]);
  for (const tool of tools) {
    assert.equal(tool.inputSchema.type, 'object', tool.name);
    assert.equal(typeof tool.description, 'string');
    assert.equal(typeof tool.annotations.readOnlyHint, 'boolean');
  }
  assert.equal(tools.find((tool) => tool.name === 'run_script').annotations.readOnlyHint, false);
  assert.equal(tools.find((tool) => tool.name === 'get_running_timers').annotations.readOnlyHint, true);
  assert.equal(MCP_TOOLS.length, tools.length);
});

test('invalid arguments and unknown tools are reported without calling the backend', async () => {
  const { tool, rpc, backend } = setup();
  const invalid = await tool('run_script', { id: 'Bad Id!' });
  assert.equal(invalid.isError, true);
  assert.match(invalid.payload.error, /Invalid arguments/);
  assert.equal((await rpc('tools/call', { name: 'nope', arguments: {} })).error.code, -32602);
  assert.equal(backend.calls.length, 0);
});

test('get_running_timers separates the running timer from paused ones', async () => {
  const { tool } = setup({
    'focus-sessions/getSnapshot': () => ok({
      timers: [
        { id: 'a', description: 'Write docs', status: 'running', elapsedMilliseconds: 61000, startedUtc: '2026-10-14T11:00:00Z', sampledUtc: 'x', segments: [] },
        { id: 'b', description: 'Review', status: 'paused', elapsedMilliseconds: 5000, startedUtc: '2026-10-14T10:00:00Z', sampledUtc: 'x', segments: [] },
      ],
      pendingEntries: [{ id: 'p' }], history: [], historyTotal: 0,
    }),
  });
  const { payload } = await tool('get_running_timers');
  assert.equal(payload.running.length, 1);
  assert.equal(payload.running[0].description, 'Write docs');
  assert.equal(payload.paused[0].id, 'b');
  assert.equal(payload.pendingDrafts, 1);
  assert.equal(payload.running[0].segments, undefined);
});

test('register_time converts a duration into an interval ending just before now', async () => {
  const { tool, backend } = setup({
    'focus-sessions/createEntry': (input) => ok({ history: [{ id: 'new1', ...input, durationMilliseconds: 5_400_000 }], pendingEntries: [], timers: [], historyTotal: 1 }),
  });
  const { payload, isError } = await tool('register_time', { description: '  Planning  ', hours: 1, minutes: 30, tags: ['ops'] });
  assert.equal(isError, false);
  const sent = backend.calls[0].input;
  assert.equal(sent.stoppedUtc, '2026-10-14T11:59:59.000Z');
  assert.equal(sent.startedUtc, '2026-10-14T10:29:59.000Z');
  assert.equal(sent.description, 'Planning');
  assert.deepEqual(sent.tags, ['ops']);
  assert.equal(payload.registered.id, 'new1');
});

test('register_time honors endedAt and rejects the future or an empty duration', async () => {
  const { tool, backend } = setup({
    'focus-sessions/createEntry': (input) => ok({ history: [{ id: 'x', ...input }] }),
  });
  await tool('register_time', { description: 'Earlier', minutes: 15, endedAt: '2026-10-14T09:00:00Z' });
  assert.equal(backend.calls[0].input.startedUtc, '2026-10-14T08:45:00.000Z');
  assert.equal((await tool('register_time', { description: 'Later', minutes: 5, endedAt: '2026-10-14T13:00:00Z' })).isError, true);
  assert.equal((await tool('register_time', { description: 'None' })).isError, true);
  assert.equal(backend.calls.length, 1);
  assert.throws(() => durationMinutes(0, 0), McpToolError);
  assert.equal(durationMinutes(2, 5), 125);
});

test('register_time surfaces a backend validation failure as a tool error', async () => {
  const { tool } = setup({ 'focus-sessions/createEntry': () => failed('The work interval is invalid.') });
  const { payload, isError } = await tool('register_time', { description: 'x', minutes: 5 });
  assert.equal(isError, true);
  assert.match(payload.error, /interval is invalid/);
});

test('list_scripts and get_script expose typed inputs and fall back to template parameters', async () => {
  const profiles = [
    { id: 'plain', name: 'Plain', aiAccess: true, interpreter: 'powershell', outputMode: 'text', timeoutSeconds: 30, scriptPath: 'C:\\s.ps1',
      parameters: [{ key: 'folder', label: 'Folder', type: 'directory', required: true, argumentIndex: 0, description: 'Where to look', example: 'C:\\data' }],
      values: { folder: 'secret-remembered' } },
    { id: 'tpl', name: 'From template', aiAccess: true, interpreter: 'python', outputMode: 'json', timeoutSeconds: 60, scriptPath: 'C:\\t.py', templateOrigin: { id: 'tpl-1', version: 1 } },
  ];
  const { tool } = setup({
    'script-runner/listProfiles': () => ok({ profiles }),
    'script-runner/listTemplates': () => ok({ templates: [{ id: 'tpl-1', description: 'Template text',
      parameters: [{ key: 'token', label: 'Token', type: 'text', required: true, sensitive: true, argumentIndex: 0 }] }] }),
  });
  const list = (await tool('list_scripts')).payload.scripts;
  assert.equal(list.length, 2);
  assert.deepEqual(list[0].inputs, [{ key: 'folder', type: 'directory', required: true }]);
  assert.equal(list[1].fromTemplate, true);
  const plain = (await tool('get_script', { id: 'plain' })).payload;
  assert.equal(plain.inputs[0].hint, 'Where to look');
  assert.equal(plain.inputs[0].example, 'C:\\data');
  assert.equal(JSON.stringify(plain).includes('secret-remembered'), false);
  const template = (await tool('get_script', { id: 'tpl' })).payload;
  assert.equal(template.description, 'Template text');
  assert.equal(template.inputs[0].sensitive, true);
  assert.equal((await tool('get_script', { id: 'missing' })).isError, true);
});

test('scripts without the AI can use flag are hidden from agents and refused when called', async () => {
  const profiles = [
    { id: 'shared', name: 'Shared', interpreter: 'python', outputMode: 'text', timeoutSeconds: 30, scriptPath: 'C:\\a.py', aiAccess: true },
    { id: 'off', name: 'Off', interpreter: 'python', outputMode: 'text', timeoutSeconds: 30, scriptPath: 'C:\\b.py', aiAccess: false },
    { id: 'legacy', name: 'Legacy', interpreter: 'python', outputMode: 'text', timeoutSeconds: 30, scriptPath: 'C:\\c.py' },
    { id: 'truthy', name: 'Truthy', interpreter: 'python', outputMode: 'text', timeoutSeconds: 30, scriptPath: 'C:\\d.py', aiAccess: 'true' },
  ];
  const { tool, backend } = setup({
    'script-runner/listProfiles': () => ok({ profiles }),
    'script-runner/runProfile': () => ok({ profileId: 'shared', exitCode: 0, timedOut: false, stdout: 'ok', stderr: '', durationMilliseconds: 1 }),
  });
  assert.deepEqual((await tool('list_scripts')).payload.scripts.map((script) => script.id), ['shared']);
  assert.equal((await tool('get_script', { id: 'shared' })).isError, false);
  for (const id of ['off', 'legacy', 'truthy', 'missing']) {
    assert.equal((await tool('get_script', { id })).isError, true, `get_script ${id}`);
    const run = await tool('run_script', { id });
    assert.equal(run.isError, true, `run_script ${id}`);
    assert.match(run.payload.error, /No script with id/);
  }
  assert.equal(backend.calls.some((call) => call.actionId === 'runProfile'), false);
  assert.equal((await tool('run_script', { id: 'shared' })).payload.state, 'success');
});

test('run_script returns the result inline when the script finishes in time', async () => {
  const { tool, backend } = setup({
    'script-runner/listProfiles': () => ok({ profiles: [{ id: 'p', aiAccess: true }] }),
    'script-runner/runProfile': () => ok({ profileId: 'p', exitCode: 0, timedOut: false, stdout: 'hi', stderr: '', durationMilliseconds: 5 }),
  });
  const { payload } = await tool('run_script', { id: 'p', values: { a: 'b', flag: true } });
  assert.equal(payload.state, 'success');
  assert.equal(payload.result.stdout, 'hi');
  assert.deepEqual(backend.calls.at(-1).input, { id: 'p', values: { a: 'b', flag: true } });
  assert.match(backend.calls.at(-1).requestId, /^mcp-/);
});

test('a slow run returns a runId, can be polled, and can be canceled', async () => {
  let rejectRun;
  const { tool, runs } = setup({
    'script-runner/listProfiles': () => ok({ profiles: [{ id: 'slow', aiAccess: true }] }),
    'script-runner/runProfile': () => new Promise((_resolve, reject) => { rejectRun = reject; }),
    cancel: () => { rejectRun(Object.assign(new Error('canceled'), { code: -32800, data: { category: 'canceled' } })); return true; },
  });
  const started = (await tool('run_script', { id: 'slow', waitSeconds: 0 })).payload;
  assert.equal(started.state, 'running');
  assert.equal(started.result, undefined);
  assert.equal((await tool('get_run', { runId: started.runId })).payload.state, 'running');
  const canceled = (await tool('cancel_run', { runId: started.runId })).payload;
  assert.equal(canceled.canceled, true);
  const finished = (await tool('get_run', { runId: started.runId, waitSeconds: 2 })).payload;
  assert.equal(finished.state, 'canceled');
  assert.equal(runs.list().length, 1);
  assert.equal((await tool('list_runs')).payload.runs[0].runId, started.runId);
  assert.equal((await tool('get_run', { runId: 'unknown' })).isError, true);
});

test('run states map timeouts, failures and backend errors', async () => {
  const { tool } = setup({
    'script-runner/listProfiles': () => ok({ profiles: ['timeout', 'fail', 'boom'].map((id) => ({ id, aiAccess: true })) }),
    'script-runner/runProfile': (input) => {
      if (input.id === 'timeout') return ok({ timedOut: true, exitCode: null, stdout: '', stderr: '' });
      if (input.id === 'fail') return { ...failed('Script exited with code 2.'), data: { exitCode: 2, timedOut: false } };
      throw new McpToolError('The backend is down.');
    },
  });
  assert.equal((await tool('run_script', { id: 'timeout' })).payload.state, 'timedOut');
  const fail = (await tool('run_script', { id: 'fail' })).payload;
  assert.equal(fail.state, 'failed');
  assert.equal(fail.result.exitCode, 2);
  const broken = (await tool('run_script', { id: 'boom' })).payload;
  assert.equal(broken.state, 'error');
  assert.match(broken.message, /backend is down/);
});

test('the run registry caps concurrent runs and keeps only the newest finished runs', async () => {
  const pending = [];
  const registry = new RunRegistry(async () => true, { maxRuns: 3, maxConcurrent: 2 });
  const spec = (execute) => ({ kind: 'script', moduleId: 'script-runner', targetId: 't', execute });
  const first = registry.start(spec(() => new Promise((resolve) => pending.push(resolve))));
  const second = registry.start(spec(() => new Promise((resolve) => pending.push(resolve))));
  assert.throws(() => registry.start(spec(async () => ok({}))), /At most 2 runs/);
  await new Promise((resolve) => setImmediate(resolve));
  pending.forEach((resolve) => resolve(ok({})));
  await registry.wait(first.runId, 1000);
  await registry.wait(second.runId, 1000);
  const ids = [];
  for (let index = 0; index < 5; index += 1) {
    const record = registry.start(spec(async () => ok({})));
    ids.push(record.runId);
    await registry.wait(record.runId, 1000);
  }
  assert.equal(registry.list().length, 3);
  assert.equal(registry.get(ids[0]), undefined);
  assert.equal(registry.get(ids[4]).state, 'success');
});

test('browser tools validate their target and forward typed requests', async () => {
  const { tool, backend } = setup({
    'browser-automation/listProfiles': () => ok({ profiles: [{ id: 'site', name: 'Site', startUrl: 'https://example.com', allowedHosts: ['example.com'], allowLocalNetwork: true }], truncated: false }),
    'browser-automation/getExplorerState': () => ok({ projectRoot: 'C:\\p', runner: { available: true }, files: [{ path: 'a.spec.ts', tests: [{ id: 't1', titlePath: ['suite', 'case'], tags: ['smoke'] }] }] }),
    'browser-automation/runAction': () => ok({ currentUrl: 'https://example.com', title: 'Example', text: null, links: [] }),
    'browser-automation/runTests': () => ok({ timedOut: false, exitCode: 0, summary: { passed: 1, failed: 0 } }),
  });
  const list = (await tool('list_browser_automations', { includeTests: true })).payload;
  assert.equal(list.profiles[0].allowLocalNetwork, undefined);
  assert.equal(list.tests.files[0].tests[0].title, 'suite > case');
  assert.equal((await tool('run_browser_automation', { target: 'profile' })).isError, true);
  assert.equal((await tool('run_browser_automation', { target: 'tests', kind: 'tag' })).isError, true);
  const action = (await tool('run_browser_automation', { target: 'profile', id: 'site', action: { kind: 'readTitle' } })).payload;
  assert.equal(action.state, 'success');
  assert.deepEqual(backend.calls.at(-1).input, { id: 'site', action: { kind: 'readTitle' } });
  const tests = (await tool('run_browser_automation', { target: 'tests', kind: 'tag', tag: 'smoke', timeoutSeconds: 120 })).payload;
  assert.equal(tests.state, 'success');
  assert.deepEqual(backend.calls.at(-1).input, { kind: 'tag', tag: 'smoke', timeoutSeconds: 120 });
});

test('tool failures never leak unexpected internal error text', async () => {
  const { tool, logs } = setup({ 'focus-sessions/getSnapshot': () => { throw new TypeError('secret internal path C:\\Users\\x'); } });
  const { payload, isError } = await tool('get_running_timers');
  assert.equal(isError, true);
  assert.equal(payload.error.includes('secret'), false);
  assert.equal(logs.some((entry) => entry.event === 'Mcp.ToolCall' && entry.properties.status === 'error'), true);
  assert.equal(logs.every((entry) => !JSON.stringify(entry).includes('"values"')), true);
});

test('the HTTP server authenticates, restricts the surface and answers MCP calls', async () => {
  const { options } = setup({ 'focus-sessions/getSnapshot': () => ok({ timers: [], pendingEntries: [], history: [] }) });
  const server = new McpHttpServer(options);
  const port = await server.start(0);
  try {
    const auth = { authorization: `Bearer ${TOKEN}` };
    const body = JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'tools/call', params: { name: 'get_running_timers', arguments: {} } });
    assert.equal((await post(port, { body })).status, 401);
    assert.equal((await post(port, { body, headers: { authorization: 'Bearer wrong' } })).status, 401);
    assert.equal((await post(port, { body, headers: { ...auth, host: 'evil.example' } })).status, 403);
    assert.equal((await post(port, { body, headers: { ...auth, origin: 'https://evil.example' } })).status, 403);
    assert.equal((await post(port, { method: 'GET', headers: auth })).status, 405);
    assert.equal((await post(port, { path: '/other', body, headers: auth })).status, 404);
    assert.equal((await post(port, { body, headers: { ...auth, 'content-type': 'text/plain' } })).status, 415);
    assert.equal((await post(port, { body: '{not json', headers: auth })).status, 400);
    assert.equal((await post(port, { body: '[]', headers: auth })).status, 400);
    assert.equal((await post(port, { body: 'x'.repeat(300_000), headers: auth }).catch(() => ({ status: 413 }))).status, 413);
    const accepted = await post(port, { body: JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }), headers: auth });
    assert.equal(accepted.status, 202);
    const reply = await post(port, { body, headers: auth });
    assert.equal(reply.status, 200);
    assert.equal(JSON.parse(JSON.parse(reply.text).result.content[0].text).summary, 'No timer is running.');
  } finally { await server.stop(); }
  await assert.rejects(post(port, { body: '{}' }));
});

test('the config persists an encrypted token and recovers from a damaged file', () => {
  const directory = isolatedDirectory('config');
  const file = path.join(directory, 'mcp.json');
  const first = loadMcpConfig(file, fakeProtector);
  assert.equal(first.enabled, true);
  assert.equal(first.port, 47821);
  assert.ok(first.token.length >= 32);
  assert.equal(fs.readFileSync(file, 'utf8').includes(first.token), false);
  assert.deepEqual(loadMcpConfig(file, fakeProtector), first);
  saveMcpConfig(file, { ...first, enabled: false, port: 50000 }, fakeProtector);
  assert.deepEqual(loadMcpConfig(file, fakeProtector), { ...first, enabled: false, port: 50000 });
  fs.writeFileSync(file, '{"version":1,"enabled":true,"port":80,"tokenCipher":"x"}');
  const repaired = loadMcpConfig(file, fakeProtector);
  assert.notEqual(repaired.token, first.token);
  assert.equal(repaired.port, 47821);
  assert.equal(claudeAddCommand(1234, 'tok'), 'claude mcp add --transport http automator http://127.0.0.1:1234/mcp --header "Authorization: Bearer tok"');
});

test('the controller starts with the app, honors the switch and reports a busy port', async () => {
  const directory = isolatedDirectory('controller');
  const logs = [];
  const controller = new McpController({
    configPath: path.join(directory, 'mcp.json'), protector: fakeProtector, backend: fakeBackend(), version: 'test',
    log: (event) => logs.push(event), ephemeralPort: true,
  });
  await controller.start();
  const listening = controller.status();
  assert.equal(listening.state, 'listening');
  assert.equal(listening.url, `http://127.0.0.1:${listening.port}/mcp`);
  assert.match(controller.claudeCommand(), /--header "Authorization: Bearer .{43}"/);
  assert.equal(JSON.stringify(listening).includes('Bearer'), false);
  const token = /Bearer (.+)"/.exec(controller.claudeCommand())[1];
  assert.equal((await post(listening.port, { body: JSON.stringify({ jsonrpc: '2.0', id: 1, method: 'ping' }), headers: { authorization: `Bearer ${token}` } })).status, 200);

  const off = await controller.setEnabled(false);
  assert.equal(off.state, 'off');
  assert.equal(off.url, null);
  await assert.rejects(post(listening.port, { body: '{}' }));

  const blocker = http.createServer();
  await new Promise((resolve) => blocker.listen(0, '127.0.0.1', resolve));
  try {
    const busyPort = blocker.address().port;
    const busy = await controller.setPort(busyPort).then(() => controller.setEnabled(true));
    assert.equal(busy.state, 'portInUse');
    assert.match(busy.error, /already in use/);
  } finally { await new Promise((resolve) => blocker.close(resolve)); }

  const rotated = controller.claudeCommand();
  controller.rotateToken();
  assert.notEqual(controller.claudeCommand(), rotated);
  const probe = http.createServer();
  await new Promise((resolve) => probe.listen(0, '127.0.0.1', resolve));
  const freePort = probe.address().port;
  await new Promise((resolve) => probe.close(resolve));
  await controller.setPort(freePort);
  assert.equal(controller.status().state, 'listening');
  assert.equal(controller.status().port, freePort);
  await controller.stop();
  assert.equal(controller.status().state, 'off');
  assert.ok(logs.includes('Mcp.Listening') && logs.includes('Mcp.PortInUse') && logs.includes('Mcp.Stopped'));
});
