const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const { randomUUID } = require('node:crypto');

const workspace = path.resolve(__dirname, '../..');

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
      AUTOMATOR_BROWSER_NODE_PATH: process.execPath,
      AUTOMATOR_BUILD_ID: `${name}-${process.pid}`,
    },
    timeout: 60_000,
  });
}

async function openBrowserAutomation(app) {
  const launcher = await app.firstWindow();
  await launcher.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
  const workspacePagePromise = app.waitForEvent('window');
  await launcher.getByRole('button', { name: 'Open Workspace' }).click();
  const page = await workspacePagePromise;
  await page.locator('.window-shell[data-surface="workspace"]').waitFor({ state: 'visible' });
  await page.getByRole('tab', { name: 'Browser Automation, tab 4' }).click();
  await page.getByRole('heading', { name: 'Playwright Test Explorer' }).waitFor({ state: 'visible' });
  try { await page.getByText('Automator example test', { exact: true }).waitFor({ state: 'visible', timeout: 30_000 }); } catch (error) { console.log('NOTICE', await page.locator('.playwright-info').allTextContents(), await page.locator('body').innerText()); throw error; }
  return page;
}

test('a Playwright run keeps its running state across tabs and is saved to the run history database', async () => {
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'electron-ui', 'browser-run-history', randomUUID());
  const backendDataDirectory = path.join(dataDirectory, 'backend');
  let app = await launchHost('browser-run-history', dataDirectory, backendDataDirectory);
  try {
    let page = await openBrowserAutomation(app);
    await fs.writeFile(path.join(dataDirectory, 'Playwright', 'tests', 'slow.spec.ts'),
      "import { test } from '@playwright/test';\n\ntest('slow check', async () => {\n  await new Promise((resolve) => setTimeout(resolve, 6000));\n});\n");
    await page.getByRole('button', { name: 'Refresh tests' }).click();
    await page.getByText('slow check', { exact: true }).waitFor({ state: 'visible', timeout: 30_000 });
    assert.equal(await page.getByRole('region', { name: 'Playwright run history' }).count(), 0, 'a new database has no run history');

    await page.getByRole('button', { name: 'Run section tests/slow.spec.ts' }).click();
    const banner = page.getByRole('status', { name: 'Playwright run in progress' });
    await banner.waitFor({ state: 'visible', timeout: 15_000 });
    assert.match(await banner.innerText(), /tests\/slow\.spec\.ts/);

    // Leaving the tab must neither hide nor stop the run.
    await page.getByRole('tab', { name: 'Script Runner, tab 2' }).click();
    await page.getByRole('region', { name: 'Script Runner' }).waitFor({ state: 'visible' });
    await page.getByRole('button', { name: /is running\. Open Browser Automation/ }).waitFor({ state: 'visible' });
    await page.getByRole('tab', { name: 'Browser Automation, tab 4' }).click();
    await banner.waitFor({ state: 'visible' });

    await banner.waitFor({ state: 'hidden', timeout: 60_000 });
    const history = page.getByRole('region', { name: 'Playwright run history' });
    await history.waitFor({ state: 'visible', timeout: 15_000 });
    const entries = history.getByRole('button', { name: /tests\/slow\.spec\.ts/ });
    assert.equal(await entries.count(), 1);
    assert.match(await entries.first().innerText(), /Passed/);
    await page.getByRole('region', { name: 'Playwright run results' }).waitFor({ state: 'visible' });

    // The history is stored in the library database, so it survives restarting the application.
    await app.close();
    app = await launchHost('browser-run-history', dataDirectory, backendDataDirectory);
    page = await openBrowserAutomation(app);
    const restored = page.getByRole('region', { name: 'Playwright run history' }).getByRole('button', { name: /tests\/slow\.spec\.ts/ });
    await restored.waitFor({ state: 'visible', timeout: 15_000 });
    assert.match(await restored.first().innerText(), /Passed/);

    await page.getByRole('button', { name: 'Clear history' }).click();
    await page.getByRole('region', { name: 'Playwright run history' }).waitFor({ state: 'detached', timeout: 15_000 });
  } finally {
    await app.close();
  }
});
