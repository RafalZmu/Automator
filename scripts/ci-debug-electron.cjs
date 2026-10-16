// TEMPORARY diagnostic for CI: dumps what the Script Runner tab renders.
const path = require('node:path');
const fs = require('node:fs');
const { _electron: electron } = require('playwright');
const workspace = path.resolve(__dirname, '..');
const out = path.join(workspace, 'artifacts', 'ci-debug');
fs.mkdirSync(out, { recursive: true });
(async () => {
  const data = path.join(out, 'data');
  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
    cwd: workspace,
    env: { ...process.env, AUTOMATOR_TEST_MODE: '1', AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: data, AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: path.join(data, 'backend'),
      AUTOMATOR_SHOW_ON_START: '1', AUTOMATOR_BUILD_ID: 'ci-debug' },
    timeout: 60000,
  });
  const log = [];
  app.process().stdout?.on('data', (d) => log.push('[main out] ' + d));
  app.process().stderr?.on('data', (d) => log.push('[main err] ' + d));
  try {
    const page = await app.firstWindow();
    page.on('console', (m) => log.push(`[console ${m.type()}] ${m.text()}`));
    page.on('pageerror', (e) => log.push(`[pageerror] ${e.message}`));
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible', timeout: 30000 });
    for (const tab of [2, 3, 4]) {
      await page.evaluate((t) => window.automator.selectTab(t), tab);
      await page.waitForTimeout(5000);
      log.push(`--- tab ${tab} body text ---\n` + (await page.evaluate(() => document.body.innerText)).slice(0, 3000));
      await page.screenshot({ path: path.join(out, `tab-${tab}.png`) });
    }
    log.push('state: ' + JSON.stringify(await page.evaluate(() => window.automator.getState?.())).slice(0, 3000));
  } catch (e) { log.push('ERROR ' + e.stack); }
  finally { await app.close().catch(() => {}); fs.writeFileSync(path.join(out, 'log.txt'), log.join('\n')); console.log(log.join('\n')); }
})();
