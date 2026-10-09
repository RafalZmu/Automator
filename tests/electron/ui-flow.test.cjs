const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const { waitForPageCondition } = require('./wait-for-page.cjs');

const workspace = path.resolve(__dirname, '../..');

function isolatedDataDirectory(name) {
  return path.join(workspace, 'artifacts', 'test-data', 'electron-ui', name, randomUUID());
}

async function launchHost(name, dataDirectory = isolatedDataDirectory(name), backendDataDirectory = path.join(dataDirectory, 'backend')) {
  return electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: backendDataDirectory,
      AUTOMATOR_BROWSER_NODE_PATH: process.execPath,
      AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_BUILD_ID: `${name}-${process.pid}`,
    },
    timeout: 60_000,
  });
}

test('module settings reads and writes require the active version and preserve all other saved settings', async () => {
  const dataDirectory = isolatedDataDirectory('module-settings');
  const backendDataDirectory = path.join(dataDirectory, 'backend');
  await fs.mkdir(backendDataDirectory, { recursive: true });
  const settingsPath = path.join(backendDataDirectory, 'settings.json');
  await fs.writeFile(settingsPath, JSON.stringify({
    SchemaVersion: 2,
    Hotkey: 'RightAlt',
    Theme: 'Dark',
    StartWithWindows: false,
    Bindings: [{ Id: 'codex', Name: 'Codex', TargetPath: 'C:\\Apps\\Codex.exe', Alias: 'cx', Arguments: '--profile work' }],
    ModuleSettings: [{ ModuleId: 'future-device-tab', SchemaVersion: 3, Value: { device: 'retained' } }],
  }));

  const app = await launchHost('module-settings', dataDirectory, backendDataDirectory);
  try {
    const page = await app.firstWindow();
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    const defaults = await page.evaluate(() => window.automator.getModuleSettings({
      contractVersion: 1, moduleId: 'launcher', settingsVersion: 1,
    }));
    assert.deepEqual(defaults, {
      contractVersion: 1, moduleId: 'launcher', settingsVersion: 1, value: {},
    });

    const inactiveReadError = await page.evaluate(async () => {
      try {
        await window.automator.getModuleSettings({
          contractVersion: 1, moduleId: 'reserved-2', settingsVersion: 1,
        });
        return null;
      } catch (error) { return error instanceof Error ? error.message : String(error); }
    });
    assert.match(inactiveReadError, /not (active|selected)/i);

    const staleReadError = await page.evaluate(async () => {
      try {
        await window.automator.getModuleSettings({
          contractVersion: 1, moduleId: 'launcher', settingsVersion: 2,
        });
        return null;
      } catch (error) { return error instanceof Error ? error.message : String(error); }
    });
    assert.match(staleReadError, /settings version/i);

    const inactiveModuleError = await page.evaluate(async () => {
      try {
        await window.automator.updateModuleSettings({
          contractVersion: 1, moduleId: 'reserved-2', settingsVersion: 1, value: { mode: 'compact' },
        });
        return null;
      } catch (error) { return error instanceof Error ? error.message : String(error); }
    });
    assert.match(inactiveModuleError, /not (active|selected)/i);

    const staleVersionError = await page.evaluate(async () => {
      try {
        await window.automator.updateModuleSettings({
          contractVersion: 1, moduleId: 'launcher', settingsVersion: 2, value: { mode: 'compact' },
        });
        return null;
      } catch (error) { return error instanceof Error ? error.message : String(error); }
    });
    assert.match(staleVersionError, /settings version/i);

    const result = await page.evaluate(() => window.automator.updateModuleSettings({
      contractVersion: 1, moduleId: 'launcher', settingsVersion: 1, value: { mode: 'compact' },
    }));
    assert.deepEqual(result, { saved: true, moduleId: 'launcher', settingsVersion: 1 });
    const persistedSettings = await page.evaluate(() => window.automator.getModuleSettings({
      contractVersion: 1, moduleId: 'launcher', settingsVersion: 1,
    }));
    assert.deepEqual(persistedSettings, {
      contractVersion: 1, moduleId: 'launcher', settingsVersion: 1, value: { mode: 'compact' },
    });

    const persisted = JSON.parse(await fs.readFile(settingsPath, 'utf8'));
    assert.equal(persisted.Hotkey, 'RightAlt');
    assert.equal(persisted.Theme, 'Dark');
    assert.equal(persisted.StartWithWindows, false);
    assert.equal(persisted.Bindings[0].Arguments, '--profile work');
    assert.deepEqual(persisted.ModuleSettings.find((entry) => entry.ModuleId === 'launcher'), {
      ModuleId: 'launcher', SchemaVersion: 1, Value: { mode: 'compact' },
    });
    assert.deepEqual(persisted.ModuleSettings.find((entry) => entry.ModuleId === 'future-device-tab'), {
      ModuleId: 'future-device-tab', SchemaVersion: 3, Value: { device: 'retained' },
    });
    assert.ok((await fs.readdir(backendDataDirectory)).some((file) => file.startsWith('settings.json.bak-')),
      'settings updates use the atomic replacement path with a recoverable backup');
  } finally {
    await app.close();
  }
});

test('settings and tab transitions preserve focus in the isolated desktop host', async () => {
  const app = await launchHost('ui-flow');
  const errors = [];
  try {
    const page = await app.firstWindow();
    await page.emulateMedia({ reducedMotion: 'reduce' });
    page.on('pageerror', (error) => errors.push(error.message));
    page.on('console', (message) => { if (message.type() === 'error') errors.push(message.text()); });
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    await page.getByRole('tab', { name: 'Launcher, tab 1' }).click();
    await page.getByRole('textbox', { name: 'Search applications' }).waitFor({ state: 'visible' });
    await page.waitForFunction(() => document.activeElement?.id === 'search');
    await assertVisibleLauncher(page);
    const inactiveModuleError = await page.evaluate(async () => {
      try {
        await window.automator.dispatchModuleAction({
          requestId: 'ui-flow-request',
          contractVersion: 1,
          moduleId: 'reserved-2',
          actionId: 'future-action',
          actionVersion: 1,
          input: {},
        });
        return null;
      } catch (error) {
        return error instanceof Error ? error.message : String(error);
      }
    });
    assert.match(inactiveModuleError, /not (active|selected)/i, 'the backend rejects a module action for a non-active tab');
    await assertVisibleLauncher(page);

    await page.getByRole('button', { name: 'Settings' }).click();
    await page.getByRole('dialog', { name: 'Settings' }).waitFor({ state: 'visible' });
    await page.getByRole('group', { name: 'Color theme' }).getByRole('button', { name: 'Dark' }).click();
    await page.getByRole('button', { name: 'Save changes' }).click();
    await page.getByRole('dialog', { name: 'Settings' }).waitFor({ state: 'hidden' });
    await page.locator('.window-shell[data-theme="dark"]').waitFor({ state: 'visible' });
    await page.getByRole('textbox', { name: 'Search applications' }).waitFor({ state: 'visible' });
    await page.getByRole('heading', { name: 'No applications yet' }).waitFor({ state: 'visible' });
    await waitForPageCondition(page, async () => {
      const [initial, diagnostics] = await Promise.all([
        window.automator.getInitialState(),
        window.automator.getWindowDiagnostics(),
      ]);
      const focusedSearch = document.activeElement?.id === 'search';
      const moduleView = document.querySelector('.module-view');
      const viewOpacity = moduleView ? Number.parseFloat(getComputedStyle(moduleView).opacity) : 0;
      const animationsSettled = document.getAnimations().every((animation) => animation.playState !== 'running');
      return initial.state?.visible === true && diagnostics?.visible === true
        && focusedSearch && viewOpacity >= 0.99 && animationsSettled;
    }, 'the dark launcher transition and search focus to settle', 5000);
    await assertVisibleLauncher(page);
    await page.screenshot({ path: path.join(workspace, 'artifacts', 'gui-migration', 'dark-launcher.png'), animations: 'allow' });

    await page.getByRole('tab', { name: 'Tab 9, tab 9' }).click();
    await page.getByRole('textbox', { name: 'Search applications' }).waitFor({ state: 'hidden' });
    await assertVisibleLauncher(page);
    await page.waitForFunction(() => document.activeElement?.getAttribute('aria-label') === 'Tab 9, tab 9');
    await page.getByRole('tab', { name: 'Launcher, tab 1' }).click();
    await page.getByRole('textbox', { name: 'Search applications' }).waitFor({ state: 'visible' });
    await assertVisibleLauncher(page);
    await page.waitForFunction(() => document.activeElement?.id === 'search');

    await page.getByRole('button', { name: 'Settings' }).click();
    await page.getByRole('dialog', { name: 'Settings' }).waitFor({ state: 'visible' });
    await page.keyboard.press('Escape');
    await waitForPageCondition(page, async () => {
      const [initial, diagnostics] = await Promise.all([
        window.automator.getInitialState(),
        window.automator.getWindowDiagnostics(),
      ]);
      return initial.state?.visible === false && diagnostics?.visible === false;
    }, 'Escape to close the backend and native window');
    assert.deepEqual(errors, []);
  } finally {
    await app.close();
  }
});

test('compact launcher uses the larger fixed size and remains non-resizable', async () => {
  const app = await launchHost('launcher-size');
  try {
    const page = await app.firstWindow();
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });

    const windowState = await app.evaluate(({ BrowserWindow, screen }) => {
      const window = BrowserWindow.getAllWindows()[0];
      const bounds = window.getBounds();
      const center = { x: bounds.x + bounds.width / 2, y: bounds.y + bounds.height / 2 };
      return {
        bounds,
        workArea: screen.getDisplayNearestPoint(center).workArea,
        resizable: window.isResizable(),
      };
    });
    assert.equal(windowState.bounds.width, Math.min(760, windowState.workArea.width));
    assert.equal(windowState.bounds.height, Math.min(800, windowState.workArea.height));
    assert.equal(windowState.resizable, false, 'the compact launcher remains fixed-size');
  } finally {
    await app.close();
  }
});

test('active tab content scrolls independently from the launcher chrome', async () => {
  const app = await launchHost('launcher-scroll');
  try {
    const page = await app.firstWindow();
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    await page.getByRole('tab', { name: 'Script Runner, tab 2' }).click();
    const activeView = page.locator('.module-view[aria-label="Script Runner"]');
    await activeView.waitFor({ state: 'visible' });
    const before = await activeView.evaluate((view) => {
      const tallContent = document.createElement('div');
      tallContent.dataset.testTallContent = 'true';
      tallContent.style.cssText = 'flex:none;height:1400px';
      view.append(tallContent);
      const selectors = ['.topbar', '.tabs', '.tab-command', '.footer'];
      return {
        overflowY: getComputedStyle(view).overflowY,
        scrollHeight: view.scrollHeight,
        clientHeight: view.clientHeight,
        chromeTop: selectors.map((selector) => document.querySelector(selector)?.getBoundingClientRect().top),
      };
    });
    assert.equal(before.overflowY, 'auto', 'the active view exposes a vertical scrollbar');
    assert.ok(before.scrollHeight > before.clientHeight, 'tall tab content extends beyond the viewport');

    const after = await activeView.evaluate((view) => {
      view.scrollTop = view.scrollHeight;
      const selectors = ['.topbar', '.tabs', '.tab-command', '.footer'];
      return {
        scrollTop: view.scrollTop,
        chromeTop: selectors.map((selector) => document.querySelector(selector)?.getBoundingClientRect().top),
      };
    });
    assert.ok(after.scrollTop > 0, 'the tab content can be scrolled');
    assert.deepEqual(after.chromeTop, before.chromeTop, 'the topbar, selector, command bar, and footer stay fixed');
  } finally {
    await app.close();
  }
});

async function assertVisibleLauncher(page) {
  const state = await page.evaluate(() => window.automator.getInitialState());
  const diagnostics = await page.evaluate(() => window.automator.getWindowDiagnostics());
  assert.equal(state.state?.visible, true, 'backend panel state remains visible');
  assert.equal(diagnostics?.visible, true, 'native BrowserWindow remains visible');
}

test('backend retry restores a visible launcher after the child process exits', async () => {
  const app = await launchHost('backend-retry');
  try {
    const page = await app.firstWindow();
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    const firstHost = await page.evaluate(() => window.automator.getHostInfo());
    assert.ok(firstHost.backendProcessId > 0);
    process.kill(firstHost.backendProcessId, 'SIGKILL');

    await page.getByRole('heading', { name: 'Automator needs to restart' }).waitFor({ state: 'visible', timeout: 15_000 });
    await page.getByRole('button', { name: 'Retry' }).click();
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible', timeout: 15_000 });
    await waitForPageCondition(page, async () => {
      const [initial, diagnostics] = await Promise.all([
        window.automator.getInitialState(),
        window.automator.getWindowDiagnostics(),
      ]);
      return initial.host.backendState === 'ready' && initial.state?.visible === true && diagnostics?.visible === true;
    }, 'backend retry to restore a visible launcher', 15_000);
    try {
      await waitForPageCondition(page, () => document.activeElement?.id === 'quick-actions-search',
        'backend retry to focus Global Action Search', 15_000);
    } catch (error) {
      console.log('BACKEND_RETRY_FOCUS_DIAGNOSTICS', await page.evaluate(() => ({
        activeElement: document.activeElement?.id,
        documentFocused: document.hasFocus(),
        searchPresent: Boolean(document.getElementById('quick-actions-search')),
      })));
      throw error;
    }
    const recovered = await page.evaluate(() => window.automator.getInitialState());
    assert.equal(recovered.host.backendState, 'ready');
    assert.equal(recovered.state.visible, true);
  } finally {
    await app.close();
  }
});

test('Workspace opens as an independent large window with local tabs and hide behavior', async () => {
  const app = await launchHost('workspace-flow');
  const errors = [];
  try {
    const launcher = await app.firstWindow();
    launcher.on('pageerror', (error) => errors.push(error.message));
    await launcher.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    const workspacePagePromise = app.waitForEvent('window');
    await launcher.getByRole('button', { name: 'Open Workspace' }).click();
    const workspacePage = await workspacePagePromise;
    await workspacePage.locator('.window-shell[data-surface="workspace"]').waitFor({ state: 'visible' });
    await waitForPageCondition(workspacePage, async () => {
      const context = await window.automator.getWindowContext();
      return context.role === 'workspace' && context.selectedTab === 2;
    }, 'Workspace to start in its own Script Runner tab');
    await workspacePage.screenshot({ path: path.join(workspace, 'artifacts', 'gui-migration', 'workspace.png'), animations: 'disabled' });

    await workspacePage.getByRole('tab', { name: 'API, tab 3' }).click();
    await workspacePage.getByRole('heading', { name: 'API', exact: true }).waitFor({ state: 'visible' });
    const [workspaceContext, launcherContext, windows] = await Promise.all([
      workspacePage.evaluate(() => window.automator.getWindowContext()),
      launcher.evaluate(() => window.automator.getWindowContext()),
      app.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows().map((window) => ({
        role: new URL(window.webContents.getURL()).searchParams.get('surface') === 'workspace' ? 'workspace' : 'launcher',
        title: window.getTitle(), visible: window.isVisible(), bounds: window.getBounds(), resizable: window.isResizable(),
      }))),
    ]);
    assert.deepEqual(workspaceContext, { role: 'workspace', selectedTab: 3 });
    assert.deepEqual(launcherContext, { role: 'launcher', selectedTab: 1 });
    const workspaceWindow = windows.find((window) => window.role === 'workspace');
    const launcherWindow = windows.find((window) => window.role === 'launcher');
    assert.ok(workspaceWindow?.visible);
    assert.ok(workspaceWindow.bounds.width >= 980 && workspaceWindow.bounds.height >= 680);
    assert.equal(workspaceWindow.resizable, true);
    assert.ok(launcherWindow?.visible, 'opening and changing Workspace tabs leaves the quick launcher visible');

    await workspacePage.getByRole('button', { name: 'Hide Workspace' }).click();
    await waitForPageCondition(launcher, async () => {
      const currentWindows = await window.automator.getInitialState();
      return currentWindows.host.backendState === 'ready';
    }, 'the shared host to remain available after Workspace hides');
    const afterHide = await app.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows().map((window) => ({
      role: new URL(window.webContents.getURL()).searchParams.get('surface') === 'workspace' ? 'workspace' : 'launcher',
      visible: window.isVisible(),
    })));
    assert.equal(afterHide.find((window) => window.role === 'workspace')?.visible, false);
    assert.equal(afterHide.find((window) => window.role === 'launcher')?.visible, true);
    assert.deepEqual(errors, []);
  } finally {
    await app.close();
  }
});

test('Browser Automation opens an Automator-managed Playwright project with an example test', async () => {
  const dataDirectory = isolatedDataDirectory('playwright-default-project');
  const app = await launchHost('playwright-default-project', dataDirectory);
  try {
    const launcher = await app.firstWindow();
    await launcher.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    const workspacePagePromise = app.waitForEvent('window');
    await launcher.getByRole('button', { name: 'Open Workspace' }).click();
    const workspacePage = await workspacePagePromise;
    await workspacePage.locator('.window-shell[data-surface="workspace"]').waitFor({ state: 'visible' });
    await workspacePage.getByRole('tab', { name: 'Browser Automation, tab 4' }).click();
    await workspacePage.getByRole('heading', { name: 'Playwright Test Explorer' }).waitFor({ state: 'visible' });
    try {
      await workspacePage.getByText('Automator example test', { exact: true }).waitFor({ state: 'visible', timeout: 25_000 });
    } catch (error) {
      console.log('Playwright UI notice:', await workspacePage.locator('.playwright-info').allTextContents());
      throw error;
    }

    const projectRoot = await workspacePage.getByRole('textbox', { name: 'Playwright project folder' }).inputValue();
    assert.equal(projectRoot, path.join(dataDirectory, 'Playwright'));
    const examplePath = path.join(projectRoot, 'tests', 'example.spec.ts');
    const exampleSource = await fs.readFile(examplePath, 'utf8');
    assert.match(exampleSource, /test\(['"]Automator example test['"]/);
  } finally {
    await app.close();
  }
});

test('Work Time supports multiple named timers and Workspace-only reports with editing, filtering, CSV export, and deletion', async () => {
  const dataDirectory = isolatedDataDirectory('work-time-reports');
  const csvPath = path.join(dataDirectory, 'work-time-report.csv');
  const app = await launchHost('work-time-reports', dataDirectory);
  try {
    const launcher = await app.firstWindow();
    await launcher.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    await launcher.getByRole('tab', { name: 'Work Time, tab 7' }).click();
    await launcher.getByRole('heading', { name: 'Work Time Log' }).waitFor({ state: 'visible' });
    assert.equal(await launcher.locator('.work-time-reports').count(), 0, 'the quick launcher hides reporting controls and history');
    await launcher.screenshot({ path: path.join(dataDirectory, 'work-time-launcher.png') });

    await launcher.getByRole('textbox', { name: 'New timer description' }).fill('Timer A');
    await launcher.getByRole('button', { name: 'Start timer' }).click();
    const timerA = launcher.getByRole('article', { name: 'Running timer Timer A' });
    await timerA.waitFor({ state: 'visible' });
    await timerA.getByRole('button', { name: 'Pause timer' }).click();
    await launcher.getByRole('textbox', { name: 'New timer description' }).fill('Timer B');
    await launcher.getByRole('button', { name: 'Start timer' }).click();
    const timerB = launcher.getByRole('article', { name: 'Running timer Timer B' });
    await timerB.waitFor({ state: 'visible' });
    await timerB.getByRole('button', { name: 'End timer' }).click();
    const draftB = launcher.getByRole('region', { name: 'Save work interval Timer B' });
    await draftB.waitFor({ state: 'visible' });
    await draftB.getByRole('textbox', { name: 'Work tags Timer B' }).fill('reporting, priority');
    await draftB.getByRole('button', { name: 'Save entry' }).click();
    await launcher.getByRole('article', { name: 'Paused timer Timer A' }).getByRole('button', { name: 'Resume timer' }).waitFor({ state: 'visible' });

    const workspacePromise = app.waitForEvent('window');
    await launcher.getByRole('button', { name: 'Open Workspace' }).click();
    const workspacePage = await workspacePromise;
    await workspacePage.locator('.window-shell[data-surface="workspace"]').waitFor({ state: 'visible' });
    await workspacePage.getByRole('heading', { name: 'Work Time Log' }).waitFor({ state: 'visible' });
    const reportRegion = workspacePage.getByRole('region', { name: 'Work time reports' });
    await reportRegion.waitFor({ state: 'visible' });

    const resumedA = workspacePage.getByRole('article', { name: 'Paused timer Timer A' });
    await resumedA.getByRole('button', { name: 'Resume timer' }).click();
    const runningA = workspacePage.getByRole('article', { name: 'Running timer Timer A' });
    await runningA.waitFor({ state: 'visible' });
    await workspacePage.waitForTimeout(1_200);
    await runningA.getByRole('button', { name: 'Pause timer' }).click();
    const pausedA = workspacePage.getByRole('article', { name: 'Paused timer Timer A' });
    await pausedA.getByRole('button', { name: 'End timer' }).click();
    const draftA = workspacePage.getByRole('region', { name: 'Save work interval Timer A' });
    await draftA.getByRole('textbox', { name: 'Work description Timer A' }).fill('Sample work item');
    await draftA.getByRole('textbox', { name: 'Work tags Timer A' }).fill('reporting, priority');
    await draftA.getByRole('button', { name: 'Save entry' }).click();
    let savedEntry = reportRegion.locator('.work-time-report-row').filter({ hasText: 'Sample work item' });
    await savedEntry.waitFor({ state: 'visible' });
    const savedB = reportRegion.locator('.work-time-report-row').filter({ hasText: 'Timer B' });
    await savedB.waitFor({ state: 'visible' });

    await reportRegion.getByRole('combobox', { name: 'Filter work log by tag' }).selectOption('priority');
    assert.equal(await reportRegion.locator('.work-time-report-row').count(), 2, 'both matching entries are retained by the tag filter');
    await savedEntry.getByRole('button', { name: 'Edit work log entry Sample work item' }).click();
    await reportRegion.getByRole('textbox', { name: 'Edit work description' }).fill('Edited work item');
    await reportRegion.getByRole('textbox', { name: 'Edit work tags' }).fill('reporting, priority, done');
    await reportRegion.getByRole('button', { name: 'Save work entry changes' }).click();
    savedEntry = reportRegion.locator('.work-time-report-row').filter({ hasText: 'Edited work item' });
    await savedEntry.getByText('Edited work item', { exact: true }).waitFor({ state: 'visible' });
    await workspacePage.screenshot({ path: path.join(dataDirectory, 'work-time-workspace.png') });

    await app.evaluate(({ dialog }, filePath) => {
      dialog.showSaveDialog = async () => ({ canceled: false, filePath });
    }, csvPath);
    await reportRegion.getByRole('button', { name: 'Export filtered work log as CSV' }).click();
    const csv = await fs.readFile(csvPath, 'utf8');
    assert.match(csv, /Edited work item/);
    assert.match(csv, /durationMilliseconds/);

    await savedEntry.getByRole('button', { name: 'Delete work log entry Edited work item' }).click();
    await savedEntry.getByRole('button', { name: 'Confirm delete work entry' }).click();
    await savedEntry.waitFor({ state: 'detached' });
    assert.equal(await launcher.locator('.work-time-reports').count(), 0, 'reports stay out of the quick launcher while Workspace is open');
  } finally {
    await app.close();
  }
});
