const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs/promises');
const path = require('node:path');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');

const workspace = path.resolve(__dirname, '../..');
const screenshotPath = path.join(workspace, 'artifacts', 'gui-migration', 'minimal-window.png');

test('Electron shows the minimal isolated launcher and focuses search on startup', async () => {
  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_BUILD_ID: 'minimal-window',
    },
    timeout: 60_000,
  });

  try {
    const page = await app.firstWindow();
    const rendererErrors = [];
    page.on('pageerror', (error) => rendererErrors.push(error.message));
    page.on('console', (message) => {
      if (message.type() === 'error') rendererErrors.push(message.text());
    });
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    await page.waitForFunction(() => document.activeElement?.id === 'quick-actions-search');
    await page.locator('.status-dot[data-ready="true"]').waitFor({ state: 'visible' });

    const state = await page.evaluate(async () => ({
      activeId: document.activeElement?.id ?? null,
      hasBridge: typeof window.automator?.getHostInfo === 'function',
      hasNodeRequire: typeof window.require !== 'undefined',
      hasNodeProcess: typeof window.process !== 'undefined',
      hostInfo: await window.automator?.getHostInfo(),
    }));
    const native = await app.evaluate(({ BrowserWindow }) => {
      const window = BrowserWindow.getAllWindows()[0];
      const handle = window.getNativeWindowHandle();
      const numericHandle = handle.length >= 8 ? handle.readBigUInt64LE(0) : BigInt(handle.readUInt32LE(0));
      return {
        windowHandleHex: `0x${numericHandle.toString(16).toUpperCase()}`,
        browserWindowFocused: window.isFocused(),
        webContentsFocused: window.webContents.isFocused(),
      };
    });
    const foregroundHandle = execFileSync(
      'powershell.exe',
      ['-NoProfile', '-NonInteractive', '-WindowStyle', 'Hidden', '-File', path.join(workspace, 'scripts', 'Get-ForegroundWindowHandle.ps1')],
      { cwd: workspace, windowsHide: true, encoding: 'utf8' },
    ).trim();

    console.log(`FOCUS_DIAGNOSTICS ${JSON.stringify({
      domActiveElement: state.activeId,
      browserWindowFocused: native.browserWindowFocused,
      webContentsFocused: native.webContentsFocused,
      launcherWindowHandle: native.windowHandleHex,
      osForegroundWindowHandle: foregroundHandle,
    })}`);

    assert.equal(state.hostInfo.trayIconReady, true);
    if (state.hostInfo.acrylicSupported) assert.equal(state.hostInfo.transparentWindow, false);
    assert.equal(state.activeId, 'quick-actions-search');
    assert.equal(state.hasBridge, true);
    assert.equal(state.hasNodeRequire, false);
    assert.equal(state.hasNodeProcess, false);
    assert.equal(state.hostInfo.protocolVersion, 3);
    assert.equal(native.browserWindowFocused, true);
    assert.equal(native.webContentsFocused, true);
    assert.equal(foregroundHandle, native.windowHandleHex);
    assert.deepEqual(rendererErrors, []);

    await fs.mkdir(path.dirname(screenshotPath), { recursive: true });
    await page.screenshot({ path: screenshotPath, animations: 'disabled' });
    console.log(`SCREENSHOT ${screenshotPath}`);
  } finally {
    await app.close();
  }
});
