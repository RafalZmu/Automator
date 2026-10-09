const assert = require('node:assert/strict');
const path = require('node:path');
const fs = require('node:fs/promises');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const workspace = path.resolve(__dirname, '../..');
async function fixture() {
  const data = path.join(workspace, 'artifacts/test-data/explorer-ui', randomUUID());
  await fs.mkdir(data, { recursive: true });
  const source = path.join(data, 'Zażółć file.fdb');
  const script = path.join(data, 'echo.ps1');
  await fs.writeFile(source, 'fixture');
  await fs.writeFile(script, '[Console]::WriteLine(($args | ConvertTo-Json -Compress))');
  const app = await electron.launch({ args: [path.join(workspace, 'dist-electron/main/main.cjs')], cwd: workspace,
    env: { ...process.env, AUTOMATOR_TEST_MODE: '1', AUTOMATOR_TEST_TRAY: '1', AUTOMATOR_SHOW_ON_START: '1', AUTOMATOR_TEST_DATA_DIRECTORY: data, AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: path.join(data, 'backend'), AUTOMATOR_BUILD_ID: `explorer-ui-${process.pid}` }, timeout: 60000 });
  try {
  const page = await app.firstWindow();
  page.setDefaultTimeout(10000);
  await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible', timeout: 60000 });
  await page.evaluate(() => window.automator.selectTab(2));
  await page.getByRole('region', { name: 'Script Runner', exact: true }).waitFor();
  async function dispatch(actionId, input) {
    return page.evaluate(({ actionId, input }) => window.automator.dispatchModuleAction({ requestId: crypto.randomUUID(), contractVersion: 1, moduleId: 'script-runner', actionId, actionVersion: 1, input }), { actionId, input });
  }
  const saved = await dispatch('saveProfile', { id: 'echo', name: 'Echo file', interpreter: 'powershell', interpreterPath: 'C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe', scriptPath: script, arguments: ['{{file.path}}', 'saved'], workingDirectory: data, outputMode: 'text', timeoutSeconds: 10 });
  assert.notEqual(saved.status, 'error', saved.message);
  await page.evaluate(() => window.automator.selectTab(1));
  await page.getByRole('region', { name: 'Launcher applications', exact: true }).waitFor();
  await page.evaluate(() => window.automator.selectTab(2));
  await page.getByRole('button', { name: 'Edit Echo file', exact: true }).waitFor();
  const launch = async actionId => app.evaluate(({ app }, request) => app.emit('second-instance', {}, ['exe', '--automator-file-action', request.actionId, '--', request.filePath], '', { fileExplorerLaunch: request }), { actionId, filePath: source });
  return { app, page, data, source, dispatch, launch };
  } catch (error) { await app.close(); throw error; }
}
test('Explorer mapping UI saves edits removes and clears mappings while registration stays disabled in test mode', async () => {
  const { app, page, dispatch } = await fixture();
  try {
    await page.getByRole('button', { name: 'Explorer actions', exact: true }).click();
    const section = page.getByRole('region', { name: 'File Explorer actions', exact: true });
    await section.getByRole('button', { name: 'Add action', exact: true }).click();
    await section.getByLabel('Saved profile', { exact: true }).selectOption('echo');
    await section.getByLabel('Menu label', { exact: true }).fill('Echo selection');
    await section.getByLabel('File extensions', { exact: true }).fill('FDB, .TXT; fdb');
    await section.getByRole('button', { name: 'Save action', exact: true }).click();
    await section.getByRole('button', { name: 'Edit Echo selection', exact: true }).waitFor();
    const listed = await dispatch('listExplorerActions', {});
    assert.deepEqual(listed.data.actions[0].extensions, ['.fdb', '.txt']);
    assert.equal(listed.data.registration.state, 'disabled');
    await section.getByRole('button', { name: 'Edit Echo selection', exact: true }).click();
    await section.getByLabel('Menu label', { exact: true }).fill('Renamed selection');
    await section.getByRole('button', { name: 'Save action', exact: true }).click();
    await section.getByRole('button', { name: 'Remove Renamed selection', exact: true }).click();
    await page.waitForFunction(() => document.querySelector('[aria-label="File Explorer actions"]').textContent.includes('No Explorer actions'));
    assert.deepEqual((await dispatch('listExplorerActions', {})).data.actions, []);
    await section.getByRole('button', { name: 'Add action', exact: true }).click();
    await section.getByLabel('Saved profile', { exact: true }).selectOption('echo');
    await section.getByLabel('Menu label', { exact: true }).fill('Again');
    await section.getByLabel('File extensions', { exact: true }).fill('.fdb');
    await section.getByRole('button', { name: 'Save action', exact: true }).click();
    await section.getByRole('button', { name: 'Remove all Explorer actions', exact: true }).click();
    await page.waitForFunction(() => document.querySelector('[aria-label="File Explorer actions"]').textContent.includes('No Explorer actions'));
    assert.deepEqual((await dispatch('listExplorerActions', {})).data.actions, []);
  } finally { await app.close(); }
});
test('Explorer launch opens a transient Unicode file form and requires Run after argument edits', async () => {
  const { app, page, source, dispatch, launch } = await fixture();
  try {
    const saved = await dispatch('saveExplorerAction', { id: 'echo-file', profileId: 'echo', label: 'Echo file action', extensions: ['.fdb'] });
    assert.notEqual(saved.status, 'error', saved.message);
    await launch('echo-file');
    const form = page.getByRole('region', { name: 'Run Explorer action', exact: true });
    await form.waitFor();
    assert.equal(await form.getByLabel('Selected file', { exact: true }).inputValue(), source);
    assert.equal(await form.getByLabel('Run arguments', { exact: true }).inputValue(), `${source}\nsaved`);
    assert.equal(await page.getByRole('region', { name: 'Script output' }).count(), 0);
    await form.getByLabel('Run arguments', { exact: true }).fill(`${source}\nedited once`);
    await form.getByRole('button', { name: 'Cancel', exact: true }).click();
    assert.equal(await form.count(), 0);
    assert.equal(await page.getByRole('region', { name: 'Script output' }).count(), 0);
    await launch('echo-file');
    await form.waitFor();
    assert.equal(await form.getByLabel('Run arguments', { exact: true }).inputValue(), `${source}\nsaved`);
    await form.getByLabel('Run arguments', { exact: true }).fill(`${source}\nedited once`);
    await form.getByRole('button', { name: 'Run', exact: true }).click();
    await page.getByRole('region', { name: 'Script output' }).filter({ hasText: 'edited once' }).waitFor();
    assert.deepEqual((await dispatch('listProfiles', {})).data.profiles.find(p => p.id === 'echo').arguments, ['{{file.path}}', 'saved']);
    await fs.unlink(source);
    await launch('echo-file');
    await form.waitFor();
    await form.getByRole('button', { name: 'Run', exact: true }).click();
    await page.getByRole('status').filter({ hasText: 'existing absolute file path' }).waitFor();
    await form.getByRole('button', { name: 'Cancel', exact: true }).click();
    await launch('stale-action');
    await page.getByRole('status').filter({ hasText: 'action no longer exists' }).waitFor();
  } finally { await app.close(); }
});
test('Explorer template mapping prefills the typed file input and keeps credentials transient on cancel', async () => {
  const { app, page, source, dispatch, launch, data } = await fixture();
  try {
    const installed = await dispatch('installTemplate', { id: 'firebird-3-backup' });
    assert.notEqual(installed.status, 'error', installed.message);
    const profile = installed.data.profile;
    await page.evaluate(() => window.automator.selectTab(1));
    await page.getByRole('region', { name: 'Launcher applications', exact: true }).waitFor();
    await page.evaluate(() => window.automator.selectTab(2));
    await page.getByRole('region', { name: 'Script Runner', exact: true }).waitFor();
    await page.getByRole('button', { name: 'Explorer actions', exact: true }).click();
    const mappings = page.getByRole('region', { name: 'File Explorer actions', exact: true });
    await mappings.getByRole('button', { name: 'Add action', exact: true }).click();
    await mappings.getByLabel('Saved profile', { exact: true }).selectOption(profile.id);
    await mappings.getByLabel('Menu label', { exact: true }).fill('Plain Firebird backup');
    await mappings.getByLabel('File extensions', { exact: true }).fill('.fdb');
    await mappings.getByLabel('Selected file parameter', { exact: true }).selectOption('database');
    await mappings.getByRole('button', { name: 'Save action', exact: true }).click();
    await mappings.getByRole('button', { name: 'Edit Plain Firebird backup', exact: true }).waitFor();
    const action = (await dispatch('listExplorerActions', {})).data.actions[0];
    await launch(action.id);
    const form = page.getByRole('region', { name: 'Run Explorer action', exact: true });
    await form.waitFor();
    assert.equal(await form.getByLabel('Selected file', { exact: true }).inputValue(), source);
    assert.equal(await form.getByLabel('Username *', { exact: true }).inputValue(), 'SYSDBA');
    assert.equal(await form.getByLabel('Password *', { exact: true }).inputValue(), 'masterkey');
    await form.getByLabel('Password *', { exact: true }).fill('not-persisted-secret');
    await form.getByRole('button', { name: 'Run', exact: true }).click();
    await page.getByRole('status').filter({ hasText: 'Backup file is invalid' }).waitFor();
    assert.equal(await page.getByRole('region', { name: 'Script output' }).count(), 0);
    await form.getByLabel('Backup file *', { exact: true }).fill(path.join(data, 'once.fbk'));
    await form.getByRole('button', { name: 'Cancel', exact: true }).click();
    await launch(action.id);
    await form.waitFor();
    assert.equal(await form.getByLabel('Password *', { exact: true }).inputValue(), 'masterkey');
    assert.equal(await form.getByLabel('Backup file *', { exact: true }).inputValue(), '');
    assert.deepEqual((await dispatch('listProfiles', {})).data.profiles.find(item => item.id === profile.id).arguments, []);
    await form.getByRole('button', { name: 'Cancel', exact: true }).click();
    await dispatch('deleteProfile', { id: profile.id });
    await launch(action.id);
    await page.getByRole('status').filter({ hasText: 'action no longer exists' }).waitFor();
  } finally { await app.close(); }
});
test('Explorer registration errors remain visible beside persisted mappings', async () => {
  const { app, page, dispatch } = await fixture();
  try {
    await dispatch('saveExplorerAction', { id: 'retained', profileId: 'echo', label: 'Retained mapping', extensions: ['.fdb'] });
    const listed = await dispatch('listExplorerActions', {});
    listed.data.registration = { state: 'error', message: 'The Explorer menu could not be updated. Reopen this section to retry.' };
    await app.evaluate(({ ipcMain }, result) => {
      ipcMain.removeHandler('automator:module-action');
      ipcMain.handle('automator:module-action', (_event, request) => {
        if (request.actionId !== 'listExplorerActions') throw new Error('Unexpected mocked module action.');
        return result;
      });
    }, listed);
    await page.getByRole('button', { name: 'Explorer actions', exact: true }).click();
    const section = page.getByRole('region', { name: 'File Explorer actions', exact: true });
    await section.getByRole('status').filter({ hasText: 'could not be updated' }).waitFor();
    await section.getByRole('button', { name: 'Edit Retained mapping', exact: true }).waitFor();
    assert.equal(listed.data.actions[0].id, 'retained');
  } finally { await app.close(); }
});
test('cold Explorer launch retains a mapped Unicode file until the Script Runner form mounts', async () => {
  const { app: seeded, source, data, dispatch } = await fixture();
  try { await dispatch('saveExplorerAction', { id: 'cold-file', profileId: 'echo', label: 'Cold file action', extensions: ['.fdb'] }); }
  finally { await seeded.close(); }
  const app = await electron.launch({ args: [path.join(workspace, 'dist-electron/main/main.cjs'), '--automator-file-action', 'cold-file', '--', source], cwd: workspace,
    env: { ...process.env, AUTOMATOR_TEST_MODE: '1', AUTOMATOR_TEST_TRAY: '1', AUTOMATOR_SHOW_ON_START: '1', AUTOMATOR_TEST_DATA_DIRECTORY: data, AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: path.join(data, 'backend'), AUTOMATOR_BUILD_ID: `explorer-cold-${process.pid}` }, timeout: 60000 });
  try {
    const page = await app.firstWindow();
    const form = page.getByRole('region', { name: 'Run Explorer action', exact: true });
    await form.waitFor({ timeout: 60000 });
    assert.equal(await form.getByLabel('Selected file', { exact: true }).inputValue(), source);
    assert.equal(await form.getByLabel('Run arguments', { exact: true }).inputValue(), `${source}\nsaved`);
    assert.equal(await page.getByRole('region', { name: 'Script output' }).count(), 0);
    await form.getByRole('button', { name: 'Cancel', exact: true }).click();
    assert.equal(await page.getByRole('region', { name: 'Script output' }).count(), 0);
  } finally { await app.close(); }
});
test('Firebird Library card path labels address the input on each card', async () => {
  const { app, page } = await fixture();
  try {
    await page.getByRole('button', { name: 'Library', exact: true }).click();
    const library = page.getByRole('region', { name: 'Script template library', exact: true });
    const plain = library.locator('.script-template-row').filter({ has: page.getByText('Firebird 3 database backup', { exact: true }) });
    const zipped = library.locator('.script-template-row').filter({ has: page.getByText('Firebird 3 database backup and ZIP', { exact: true }) });
    await plain.waitFor();
    await zipped.waitFor();
    const plainInput = plain.getByLabel('Target database *', { exact: true });
    const zippedInput = zipped.getByLabel('Target database *', { exact: true });
    assert.equal(await plainInput.count(), 1);
    assert.equal(await zippedInput.count(), 1);
    assert.notEqual(await plainInput.getAttribute('id'), await zippedInput.getAttribute('id'));
    assert.equal(await plainInput.evaluate(element => element.closest('.script-template-row').textContent.includes('Firebird 3 database backup and ZIP')), false);
    assert.equal(await zippedInput.evaluate(element => element.closest('.script-template-row').textContent.includes('Firebird 3 database backup and ZIP')), true);
  } finally { await app.close(); }
});
test('stopping an Explorer template run clears sensitive inputs and preserves editable non-sensitive values', async () => {
  const { app, page, data, source, dispatch, launch } = await fixture();
  try {
    const installed = await dispatch('installTemplate', { id: 'firebird-3-backup' });
    assert.notEqual(installed.status, 'error', installed.message);
    const mapped = await dispatch('saveExplorerAction', { id: 'stoppable-template', profileId: installed.data.profile.id, label: 'Stoppable template', extensions: ['.fdb'], fileParameterKey: 'database' });
    assert.notEqual(mapped.status, 'error', mapped.message);
    await launch('stoppable-template');
    const form = page.getByRole('region', { name: 'Run Explorer action', exact: true });
    await form.waitFor();
    const backup = path.join(data, 'retained backup.fbk');
    const gbak = path.join(data, 'fixture gbak.exe');
    await form.getByLabel('Backup file *', { exact: true }).fill(backup);
    await form.getByLabel('gbak executable *', { exact: true }).fill(gbak);
    await form.getByLabel('Username *', { exact: true }).fill('retained-user');
    await form.getByLabel('Password *', { exact: true }).fill('transient-test-secret');
    await app.evaluate(({ ipcMain }) => {
      let rejectRun;
      ipcMain.removeHandler('automator:module-action');
      ipcMain.handle('automator:module-action', (_event, request) => {
        if (request.actionId !== 'runExplorerAction') throw new Error('Unexpected mocked action.');
        return new Promise((_resolve, reject) => { rejectRun = reject; });
      });
      ipcMain.removeHandler('automator:module-action-cancel');
      ipcMain.handle('automator:module-action-cancel', () => {
        rejectRun(new Error('The test template run was canceled.'));
        return { canceled: true };
      });
    });
    await form.getByRole('button', { name: 'Run', exact: true }).click();
    await form.getByRole('button', { name: 'Stop run', exact: true }).click();
    await form.getByRole('button', { name: 'Run', exact: true }).waitFor();
    assert.equal(await form.getByLabel('Password *', { exact: true }).inputValue(), '');
    assert.equal(await form.getByLabel('Selected file', { exact: true }).inputValue(), source);
    assert.equal(await form.getByLabel('Backup file *', { exact: true }).inputValue(), backup);
    assert.equal(await form.getByLabel('gbak executable *', { exact: true }).inputValue(), gbak);
    assert.equal(await form.getByLabel('Username *', { exact: true }).inputValue(), 'retained-user');
    assert.equal(await form.getByLabel('Password *', { exact: true }).isEditable(), true);
    await form.getByLabel('Password *', { exact: true }).fill('replacement-test-secret');
    assert.equal(await form.getByLabel('Password *', { exact: true }).inputValue(), 'replacement-test-secret');
    await form.getByRole('button', { name: 'Cancel', exact: true }).click();
  } finally { await app.close(); }
});
