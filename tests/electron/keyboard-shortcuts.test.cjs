const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');
const { waitForPageCondition } = require('./wait-for-page.cjs');

const workspace = path.resolve(__dirname, '../..');

async function launchHost(name) {
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'keyboard-shortcuts', name, randomUUID());
  const app = await electron.launch({
    args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')], cwd: workspace,
    env: { ...process.env, AUTOMATOR_TEST_MODE: '1', AUTOMATOR_TEST_TRAY: '1',
      AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
      AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY: path.join(dataDirectory, 'backend'),
      AUTOMATOR_BROWSER_NODE_PATH: process.execPath, AUTOMATOR_SHOW_ON_START: '1',
      AUTOMATOR_BUILD_ID: `keyboard-${name}-${process.pid}` }, timeout: 60_000,
  });
  const page = await app.firstWindow();
  page.setDefaultTimeout(15_000);
  await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
  return { app, page };
}

async function openWorkspace(app, launcher) {
  const opened = app.waitForEvent('window');
  await launcher.getByRole('button', { name: 'Open Workspace', exact: true }).click();
  const page = await opened;
  page.setDefaultTimeout(15_000);
  await page.locator('.window-shell[data-surface="workspace"]').waitFor({ state: 'visible' });
  return page;
}

async function waitForSelectedTab(page, slot) {
  await page.waitForFunction((expected) => {
    const selected = document.querySelector('[role="tab"][aria-selected="true"]');
    return Boolean(selected?.getAttribute('aria-label')?.endsWith(`, tab ${expected}`));
  }, slot, { timeout: 5000 }).catch(async (error) => {
    const current = await page.evaluate(() => document.querySelector('[role="tab"][aria-selected="true"]')?.getAttribute('aria-label') ?? 'none');
    throw new Error(`${error.message} Selected tab was: ${current}`);
  });
}

test('launcher keyboard layer: reference panel, Ctrl+K, tab cycling and Settings without the mouse', async () => {
  const { app, page } = await launchHost('launcher');
  try {
    const reference = page.getByRole('heading', { name: 'Keyboard shortcuts', exact: true });
    await page.keyboard.press('Control+/');
    await reference.waitFor({ state: 'visible' });
    // Ctrl+/ toggles the reference. Escape is not used here: the compact launcher's native key layer owns Escape.
    await page.keyboard.press('Control+/');
    await waitForPageCondition(page, () => !document.querySelector('.shortcut-panel'), 'Ctrl+/ to close the shortcut reference');
    await page.keyboard.press('F1');
    await reference.waitFor({ state: 'visible' });
    await page.keyboard.press('F1');
    await waitForPageCondition(page, () => !document.querySelector('.shortcut-panel'), 'F1 to toggle the shortcut reference closed');

    await page.keyboard.press('Control+k');
    await waitForPageCondition(page, () => document.activeElement?.id === 'quick-actions-search', 'Ctrl+K to focus Quick Actions');

    await page.keyboard.press('Control+Tab');
    await waitForSelectedTab(page, 1);
    await page.keyboard.press('Control+Tab');
    await waitForSelectedTab(page, 2);
    await page.keyboard.press('Control+Shift+Tab');
    await waitForSelectedTab(page, 1);

    await page.keyboard.press('Control+,');
    await page.getByRole('dialog').waitFor({ state: 'visible' });
    // Escape in Settings hides the launcher, as the existing Settings contract requires.
    await page.keyboard.press('Escape');
    await waitForPageCondition(page, () => !document.querySelector('[role="dialog"]'), 'Escape to close Settings');
  } finally { await app.close(); }
});

test('Script Runner keyboard path: Ctrl+N opens the editor with the name focused and Ctrl+S respects validation', async () => {
  const { app, page } = await launchHost('script-runner');
  try {
    await page.keyboard.press('Control+Tab');
    await page.keyboard.press('Control+Tab');
    await waitForSelectedTab(page, 2);
    // Module settings load asynchronously; the command (like the New profile button) is disabled until they are ready.
    await page.waitForFunction(() => [...document.querySelectorAll('button')]
      .some((button) => button.textContent?.trim() === 'New profile' && !button.disabled), null, { timeout: 15_000 });

    await page.keyboard.press('Control+n');
    await page.locator('.script-editor').waitFor({ state: 'visible' });
    await waitForPageCondition(page, () => {
      const active = document.activeElement;
      return active instanceof HTMLInputElement && Boolean(active.closest('.script-editor'));
    }, 'Ctrl+N to focus the profile name');
    await page.keyboard.type('Keyboard fixture');

    // Required script fields are still empty, so Ctrl+S must not submit; the editor stays open with the typed name.
    await page.keyboard.press('Control+s');
    await page.locator('.script-editor').waitFor({ state: 'visible' });
    assert.equal(await page.locator('.script-editor input').first().inputValue(), 'Keyboard fixture');
  } finally { await app.close(); }
});

test('workspace keyboard path: Ctrl+K opens the tab action search and Escape steps back before hiding', async () => {
  const { app, page } = await launchHost('workspace');
  try {
    const workspacePage = await openWorkspace(app, page);
    await workspacePage.keyboard.press('2');
    await waitForSelectedTab(workspacePage, 2);

    await workspacePage.keyboard.press('Control+k');
    await waitForPageCondition(workspacePage, () => document.activeElement?.id === 'tab-command-input', 'Ctrl+K to focus the tab action search');
    await workspacePage.keyboard.type('new');
    await workspacePage.locator('.tab-command-results').waitFor({ state: 'visible' });

    await workspacePage.keyboard.press('Escape');
    await waitForPageCondition(workspacePage, () => document.getElementById('tab-command-input')?.value === '', 'Escape to clear the action search');
    assert.equal(await workspacePage.evaluate(() => document.activeElement?.id), 'tab-command-input', 'the first Escape keeps focus in the search');

    await workspacePage.keyboard.press('Escape');
    await waitForPageCondition(workspacePage, () => document.activeElement?.id !== 'tab-command-input', 'Escape to leave the empty search field');
    assert.equal(await workspacePage.locator('.window-shell[data-surface="workspace"]').isVisible(), true, 'leaving the field must not hide the workspace');
  } finally { await app.close(); }
});

test('catalog keyboard path: arrows move through apps, Enter starts the alias, Escape backs out', async () => {
  const { app, page } = await launchHost('catalog');
  try {
    await page.keyboard.press('/');
    const search = page.getByRole('textbox', { name: 'Search application catalog' });
    await search.waitFor({ state: 'visible' });
    await waitForPageCondition(page, async () => {
      const initial = await window.automator.getInitialState();
      return initial.state?.mode === 'catalog' && !initial.state.catalogLoading && initial.state.catalogTotalCount > 0;
    }, 'Windows catalog discovery', 20_000);
    await search.fill('Windows PowerShell');
    await page.locator('.app-row-main').first().waitFor({ state: 'visible' });

    await page.keyboard.press('ArrowDown');
    await waitForPageCondition(page, () => document.activeElement?.classList.contains('app-row-main'), 'ArrowDown to enter the app list');
    await page.keyboard.press('ArrowUp');
    await waitForPageCondition(page, () => document.activeElement?.id === 'catalog-search', 'ArrowUp from the first app to return to search');

    await page.keyboard.press('Enter');
    await page.locator('section[aria-label="Assign application alias"]').waitFor({ state: 'visible' });
    await page.keyboard.press('Escape');
    await waitForPageCondition(page, () => !document.querySelector('section[aria-label="Assign application alias"]'), 'Escape to cancel alias editing');
    assert.equal(await page.locator('.window-shell').isVisible(), true, 'Escape must cancel alias editing before hiding');
  } finally { await app.close(); }
});
