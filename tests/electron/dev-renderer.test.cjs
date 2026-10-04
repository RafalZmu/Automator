const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { setTimeout: delay } = require('node:timers/promises');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');

const workspace = path.resolve(__dirname, '../..');
const rendererUrl = 'http://127.0.0.1:5173/';
const viteScript = path.join(workspace, 'node_modules', 'vite', 'bin', 'vite.js');

async function isThisProjectServingVite() {
  try {
    const response = await fetch(rendererUrl);
    const html = await response.text();
    return response.ok && html.includes('id="root"') && html.includes('/ui/main.tsx');
  } catch { return false; }
}

async function ensureVite() {
  if (await isThisProjectServingVite()) return { child: undefined };
  const child = spawn(process.execPath, [viteScript, '--host', '127.0.0.1'], {
    cwd: workspace,
    stdio: 'ignore',
    windowsHide: true,
  });
  for (let attempt = 0; attempt < 100; attempt += 1) {
    if (child.exitCode !== null) throw new Error(`Vite exited with code ${child.exitCode}.`);
    if (await isThisProjectServingVite()) return { child };
    await delay(100);
  }
  child.kill();
  throw new Error('Vite did not become ready on port 5173.');
}

test('Electron development URL runs Vite React preamble under the development-only CSP', async () => {
  const { child: vite } = await ensureVite();
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'electron-dev-url', randomUUID());
  fs.mkdirSync(dataDirectory, { recursive: true });
  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: {
      ...process.env,
      AUTOMATOR_TEST_MODE: '1',
      AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_RENDERER_URL: rendererUrl,
      AUTOMATOR_BUILD_ID: `electron-dev-url-${process.pid}`,
    },
    timeout: 60_000,
  });
  const failures = [];
  try {
    const page = await app.firstWindow();
    page.on('pageerror', (error) => failures.push(error.message));
    page.on('console', (message) => {
      if (message.type() === 'error' || /content security policy/i.test(message.text())) failures.push(message.text());
    });
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    await page.waitForFunction(() => window.automator?.getHostInfo);
    assert.equal((await page.evaluate(() => window.automator.getHostInfo())).backendState, 'ready');
    assert.deepEqual(failures, []);
  } finally {
    await app.close();
    if (vite && vite.exitCode === null) vite.kill();
  }
});
