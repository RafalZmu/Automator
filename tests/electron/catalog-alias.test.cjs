const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const { waitForPageCondition } = require('./wait-for-page.cjs');

const workspace = path.resolve(__dirname, '../..');

test('real backend catalog discovery saves a selected app alias to isolated schema-2 settings', async () => {
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'catalog-alias', randomUUID());
  const backendDataDirectory = path.join(dataDirectory, 'backend');
  await fs.mkdir(backendDataDirectory, { recursive: true });
  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: backendDataDirectory,
      AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_BUILD_ID: `catalog-alias-${process.pid}`,
    },
    timeout: 60_000,
  });

  try {
    const page = await app.firstWindow();
    const errors = [];
    page.on('pageerror', (error) => errors.push(error.message));
    page.on('console', (message) => { if (message.type() === 'error') errors.push(message.text()); });
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    await page.getByRole('button', { name: 'Open application catalog' }).click();
    await page.getByRole('textbox', { name: 'Search application catalog' }).waitFor({ state: 'visible' });
    await waitForPageCondition(page, async () => {
      const initial = await window.automator.getInitialState();
      return initial.state?.mode === 'catalog' && initial.state.catalogLoading === false
        && initial.state.catalogTotalCount > 0;
    }, 'real Windows catalog discovery to finish', 15_000);

    await page.getByRole('textbox', { name: 'Search application catalog' }).fill('Windows PowerShell');
    await waitForPageCondition(page, async () => {
      const initial = await window.automator.getInitialState();
      return initial.state?.catalogApps.some((binding) => binding.name === 'Windows PowerShell');
    }, 'Windows PowerShell to appear in real catalog search', 10_000);
    const appRow = page.getByRole('button', { name: 'Select Windows PowerShell to assign an alias' });
    await appRow.waitFor({ state: 'visible' });
    const catalogState = await page.evaluate(() => window.automator.getInitialState());
    const candidate = catalogState.state.catalogApps.find((binding) => binding.name === 'Windows PowerShell');
    assert.ok(candidate);
    await fs.access(candidate.targetPath);

    await appRow.click();
    await page.getByRole('heading', { name: 'Assign an alias' }).waitFor({ state: 'visible' });
    await page.getByLabel('Unique letters only').fill('whp');
    await page.getByRole('button', { name: 'Save alias' }).click();
    await waitForPageCondition(page, async () => {
      const initial = await window.automator.getInitialState();
      return initial.state?.mode === 'launcher' && initial.state.bindings.some((binding) => binding.alias === 'whp');
    }, 'the saved alias to appear in backend state');
    const savedState = await page.evaluate(() => window.automator.getInitialState());
    const savedBinding = savedState.state.bindings.find((binding) => binding.alias === 'whp');
    assert.equal(savedBinding?.id, candidate.id);
    assert.equal(savedBinding?.targetPath, candidate.targetPath);

    const persisted = JSON.parse(await fs.readFile(path.join(backendDataDirectory, 'settings.json'), 'utf8'));
    assert.equal(persisted.SchemaVersion, 2);
    assert.ok(persisted.Bindings.some((binding) => binding.Id === candidate.id
      && binding.TargetPath === candidate.targetPath && binding.Alias === 'whp'));
    await page.getByRole('heading', { name: 'Assign an alias' }).waitFor({ state: 'hidden' });
    await page.getByRole('button', { name: 'Edit alias whp for Windows PowerShell' }).waitFor({ state: 'visible' });
    await page.getByRole('button', { name: 'Remove Windows PowerShell from launcher' }).click({ timeout: 3000 });
    await page.getByRole('button', { name: 'Launch Windows PowerShell', exact: true }).waitFor({ state: 'hidden' });
    const removedState = await page.evaluate(() => window.automator.getInitialState());
    assert.equal(removedState.state.bindings.some((binding) => binding.id === candidate.id), false);
    const afterRemoval = JSON.parse(await fs.readFile(path.join(backendDataDirectory, 'settings.json'), 'utf8'));
    assert.equal(afterRemoval.Bindings.some((binding) => binding.Id === candidate.id), false);
    await fs.access(candidate.targetPath);
    await page.getByRole('button', { name: 'Open application catalog' }).click();
    await page.getByRole('textbox', { name: 'Search application catalog' }).fill('Windows PowerShell');
    await page.getByRole('button', { name: 'Select Windows PowerShell to assign an alias' }).waitFor({ state: 'visible' });
    assert.equal(await page.getByRole('button', { name: 'Remove Windows PowerShell from launcher' }).count(), 0);
    assert.deepEqual(errors, []);
    await fs.mkdir(path.join(workspace, 'artifacts', 'gui-migration'), { recursive: true });
    await page.screenshot({ path: path.join(workspace, 'artifacts', 'gui-migration', 'catalog-alias.png'), animations: 'disabled' });
  } finally {
    await app.close();
  }
});
