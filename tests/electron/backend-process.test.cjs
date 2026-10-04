const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const fs = require('node:fs/promises');
const path = require('node:path');
const { once } = require('node:events');
const { randomUUID } = require('node:crypto');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');
const backendPath = path.join(workspace, 'src', 'Automator.Backend', 'bin', 'Debug', 'net10.0-windows10.0.17763.0', 'Automator.Backend.exe');
const modulePath = path.join(workspace, 'artifacts', 'test-tools', 'backend-process', 'backendProcess.mjs');

async function loadBackendProcess() {
  return (await import(pathToFileURL(modulePath).href)).BackendProcess;
}

function isolatedDirectory() {
  return path.join(workspace, 'artifacts', 'test-data', 'backend-process', randomUUID());
}

function waitForExit(backend, timeoutMs = 15000) {
  return new Promise((resolve, reject) => {
    const timeout = setTimeout(() => {
      backend.off('exit', onExit);
      reject(new Error('Backend exit timed out.'));
    }, timeoutMs);
    const onExit = (value) => {
      clearTimeout(timeout);
      resolve(value);
    };
    backend.once('exit', onExit);
  });
}

test('binding removal persists, preserves custom files, prevents alias launch, and survives reload', async () => {
  const BackendProcess = await loadBackendProcess();
  const dataDirectory = isolatedDirectory();
  await fs.mkdir(dataDirectory, { recursive: true });
  const targetPath = path.join(dataDirectory, 'custom.exe');
  await fs.writeFile(targetPath, 'retained custom executable');
  const settingsPath = path.join(dataDirectory, 'settings.json');
  const settings = { SchemaVersion: 2, Hotkey: 'RightAlt', Theme: 'Dark', StartWithWindows: false,
    Bindings: [{ Id: 'custom', Name: 'Custom', TargetPath: targetPath, Alias: 'custom', Arguments: '--retained' }],
    ModuleSettings: [] };
  await fs.writeFile(settingsPath, JSON.stringify(settings));
  const backend = new BackendProcess(backendPath, [], workspace, process.env, () => {});
  const initialization = { protocolVersion: 3, buildId: `remove-${process.pid}`, hostProcessId: process.pid,
    hostExecutablePath: process.execPath, portableExecutablePath: null, testMode: true, dataDirectory };
  try {
    await backend.start(initialization);
    // Turn the settings destination into a directory to cause a real atomic-save failure.
    await fs.rename(settingsPath, settingsPath + '.original');
    await fs.mkdir(settingsPath);
    await assert.rejects(backend.request('binding/remove', { bindingId: 'custom' }));
    const unchanged = await backend.request('getState', {});
    assert.equal(unchanged.bindings.length, 1);
    assert.equal(unchanged.bindings[0].alias, 'custom');
    await fs.rmdir(settingsPath);
    await fs.rename(settingsPath + '.original', settingsPath);
    const removed = await backend.request('binding/remove', { bindingId: 'custom' });
    assert.deepEqual(removed.bindings, []);
    assert.deepEqual(JSON.parse(await fs.readFile(settingsPath, 'utf8')).Bindings, []);
    assert.equal(await fs.readFile(targetPath, 'utf8'), 'retained custom executable');
    await assert.rejects(backend.request('launcher/activateBinding', { bindingId: 'custom' }), /no longer exists/i);
    const queried = await backend.request('launcher/setQuery', { query: 'custom' });
    assert.equal(queried.matchKind, 'None');
    await assert.rejects(backend.request('binding/remove', { bindingId: 'custom' }), /no longer exists/i);
    await backend.stop();
    const restarted = await backend.start(initialization);
    assert.deepEqual(restarted.state.bindings, []);
  } finally { await backend.stop().catch(() => {}); }
});

test('versioned backups transfer module profiles and return path repair warnings', async () => {
  const BackendProcess = await loadBackendProcess();
  const sourceDirectory = isolatedDirectory();
  const destinationDirectory = isolatedDirectory();
  const backupPath = path.join(sourceDirectory, 'automator-backup.json');
  await fs.mkdir(sourceDirectory, { recursive: true });
  await fs.mkdir(destinationDirectory, { recursive: true });
  const initialization = (dataDirectory, suffix) => ({ protocolVersion: 3, buildId: `backup-${suffix}-${process.pid}`,
    hostProcessId: process.pid, hostExecutablePath: process.execPath, portableExecutablePath: null,
    testMode: true, dataDirectory });
  const source = new BackendProcess(backendPath, [], workspace, process.env, () => {});
  const destination = new BackendProcess(backendPath, [], workspace, process.env, () => {});
  try {
    await source.start(initialization(sourceDirectory, 'source'));
    await source.request('launcher/open', {});
    await source.request('launcher/selectTab', { tab: 2 });
    const saved = await source.request('automation/moduleAction', {
      requestId: 'save-backup-profile', contractVersion: 1, moduleId: 'script-runner',
      actionId: 'saveProfile', actionVersion: 1,
      input: {
        id: 'backup-script', name: 'Backup script', interpreter: 'python',
        interpreterPath: 'C:\\Missing\\python.exe', scriptPath: 'C:\\Missing\\backup.py',
        arguments: [], workingDirectory: 'C:\\Missing', outputMode: 'text', timeoutSeconds: 10,
      },
    });
    assert.equal(saved.status, 'success');
    assert.deepEqual(await source.request('settings/export', { path: backupPath }), { exported: true });
    const exported = JSON.parse(await fs.readFile(backupPath, 'utf8'));
    assert.equal(exported.format, 'automator-backup');
    assert.equal(exported.formatVersion, 1);
    assert.equal(exported.automationLibrary.format, 'automator-library');
    assert.equal(exported.automationLibrary.records.length, 1);
    await source.stop();

    await destination.start(initialization(destinationDirectory, 'destination'));
    const report = await destination.request('settings/import', { path: backupPath });
    assert.equal(report.includesAutomationLibrary, true);
    assert.equal(report.importedLibraryRecords, 1);
    assert.equal(report.state.theme, 'Light');
    assert.ok(report.warnings.some((warning) => warning.recordId === 'backup-script' && warning.code === 'missing-script-path'));
    assert.ok(report.warnings.some((warning) => warning.recordId === 'backup-script' && warning.code === 'missing-interpreter-path'));
    assert.ok(report.warnings.some((warning) => warning.recordId === 'backup-script' && warning.code === 'missing-working-directory'));

    await destination.request('launcher/open', {});
    await destination.request('launcher/selectTab', { tab: 2 });
    const listed = await destination.request('automation/moduleAction', {
      requestId: 'list-imported-profiles', contractVersion: 1, moduleId: 'script-runner',
      actionId: 'listProfiles', actionVersion: 1, input: {},
    });
    assert.equal(listed.status, 'success');
    assert.equal(listed.data.profiles[0].id, 'backup-script');
  } finally {
    await source.stop().catch(() => {});
    await destination.stop().catch(() => {});
  }
});

test('backend keeps stdout as JSON-RPC, serializes stateChanged params, and exits on stdin EOF', async () => {
  const BackendProcess = await loadBackendProcess();
  const dataDirectory = isolatedDirectory();
  await fs.mkdir(dataDirectory, { recursive: true });
  const logEvents = [];
  const backend = new BackendProcess(backendPath, [], workspace, process.env,
    (event, message) => logEvents.push({ event, message }));
  let notificationResolve;
  const notificationReceived = new Promise((resolve) => { notificationResolve = resolve; });
  backend.on('notification', (value) => {
    if (value.method === 'stateChanged') notificationResolve(value);
  });

  try {
    const initialized = await backend.start({
      protocolVersion: 3,
      buildId: `process-${process.pid}`,
      hostProcessId: process.pid,
      hostExecutablePath: process.execPath,
      portableExecutablePath: null,
      testMode: true,
      dataDirectory,
    });
    assert.equal(initialized.state.buildId, `process-${process.pid}`);
    const state = await backend.request('launcher/setQuery', { query: 'rpc-fixture' });
    const notification = await notificationReceived;
    assert.equal(state.query, 'rpc-fixture');
    assert.equal(notification.jsonrpc, '2.0');
    assert.equal(notification.params.query, 'rpc-fixture');
    assert.equal(Object.hasOwn(notification, 'parameters'), false);
    assert.equal(logEvents.some((entry) => entry.event === 'Backend.ProtocolError'), false);

    const processExit = waitForExit(backend);
    await backend.stop();
    const exit = await processExit;
    assert.equal(exit.expected, true);
    assert.equal(backend.childProcessId, null);

    const backendLog = await readBackendLogs(dataDirectory);
    assert.match(backendLog, /"event":"Backend\.Stopped"/);
  } finally {
    await backend.stop().catch(() => {});
  }
});

test('backend exits and cleans up when its host process dies', async () => {
  const BackendProcess = await loadBackendProcess();
  const dataDirectory = isolatedDirectory();
  await fs.mkdir(dataDirectory, { recursive: true });
  const host = spawn(process.execPath, ['-e', 'setInterval(() => {}, 1000)'], {
    cwd: workspace,
    stdio: 'ignore',
    windowsHide: true,
  });
  await once(host, 'spawn');
  const backend = new BackendProcess(backendPath, [], workspace, process.env, () => {});

  try {
    await backend.start({
      protocolVersion: 3,
      buildId: `host-death-${process.pid}`,
      hostProcessId: host.pid,
      hostExecutablePath: process.execPath,
      portableExecutablePath: null,
      testMode: true,
      dataDirectory,
    });
    const backendExit = waitForExit(backend);
    host.kill();
    const exit = await backendExit;
    assert.equal(exit.expected, false);
    assert.equal(backend.childProcessId, null);
    const backendLog = await readBackendLogs(dataDirectory);
    assert.match(backendLog, /"event":"Backend\.HostExited"/);
    assert.match(backendLog, /"event":"Backend\.Stopped"/);
  } finally {
    if (host.exitCode === null && host.signalCode === null) host.kill();
    await backend.stop().catch(() => {});
  }
});

test('backend process parser rejects JSON null and malformed response envelopes without throwing', async (t) => {
  const BackendProcess = await loadBackendProcess();
  const dataDirectory = isolatedDirectory();
  await fs.mkdir(dataDirectory, { recursive: true });
  const state = await createValidInitializationResult(BackendProcess, dataDirectory);
  const cases = [
    ['null', 'invalid JSON-RPC envelope'],
    ['{"jsonrpc":"1.0","id":2,"result":{}}', 'invalid JSON-RPC envelope'],
    ['{"jsonrpc":"2.0","id":2,"result":{},"error":{"code":-1,"message":"bad"}}', 'invalid id, result, or error object'],
  ];

  for (const [line, expectedMessage] of cases) {
    await t.test(`rejects ${line}`, async () => {
      const script = [
        'const readline=require("node:readline");',
        `const initialized=${JSON.stringify(state)};`,
        `const responseLine=${JSON.stringify(line)};`,
        'const write=value=>process.stdout.write(JSON.stringify(value)+String.fromCharCode(10));',
        'const input=readline.createInterface({input:process.stdin});',
        'input.on("line",line=>{const request=JSON.parse(line);if(request.method==="initialize")write({jsonrpc:"2.0",id:request.id,result:initialized});else process.stdout.write(responseLine+String.fromCharCode(10));});',
      ].join('\n');
      const fake = new BackendProcess(process.execPath, ['-e', script], workspace, process.env, () => {});
      await fake.start({
        protocolVersion: 3,
        buildId: 'fake-parser',
        hostProcessId: process.pid,
        hostExecutablePath: process.execPath,
        portableExecutablePath: null,
        testMode: true,
        dataDirectory,
      });
      const failure = once(fake, 'failure');
      await assert.rejects(fake.request('getState', {}), new RegExp(expectedMessage));
      const [error] = await failure;
      assert.match(error.message, new RegExp(expectedMessage));
      await fake.stop().catch(() => {});
    });
  }
});

async function createValidInitializationResult(BackendProcess, dataDirectory) {
  const backend = new BackendProcess(backendPath, [], workspace, process.env, () => {});
  try {
    return await backend.start({
      protocolVersion: 3,
      buildId: `fake-state-${process.pid}`,
      hostProcessId: process.pid,
      hostExecutablePath: process.execPath,
      portableExecutablePath: null,
      testMode: true,
      dataDirectory,
    });
  } finally {
    await backend.stop();
  }
}

async function readBackendLogs(dataDirectory) {
  const logDirectory = path.join(dataDirectory, 'Logs');
  const files = (await fs.readdir(logDirectory)).filter((name) => name.startsWith('Automator-'));
  return (await Promise.all(files.map((name) => fs.readFile(path.join(logDirectory, name), 'utf8')))).join('\n');
}
