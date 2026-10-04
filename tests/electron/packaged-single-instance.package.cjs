const assert = require('node:assert/strict');
const { execFileSync, spawn } = require('node:child_process');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');
const executable = path.join(workspace, 'artifacts', 'electron-dist', 'win-unpacked', 'Automator.exe');
const helper = path.join(workspace, 'scripts', 'Restart-Automator.ps1');

async function readEvents(logPath) {
  try {
    return (await fs.readFile(logPath, 'utf8'))
      .split(/\r?\n/)
      .filter((line) => line.trim().length > 0)
      .map((line) => JSON.parse(line));
  } catch (error) {
    if (error.code === 'ENOENT') return [];
    throw error;
  }
}

async function waitForEvent(logPath, predicate, child, description, timeoutMs = 60_000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (child.exitCode !== null) throw new Error(`${description}; process exited with ${child.exitCode}.`);
    const events = await readEvents(logPath);
    const match = events.find(predicate);
    if (match) return match;
    await new Promise((resolve) => setTimeout(resolve, 100));
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

function launch(buildId, dataDirectory, backendDirectory) {
  return spawn(executable, [], {
    cwd: path.dirname(executable),
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: backendDirectory,
      AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_BUILD_ID: buildId,
    },
    stdio: 'ignore',
  });
}

test('packaged desktop host enforces one instance before starting another backend', { timeout: 90_000 }, async () => {
  await fs.access(executable);
  const runId = randomUUID();
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'packaged-single-instance', runId);
  const backendDirectory = path.join(dataDirectory, 'backend');
  const logDirectory = path.join(dataDirectory, 'Logs');
  const logPath = path.join(logDirectory, `automator-${new Date().toISOString().slice(0, 10)}.jsonl`);
  const primaryBuildId = `packaged-single-primary-${runId}`;
  const duplicateBuildId = `packaged-single-duplicate-${runId}`;
  let primary;
  let duplicate;

  await fs.mkdir(backendDirectory, { recursive: true });
  try {
    primary = launch(primaryBuildId, dataDirectory, backendDirectory);
    const ready = await waitForEvent(logPath,
      (event) => event.Event === 'App.Ready' && event.BuildId === primaryBuildId,
      primary, 'the packaged primary host to become ready');
    assert.equal(ready.visible, true);
    assert.equal(ready.backendHookInstalled, true);
    assert.equal(ready.trayIconReady, true);

    duplicate = launch(duplicateBuildId, dataDirectory, backendDirectory);
    const duplicateExit = await new Promise((resolve, reject) => {
      const timeout = setTimeout(() => reject(new Error('The duplicate host did not exit after losing the single-instance lock.')), 15_000);
      duplicate.once('error', (error) => { clearTimeout(timeout); reject(error); });
      duplicate.once('exit', (code, signal) => { clearTimeout(timeout); resolve({ code, signal }); });
    });
    assert.equal(duplicateExit.code, 0, `duplicate host should exit cleanly (signal ${duplicateExit.signal ?? 'none'})`);

    const rejection = await waitForEvent(logPath,
      (event) => event.Event === 'Instance.DuplicateLaunch' && event.BuildId === duplicateBuildId,
      primary, 'the packaged duplicate-instance rejection');
    assert.notEqual(rejection.ProcessId, ready.ProcessId);
    assert.equal(primary.exitCode, null, 'the original packaged host should remain running');

    const backendStarts = (await readEvents(logPath)).filter((event) =>
      event.Event === 'Backend.Ready' && event.BuildId.startsWith(`packaged-single-`));
    assert.equal(backendStarts.length, 1, 'only the primary process should start a native backend');
    assert.equal(backendStarts[0].BuildId, primaryBuildId);
  } finally {
    try {
      execFileSync('powershell.exe', [
        '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', helper, '-StopOnly',
      ], { cwd: workspace, encoding: 'utf8', timeout: 20_000, stdio: 'pipe' });
    } catch (error) {
      const output = `${error.stdout ?? ''}${error.stderr ?? ''}`;
      throw new Error(`The workspace helper could not clean up packaged test hosts: ${output || error.message}`);
    }
  }
});
