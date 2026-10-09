const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const { waitForPageCondition } = require('./wait-for-page.cjs');

const workspace = path.resolve(__dirname, '../..');

async function launchHost(name) {
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'tab-visibility', name, randomUUID());
  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')], cwd: workspace,
    env: { ...process.env, AUTOMATOR_TEST_MODE: '1', AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: path.join(dataDirectory, 'backend'),
      AUTOMATOR_BROWSER_NODE_PATH: process.execPath, AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_BUILD_ID: `${name}-${process.pid}` }, timeout: 60_000,
  });
  const page = await app.firstWindow();
  page.setDefaultTimeout(15_000);
  await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
  return { app, page, dataDirectory };
}

async function assertButtonInView(button) {
  assert.equal(await button.isVisible(), true, 'required action button must be visible');
  const geometry = await buttonGeometry(button);
  assert.ok(geometry.inside, `required action button must fit visible content bounds: ${JSON.stringify(geometry)}`);
  assert.ok(geometry.uncovered, 'required action button must not be covered by another surface');
}

async function buttonGeometry(button) {
  return button.evaluate((element) => {
    const rect = element.getBoundingClientRect();
    const bounds = { left: 0, top: 0, right: innerWidth, bottom: innerHeight };
    const scrollTargets = [];
    for (let ancestor = element.parentElement; ancestor; ancestor = ancestor.parentElement) {
      const css = getComputedStyle(ancestor);
      const box = ancestor.getBoundingClientRect();
      const left = box.left + ancestor.clientLeft;
      const top = box.top + ancestor.clientTop;
      if (/auto|scroll|hidden|clip/.test(css.overflowX)) {
        bounds.left = Math.max(bounds.left, left);
        bounds.right = Math.min(bounds.right, left + ancestor.clientWidth);
      }
      if (/auto|scroll|hidden|clip/.test(css.overflowY)) {
        bounds.top = Math.max(bounds.top, top);
        bounds.bottom = Math.min(bounds.bottom, top + ancestor.clientHeight);
      }
      if (/auto|scroll/.test(css.overflowY) && ancestor.scrollHeight > ancestor.clientHeight) {
        scrollTargets.push({ left, top, right: left + ancestor.clientWidth,
          bottom: top + ancestor.clientHeight });
      }
    }
    const inside = rect.width > 0 && rect.height > 0 && rect.left >= bounds.left - 1
      && rect.right <= bounds.right + 1 && rect.top >= bounds.top - 1 && rect.bottom <= bounds.bottom + 1;
    const hit = document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2);
    return { rect: { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom }, bounds,
      inside, uncovered: !!hit && element.contains(hit), scrollTargets };
  });
}

// Scroll with user wheel input, then assert BEFORE click can perform auto-scrolling.
// Hidden controls, horizontal clipping, and controls in non-scrollable areas still fail.
async function visibleButton(page, scope, name, { enabled = true } = {}) {
  const button = scope.getByRole('button', { name, exact: true });
  await button.waitFor({ state: 'visible' });
  if (enabled) {
    const deadline = Date.now() + 10_000;
    while (!await button.isEnabled() && Date.now() < deadline) await page.waitForTimeout(40);
    assert.equal(await button.isEnabled(), true, `${name} must be enabled`);
  }
  for (let attempt = 0; attempt < 20; attempt++) {
    const geometry = await buttonGeometry(button);
    if (geometry.inside) break;
    const target = geometry.scrollTargets.find((box) => geometry.rect.top < box.top - 1
      || geometry.rect.bottom > box.bottom + 1);
    if (!target) break;
    const left = Math.max(target.left, geometry.bounds.left);
    const right = Math.min(target.right, geometry.bounds.right);
    const top = Math.max(target.top, geometry.bounds.top);
    const bottom = Math.min(target.bottom, geometry.bounds.bottom);
    if (right <= left || bottom <= top) break;
    await page.mouse.move((left + right) / 2, (top + bottom) / 2);
    await page.mouse.wheel(0, geometry.rect.top < geometry.bounds.top ? -180 : 180);
    await page.waitForTimeout(80);
  }
  await assertButtonInView(button);
  return button;
}

async function clickButton(page, scope, name) {
  await (await visibleButton(page, scope, name)).click();
}

async function selectTab(page, name, number) {
  const tab = page.getByRole('tab', { name: `${name}, tab ${number}`, exact: true });
  await tab.click();
}

async function openWorkspace(app, launcher) {
  const opened = app.waitForEvent('window');
  await clickButton(launcher, launcher, 'Open Workspace');
  const page = await opened;
  page.setDefaultTimeout(15_000);
  await page.locator('.window-shell[data-surface="workspace"]').waitFor({ state: 'visible' });
  return page;
}

async function saveScriptFixture(page, dataDirectory) {
  await selectTab(page, 'Script Runner', 2);
  await clickButton(page, page, 'New profile');
  const script = path.join(dataDirectory, 'visibility.ps1');
  await fs.mkdir(dataDirectory, { recursive: true });
  await fs.writeFile(script, 'Write-Output "visibility fixture"\n');
  await page.getByLabel('Name', { exact: true }).fill('Visibility script');
  await page.getByLabel('Profile key', { exact: true }).fill('visibility-script');
  await page.getByRole('combobox').first().selectOption('powershell');
  await page.getByLabel('Interpreter executable', { exact: true }).fill(path.join(process.env.SystemRoot, 'System32', 'WindowsPowerShell', 'v1.0', 'powershell.exe'));
  await page.getByLabel('Script file').fill(script);
  await page.getByLabel('Working directory').fill(dataDirectory);
  await visibleButton(page, page, 'Cancel');
  await clickButton(page, page, 'Save profile');
  await page.getByRole('button', { name: 'Edit Visibility script', exact: true }).waitFor({ state: 'visible' });
}

test('happy-path visibility rejects hidden viewport-clipped and ancestor-clipped buttons', async () => {
  const { app, page } = await launchHost('visibility-guard');
  try {
    await page.getByRole('tab', { name: 'Work Time, tab 7' }).click();
    const button = page.getByRole('button', { name: 'Start timer', exact: true, includeHidden: true });
    await button.waitFor({ state: 'visible' });
    await assertButtonInView(button);
    await button.evaluate((element) => { element.style.visibility = 'hidden'; });
    await assert.rejects(assertButtonInView(button), /must be visible/);
    await button.evaluate((element) => { element.style.visibility = ''; });
    const originalParentStyle = await button.evaluate((element) => {
      const parent = element.parentElement;
      const previous = parent.getAttribute('style');
      parent.style.setProperty('max-height', '1px', 'important');
      parent.style.overflow = 'hidden';
      return previous;
    });
    assert.equal(await button.isVisible(), true, 'CSS visibility alone misses ancestor clipping');
    await assert.rejects(assertButtonInView(button), /visible content bounds/);
    await button.evaluate((element, previous) => {
      if (previous === null) element.parentElement.removeAttribute('style');
      else element.parentElement.setAttribute('style', previous);
    }, originalParentStyle);
    await button.evaluate((element) => {
      element.style.position = 'fixed';
      element.style.top = '2000px';
    });
    assert.equal(await button.isVisible(), true, 'CSS visibility alone misses viewport clipping');
    await assert.rejects(assertButtonInView(button), /visible content bounds/);
  } finally { await app.close(); }
});

test('Launcher alias happy path keeps catalog and editor buttons within visible bounds', async () => {
  const { app, page, dataDirectory } = await launchHost('launcher');
  try {
    await clickButton(page, page, 'Open application catalog');
    await waitForPageCondition(page, async () => {
      const initial = await window.automator.getInitialState();
      return initial.state?.mode === 'catalog' && !initial.state.catalogLoading && initial.state.catalogTotalCount > 0;
    }, 'Windows catalog discovery', 15_000);
    await page.getByRole('textbox', { name: 'Search application catalog' }).fill('Windows PowerShell');
    await clickButton(page, page, 'Select Windows PowerShell to assign an alias');
    const editor = page.getByRole('region', { name: 'Assign application alias' });
    await visibleButton(page, editor, 'Cancel alias editing');
    await visibleButton(page, editor, 'Cancel');
    await page.getByLabel('Unique letters only').fill('vhp');
    await clickButton(page, editor, 'Save alias');
    await visibleButton(page, page, 'Launch Windows PowerShell');
    await clickButton(page, page, 'Edit alias vhp for Windows PowerShell');
    await page.getByLabel('Unique letters only').fill('vhq');
    await clickButton(page, editor, 'Save alias');
    await visibleButton(page, page, 'Edit alias vhq for Windows PowerShell');
    const saved = JSON.parse(await fs.readFile(path.join(dataDirectory, 'backend', 'settings.json'), 'utf8'));
    assert.ok(saved.Bindings.some((binding) => binding.Name === 'Windows PowerShell' && binding.Alias === 'vhq'));
  } finally { await app.close(); }
});

test('Script Runner profile happy path keeps create save and edit buttons within visible bounds', async () => {
  const { app, page, dataDirectory } = await launchHost('script-runner');
  try {
    await saveScriptFixture(page, dataDirectory);
    await visibleButton(page, page, 'Run Visibility script');
    await clickButton(page, page, 'Edit Visibility script');
    await visibleButton(page, page, 'Close editor');
    await page.getByLabel('Name', { exact: true }).fill('Edited visibility script');
    await clickButton(page, page, 'Save profile');
    await visibleButton(page, page, 'Edit Edited visibility script');
    await clickButton(page, page, 'Edit Edited visibility script');
    assert.equal(await page.getByLabel('Name', { exact: true }).inputValue(), 'Edited visibility script');
  } finally { await app.close(); }
});

test('Script Runner Library exposes a primary Add to Script Runner action on the Firebird card', async () => {
  const { app, page, dataDirectory } = await launchHost('script-runner-library');
  try {
    await selectTab(page, 'Script Runner', 2);
    await clickButton(page, page, 'Library');
    const library = page.getByRole('region', { name: 'Script template library', exact: true });
    const firebirdCard = library.locator('.script-template-row').filter({ hasText: 'Firebird 3 database backup and ZIP' });
    await firebirdCard.waitFor({ state: 'visible' });

    const isolatedDatabasePath = path.join(dataDirectory, 'isolated-target.fdb');
    const targetDatabase = firebirdCard.getByLabel('Target database *', { exact: true });
    await targetDatabase.waitFor({ state: 'visible' });
    const databaseGeometry = await buttonGeometry(targetDatabase);
    assert.equal(await targetDatabase.isVisible(), true, 'Target database must be visible on the Firebird card before Details');
    assert.ok(databaseGeometry.inside, `Target database must fit visible content bounds: ${JSON.stringify(databaseGeometry)}`);
    assert.ok(databaseGeometry.uncovered, 'Target database must not be covered by another surface');
    await targetDatabase.fill(isolatedDatabasePath);
    assert.equal(await targetDatabase.inputValue(), isolatedDatabasePath);

    await visibleButton(page, firebirdCard, 'Details');
    const addButton = await visibleButton(page, firebirdCard, 'Add to Script Runner');
    assert.equal(await addButton.evaluate((element) => element.classList.contains('primary-button')), true,
      'the Library add action must use the distinct primary button style');
    await addButton.click();
    await visibleButton(page, library, 'Run template');
    assert.equal(await targetDatabase.inputValue(), isolatedDatabasePath, 'Installing the profile must preserve the target database path in the Library form');

    const closeLibrary = await visibleButton(page, library, 'Close library');
    await closeLibrary.click();
    await library.waitFor({ state: 'detached' });
    await visibleButton(page, page, 'Edit Firebird 3 database backup and ZIP');

    await clickButton(page, page, 'Library');
    const reopenedLibrary = page.getByRole('region', { name: 'Script template library', exact: true });
    const reopenedCard = reopenedLibrary.locator('.script-template-row').filter({ hasText: 'Firebird 3 database backup and ZIP' });
    await reopenedCard.waitFor({ state: 'visible' });
    await visibleButton(page, reopenedCard, 'Profile added', { enabled: false });
    await clickButton(page, reopenedCard, 'Details');
    await visibleButton(page, reopenedLibrary, 'Run template');
    assert.equal(await reopenedCard.getByLabel('Target database *', { exact: true }).inputValue(), isolatedDatabasePath,
      'Reopening Details for the installed profile must preserve the target database path');
  } finally { await app.close(); }
});

test('API profile happy path keeps create save and edit buttons within visible bounds', async () => {
  const { app, page } = await launchHost('api');
  try {
    await selectTab(page, 'API', 3);
    await visibleButton(page, page, 'Create API profile');
    await clickButton(page, page, 'New profile');
    await visibleButton(page, page, 'Close editor');
    await page.getByLabel('Name', { exact: true }).fill('Visibility API');
    await page.getByLabel('Profile key', { exact: true }).fill('visibility-api');
    await page.getByPlaceholder('https://api.example.com/v1/status').fill('https://example.com/visibility');
    await visibleButton(page, page, 'Cancel');
    await clickButton(page, page, 'Save profile');
    await visibleButton(page, page, 'Run Visibility API');
    await clickButton(page, page, 'Edit Visibility API');
    await page.getByLabel('Name', { exact: true }).fill('Edited visibility API');
    await clickButton(page, page, 'Save profile');
    await visibleButton(page, page, 'Edit Edited visibility API');
    await clickButton(page, page, 'Edit Edited visibility API');
    assert.equal(await page.getByLabel('Name', { exact: true }).inputValue(), 'Edited visibility API');
  } finally { await app.close(); }
});

test('Browser Automation section happy path keeps create and tag buttons within visible bounds', async () => {
  const { app, page: launcher, dataDirectory } = await launchHost('browser');
  try {
    const page = await openWorkspace(app, launcher);
    await selectTab(page, 'Browser Automation', 4);
    await page.getByText('Automator example test', { exact: true }).waitFor({ state: 'visible', timeout: 25_000 });
    await visibleButton(page, page, 'Browse');
    await visibleButton(page, page, 'Use folder');
    await clickButton(page, page, 'Refresh tests');
    await clickButton(page, page, 'New section');
    await page.getByLabel('Test file in the project', { exact: true }).fill('tests/visibility.spec.ts');
    await page.getByLabel('Test name', { exact: true }).fill('Visibility section test');
    await visibleButton(page, page, 'Cancel');
    await clickButton(page, page, 'Create file');
    await page.getByText('Visibility section test', { exact: true }).waitFor({ state: 'visible' });
    await visibleButton(page, page, 'Run section tests/visibility.spec.ts');
    await visibleButton(page, page, 'Run Visibility section test');
    await clickButton(page, page, 'Edit tags for Visibility section test');
    await page.getByRole('textbox', { name: 'Tags for Visibility section test', exact: true }).fill('visibility');
    await visibleButton(page, page, 'Cancel tag editing');
    await clickButton(page, page, 'Save tags');
    await page.locator('.playwright-tag').getByText('visibility', { exact: true }).waitFor({ state: 'visible' });
    assert.match(await fs.readFile(path.join(dataDirectory, 'Playwright', 'tests', 'visibility.spec.ts'), 'utf8'), /Visibility section test/);
  } finally { await app.close(); }
});

test('Workflows editor happy path keeps step and save buttons within visible bounds', async () => {
  const { app, page, dataDirectory } = await launchHost('workflows');
  try {
    await saveScriptFixture(page, dataDirectory);
    await selectTab(page, 'Workflows', 5);
    await visibleButton(page, page, 'Create a workflow');
    await clickButton(page, page, 'New workflow');
    const editor = page.getByRole('region', { name: 'Workflow editor' });
    await visibleButton(page, editor, 'Close');
    await editor.getByLabel('Name', { exact: true }).fill('Visibility workflow');
    await editor.getByLabel('Workflow key', { exact: true }).fill('visibility-workflow');
    await clickButton(page, editor, 'Add step');
    await editor.getByRole('combobox', { name: /^Saved profile/ }).selectOption('visibility-script');
    await visibleButton(page, editor, 'Add mapping');
    await visibleButton(page, editor, 'Remove step 1');
    await visibleButton(page, editor, 'Save workflow');
    await clickButton(page, editor, 'Cancel');
    await editor.waitFor({ state: 'detached' });
  } finally { await app.close(); }
});

test('Scheduler save happy path keeps create calendar and editor buttons within visible bounds', async () => {
  const { app, page, dataDirectory } = await launchHost('scheduler');
  try {
    await saveScriptFixture(page, dataDirectory);
    await selectTab(page, 'Scheduler', 6);
    await clickButton(page, page, 'Calendar');
    assert.equal(await page.getByRole('button', { name: 'Calendar', exact: true }).getAttribute('aria-pressed'), 'true');
    await clickButton(page, page, 'Agenda');
    await clickButton(page, page, 'New');
    await visibleButton(page, page, 'Close editor');
    await page.getByLabel('Name', { exact: true }).fill('Visibility schedule');
    await page.getByLabel('Enabled', { exact: true }).uncheck();
    await visibleButton(page, page, 'Cancel');
    await clickButton(page, page, 'Save');
    await visibleButton(page, page, 'Enable');
    await visibleButton(page, page, 'Run Visibility schedule now');
    await clickButton(page, page, 'Edit Visibility schedule');
    await page.getByLabel('Name', { exact: true }).fill('Edited visibility schedule');
    await clickButton(page, page, 'Save');
    await visibleButton(page, page, 'Edit Edited visibility schedule');
    await visibleButton(page, page, 'Enable');
  } finally { await app.close(); }
});

test('Work Time named timer and report happy path keeps controls within visible bounds', async () => {
  const { app, page: launcher, dataDirectory } = await launchHost('work-time');
  try {
    const page = await openWorkspace(app, launcher);
    await selectTab(page, 'Work Time', 7);
    await page.getByLabel('New timer description').fill('Visibility work item');
    await clickButton(page, page, 'Start timer');
    const timer = page.getByRole('article', { name: 'Running timer Visibility work item' });
    await timer.waitFor({ state: 'visible' });
    await page.waitForTimeout(1_200);
    await clickButton(page, timer, 'Pause timer');
    const paused = page.getByRole('article', { name: 'Paused timer Visibility work item' });
    await clickButton(page, paused, 'End timer');
    const draft = page.getByRole('region', { name: 'Save work interval Visibility work item' });
    await draft.getByLabel('Work tags Visibility work item').fill('visibility');
    await clickButton(page, draft, 'Save entry');
    const report = page.getByRole('region', { name: 'Work time reports' });
    await clickButton(page, report, 'Edit work log entry Visibility work item');
    await report.getByLabel('Edit work description').fill('Edited visibility work item');
    await clickButton(page, report, 'Save work entry changes');
    const csvPath = path.join(dataDirectory, 'report.csv');
    await app.evaluate(({ dialog }, filePath) => {
      dialog.showSaveDialog = async () => ({ canceled: false, filePath });
    }, csvPath);
    await clickButton(page, report, 'Export filtered work log as CSV');
    assert.match(await fs.readFile(csvPath, 'utf8'), /Edited visibility work item/);
    await clickButton(page, report, 'Delete work log entry Edited visibility work item');
    await clickButton(page, report, 'Confirm delete work entry');
    await report.getByText('Edited visibility work item', { exact: true }).waitFor({ state: 'detached' });
  } finally { await app.close(); }
});

test('Website Launcher save happy path keeps shortcut group and website buttons within visible bounds', async () => {
  const { app, page } = await launchHost('website-launcher');
  try {
    await selectTab(page, 'Website Launcher', 8);
    await clickButton(page, page, 'Add shortcut');
    await page.getByLabel('Name', { exact: true }).fill('Visibility websites');
    await page.getByLabel('Tab shortcut', { exact: true }).fill('visibility');
    await page.getByLabel('Window 1 tab 1 name').fill('Example');
    await page.getByLabel('Window 1 tab 1 URL').fill('https://example.com/visibility');
    await visibleButton(page, page, 'Add website to this window');
    await visibleButton(page, page, 'Add browser window');
    await clickButton(page, page, 'Save');
    await visibleButton(page, page, 'Launch Visibility websites');
    await page.getByLabel('Name', { exact: true }).fill('Edited visibility websites');
    await clickButton(page, page, 'Save');
    await visibleButton(page, page, 'Launch Edited visibility websites');
    const saved = await page.evaluate(() => window.automator.getModuleSettings({
      contractVersion: 1, moduleId: 'website-launcher', settingsVersion: 1,
    }));
    assert.equal(saved.value.rows[0].name, 'Edited visibility websites');
    assert.equal(saved.value.rows[0].groups[0].websites[0].url, 'https://example.com/visibility');
  } finally { await app.close(); }
});
