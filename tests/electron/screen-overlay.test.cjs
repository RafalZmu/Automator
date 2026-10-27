const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');

const workspace = path.resolve(__dirname, '../..');

async function launchHost() {
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'electron-overlay', randomUUID());
  await fs.mkdir(dataDirectory, { recursive: true });
  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: path.join(dataDirectory, 'backend'),
      AUTOMATOR_BROWSER_NODE_PATH: process.execPath,
      AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_BUILD_ID: `overlay-${process.pid}`,
    },
    timeout: 60_000,
  });
  const page = await app.firstWindow();
  page.setDefaultTimeout(15_000);
  await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible', timeout: 60_000 });
  return { app, page, dataDirectory };
}

async function workTimeAction(page, actionId, input) {
  const result = await page.evaluate(async ({ actionId, input }) => {
    const { state } = await window.automator.getInitialState();
    const tab = state.tabs.find((candidate) => candidate.id === 'focus-sessions');
    const action = tab.actions.find((candidate) => candidate.id === actionId);
    await window.automator.selectTab(tab.slot);
    const reply = await window.automator.dispatchModuleAction({
      requestId: crypto.randomUUID(), contractVersion: tab.contractVersion, moduleId: 'focus-sessions', actionId, actionVersion: action.version, input,
    });
    await window.automator.selectTab(1);
    return reply;
  }, { actionId, input });
  assert.notEqual(result.status, 'error', result.message);
  return result;
}

/** Describes the overlay window as the host sees it, or null when no window hosts the overlay page. */
async function readOverlay(app) {
  return app.evaluate(async ({ BrowserWindow, screen }) => {
    for (const window of BrowserWindow.getAllWindows()) {
      if (window.isDestroyed() || !window.webContents.getURL().startsWith('data:text/html')) continue;
      const hosted = await window.webContents.executeJavaScript('typeof window.setItems === "function"').catch(() => false);
      if (!hosted) continue;
      const image = await window.webContents.capturePage();
      return {
        visible: window.isVisible(),
        alwaysOnTop: window.isAlwaysOnTop(),
        focusable: window.isFocusable(),
        bounds: window.getBounds(),
        workAreas: screen.getAllDisplays().map((display) => display.workArea),
        text: await window.webContents.executeJavaScript('document.body.innerText'),
        pixels: image.toPNG().toString('base64'),
      };
    }
    return null;
  });
}

async function waitFor(read, accept, what, timeoutMs = 20_000) {
  const deadline = Date.now() + timeoutMs;
  let last;
  while (Date.now() < deadline) {
    last = await read();
    if (accept(last)) return last;
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
  assert.fail(`${what}; last seen: ${JSON.stringify(last && { ...last, pixels: undefined })}`);
}

test('a running Work Time timer shows a visible, click-through overlay that disappears when the timer pauses', async () => {
  const { app, page, dataDirectory } = await launchHost();
  try {
    assert.equal(await readOverlay(app), null, 'no overlay while no timer runs');

    const started = await workTimeAction(page, 'start', { description: 'Overlay e2e' });
    const timer = started.data.timers.find((candidate) => candidate.status === 'running');
    assert.ok(timer, 'the timer is running');

    const overlay = await waitFor(() => readOverlay(app), (value) => value?.visible && /Overlay e2e/.test(value.text), 'the overlay never became visible with the timer text');
    assert.match(overlay.text, /^\d+:\d\d:\d\d\s+Overlay e2e$/);
    assert.equal(overlay.alwaysOnTop, true);
    assert.equal(overlay.focusable, false, 'the overlay must not take focus');
    assert.ok(overlay.workAreas.some((area) => overlay.bounds.x >= area.x && overlay.bounds.y >= area.y
      && overlay.bounds.x + overlay.bounds.width <= area.x + area.width && overlay.bounds.y + overlay.bounds.height <= area.y + area.height),
    `the overlay lies inside a display work area: ${JSON.stringify({ bounds: overlay.bounds, areas: overlay.workAreas })}`);

    const screenshot = path.join(dataDirectory, 'overlay.png');
    await fs.writeFile(screenshot, Buffer.from(overlay.pixels, 'base64'));
    assert.ok((await fs.stat(screenshot)).size > 200, 'the overlay rendered a non-empty image');

    await workTimeAction(page, 'pause', { id: timer.id });
    await waitFor(() => readOverlay(app), (value) => value === null, 'the overlay stayed after the timer paused');
  } finally {
    await app.close().catch(() => undefined);
  }
});
