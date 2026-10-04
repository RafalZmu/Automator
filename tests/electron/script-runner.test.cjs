const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const { randomUUID } = require('node:crypto');

const workspace = path.resolve(__dirname, '../..');

function isolatedDataDirectory(name) {
  return path.join(workspace, 'artifacts', 'test-data', 'electron-ui', name, randomUUID());
}

async function launchHost(name, dataDirectory, backendDataDirectory) {
  return electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: backendDataDirectory,
      AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_BUILD_ID: `${name}-${process.pid}`,
    },
    timeout: 60_000,
  });
}

async function openScriptRunner(app) {
  const page = await app.firstWindow();
  await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
  await page.evaluate(() => window.automator.selectTab(2));
  await page.getByRole('region', { name: 'Script Runner' }).waitFor({ state: 'visible' });
  return page;
}

async function fillProfileFields(page, { name, id, interpreterPath, scriptPath }) {
  await page.getByLabel('Name', { exact: true }).fill(name);
  await page.getByLabel('Profile key', { exact: true }).fill(id);
  await page.getByLabel('Interpreter executable', { exact: true }).fill(interpreterPath);
  await page.getByLabel('Script file').fill(scriptPath);
  await page.getByLabel('Working directory').fill('C:\\Scripts');
}

test('Script Runner remembers saved interpreter defaults and leaves saved profile paths as overrides', async () => {
  const dataDirectory = isolatedDataDirectory('script-runner-defaults');
  const backendDataDirectory = path.join(dataDirectory, 'backend');
  let app = await launchHost('script-runner-defaults', dataDirectory, backendDataDirectory);
  try {
    let page = await openScriptRunner(app);
    await page.getByRole('button', { name: 'New profile' }).click();
    await fillProfileFields(page, {
      name: 'Per-profile override',
      id: 'per-profile-override',
      interpreterPath: 'C:\\Python\\override.exe',
      scriptPath: 'C:\\Scripts\\override.py',
    });
    await page.getByRole('button', { name: 'Save profile' }).click();
    await page.getByRole('button', { name: 'Edit Per-profile override' }).waitFor({ state: 'visible' });

    let settings = await page.evaluate(() => window.automator.getModuleSettings({
      contractVersion: 1, moduleId: 'script-runner', settingsVersion: 1,
    }));
    assert.equal(settings.value.interpreterDefaults.python, 'C:\\Python\\override.exe');

    await page.getByRole('button', { name: 'New profile' }).click();
    await page.getByLabel('Name', { exact: true }).fill('Rejected PowerShell');
    await page.getByLabel('Profile key', { exact: true }).fill('rejected-powershell');
    await page.getByRole('combobox').nth(0).selectOption('powershell');
    await page.getByLabel('Interpreter executable', { exact: true }).fill('C:\\PowerShell\\unsaved.exe');
    await page.getByLabel('Script file').fill('C:\\Scripts\\wrong-extension.txt');
    await page.getByLabel('Working directory').fill('C:\\Scripts');
    await page.getByRole('button', { name: 'Save profile' }).click();
    await page.getByRole('status').filter({ hasText: '.ps1' }).waitFor({ state: 'visible' });
    settings = await page.evaluate(() => window.automator.getModuleSettings({
      contractVersion: 1, moduleId: 'script-runner', settingsVersion: 1,
    }));
    assert.equal(settings.value.interpreterDefaults.powershell, '', 'a rejected profile must not write its interpreter path as the default');

    await page.getByLabel('Script file').fill('C:\\Scripts\\accepted.ps1');
    await page.getByRole('button', { name: 'Save profile' }).click();
    await page.getByRole('button', { name: 'Edit Rejected PowerShell' }).waitFor({ state: 'visible' });

    await page.getByRole('button', { name: 'New profile' }).click();
    await page.getByRole('combobox').nth(0).selectOption('bash');
    await fillProfileFields(page, {
      name: 'Bash profile',
      id: 'bash-profile',
      interpreterPath: 'C:\\Git\\bin\\bash.exe',
      scriptPath: 'C:\\Scripts\\batch.sh',
    });
    await page.getByRole('button', { name: 'Save profile' }).click();
    await page.getByRole('button', { name: 'Edit Bash profile' }).waitFor({ state: 'visible' });
    settings = await page.evaluate(() => window.automator.getModuleSettings({
      contractVersion: 1, moduleId: 'script-runner', settingsVersion: 1,
    }));
    assert.equal(settings.value.interpreterDefaults.powershell, 'C:\\PowerShell\\unsaved.exe');
    assert.equal(settings.value.interpreterDefaults.bash, 'C:\\Git\\bin\\bash.exe');
    await app.close();

    app = await launchHost('script-runner-defaults-reload', dataDirectory, backendDataDirectory);
    page = await openScriptRunner(app);
    await page.getByRole('button', { name: 'New profile' }).click();
    assert.equal(await page.getByLabel('Interpreter executable', { exact: true }).inputValue(), 'C:\\Python\\override.exe');
    await page.getByRole('combobox').nth(0).selectOption('bash');
    assert.equal(await page.getByLabel('Interpreter executable', { exact: true }).inputValue(), 'C:\\Git\\bin\\bash.exe');
    await page.getByRole('combobox').nth(0).selectOption('powershell');
    assert.equal(await page.getByLabel('Interpreter executable', { exact: true }).inputValue(), 'C:\\PowerShell\\unsaved.exe');
    await page.getByRole('button', { name: 'Close editor' }).click();

    await page.getByRole('button', { name: 'New profile' }).click();
    await fillProfileFields(page, {
      name: 'Later default',
      id: 'later-default',
      interpreterPath: 'C:\\Python\\later.exe',
      scriptPath: 'C:\\Scripts\\later.py',
    });
    await page.getByRole('button', { name: 'Save profile' }).click();
    await page.getByRole('button', { name: 'Edit Later default' }).waitFor({ state: 'visible' });

    await page.getByRole('button', { name: 'Edit Per-profile override' }).click();
    assert.equal(await page.getByLabel('Interpreter executable', { exact: true }).inputValue(), 'C:\\Python\\override.exe');
    await page.getByRole('combobox').nth(0).selectOption('bash');
    assert.equal(await page.getByLabel('Interpreter executable', { exact: true }).inputValue(), 'C:\\Git\\bin\\bash.exe');
    await page.getByRole('combobox').nth(0).selectOption('powershell');
    assert.equal(await page.getByLabel('Interpreter executable', { exact: true }).inputValue(), 'C:\\PowerShell\\unsaved.exe');
    await page.getByRole('combobox').nth(0).selectOption('python');
    assert.equal(await page.getByLabel('Interpreter executable', { exact: true }).inputValue(), 'C:\\Python\\later.exe');
    settings = await page.evaluate(() => window.automator.getModuleSettings({
      contractVersion: 1, moduleId: 'script-runner', settingsVersion: 1,
    }));
    assert.equal(settings.value.interpreterDefaults.python, 'C:\\Python\\later.exe');
  } finally {
    await app.close();
  }
});

test('Script Runner file picker offers the PowerShell script extension', async () => {
  const { isScriptRunnerInterpreter, scriptFileFilter } = await import(pathToFileURL(path.join(workspace, 'contracts', 'scriptRunner.ts')).href);
  assert.equal(isScriptRunnerInterpreter('powershell'), true);
  assert.deepEqual(scriptFileFilter('powershell'), { name: 'PowerShell scripts', extensions: ['ps1'] });
});
