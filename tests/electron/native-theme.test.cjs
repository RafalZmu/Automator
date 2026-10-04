const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const { waitForPageCondition } = require('./wait-for-page.cjs');

const workspace = path.resolve(__dirname, '../..');

test('saved dark theme is applied before the native window is shown', { timeout: 15_000 }, async () => {
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'native-theme', randomUUID());
  const backendDataDirectory = path.join(dataDirectory, 'backend-seed');
  await fs.mkdir(backendDataDirectory, { recursive: true });
  await fs.writeFile(path.join(backendDataDirectory, 'settings.json'), JSON.stringify({
    SchemaVersion: 1,
    Hotkey: 'RightControl',
    Theme: 'Dark',
    StartWithWindows: false,
    Bindings: [],
  }), 'utf8');

  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_START_HIDDEN: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: backendDataDirectory,
      AUTOMATOR_BUILD_ID: `native-theme-${process.pid}`,
    },
    timeout: 60_000,
  });

  try {
    const page = await app.firstWindow();
    await waitForPageCondition(page, async () => {
      const initial = await window.automator?.getInitialState();
      return initial?.state?.theme === 'Dark';
    }, 'the backend to report the saved dark theme');
    const initial = await page.evaluate(() => window.automator.getInitialState());
    const diagnostics = await page.evaluate(() => window.automator.getWindowDiagnostics());
    assert.equal(initial.host.themeSource, 'dark');
    assert.equal(initial.state.visible, false);
    assert.equal(diagnostics.visible, false);
  } finally {
    await app.close();
  }

  const hostLogNames = (await fs.readdir(path.join(dataDirectory, 'Logs'))).filter((name) => name.endsWith('.jsonl'));
  const hostEvents = (await Promise.all(hostLogNames.map(async (name) => fs.readFile(path.join(dataDirectory, 'Logs', name), 'utf8'))))
    .join('\n').split(/\r?\n/).filter((line) => line.trim()).map((line) => JSON.parse(line));
  const stoppingIndex = hostEvents.findIndex((event) => event.Event === 'App.Stopping');
  assert.notEqual(stoppingIndex, -1, 'host shutdown should stop the backend before exit');
  assert.equal(hostEvents.slice(stoppingIndex + 1).some((event) => [
    'Panel.NativeShowEvent', 'Panel.RecoveryShown', 'Tray.FallbackPanelShown',
  ].includes(event.Event)), false, 'a late ready-to-show callback must not reopen the panel during shutdown');
});
