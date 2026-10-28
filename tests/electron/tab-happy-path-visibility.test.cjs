const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const { waitForPageCondition } = require('./wait-for-page.cjs');

const workspace = path.resolve(__dirname, '../..');

async function launchHost(name, { moduleSettings, dataDirectory: existingDataDirectory } = {}) {
  const dataDirectory = existingDataDirectory ?? path.join(workspace, 'artifacts', 'test-data', 'tab-visibility', name, randomUUID());
  if (moduleSettings) {
    // Seed the isolated backend settings so the host starts with these module preferences already saved.
    await fs.mkdir(path.join(dataDirectory, 'backend'), { recursive: true });
    await fs.writeFile(path.join(dataDirectory, 'backend', 'settings.json'), JSON.stringify({
      SchemaVersion: 2, Hotkey: 'RightControl', Theme: 'Light', StartWithWindows: false, Bindings: [], ModuleSettings: moduleSettings,
    }));
  }
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

async function assertTabIcons(page, tabs) {
  for (const { name, slot, label } of tabs) {
    const tab = page.getByRole('tab', { name: label ?? `${name}, tab ${slot}`, exact: true });
    await tab.waitFor({ state: 'visible' });
    const icon = tab.locator('svg[aria-hidden="true"]');
    assert.equal(await icon.count(), 1, `${name} tab must show one decorative icon`);
    const bounds = await icon.boundingBox();
    // Layout rounds the 12px decorative icon to about 11.99px; accept sub-pixel rounding.
    assert.ok(bounds && bounds.width >= 11.5 && bounds.height >= 11.5,
      `${name} tab icon must have visible dimensions`);
  }
}

async function saveScriptFixture(page, dataDirectory) {
  await selectTab(page, 'Script Runner', 2);
  await clickButton(page, page, 'New script');
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
  await clickButton(page, page, 'Save script');
  await page.getByRole('button', { name: 'Edit Visibility script', exact: true }).waitFor({ state: 'visible' });
}

test('numbered tabs show an identifying icon in the launcher and Workspace', async () => {
  const { app, page } = await launchHost('tab-icons');
  const tabs = [
    { name: 'Launcher', slot: 1 },
    { name: 'Script Runner', slot: 2 },
    { name: 'API', slot: 3 },
    { name: 'Browser Automation', slot: 4 },
    { name: 'Workflows', slot: 5 },
    { name: 'Scheduler', slot: 6 },
    { name: 'Work Time', slot: 7 },
    { name: 'Website Launcher', slot: 8 },
    { name: 'Snippets', slot: 9 },
  ];
  // Rows hold nine tabs, so Quick notes is key 1 of the second row.
  const quickNotes = [{ name: 'Quick notes', slot: 1, label: 'Quick notes, tab 1 of row 2' }];
  try {
    await assertTabIcons(page, tabs);
    await clickButton(page, page, 'Next tab row');
    await assertTabIcons(page, quickNotes);
    const workspacePage = await openWorkspace(app, page);
    await assertTabIcons(workspacePage, tabs.slice(1));
    await clickButton(workspacePage, workspacePage, 'Next tab row');
    await assertTabIcons(workspacePage, quickNotes);
  } finally { await app.close(); }
});

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
    await clickButton(page, page, 'Save script');
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
    await visibleButton(page, firebirdCard, 'Profile added', { enabled: false });
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
    await page.getByRole('region', { name: 'Run Firebird 3 database backup and ZIP', exact: true }).waitFor({ state: 'visible' });
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

test('Snippets save and copy happy path keeps its buttons within visible bounds and fills the clipboard', async () => {
  const { app, page } = await launchHost('snippets');
  const clearClipboard = () => app.evaluate(({ clipboard }) => clipboard.writeText('cleared'));
  const waitForClipboard = async (expected) => {
    const deadline = Date.now() + 5_000;
    let actual = '';
    while (Date.now() < deadline) {
      // Windows stores clipboard line breaks as CRLF, which is what pasting apps expect.
      actual = (await app.evaluate(({ clipboard }) => clipboard.readText())).replace(/\r\n/g, '\n');
      if (actual === expected) return;
      await page.waitForTimeout(50);
    }
    assert.equal(actual, expected);
  };
  try {
    await selectTab(page, 'Snippets', 9);
    await clickButton(page, page, 'Add snippet');
    await page.getByLabel('Name', { exact: true }).fill('Plan command');
    await page.getByLabel('Shortcut', { exact: true }).fill('plan');
    await page.getByLabel('Text to copy', { exact: true }).fill('/plan\nsecond line');
    await clickButton(page, page, 'Save');
    await page.getByRole('status').filter({ hasText: 'Snippets saved.' }).waitFor({ state: 'visible' });

    // The Copy button copies the text exactly as written.
    await clearClipboard();
    await clickButton(page, page, 'Copy Plan command');
    await waitForClipboard('/plan\nsecond line');
    await page.getByRole('status').filter({ hasText: 'Copied Plan command to the clipboard.' }).waitFor({ state: 'visible' });

    // Typing the shortcut in the tab search copies it too, and the notice never repeats the text itself.
    await clearClipboard();
    const search = page.getByPlaceholder('Search actions in this tab…');
    await search.fill('plan');
    await search.press('Enter');
    await waitForClipboard('/plan\nsecond line');
    assert.equal(await page.getByRole('status').filter({ hasText: 'second line' }).count(), 0);

    const saved = await page.evaluate(() => window.automator.getModuleSettings({
      contractVersion: 1, moduleId: 'snippets', settingsVersion: 1,
    }));
    assert.deepEqual(saved.value.snippets.map(({ name, alias, value }) => ({ name, alias, value })), [
      { name: 'Plan command', alias: 'plan', value: '/plan\nsecond line' },
    ]);
  } finally { await app.close(); }
});

test('tab rows page with the arrow keys and number keys follow the focused row', async () => {
  const { app, page } = await launchHost('tab-rows');
  const selected = async () => (await page.evaluate(() => window.automator.getInitialState())).state.selectedTab;
  const indicator = page.getByLabel('Tab row 2 of 2', { exact: true });
  try {
    await page.getByLabel('Tab row 1 of 2', { exact: true }).waitFor({ state: 'visible' });
    assert.equal(await page.getByRole('tab').count(), 9, 'the first row shows nine tabs');
    // The empty Quick Actions search box has nothing to move through, so Down moves to the second row from there.
    await page.keyboard.press('ArrowDown');
    await indicator.waitFor({ state: 'visible' });
    assert.equal(await page.getByRole('tab').count(), 1, 'the second row shows only Quick notes');
    await page.evaluate(() => { if (document.activeElement instanceof HTMLElement) document.activeElement.blur(); });
    await page.keyboard.press('2');
    assert.equal(await selected(), 1, 'key 2 of row two holds no tab');
    await page.keyboard.press('1');
    await page.waitForFunction(async () => (await window.automator.getInitialState()).state.selectedTab === 10);
    await page.keyboard.press('ArrowUp');
    await page.getByLabel('Tab row 1 of 2', { exact: true }).waitFor({ state: 'visible' });
    await page.keyboard.press('2');
    await page.waitForFunction(async () => (await window.automator.getInitialState()).state.selectedTab === 2);
  } finally { await app.close(); }
});

test('dragging a tab onto another swaps their positions and numbers, and the order persists', async () => {
  const first = await launchHost('tab-reorder');
  const order = async (page) => (await page.evaluate(() => window.automator.getInitialState())).state.tabOrder;
  try {
    await first.page.getByRole('tab', { name: 'API, tab 3', exact: true }).dragTo(first.page.getByRole('tab', { name: 'Script Runner, tab 2', exact: true }));
    await first.page.getByRole('tab', { name: 'API, tab 2', exact: true }).waitFor({ state: 'visible' });
    await first.page.getByRole('tab', { name: 'Script Runner, tab 3', exact: true }).waitFor({ state: 'visible' });
    assert.deepEqual((await order(first.page)).slice(0, 4), [1, 3, 2, 4]);
    // The launcher is pinned: dragging it or dropping onto it changes nothing.
    await first.page.getByRole('tab', { name: 'Work Time, tab 7', exact: true }).dragTo(first.page.getByRole('tab', { name: 'Launcher, tab 1', exact: true }));
    assert.deepEqual((await order(first.page)).slice(0, 4), [1, 3, 2, 4]);
  } finally { await first.app.close(); }

  const second = await launchHost('tab-reorder', { dataDirectory: first.dataDirectory });
  try {
    await second.page.getByRole('tab', { name: 'API, tab 2', exact: true }).waitFor({ state: 'visible' });
    assert.deepEqual((await order(second.page)).slice(0, 4), [1, 3, 2, 4]);
    // Number keys follow the new order: key 2 now opens API (slot 3).
    await second.page.evaluate(() => { if (document.activeElement instanceof HTMLElement) document.activeElement.blur(); });
    await second.page.keyboard.press('2');
    await second.page.waitForFunction(async () => (await window.automator.getInitialState()).state.selectedTab === 3);
  } finally { await second.app.close(); }
});

test('Quick notes add, tick, edit, and delete happy path stores notes in the library and shows the unticked dot', async () => {
  const { app, page } = await launchHost('quick-notes');
  // Read the stored notes through the module's own list action, so the test sees what the library holds.
  const savedNotes = () => page.evaluate(async () => (await window.automator.dispatchModuleAction({
    requestId: crypto.randomUUID(), contractVersion: 1, moduleId: 'notes', actionId: 'listNotes', actionVersion: 1, input: {},
  })).data.notes);
  try {
    await clickButton(page, page, 'Next tab row');
    await page.getByRole('tab', { name: 'Quick notes, tab 1 of row 2', exact: true }).click();
    await clickButton(page, page, 'Add note');
    await page.getByLabel('Title', { exact: true }).fill('Shopping');
    await page.getByLabel('Note (markdown)', { exact: true }).fill('# Buy\n- [ ] milk\n- [x] bread');
    await clickButton(page, page, 'Save note');
    await page.getByRole('status').filter({ hasText: 'Note saved.' }).waitFor({ state: 'visible' });

    // The new note opens expanded with its markdown rendered, and its dot shows an unticked checkbox.
    await page.getByRole('heading', { name: 'Buy' }).waitFor({ state: 'visible' });
    await page.getByRole('img', { name: '1 unticked checkbox' }).waitFor({ state: 'visible' });

    // Ticking the last open checkbox persists it and turns the dot green.
    // The checkbox is controlled and updates after the save round trip, so click it instead of asserting an instant state change.
    await page.getByRole('checkbox', { name: 'milk' }).click();
    await page.getByRole('img', { name: 'All checkboxes ticked' }).waitFor({ state: 'visible' });
    await page.waitForFunction(async () => (await window.automator.dispatchModuleAction({
      requestId: crypto.randomUUID(), contractVersion: 1, moduleId: 'notes', actionId: 'listNotes', actionVersion: 1, input: {},
    })).data.notes[0]?.body === '# Buy\n- [x] milk\n- [x] bread');

    // Collapsing hides the rendered note, and editing changes the title and text.
    await page.getByRole('button', { name: /^Shopping/ }).click();
    await page.getByRole('heading', { name: 'Buy' }).waitFor({ state: 'hidden' });
    await page.getByRole('button', { name: /^Shopping/ }).click();
    await clickButton(page, page, 'Edit Shopping');
    await page.getByLabel('Title', { exact: true }).fill('Groceries');
    await clickButton(page, page, 'Save note');
    await page.getByRole('button', { name: /^Groceries/ }).waitFor({ state: 'visible' });
    assert.deepEqual((await savedNotes()).map(({ title }) => title), ['Groceries']);

    await clickButton(page, page, 'Delete Groceries');
    await page.getByText('No notes yet').waitFor({ state: 'visible' });
    assert.deepEqual(await savedNotes(), []);
  } finally { await app.close(); }
});

test('Snippets copy from the launch panel, close the launcher, and never open their tab', async () => {
  const longText = `${'x'.repeat(2500)}\nend`;
  const snippets = [
    { id: 'plan', name: 'Plan command', alias: 'plan', value: '/plan' },
    { id: 'long', name: 'Long text', alias: 'longtext', value: longText },
    { id: 'unlisted', name: 'No shortcut', alias: '', value: 'copy button only' },
  ];
  for (const { alias, expected } of [{ alias: 'plan', expected: '/plan' }, { alias: 'longtext', expected: longText }]) {
    const { app, page } = await launchHost(`snippets-launch-panel-${alias}`, {
      moduleSettings: [{ ModuleId: 'snippets', SchemaVersion: 1, Value: { snippets } }],
    });
    try {
      await app.evaluate(({ clipboard }) => clipboard.writeText('cleared'));
      const search = page.getByPlaceholder('Search apps, profiles, and actions…');
      await search.fill(alias);
      // The shortcut is listed by its name and alias, never by its text.
      await page.getByRole('option', { name: /Snippets · copy to clipboard/ }).waitFor({ state: 'visible' });
      await search.press('Enter');

      const deadline = Date.now() + 5_000;
      let clipboard = '';
      while (Date.now() < deadline) {
        clipboard = (await app.evaluate(({ clipboard: board }) => board.readText())).replace(/\r\n/g, '\n');
        if (clipboard === expected) break;
        await page.waitForTimeout(50);
      }
      assert.equal(clipboard, expected, `${alias} must copy its exact text`);
      await waitForPageCondition(page, async () => {
        const [initial, diagnostics] = await Promise.all([window.automator.getInitialState(), window.automator.getWindowDiagnostics()]);
        return initial.state?.visible === false && diagnostics?.visible === false;
      }, `the launcher to close after copying ${alias}`);
      const state = (await page.evaluate(() => window.automator.getInitialState())).state;
      assert.equal(state.selectedTab, 1, 'the launch panel must not switch to the Snippets tab');
      assert.equal(await page.getByRole('region', { name: 'Snippets', exact: true }).count(), 0, 'the Snippets view must never have opened');
    } finally { await app.close(); }
  }
});
