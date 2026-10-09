const assert = require('node:assert/strict');
const { test } = require('node:test');
test('Explorer command parser preserves one Unicode path and rejects malformed commands', async () => {
  const { parseFileExplorerLaunch } = await import('../../electron/fileExplorerLaunch.ts');
  assert.deepEqual(parseFileExplorerLaunch(['Automator.exe', '--automator-file-action', 'backup', '--', 'C:\\Bazy danych\\żółć.fdb']), { actionId: 'backup', filePath: 'C:\\Bazy danych\\żółć.fdb' });
  for (const args of [[], ['--automator-file-action', 'backup'], ['--automator-file-action', '../evil', '--', 'C:\\a.fdb'], ['--automator-file-action', 'backup', '--', 'relative.fdb'], ['--automator-file-action', 'backup', '--', '\\rooted.fdb'], ['--automator-file-action', 'backup', '--', 'C:\\a.fdb', 'extra']]) assert.equal(parseFileExplorerLaunch(args), null);
});
test('Explorer queue retains cold requests until ready and serializes existing-instance requests', async () => {
  const { FileExplorerLaunchQueue } = await import('../../electron/fileExplorerLaunch.ts');
  const queue = new FileExplorerLaunchQueue();
  const delivered = [];
  queue.enqueue({ actionId: 'first', filePath: 'C:\\one.fdb' });
  await queue.drain(false, async request => delivered.push(request.actionId));
  assert.deepEqual(delivered, []);
  queue.enqueue({ actionId: 'second', filePath: 'C:\\two.fdb' });
  await queue.drain(true, async request => delivered.push(request.actionId));
  assert.deepEqual(delivered, ['first', 'second']);
});
test('only stable packaged hosts advertise Explorer registration executable', async () => {
  const { stableExplorerExecutable } = await import('../../electron/fileExplorerLaunch.ts');
  const executable = 'C:\\Apps\\Automator.exe';
  assert.equal(stableExplorerExecutable(true, false, false, executable, false), executable);
  for (const [packaged, testMode, portable, temporary] of [[false,false,false,false],[true,true,false,false],[true,false,true,false],[true,false,false,true]]) assert.equal(stableExplorerExecutable(packaged,testMode,portable,executable,temporary), null);
});
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { _electron: electron } = require('playwright');
const { spawn } = require('node:child_process');
test('cold and second-instance Explorer launches open Script Runner and preserve the backend', async () => {
  const workspace = path.resolve(__dirname, '../..');
  const main = path.join(workspace, 'dist-electron/main/main.cjs');
  const data = path.join(workspace, 'artifacts/test-data/explorer-handoff', randomUUID());
  const env = { ...process.env, AUTOMATOR_TEST_MODE: '1', AUTOMATOR_TEST_DATA_DIRECTORY: data,
    AUTOMATOR_SHOW_ON_START: '1', AUTOMATOR_BUILD_ID: `explorer-${process.pid}` };
  const firstPath = 'C:\\Bazy danych\\żółć.fdb';
  const app = await electron.launch({ args: [main, '--automator-file-action', 'first', '--', firstPath], cwd: workspace, env, timeout: 60000 });
  try {
    const page = await app.firstWindow();
    await page.waitForFunction(() => !!window.automator, null, { timeout: 60000 });
    await page.evaluate(() => {
      window.explorerRequests = [];
      window.unsubscribeExplorer = window.automator.onFileExplorerLaunch(request => window.explorerRequests.push(request));
    });
    await page.waitForFunction(() => window.explorerRequests.length === 1, null, { timeout: 60000 });
    assert.deepEqual(await page.evaluate(() => window.explorerRequests[0]), { actionId: 'first', filePath: firstPath });
    const before = await page.evaluate(() => window.automator.getHostInfo());
    assert.equal((await page.evaluate(() => window.automator.getInitialState())).state.selectedTab, 2);
    const child = spawn(require('electron'), [main, '--automator-file-action', 'second', '--', 'C:\\Zażółć plik.fdb'], { cwd: workspace, env, windowsHide: true, stdio: 'ignore' });
    child.on('error', error => { throw error; });
    child.unref();
    await page.waitForFunction(() => window.explorerRequests.length === 2, null, { timeout: 60000 });
    assert.deepEqual(await page.evaluate(() => window.explorerRequests[1]), { actionId: 'second', filePath: 'C:\\Zażółć plik.fdb' });
    assert.equal((await page.evaluate(() => window.automator.getHostInfo())).backendProcessId, before.backendProcessId);
    await page.evaluate(() => window.unsubscribeExplorer());
    await app.evaluate(({ app }) => app.emit('second-instance', {}, ['exe', '--automator-file-action', 'third', '--', 'C:\\third.fdb'], '', { fileExplorerLaunch: { actionId: 'third', filePath: 'C:\\third.fdb' } }));
    assert.equal(await page.evaluate(() => window.explorerRequests.length), 2);
    await page.evaluate(() => { window.automator.onFileExplorerLaunch(request => window.explorerRequests.push(request)); });
    await page.waitForFunction(() => window.explorerRequests.length === 3);
  } finally { await app.close(); }
});
