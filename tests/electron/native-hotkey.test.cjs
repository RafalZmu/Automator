const assert = require('node:assert/strict');
const { spawn, execFileSync } = require('node:child_process');
const fs = require('node:fs/promises');
const path = require('node:path');
const { randomUUID } = require('node:crypto');
const { setTimeout: delay } = require('node:timers/promises');
const { test } = require('node:test');
const { _electron: electron } = require('playwright');

const workspace = path.resolve(__dirname, '../..');
const powershell = 'powershell.exe';
const hotkeyTargetScript = path.join(__dirname, 'KeyboardTarget.ps1');
const focusTargetScript = path.join(__dirname, 'Focus-TestWindow.ps1');
const sendKeyScript = path.join(__dirname, 'Send-Key.ps1');

function panelBoundsAt(pointer, workArea) {
  const width = Math.max(1, Math.min(760, workArea.width));
  const height = Math.max(1, Math.min(800, workArea.height));
  const left = Math.max(workArea.x, Math.min(Math.round(pointer.x - width / 2), workArea.x + workArea.width - width));
  const top = Math.max(workArea.y, Math.min(pointer.y + 18, workArea.y + workArea.height - height));
  return { x: left, y: top, width, height };
}

async function waitFor(predicate, description, timeoutMs = 5000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    if (await predicate()) return;
    await delay(40);
  }
  throw new Error(`Timed out waiting for ${description}.`);
}

test('real Right Ctrl and Escape input reaches the global hook from an owned foreground app', async () => {
  const dataDirectory = path.join(workspace, 'artifacts', 'test-data', 'native-hotkey', randomUUID());
  const fixtureStatePath = path.join(dataDirectory, 'fixture-text.txt');
  const fixtureCommandDirectory = path.join(dataDirectory, 'fixture-commands');
  const fixtureResultDirectory = path.join(dataDirectory, 'fixture-results');
  await fs.mkdir(dataDirectory, { recursive: true });
  await fs.mkdir(fixtureCommandDirectory, { recursive: true });
  await fs.mkdir(fixtureResultDirectory, { recursive: true });
  await fs.writeFile(fixtureStatePath, '', 'utf8');
  const fixture = spawn(powershell, [
    '-NoProfile', '-NonInteractive', '-STA', '-ExecutionPolicy', 'Bypass', '-File', hotkeyTargetScript,
    fixtureStatePath, fixtureCommandDirectory, fixtureResultDirectory,
  ], { cwd: workspace, windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
  let output = '';
  let keyOutput = '';
  let originalCursor;
  let fixtureCommandSequence = 0;
  let keyCommandSequence = 0;
  let keySender;
  fixture.stdout.setEncoding('utf8');
  fixture.stderr.setEncoding('utf8');
  fixture.stdout.on('data', (chunk) => { output += chunk; });
  fixture.stderr.on('data', (chunk) => { output += chunk; });
  async function fixtureCommand(command) {
    const commandId = String(++fixtureCommandSequence);
    const commandPath = path.join(fixtureCommandDirectory, `command-${commandId}.txt`);
    const temporaryCommandPath = `${commandPath}.tmp`;
    const resultPath = path.join(fixtureResultDirectory, `result-${commandId}.txt`);
    await fs.writeFile(temporaryCommandPath, command, 'utf8');
    await fs.rename(temporaryCommandPath, commandPath);
    await waitFor(async () => fs.access(resultPath).then(() => true, () => false), `fixture command ${commandId}`);
    const result = (await fs.readFile(resultPath, 'utf8')).trim().toUpperCase();
    if (result.startsWith('ERROR:')) throw new Error(`Fixture command failed: ${result}`);
    return result;
  }
  const foregroundWindowHandle = async () => fixtureCommand('PROBE');
  async function sendKey(virtualKey) {
    const commandId = String(++keyCommandSequence);
    const marker = `KEY_DONE:${commandId}`;
    const errorMarker = `KEY_ERROR:${commandId}:`;
    keySender.stdin.write(`${commandId}:${virtualKey}\n`);
    await waitFor(() => keyOutput.includes(marker) || keyOutput.includes(errorMarker), `synthetic key ${commandId}`);
    if (keyOutput.includes(errorMarker)) {
      const errorOffset = keyOutput.indexOf(errorMarker) + errorMarker.length;
      throw new Error(`The persistent key sender failed: ${keyOutput.slice(errorOffset).split(/\r?\n/, 1)[0]}`);
    }
  }
  const setCursorPosition = async (point) => { await fixtureCommand(`CURSOR:${point.x}:${point.y}`); };
  let app;
  try {
    await waitFor(() => output.includes('FIXTURE_READY:'), 'the owned WinForms target window');
    const fixtureHandle = `0x${BigInt(output.match(/FIXTURE_READY:(\d+)/)[1]).toString(16)}`.toUpperCase();
    execFileSync(powershell, [
      '-NoProfile', '-NonInteractive', '-WindowStyle', 'Hidden', '-ExecutionPolicy', 'Bypass',
      '-File', focusTargetScript, '-WindowHandleHex', fixtureHandle,
    ], { cwd: workspace, windowsHide: true, encoding: 'utf8', timeout: 10_000 });
    await waitFor(async () => (await foregroundWindowHandle()) === fixtureHandle, 'the owned target to be foreground');

    app = await electron.launch({
      args: [path.join(workspace, 'dist-electron', 'main', 'main.cjs')],
      cwd: workspace,
      env: {
        ...process.env,
        AUTOMATOR_TEST_MODE: '1',
        AUTOMATOR_TEST_TRAY: '1',
        AUTOMATOR_TEST_START_HIDDEN: '1',
        AUTOMATOR_TEST_DATA_DIRECTORY: dataDirectory,
        AUTOMATOR_BUILD_ID: `native-hotkey-${process.pid}`,
      },
      timeout: 60_000,
    });
    const page = await app.firstWindow();
    await page.getByRole('main', { name: 'Automator launcher' }).waitFor({ state: 'visible' });
    await waitFor(() => page.evaluate(async () => {
      const [initial, diagnostics] = await Promise.all([
        window.automator.getInitialState(),
        window.automator.getWindowDiagnostics(),
      ]);
      return initial.host.backendState === 'ready' && initial.state?.visible === false && diagnostics?.visible === false;
    }), 'the tray host to finish initializing while its panel is hidden');
    const windowHandle = (await page.evaluate(() => window.automator.getWindowDiagnostics())).windowHandleHex.toUpperCase();
    execFileSync(powershell, [
      '-NoProfile', '-NonInteractive', '-WindowStyle', 'Hidden', '-ExecutionPolicy', 'Bypass',
      '-File', focusTargetScript, '-WindowHandleHex', fixtureHandle,
    ], { cwd: workspace, windowsHide: true, encoding: 'utf8', timeout: 10_000 });
    await waitFor(async () => (await foregroundWindowHandle()) === fixtureHandle, 'the owned target to be foreground after host startup');
    assert.equal(await fixtureCommand('NORMAL'), fixtureHandle, 'the fixture should remain foreground after leaving topmost mode');
    keySender = spawn(powershell, [
      '-NoProfile', '-NonInteractive', '-WindowStyle', 'Hidden', '-ExecutionPolicy', 'Bypass', '-File', sendKeyScript,
    ], { cwd: workspace, windowsHide: true, stdio: ['pipe', 'pipe', 'pipe'] });
    keySender.stdout.setEncoding('utf8');
    keySender.stderr.setEncoding('utf8');
    keySender.stdout.on('data', (chunk) => { keyOutput += chunk; });
    keySender.stderr.on('data', (chunk) => { keyOutput += chunk; });
    await waitFor(() => keyOutput.includes('KEY_SENDER_READY'), 'the persistent key sender to start');
    const placement = await app.evaluate(({ BrowserWindow, screen }) => {
      const bounds = BrowserWindow.getAllWindows()[0].getBounds();
      const center = { x: bounds.x + bounds.width / 2, y: bounds.y + bounds.height / 2 };
      return {
        bounds,
        workArea: screen.getDisplayNearestPoint(center).workArea,
        cursor: screen.getCursorScreenPoint(),
      };
    });
    originalCursor = placement.cursor;
    const alternatePointer = [
      { x: placement.workArea.x + 16, y: placement.workArea.y + 16 },
      { x: placement.workArea.x + placement.workArea.width - 16, y: placement.workArea.y + 16 },
      { x: placement.workArea.x + 16, y: placement.workArea.y + placement.workArea.height - 16 },
      { x: placement.workArea.x + placement.workArea.width - 16, y: placement.workArea.y + placement.workArea.height - 16 },
    ].find((point) => {
      const candidate = panelBoundsAt(point, placement.workArea);
      return candidate.x !== placement.bounds.x || candidate.y !== placement.bounds.y;
    });
    assert.ok(alternatePointer, 'test display has enough room to place the panel differently');
    await setCursorPosition(placement.cursor);
    await sendKey(0xA3);
    await waitFor(async () => (await page.evaluate(() => window.automator.getWindowDiagnostics()))?.visible === true, 'Right Ctrl to open the tray panel');
    const initial = await page.evaluate(() => window.automator.getInitialState());
    assert.equal(initial.state.previousForegroundHwnd?.toUpperCase(), fixtureHandle);
    await waitFor(async () => (await page.evaluate(() => window.automator.getWindowDiagnostics()))?.foregroundHwndHex?.toUpperCase() === windowHandle,
      'the launcher HWND to become foreground');
    await page.getByPlaceholder('Search apps, profiles, and actions…').waitFor({ state: 'visible' });
    await page.waitForFunction(() => document.activeElement?.id === 'quick-actions-search');
    const firstBounds = await app.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows()[0].getBounds());
    await setCursorPosition(alternatePointer);
    const boundsAfterPointerMove = await app.evaluate(({ BrowserWindow }) => BrowserWindow.getAllWindows()[0].getBounds());
    assert.deepEqual(boundsAfterPointerMove, firstBounds, 'moving the pointer after opening must preserve the panel position');

    await sendKey(0x39);
    await waitFor(async () => (await page.evaluate(() => window.automator.getInitialState())).state?.selectedTab === 9, 'the native 9 shortcut to select tab 9');
    await page.waitForFunction(() => document.activeElement?.getAttribute('aria-label') === 'Tab 9, tab 9');
    await sendKey(0x31);
    await waitFor(async () => (await page.evaluate(() => window.automator.getInitialState())).state?.selectedTab === 1, 'the native 1 shortcut to return to tab 1');
    await page.waitForFunction(() => document.activeElement?.id === 'search');
    await sendKey(0x41);
    await waitFor(async () => (await page.evaluate(() => window.automator.getInitialState())).state?.query === 'a', 'native alphabetic input to reach the launcher query');
    assert.equal(await fs.readFile(fixtureStatePath, 'utf8'), '', 'the previous foreground app did not receive the typed letter');

    await sendKey(0x1B);
    await waitFor(async () => (await page.evaluate(() => window.automator.getWindowDiagnostics()))?.visible === false, 'Escape to close the focused launcher');
    await waitFor(async () => (await foregroundWindowHandle()) === fixtureHandle, 'Windows to reactivate the previous foreground app after Escape');
  } finally {
    keySender?.kill();
    await app?.close().catch(() => {});
    if (originalCursor) {
      try { await setCursorPosition(originalCursor); } catch { /* Restore is best-effort during test cleanup. */ }
    }
    if (fixture.exitCode === null && fixture.signalCode === null) fixture.kill();
    await Promise.race([new Promise((resolve) => fixture.once('exit', resolve)), delay(5000)]);
  }

  const logDirectory = path.join(dataDirectory, 'Logs');
  const logFiles = (await fs.readdir(logDirectory)).filter((name) => name.startsWith('automator-'));
  const hostLogs = (await Promise.all(logFiles.map((name) => fs.readFile(path.join(logDirectory, name), 'utf8')))).join('\n');
  assert.match(hostLogs, /"Event":"Panel\.NativeFocusConfirmed"/);
  const hostReady = hostLogs.split('\n').filter((line) => line.trim()).map((line) => JSON.parse(line)).find((event) => event.Event === 'App.Ready');
  assert.ok(hostReady);
  const backendLogDirectory = path.join(dataDirectory, 'backend', `native-hotkey-${process.pid}-${hostReady.ProcessId}`, 'Logs');
  const backendLogFiles = (await fs.readdir(backendLogDirectory)).filter((name) => name.endsWith('.log'));
  const backendLogs = (await Promise.all(backendLogFiles.map((name) => fs.readFile(path.join(backendLogDirectory, name), 'utf8')))).join('\n');
  assert.match(backendLogs, /"event":"Keyboard\.OpenerTapped"/);
  assert.match(backendLogs, /"event":"Keyboard\.NavigationConsumed"/);
  assert.match(backendLogs, /"event":"Launcher\.Opened"/);
});
