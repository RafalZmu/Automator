import { app, BrowserWindow, dialog, ipcMain, Menu, nativeImage, nativeTheme, Notification, screen, Tray, type IpcMainInvokeEvent } from 'electron';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { randomUUID } from 'node:crypto';
import { BackendProcess } from './backendProcess';
import { computePanelBounds, getWindowsDisplayCapabilities } from './platform';
import {
  automationHttpResultSchema,
  automationKeyboardEligibilityResultSchema,
  moduleSettingsUpdateResultSchema,
  moduleSettingsGetResultSchema,
  automationResultSchema,
  automationNotificationSchema,
  globalVariableSnapshotSchema,
  backendUiStateSchema,
  PROTOCOL_VERSION,
  validateRpcRequest,
  type BackendUiState,
  type RpcMethod,
} from '../contracts/rpc';
import { AutomationServiceError, createAutomationHttpIpcFailure } from '../contracts/automationHttpIpc.ts';
import { runActivitySnapshotSchema } from '../contracts/activity';
import { isScriptRunnerInterpreter, scriptFileFilter } from '../contracts/scriptRunner';
import { authorizeAutomationServiceCall, type AutomationWindowContext } from './automationServiceAuthorization';
import { injectHostWindowContext } from './hostWindowContext';
import { projectStateForWindow, WindowContextRegistry } from './windowContextRegistry';

const moduleDirectory = __dirname;
const workspaceRoot = path.resolve(moduleDirectory, '../..');
const buildId = process.env.AUTOMATOR_BUILD_ID ?? (app.isPackaged ? app.getVersion() : 'dev');
const testMode = process.env.AUTOMATOR_TEST_MODE === '1';
const rendererUrl = process.env.AUTOMATOR_RENDERER_URL;
  const testData = path.resolve(process.env.AUTOMATOR_TEST_DATA_DIRECTORY
    ?? path.resolve(workspaceRoot, 'artifacts/test-data/electron-host'));

app.setName('Automator');
if (testMode) {
  fs.mkdirSync(testData, { recursive: true });
  app.setPath('userData', testData);
}
const singleInstance = app.requestSingleInstanceLock();
if (!singleInstance) app.quit();

const localData = testMode
  ? app.getPath('userData')
  : path.join(process.env.LOCALAPPDATA ?? app.getPath('userData'), 'Automator');
const logDirectory = path.join(localData, 'Logs');
const logPath = path.join(logDirectory, `automator-${new Date().toISOString().slice(0, 10)}.jsonl`);
const panelSize = { width: 520, height: 550 };
const backendMethods: Record<string, RpcMethod> = {
  'set-query': 'launcher/setQuery',
  'open-catalog': 'launcher/openCatalog',
  'select-catalog-binding': 'catalog/select',
  'select-binding': 'binding/select',
  'save-alias': 'binding/saveAlias',
  'remove-binding': 'binding/remove',
  'activate-binding': 'launcher/activateBinding',
  'set-mode': 'launcher/setMode',
  'update-settings': 'settings/update',
  'start-hotkey-recording': 'hotkey/startRecording',
  'cancel-hotkey-recording': 'hotkey/cancelRecording',
  'open-log-folder': 'settings/openLogFolder',
};

let sessionId: string = randomUUID();
let mainWindow: BrowserWindow | undefined;
let workspaceWindow: BrowserWindow | undefined;
const windowContexts = new WindowContextRegistry<object>();
let tray: Tray | undefined;
let backend: BackendProcess | undefined;
let currentState: BackendUiState | undefined;
let backendStatus: 'ready' | 'starting' | 'failed' = 'starting';
let backendFailure = '';
let trayIconReady = false;
let rendererReady = false;
let nativeDialogActive = false;
let primaryFocusPending = false;
let focusSequenceRunning = false;
let focusRequestPending = false;
let pendingFocusNonce: string | undefined;
let pendingFocusMode: BackendUiState['mode'] | undefined;
let initialPanelPlacementApplied = false;
let backendGeneration = 0;
let panelVisibilityEpoch = 0;
let retryPromise: Promise<{ state: BackendUiState | null; host: ReturnType<typeof getHostInfo> }> | undefined;
let quitRequested = false;
let quitFinalized = false;
let readyLogged = false;

function writeLog(event: string, message: string, properties: Record<string, unknown> = {}): void {
  const entry = {
    Timestamp: new Date().toISOString(),
    Level: event.endsWith('Failed') || event.endsWith('Error') ? 'Error' : 'Information',
    Event: event,
    Message: message,
    ProcessId: process.pid,
    Source: 'ElectronHost',
    SessionId: sessionId,
    BuildId: buildId,
    ...properties,
  };
  try {
    fs.mkdirSync(logDirectory, { recursive: true });
    fs.appendFileSync(logPath, `${JSON.stringify(entry)}\n`, 'utf8');
  } catch (error) {
    console.error(`[${event}]`, message, error);
  }
}

function getDisplayCapabilities() {
  return getWindowsDisplayCapabilities(
    process.platform,
    os.release(),
    testMode ? process.env.AUTOMATOR_TEST_WINDOWS_BUILD : undefined,
  );
}

function getWindowHandleHex(window: BrowserWindow): string {
  const handle = window.getNativeWindowHandle();
  const value = handle.length >= 8 ? handle.readBigUInt64LE(0) : BigInt(handle.readUInt32LE(0));
  return `0x${value.toString(16).toUpperCase()}`;
}

function placeNearPointer(window: BrowserWindow): void {
  if (initialPanelPlacementApplied) return;
  const pointer = screen.getCursorScreenPoint();
  const display = screen.getDisplayNearestPoint(pointer);
  window.setBounds(computePanelBounds(pointer, display.workArea, panelSize), false);
  initialPanelPlacementApplied = true;
}

function getHostInfo() {
  const display = getDisplayCapabilities();
  return {
    protocolVersion: PROTOCOL_VERSION,
    buildId,
    processId: process.pid,
    backendProcessId: testMode ? backend?.childProcessId ?? null : null,
    testMode,
    backendState: backendStatus,
    acrylicSupported: display.acrylicSupported,
    nativeCornersSupported: display.nativeCornersSupported,
    transparentWindow: display.transparentWindow,
    themeSource: nativeTheme.themeSource,
    trayIconReady,
  };
}

function liveWindows(): BrowserWindow[] {
  return [mainWindow, workspaceWindow].filter((window): window is BrowserWindow => Boolean(window && !window.isDestroyed()));
}

function sendToAllWindows(channel: string, value: unknown): void {
  for (const window of liveWindows()) window.webContents.send(channel, value);
}

function sendStateToWindows(state: BackendUiState): void {
  for (const window of liveWindows()) {
    const context = windowContexts.get(window.webContents);
    if (context) window.webContents.send('automator:state-changed', projectStateForWindow(state, context));
  }
}

function getWindowContext(event: IpcMainInvokeEvent): AutomationWindowContext | null {
  const owner = BrowserWindow.fromWebContents(event.sender);
  if (!owner || owner.isDestroyed() || event.senderFrame !== event.sender.mainFrame) return null;
  return windowContexts.get(event.sender);
}

function assertMainFrame(event: IpcMainInvokeEvent, allowedRoles: AutomationWindowContext['role'][] = ['launcher']): AutomationWindowContext {
  const context = getWindowContext(event);
  if (!context || !allowedRoles.includes(context.role)) throw new Error('This renderer is not allowed to invoke Automator commands.');
  return context;
}

function invokeBackend(event: IpcMainInvokeEvent, method: RpcMethod, params: Record<string, unknown> = {}): Promise<unknown> {
  assertMainFrame(event);
  if (quitRequested) throw new Error('Automator is shutting down.');
  if (!backend || backendStatus !== 'ready') throw new Error(backendFailure || 'Automator backend is unavailable.');
  const request = validateRpcRequest({ jsonrpc: '2.0', id: 1, method, params });
  return backend.request(request.method, request.params);
}

function invokeModuleBackend(event: IpcMainInvokeEvent, method: RpcMethod, params: Record<string, unknown> = {}): Promise<unknown> {
  const context = assertMainFrame(event, ['launcher', 'workspace']);
  if (quitRequested) throw new Error('Automator is shutting down.');
  if (!backend || backendStatus !== 'ready') throw new Error(backendFailure || 'Automator backend is unavailable.');
  const trustedParams = injectHostWindowContext(params, context);
  const request = validateRpcRequest({ jsonrpc: '2.0', id: 1, method, params: trustedParams });
  return backend.request(request.method, request.params);
}

function invokeHostServiceBackend(event: IpcMainInvokeEvent, method: RpcMethod, params: Record<string, unknown> = {}): Promise<unknown> {
  assertMainFrame(event, ['launcher', 'workspace']);
  if (quitRequested) throw new Error('Automator is shutting down.');
  if (!backend || backendStatus !== 'ready') throw new Error(backendFailure || 'Automator backend is unavailable.');
  const request = validateRpcRequest({ jsonrpc: '2.0', id: 1, method, params });
  return backend.request(request.method, request.params);
}

function invokeAutomationService(
  event: IpcMainInvokeEvent,
  method: Extract<RpcMethod,
    | 'automation/keyboardEligibility'
    | 'automation/keyboardSubscribe'
    | 'automation/keyboardUnsubscribe'
    | 'automation/httpRequest'
    | 'automation/httpCancel'>,
  params: Record<string, unknown>,
): Promise<unknown> | unknown {
  const context = assertMainFrame(event, ['launcher', 'workspace']);
  const request = validateRpcRequest({ jsonrpc: '2.0', id: 1, method,
    params: injectHostWindowContext(params, context) });
  const moduleId = request.params.moduleId as string;
  const cleanup = method === 'automation/keyboardUnsubscribe' || method === 'automation/httpCancel';
  const capabilityId = method.startsWith('automation/keyboard') ? 'keyboard.input' : 'http.request';
  const disposition = authorizeAutomationServiceCall(true, currentState, moduleId, capabilityId, cleanup, context);
  if (disposition === 'noop') return method === 'automation/keyboardUnsubscribe' ? { unsubscribed: true } : { canceled: false };
  if (quitRequested) {
    if (cleanup) return method === 'automation/keyboardUnsubscribe' ? { unsubscribed: true } : { canceled: false };
    throw new Error('Automator is shutting down.');
  }
  if (!backend || backendStatus !== 'ready') {
    if (cleanup) return method === 'automation/keyboardUnsubscribe' ? { unsubscribed: true } : { canceled: false };
    throw new Error(backendFailure || 'Automator backend is unavailable.');
  }
  return backend.request(request.method, request.params);
}

function applyBackendState(value: unknown, generation = backendGeneration, presentOnOpen = true): BackendUiState | undefined {
  if (quitRequested || generation !== backendGeneration) return undefined;
  const parsed = backendUiStateSchema.safeParse(value);
  if (!parsed.success) {
    writeLog('Backend.StateRejected', 'A backend state notification failed TypeScript contract validation.', { error: parsed.error.message });
    return undefined;
  }
  const previous = currentState;
  if (previous && parsed.data.buildId === previous.buildId && parsed.data.revision <= previous.revision) return previous;
  currentState = parsed.data;
  if (mainWindow && !mainWindow.isDestroyed()) {
    try { windowContexts.setSelectedTab(mainWindow.webContents, parsed.data.selectedTab); } catch { /* Window is not registered during early startup. */ }
  }
  nativeTheme.themeSource = parsed.data.theme === 'Dark' ? 'dark' : 'light';
  sendStateToWindows(parsed.data);
  if (parsed.data.visible && !previous?.visible && presentOnOpen) void presentPanel();
  else if (parsed.data.visible && previous?.visible
      && (parsed.data.mode !== previous.mode || parsed.data.selectedTab !== previous.selectedTab)) void requestPrimaryControlFocus();
  else if (previous?.visible && !parsed.data.visible) {
    panelVisibilityEpoch++;
    pendingFocusNonce = undefined;
    pendingFocusMode = undefined;
    if (mainWindow && !mainWindow.isDestroyed() && mainWindow.isVisible()) mainWindow.hide();
    if (!parsed.data.busy && parsed.data.previousForegroundHwnd && backend && backendStatus === 'ready') {
      const windowHandleHex = parsed.data.previousForegroundHwnd;
      void backend.request('launcher/restoreForeground', { windowHandleHex }).then((result) => {
        writeLog('Panel.PreviousForegroundRestoreResult', 'Requested restoration of the window that was active before the launcher opened.', {
          targetWindowHandle: windowHandleHex,
          result,
        });
      }).catch((error) => writeLog('Panel.PreviousForegroundRestoreRequestFailed', 'Could not request restoration of the prior foreground window.', {
        targetWindowHandle: windowHandleHex,
        error: String(error),
      }));
    }
  }
  return parsed.data;
}

async function startBackend(): Promise<void> {
  if (backend) await stopBackend();
  backendGeneration++;
  const generation = backendGeneration;
  currentState = undefined;
  sessionId = randomUUID();
  backendStatus = 'starting';
  backendFailure = '';
  sendToAllWindows('automator:backend-status', { status: backendStatus, message: '' });
  const backendFolder = app.isPackaged
    ? path.join(process.resourcesPath, 'backend')
    : path.join(workspaceRoot, 'src/Automator.Backend/bin/Debug/net10.0-windows10.0.17763.0');
  const backendExe = path.join(backendFolder, 'Automator.Backend.exe');
  if (!fs.existsSync(backendExe)) throw new Error(`Backend executable is missing: ${backendExe}`);

  const portablePath = process.env.PORTABLE_EXECUTABLE_FILE && path.isAbsolute(process.env.PORTABLE_EXECUTABLE_FILE)
    && fs.existsSync(process.env.PORTABLE_EXECUTABLE_FILE)
    ? process.env.PORTABLE_EXECUTABLE_FILE
    : null;
  if (app.isPackaged && !portablePath && isPathWithin(process.execPath, os.tmpdir())) {
    throw new Error('Portable host did not provide PORTABLE_EXECUTABLE_FILE; startup registration is disabled until the stable executable path is known.');
  }
  const testBackendDirectory = process.env.AUTOMATOR_TEST_BACKEND_DATA_DIRECTORY;
  const dataDirectory = testMode
    ? path.resolve(testBackendDirectory ?? path.resolve(testData, 'backend', `${buildId}-${process.pid}`))
    : null;
  if (dataDirectory && !isPathWithin(dataDirectory, testData))
    throw new Error('The isolated backend data directory must remain inside the isolated test data root.');
  if (dataDirectory) fs.mkdirSync(dataDirectory, { recursive: true });

  const backendEnvironment = { ...process.env };
  const browserRuntimeFolder = app.isPackaged
    ? path.join(process.resourcesPath, 'browser-runtime')
    : path.join(workspaceRoot, 'node_modules');
  backendEnvironment.AUTOMATOR_PLAYWRIGHT_PROJECT_ROOT = path.join(localData, 'Playwright');
  backendEnvironment.AUTOMATOR_PLAYWRIGHT_MODULE_ROOT = app.isPackaged
    ? path.join(browserRuntimeFolder, 'packages', 'playwright')
    : path.join(browserRuntimeFolder, 'playwright');
  if (app.isPackaged) {
    backendEnvironment.AUTOMATOR_BROWSER_NODE_PATH = path.join(browserRuntimeFolder, 'node.exe');
    backendEnvironment.AUTOMATOR_BROWSER_WORKER_PATH = path.join(browserRuntimeFolder, 'browser-worker.cjs');
  }
  const child = new BackendProcess(backendExe, [], backendFolder, backendEnvironment, writeLog);
  backend = child;
  child.on('notification', (notification: { method: string; params: unknown }) => {
    if (backend !== child || generation !== backendGeneration) return;
    if (notification.method === 'stateChanged') applyBackendState(notification.params, generation);
    else if (notification.method === 'automation/notification') {
      const parsed = automationNotificationSchema.safeParse({ jsonrpc: '2.0', ...notification });
      if (!parsed.success) {
        writeLog('Backend.NotificationRejected', 'The backend sent an invalid automation notification.');
        return;
      }
      try {
        if (Notification.isSupported()) new Notification(parsed.data.params).show();
      } catch {
        writeLog('Backend.NotificationFailed', 'Windows could not display an automation notification.');
      }
    }
    else sendToAllWindows('automator:backend-notification', notification);
  });
  child.on('failure', (error: Error) => { if (backend === child && generation === backendGeneration) failBackend(error.message); });
  child.on('exit', ({ expected, code }: { expected: boolean; code: number | null }) => {
    if (backend !== child || generation !== backendGeneration || quitRequested) return;
    backendStatus = 'failed';
    backendFailure = `The backend exited${code === null ? '' : ` with code ${code}`}.`;
    if (!expected) writeLog('Backend.UnexpectedExit', backendFailure);
    sendToAllWindows('automator:backend-failure', backendFailure);
    sendToAllWindows('automator:backend-status', { status: backendStatus, message: backendFailure });
    currentState = undefined;
  });

  try {
    const result = await child.start({
      buildId,
      hostProcessId: process.pid,
      hostExecutablePath: process.execPath,
      portableExecutablePath: portablePath,
      testMode,
      dataDirectory,
    }) as { state: BackendUiState; sessionId: string };
    sessionId = result.sessionId ?? sessionId;
    const state = applyBackendState(result.state, generation);
    if (!state) throw new Error('Backend initial state failed contract validation.');
    backendStatus = 'ready';
    sendToAllWindows('automator:backend-status', { status: backendStatus, message: '' });
    writeLog('Backend.Ready', 'The backend initialized protocol, settings, foreground monitor and keyboard hook.', {
      backendProcessId: child.childProcessId,
      hookInstalled: state.hookInstalled,
      stateRevision: state.revision,
      theme: state.theme,
      nativeThemeSource: nativeTheme.themeSource,
      testMode,
    });
  } catch (error) {
    backendStatus = 'failed';
    backendFailure = error instanceof Error ? error.message : String(error);
    try { await child.stop(); }
    catch (stopError) { writeLog('Backend.StopFailed', 'Could not stop a failed backend initialization.', { error: String(stopError) }); }
    if (backend === child && child.childProcessId === null) backend = undefined;
    throw error;
  }
}

function isPathWithin(candidate: string, root: string): boolean {
  const relative = path.relative(path.resolve(root), path.resolve(candidate));
  return relative === '' || (!relative.startsWith('..') && !path.isAbsolute(relative));
}

function failBackend(message: string): void {
  if (quitRequested) return;
  backendStatus = 'failed';
  backendFailure = message;
  currentState = undefined;
  writeLog('Backend.Failed', 'The renderer backend failed.', { error: message });
  sendToAllWindows('automator:backend-failure', message);
  sendToAllWindows('automator:backend-status', { status: backendStatus, message });
}

function showFailureWindow(): void {
  if (quitRequested || !mainWindow || mainWindow.isDestroyed()) return;
  placeNearPointer(mainWindow);
  mainWindow.show();
  mainWindow.focus();
  mainWindow.webContents.focus();
  writeLog('Panel.RecoveryShown', 'Showing the backend recovery panel.', { error: backendFailure });
}

async function stopBackend(): Promise<void> {
  const oldBackend = backend;
  if (!oldBackend) return;
  await oldBackend.stop();
  if (backend === oldBackend) backend = undefined;
}

function createTray(): boolean {
  const iconPath = path.resolve(moduleDirectory, '../../assets/automator.png');
  const icon = nativeImage.createFromPath(iconPath);
  if (icon.isEmpty()) {
    writeLog('Tray.IconUnavailable', 'The tray PNG could not be decoded.', { iconPath });
    return false;
  }
  try {
    tray = new Tray(icon);
  } catch (error) {
    writeLog('Tray.CreateFailed', 'Windows could not create the tray icon.', {
      iconPath,
      error: error instanceof Error ? error.message : String(error),
    });
    return false;
  }
  trayIconReady = true;
  tray.setToolTip('Automator');
  tray.setContextMenu(Menu.buildFromTemplate([
    { label: 'Open Automator', click: () => void showLauncher() },
    { label: 'Open Workspace', click: () => { showWorkspace(); } },
    { type: 'separator' },
    { label: 'Quit', click: () => app.quit() },
  ]));
  tray.on('click', () => void showLauncher());
  return true;
}

function registerIpc(): void {
  ipcMain.handle('automator:get-initial-state', (event) => {
    const context = assertMainFrame(event, ['launcher', 'workspace']);
    const initial = getInitialState();
    return { ...initial, state: initial.state ? projectStateForWindow(initial.state, context) : null };
  });
  ipcMain.handle('automator:get-host-info', (event) => {
    assertMainFrame(event, ['launcher', 'workspace']);
    return getHostInfo();
  });
  ipcMain.handle('automator:get-window-context', (event) => assertMainFrame(event, ['launcher', 'workspace']));
  ipcMain.handle('automator:open-workspace', (event) => {
    assertMainFrame(event, ['launcher', 'workspace']);
    return showWorkspace();
  });
  ipcMain.handle('automator:get-window-diagnostics', async (event) => {
    assertMainFrame(event);
    if (!testMode) throw new Error('Window diagnostics are available only in isolated test mode.');
    if (!mainWindow || mainWindow.isDestroyed()) return null;
    const windowHandleHex = getWindowHandleHex(mainWindow);
    const foreground = backend && backendStatus === 'ready'
      ? await backend.request('launcher/verifyForeground', { windowHandleHex }) as { foregroundHwnd?: string }
      : null;
    return {
      windowHandleHex,
      browserWindowFocused: mainWindow.isFocused(),
      webContentsFocused: mainWindow.webContents.isFocused(),
      visible: mainWindow.isVisible(),
      foregroundHwndHex: foreground?.foregroundHwnd ?? null,
      buildId,
    };
  });
  ipcMain.handle('automator:renderer-ready', (event) => {
    const context = assertMainFrame(event, ['launcher', 'workspace']);
    if (context.role === 'launcher') {
      rendererReady = true;
      if (currentState) sendStateToWindows(currentState);
      maybeLogReady();
    } else if (currentState) {
      const projected = projectStateForWindow(currentState, context);
      event.sender.send('automator:state-changed', projected);
    }
    return true;
  });
  ipcMain.handle('automator:confirm-focus', async (event, request: { nonce: string; mode: BackendUiState['mode']; focused: boolean }) => {
    assertMainFrame(event);
    if (!request || request.nonce !== pendingFocusNonce || request.mode !== pendingFocusMode
        || !currentState?.visible || nativeDialogActive || currentState.mode !== request.mode) return false;
    const generation = backendGeneration;
    const visibilityEpoch = panelVisibilityEpoch;
    const mode = currentState.mode;
    const foregroundCheck = await verifyNativeForeground();
    if (generation !== backendGeneration || visibilityEpoch !== panelVisibilityEpoch
        || !currentState?.visible || currentState.mode !== mode || nativeDialogActive) return false;
    const focused = request.focused === true && foregroundCheck.matches;
    await updateWindowContext(focused);
    writeLog(focused ? 'Panel.DomFocusConfirmed' : 'Panel.DomFocusRejected', 'Focus handshake completed.', {
      foreground: foregroundCheck.matches,
      foregroundHwnd: foregroundCheck.foregroundHwnd,
      focused: request.focused,
      nonce: request.nonce,
      mode: currentState.mode,
    });
    pendingFocusNonce = undefined;
    return focused;
  });

  for (const [command, method] of Object.entries(backendMethods)) {
    ipcMain.handle(`automator:${command}`, async (event, params: Record<string, unknown> = {}) => {
      const result = await invokeBackend(event, method, params ?? {});
      const parsedState = backendUiStateSchema.safeParse(result);
      if (parsedState.success) applyBackendState(parsedState.data);
      return result;
    });
  }

  ipcMain.handle('automator:select-tab', async (event, params: { tab?: unknown }) => {
    const context = assertMainFrame(event, ['launcher', 'workspace']);
    const tab = Number(params?.tab);
    if (!Number.isInteger(tab) || tab < 1 || tab > 9) throw new Error('The requested tab slot is invalid.');
    if (context.role === 'launcher') {
      const result = await invokeBackend(event, 'launcher/selectTab', { tab });
      const parsed = backendUiStateSchema.safeParse(result);
      if (parsed.success) applyBackendState(parsed.data);
      return result;
    }
    windowContexts.setSelectedTab(event.sender, tab);
    return currentState ? projectStateForWindow(currentState, { ...context, selectedTab: tab }) : null;
  });
  ipcMain.handle('automator:close', async (event) => {
    const context = assertMainFrame(event, ['launcher', 'workspace']);
    if (context.role === 'launcher') {
      await closeLauncher();
      return currentState;
    }
    if (workspaceWindow && !workspaceWindow.isDestroyed()) workspaceWindow.hide();
    return currentState ? projectStateForWindow(currentState, context) : null;
  });

  ipcMain.handle('automator:automation-keyboard-eligibility', async (event, params: Record<string, unknown>) => {
    const result = await invokeAutomationService(event, 'automation/keyboardEligibility', params ?? {});
    const parsed = automationKeyboardEligibilityResultSchema.safeParse(result);
    if (!parsed.success) throw new Error('Backend returned an invalid keyboard eligibility result.');
    return parsed.data;
  });
  ipcMain.handle('automator:automation-keyboard-subscribe', async (event, params: Record<string, unknown>) => {
    const result = await invokeAutomationService(event, 'automation/keyboardSubscribe', params ?? {}) as { subscribed?: unknown };
    if (!result || typeof result.subscribed !== 'boolean') throw new Error('Backend returned an invalid keyboard subscription result.');
    return { subscribed: result.subscribed };
  });
  ipcMain.handle('automator:automation-keyboard-unsubscribe', async (event, params: Record<string, unknown>) => {
    const result = await invokeAutomationService(event, 'automation/keyboardUnsubscribe', params ?? {}) as { unsubscribed?: unknown };
    if (!result || typeof result.unsubscribed !== 'boolean') throw new Error('Backend returned an invalid keyboard unsubscribe result.');
    return { unsubscribed: result.unsubscribed };
  });
  ipcMain.handle('automator:automation-http-request', async (event, params: Record<string, unknown>) => {
    try {
      const result = await invokeAutomationService(event, 'automation/httpRequest', params ?? {});
      const parsed = automationHttpResultSchema.safeParse(result);
      if (!parsed.success) {
        return createAutomationHttpIpcFailure(new AutomationServiceError(
          'invalidResponse',
          'Backend returned an invalid HTTP service result.',
        ));
      }
      return { ok: true as const, result: parsed.data };
    } catch (error) {
      return createAutomationHttpIpcFailure(error);
    }
  });
  ipcMain.handle('automator:automation-http-cancel', async (event, params: Record<string, unknown>) => {
    const result = await invokeAutomationService(event, 'automation/httpCancel', params ?? {}) as { canceled?: unknown };
    if (!result || typeof result.canceled !== 'boolean') throw new Error('Backend returned an invalid HTTP cancellation result.');
    return { canceled: result.canceled };
  });
  ipcMain.handle('automator:module-action', async (event, params: Record<string, unknown>) => {
    const result = await invokeModuleBackend(event, 'automation/moduleAction', params ?? {});
    const parsed = automationResultSchema.safeParse(result);
    if (!parsed.success) throw new Error('Backend returned an invalid module action result.');
    return parsed.data;
  });
  ipcMain.handle('automator:module-action-cancel', async (event, params: Record<string, unknown>) => {
    const result = await invokeModuleBackend(event, 'automation/moduleActionCancel', params ?? {}) as { canceled?: unknown };
    if (!result || typeof result.canceled !== 'boolean') throw new Error('Backend returned an invalid module action cancellation result.');
    return { canceled: result.canceled };
  });
  ipcMain.handle('automator:pick-script-file', async (event, interpreter: unknown) => {
    const context = assertMainFrame(event, ['launcher', 'workspace']);
    if (context.selectedTab !== 2 || (context.role === 'launcher' && (!currentState?.visible || currentState.mode !== 'launcher')))
      throw new Error('File selection is only available in the active Script Runner tab.');
    if (!isScriptRunnerInterpreter(interpreter)) throw new Error('The script type is invalid.');
    const selected = await showDialogForCaller(event, {
      title: 'Select a script',
      properties: ['openFile'],
      filters: [scriptFileFilter(interpreter), { name: 'All files', extensions: ['*'] }],
    });
    return selected.canceled || selected.filePaths.length === 0 ? null : selected.filePaths[0];
  });
  ipcMain.handle('automator:pick-working-directory', async (event) => {
    const context = assertMainFrame(event, ['launcher', 'workspace']);
    if (context.selectedTab !== 2 || (context.role === 'launcher' && (!currentState?.visible || currentState.mode !== 'launcher')))
      throw new Error('Folder selection is only available in the active Script Runner tab.');
    const selected = await showDialogForCaller(event, {
      title: 'Select a working directory',
      properties: ['openDirectory', 'createDirectory'],
    });
    return selected.canceled || selected.filePaths.length === 0 ? null : selected.filePaths[0];
  });
  ipcMain.handle('automator:pick-browser-project-directory', async (event) => {
    const context = assertMainFrame(event, ['launcher', 'workspace']);
    if (context.selectedTab !== 4 || (context.role === 'launcher' && (!currentState?.visible || currentState.mode !== 'launcher')))
      throw new Error('Folder selection is only available in the active Browser Automation tab.');
    const selected = await showDialogForCaller(event, {
      title: 'Select a Playwright project',
      properties: ['openDirectory'],
    });
    return selected.canceled || selected.filePaths.length === 0 ? null : selected.filePaths[0];
  });
  ipcMain.handle('automator:module-settings-update', async (event, params: Record<string, unknown>) => {
    const result = await invokeModuleBackend(event, 'module/settingsUpdate', params ?? {});
    const parsed = moduleSettingsUpdateResultSchema.safeParse(result);
    if (!parsed.success) throw new Error('Backend returned an invalid module settings update result.');
    return parsed.data;
  });
  ipcMain.handle('automator:module-settings-get', async (event, params: Record<string, unknown>) => {
    const result = await invokeModuleBackend(event, 'module/settingsGet', params ?? {});
    const parsed = moduleSettingsGetResultSchema.safeParse(result);
    if (!parsed.success) throw new Error('Backend returned an invalid module settings result.');
    return parsed.data;
  });
  ipcMain.handle('automator:variables-get', async (event) => {
    const result = await invokeHostServiceBackend(event, 'variables/get', { version: 1 });
    const parsed = globalVariableSnapshotSchema.safeParse(result);
    if (!parsed.success) throw new Error('Backend returned invalid global variables.');
    return parsed.data;
  });
  ipcMain.handle('automator:variables-set', async (event, values: unknown) => {
    const request = { version: 1, values };
    const validated = validateRpcRequest({ jsonrpc: '2.0', id: 1, method: 'variables/set', params: request });
    const result = await invokeHostServiceBackend(event, 'variables/set', validated.params);
    const parsed = globalVariableSnapshotSchema.safeParse(result);
    if (!parsed.success) throw new Error('Backend returned invalid saved global variables.');
    return parsed.data;
  });
  ipcMain.handle('automator:activity-list', async (event) => {
    const result = await invokeHostServiceBackend(event, 'activity/list', {});
    const parsed = runActivitySnapshotSchema.safeParse(result);
    if (!parsed.success) throw new Error('Backend returned invalid run activity.');
    return parsed.data;
  });

  ipcMain.handle('automator:add-custom-app', (event) => withOpenFileDialog(event, [
    { name: 'Applications and shortcuts', extensions: ['exe', 'lnk'] },
  ], 'catalog/addCustom', (filePath) => ({ path: filePath })));
  ipcMain.handle('automator:import-settings', (event) => withOpenFileDialog(event, [
    { name: 'Automator settings', extensions: ['json'] },
  ], 'settings/import', (filePath) => ({ path: filePath })));
  ipcMain.handle('automator:export-settings', async (event) => {
    assertMainFrame(event);
    const filePath = await withNativeDialog(async () => {
      const result = await dialog.showSaveDialog(mainWindow!, {
        title: 'Export Automator settings',
        defaultPath: path.join(app.getPath('documents'), 'automator-settings.json'),
        filters: [{ name: 'Automator settings', extensions: ['json'] }],
      });
      return result.canceled ? null : result.filePath;
    });
    if (!filePath) return false;
    await invokeBackend(event, 'settings/export', { path: filePath });
    return true;
  });
  ipcMain.handle('automator:save-work-time-csv', async (event, csvText: unknown) => {
    const context = assertMainFrame(event, ['workspace']);
    if (context.selectedTab !== 7) throw new Error('Work Time CSV export is only available in the Workspace Work Time tab.');
    if (typeof csvText !== 'string' || Buffer.byteLength(csvText, 'utf8') > 8 * 1024 * 1024)
      throw new Error('The Work Time CSV is invalid or exceeds the export size limit.');

    const owner = BrowserWindow.fromWebContents(event.sender);
    if (!owner || owner.isDestroyed()) throw new Error('The requesting Workspace window is no longer available.');
    const selected = await dialog.showSaveDialog(owner, {
      title: 'Export Work Time report',
      defaultPath: path.join(app.getPath('documents'), 'automator-work-time.csv'),
      filters: [{ name: 'CSV files', extensions: ['csv'] }],
    });
    if (selected.canceled || !selected.filePath) return false;
    const filePath = path.extname(selected.filePath).toLowerCase() === '.csv' ? selected.filePath : `${selected.filePath}.csv`;
    await fs.promises.writeFile(filePath, csvText, { encoding: 'utf8' });
    return true;
  });
  ipcMain.handle('automator:relink-binding', (event, bindingId: string) => withOpenFileDialog(event, [
    { name: 'Applications and shortcuts', extensions: ['exe', 'lnk'] },
  ], 'settings/relink', (filePath) => ({ bindingId, path: filePath })));
  ipcMain.handle('automator:retry-backend', async (event) => {
    assertMainFrame(event);
    if (quitRequested) throw new Error('Automator is shutting down.');
    retryPromise ??= (async () => {
      await startBackend();
      if (quitRequested) {
        await stopBackend();
        throw new Error('Automator is shutting down.');
      }
      if (!backend || !mainWindow || mainWindow.isDestroyed()) throw new Error('Automator host is unavailable after backend restart.');
      mainWindow.hide();
      await new Promise((resolve) => setTimeout(resolve, 50));
      if (quitRequested) {
        await stopBackend();
        throw new Error('Automator is shutting down.');
      }
      const reopened = await backend.request('launcher/open', {});
      if (quitRequested) throw new Error('Automator is shutting down.');
      const openedState = applyBackendState(reopened, backendGeneration, false);
      if (!openedState?.visible) throw new Error('The backend restarted but could not reopen the launcher.');
      await presentPanel();
      const value = getInitialState();
      sendToAllWindows('automator:backend-status', { status: backendStatus, message: '' });
      maybeLogReady();
      return value;
    })().finally(() => { retryPromise = undefined; });
    return retryPromise;
  });
}

async function withOpenFileDialog(
  event: IpcMainInvokeEvent,
  filters: Electron.FileFilter[],
  method: RpcMethod,
  parameters: (filePath: string) => Record<string, unknown>,
): Promise<unknown> {
  assertMainFrame(event);
  const selected = await withNativeDialog(async () => dialog.showOpenDialog(mainWindow!, {
    title: method === 'catalog/addCustom' ? 'Add an application' : method === 'settings/import' ? 'Import settings' : 'Relink application',
    properties: ['openFile'],
    filters,
  }));
  if (selected.canceled || selected.filePaths.length === 0) return null;
  const result = await invokeBackend(event, method, parameters(selected.filePaths[0]));
  const parsedState = backendUiStateSchema.safeParse(result);
  if (parsedState.success) applyBackendState(parsedState.data);
  return result;
}

async function showDialogForCaller(
  event: IpcMainInvokeEvent,
  options: Electron.OpenDialogOptions,
): Promise<Electron.OpenDialogReturnValue> {
  const context = assertMainFrame(event, ['launcher', 'workspace']);
  const owner = BrowserWindow.fromWebContents(event.sender);
  if (!owner || owner.isDestroyed()) throw new Error('The requesting Automator window is no longer available.');
  const show = () => dialog.showOpenDialog(owner, options);
  return context.role === 'launcher' ? withNativeDialog(show) : show();
}

async function withNativeDialog<T>(action: () => Promise<T>): Promise<T> {
  nativeDialogActive = true;
  await updateWindowContext(false);
  try {
    if (quitRequested) throw new Error('Automator is shutting down.');
    return await action();
  }
  finally {
    nativeDialogActive = false;
    if (!quitRequested && currentState?.visible) {
      await updateWindowContext(false);
      if (mainWindow && !mainWindow.isDestroyed()) {
        mainWindow.show();
        mainWindow.focus();
        void verifyAndRequestFocus();
      }
    }
  }
}

function getInitialState(): { state: BackendUiState | null; host: ReturnType<typeof getHostInfo> } {
  return { state: currentState ?? null, host: getHostInfo() };
}

async function showLauncher(): Promise<void> {
  if (quitRequested) return;
  if (!mainWindow || mainWindow.isDestroyed()) {
    writeLog('Panel.ShowSkipped', 'A show request arrived before the launcher window was available.');
    return;
  }
  if (!backend || backendStatus !== 'ready') {
    failBackend(backendFailure || 'The backend is not ready.');
    showFailureWindow();
    return;
  }
  try {
    const wasVisible = currentState?.visible === true;
    // Backend captures the pointer app and previous foreground before mainWindow is shown.
    const result = await backend.request('launcher/open', {}) as BackendUiState;
    if (quitRequested) return;
    const state = applyBackendState(result);
    if (state?.visible && wasVisible && mainWindow && !mainWindow.isDestroyed()) {
      mainWindow.show();
      mainWindow.focus();
      mainWindow.webContents.focus();
      void requestPrimaryControlFocus();
    }
  } catch (error) {
    failBackend(error instanceof Error ? error.message : String(error));
    showFailureWindow();
  }
}

async function presentPanel(): Promise<void> {
  if (quitRequested || !mainWindow || mainWindow.isDestroyed() || !currentState?.visible || nativeDialogActive) return;
  const owned = mainWindow;
  const generation = backendGeneration;
  const visibilityEpoch = panelVisibilityEpoch;
  placeNearPointer(owned);
  await updateWindowContext(false);
  if (quitRequested || mainWindow !== owned || owned.isDestroyed() || generation !== backendGeneration
      || visibilityEpoch !== panelVisibilityEpoch || !currentState?.visible || nativeDialogActive) return;
  writeLog('Panel.ShowRequested', 'Showing the launcher panel.', {
    visibleBefore: owned.isVisible(),
    loadingMainFrame: owned.webContents.isLoadingMainFrame(),
    previousForegroundHwnd: currentState.previousForegroundHwnd,
  });
  owned.show();
  owned.focus();
  owned.webContents.focus();
  if (owned.webContents.isLoadingMainFrame()) primaryFocusPending = true;
  else void verifyAndRequestFocus();
}

async function verifyAndRequestFocus(): Promise<void> {
  await requestPrimaryControlFocus();
}

async function requestPrimaryControlFocus(): Promise<void> {
  if (quitRequested) return;
  if (focusSequenceRunning) {
    focusRequestPending = true;
    return;
  }
  if (!mainWindow || mainWindow.isDestroyed() || !currentState?.visible || nativeDialogActive || !rendererReady) {
    primaryFocusPending = true;
    return;
  }
  focusSequenceRunning = true;
  const owned = mainWindow;
  const generation = backendGeneration;
  const visibilityEpoch = panelVisibilityEpoch;
  const mode = currentState.mode;
  try {
    await updateWindowContext(false);
    if (quitRequested || mainWindow !== owned || owned.isDestroyed() || generation !== backendGeneration
        || visibilityEpoch !== panelVisibilityEpoch || !currentState?.visible || currentState.mode !== mode
        || nativeDialogActive) return;

    let foregroundCheck = await verifyNativeForeground(3);
    let acquisitionAttempts = 0;
    while (!quitRequested && !foregroundCheck.matches && acquisitionAttempts < 3) {
      const previousForeground = currentState.previousForegroundHwnd?.toUpperCase();
      const foreground = foregroundCheck.foregroundHwnd?.toUpperCase();
      const launcherOrPriorIsStillActive = owned.isFocused()
        || Boolean(previousForeground && foreground === previousForeground);
      if (!launcherOrPriorIsStillActive || !backend || backendStatus !== 'ready') break;

      acquisitionAttempts++;
      const acquisition = await backend.request('launcher/acquireForeground', {}) as {
        acquired?: boolean; foregroundHwnd?: string | null; inputQueuesAttached?: boolean;
        attachError?: number; bringToTopSucceeded?: boolean; setForegroundSucceeded?: boolean;
      };
    if (quitRequested || mainWindow !== owned || owned.isDestroyed() || generation !== backendGeneration
        || visibilityEpoch !== panelVisibilityEpoch || !currentState?.visible || currentState.mode !== mode
        || nativeDialogActive) return;
      writeLog(acquisition.acquired ? 'Panel.ForegroundAcquireConfirmed' : 'Panel.ForegroundAcquireAttempted',
        acquisition.acquired ? 'Native activation made Automator the foreground HWND.' : 'Tried the bounded native foreground handoff.', {
          expectedWindowHandle: getWindowHandleHex(owned),
          foregroundHwnd: acquisition.foregroundHwnd,
          inputQueuesAttached: acquisition.inputQueuesAttached,
          attachError: acquisition.attachError,
          bringToTopSucceeded: acquisition.bringToTopSucceeded,
          setForegroundSucceeded: acquisition.setForegroundSucceeded,
          attempt: acquisitionAttempts,
        });
      foregroundCheck = await verifyNativeForeground(3);
    }

    if (quitRequested || mainWindow !== owned || owned.isDestroyed() || generation !== backendGeneration
        || visibilityEpoch !== panelVisibilityEpoch || !currentState?.visible || currentState.mode !== mode
        || nativeDialogActive) return;
    if (!foregroundCheck.matches) {
      await updateWindowContext(false);
      writeLog('Panel.NativeFocusFailed', 'Windows did not report Automator as the foreground HWND; DOM focus was withheld.', {
        expectedWindowHandle: getWindowHandleHex(owned),
        foregroundHwnd: foregroundCheck.foregroundHwnd,
        browserWindowFocused: owned.isFocused(),
        acquisitionAttempts,
      });
      return;
    }
    pendingFocusNonce = randomUUID();
    pendingFocusMode = mode;
    const request = { nonce: pendingFocusNonce, mode };
    owned.webContents.send('automator:focus-primary-control', request);
    primaryFocusPending = false;
    writeLog('Panel.NativeFocusConfirmed', 'OS foreground HWND matched before requesting DOM focus.', {
      windowHandle: getWindowHandleHex(owned),
      mode,
      nonce: request.nonce,
    });
  } catch (error) {
    writeLog('Panel.FocusHandshakeFailed', 'Could not complete the native focus handshake.', { error: String(error) });
  } finally {
    focusSequenceRunning = false;
    if (focusRequestPending) {
      focusRequestPending = false;
      void requestPrimaryControlFocus();
    }
  }
}

async function verifyNativeForeground(maxAttempts = 24): Promise<{ matches: boolean; foregroundHwnd: string | null }> {
  if (quitRequested || !mainWindow || mainWindow.isDestroyed() || !backend || backendStatus !== 'ready') return { matches: false, foregroundHwnd: null };
  const windowHandleHex = getWindowHandleHex(mainWindow);
  let foregroundHwnd: string | null = null;
  for (let attempt = 0; attempt < maxAttempts; attempt++) {
    if (quitRequested || !mainWindow || mainWindow.isDestroyed()) break;
    const response = await backend.request('launcher/verifyForeground', { windowHandleHex }) as { foreground?: boolean; foregroundHwnd?: string };
    foregroundHwnd = response.foregroundHwnd ?? foregroundHwnd;
    if (response.foreground === true) return { matches: true, foregroundHwnd };
    await new Promise((resolve) => setTimeout(resolve, 35));
  }
  return { matches: false, foregroundHwnd };
}

async function updateWindowContext(rendererFocused: boolean): Promise<void> {
  if (quitRequested || !mainWindow || mainWindow.isDestroyed() || !backend || backendStatus !== 'ready' || !currentState) return;
  try {
    await backend.request('launcher/setWindowContext', {
      windowHandleHex: getWindowHandleHex(mainWindow),
      visible: currentState.visible && mainWindow.isVisible(),
      rendererFocused,
      nativeDialogActive,
      mode: currentState.mode,
    });
  } catch (error) {
    writeLog('Panel.ContextUpdateFailed', 'Could not update the native launcher context.', { error: String(error) });
  }
}

async function closeLauncher(): Promise<void> {
  if (quitRequested) return;
  if (!backend || backendStatus !== 'ready') {
    mainWindow?.hide();
    return;
  }
  try {
    const result = await backend.request('launcher/close', {}) as BackendUiState;
    applyBackendState(result);
  } catch (error) {
    writeLog('Panel.CloseFailed', 'Could not close the launcher through the backend.', { error: String(error) });
    mainWindow?.hide();
  }
}

function maybeLogReady(): void {
  if (readyLogged || backendStatus !== 'ready' || !rendererReady) return;
  if (!mainWindow || mainWindow.isDestroyed()) return;
  const visible = mainWindow.isVisible();
  const hookReady = currentState?.hookInstalled === true;
  const mustShowPanel = (testMode && process.env.AUTOMATOR_TEST_START_HIDDEN !== '1')
    || process.env.AUTOMATOR_SHOW_ON_START === '1'
    || !trayIconReady
    || !hookReady;
  if (mustShowPanel && !visible) return;
  readyLogged = true;
  writeLog('App.Ready', 'Backend, native window, renderer and tray or visible fallback are ready.', {
    windowHandle: getWindowHandleHex(mainWindow),
    backendProcessId: backend?.childProcessId ?? null,
    backendHookInstalled: currentState?.hookInstalled ?? false,
    degraded: !hookReady,
    trayIconReady,
    visible,
    surfaceState: visible && !trayIconReady ? 'visible-fallback' : trayIconReady ? 'tray' : 'visible-fallback',
    acrylicSupported: getDisplayCapabilities().acrylicSupported,
  });
}

function showWorkspace(): boolean {
  if (quitRequested) return false;
  if (!workspaceWindow || workspaceWindow.isDestroyed()) createWorkspaceWindow();
  if (!workspaceWindow || workspaceWindow.isDestroyed()) return false;
  workspaceWindow.show();
  workspaceWindow.focus();
  return true;
}

function createWorkspaceWindow(): void {
  const rendererFile = path.resolve(moduleDirectory, '../renderer/index.html');
  const packagedRendererUrl = pathToFileURL(rendererFile).toString();
  const allowedDev = rendererUrl ? new URL(rendererUrl) : undefined;
  if (allowedDev && (allowedDev.protocol !== 'http:' || allowedDev.hostname !== '127.0.0.1' || allowedDev.port !== '5173')) {
    throw new Error('AUTOMATOR_RENDERER_URL must use the configured 127.0.0.1:5173 development origin.');
  }

  workspaceWindow = new BrowserWindow({
    width: 1380,
    height: 900,
    minWidth: 980,
    minHeight: 680,
    show: false,
    frame: true,
    transparent: false,
    backgroundColor: currentState?.theme === 'Dark' ? '#1c1c1c' : '#f6f8fb',
    icon: path.resolve(moduleDirectory, '../../assets/automator.png'),
    title: 'Automator Workspace',
    roundedCorners: true,
    resizable: true,
    maximizable: true,
    minimizable: true,
    fullscreenable: true,
    alwaysOnTop: false,
    skipTaskbar: false,
    webPreferences: {
      preload: path.resolve(moduleDirectory, '../preload/preload.cjs'),
      contextIsolation: true,
      sandbox: true,
      nodeIntegration: false,
      webSecurity: true,
      webviewTag: false,
      spellcheck: false,
    },
  });
  const owned = workspaceWindow;
  const disposeContext = windowContexts.register(owned.webContents, 'workspace');
  windowContexts.setSelectedTab(owned.webContents, currentState?.selectedTab && currentState.selectedTab > 1 ? currentState.selectedTab : 2);
  owned.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  owned.webContents.on('will-attach-webview', (event) => event.preventDefault());
  owned.webContents.on('will-navigate', (event, url) => {
    if (!isAllowedRendererUrl(url, packagedRendererUrl, allowedDev)) event.preventDefault();
  });
  owned.webContents.on('will-redirect', (event, url) => {
    if (!isAllowedRendererUrl(url, packagedRendererUrl, allowedDev)) event.preventDefault();
  });
  owned.webContents.on('render-process-gone', (_event, details) => {
    writeLog('Workspace.RendererCrashed', 'Workspace renderer exited; reloading the frontend.', { reason: details.reason, exitCode: details.exitCode });
    if (!owned.isDestroyed()) setTimeout(() => { if (!owned.isDestroyed()) void loadRenderer(owned, packagedRendererUrl, 'workspace'); }, 250);
  });
  owned.webContents.on('did-fail-load', (_event, code, description, validatedUrl, isMainFrame) => {
    if (isMainFrame) writeLog('Workspace.RendererLoadFailed', 'Workspace renderer navigation failed.', { code, description, validatedUrl });
  });
  owned.on('close', (event) => {
    if (!quitRequested) {
      event.preventDefault();
      owned.hide();
    }
  });
  owned.on('closed', () => {
    disposeContext();
    if (workspaceWindow === owned) workspaceWindow = undefined;
  });
  void loadRenderer(owned, packagedRendererUrl, 'workspace');
  writeLog('Workspace.Created', 'Created the large workspace window.', { windowHandle: getWindowHandleHex(owned) });
}

function createWindow(): void {
  const capabilities = getDisplayCapabilities();
  nativeTheme.themeSource = currentState?.theme === 'Dark' ? 'dark' : 'light';
  writeLog('Panel.NativeThemeInitialized', 'Applied the saved theme before creating the native window.', {
    savedTheme: currentState?.theme ?? null,
    themeSource: nativeTheme.themeSource,
  });
  const rendererFile = path.resolve(moduleDirectory, '../renderer/index.html');
  const packagedRendererUrl = pathToFileURL(rendererFile).toString();
  const allowedDev = rendererUrl ? new URL(rendererUrl) : undefined;
  if (allowedDev && (allowedDev.protocol !== 'http:' || allowedDev.hostname !== '127.0.0.1' || allowedDev.port !== '5173')) {
    throw new Error('AUTOMATOR_RENDERER_URL must use the configured 127.0.0.1:5173 development origin.');
  }

  mainWindow = new BrowserWindow({
    ...panelSize,
    show: false,
    frame: false,
    transparent: capabilities.transparentWindow,
    ...(capabilities.backgroundColor ? { backgroundColor: capabilities.backgroundColor } : {}),
    icon: path.resolve(moduleDirectory, '../../assets/automator.png'),
    title: 'Automator',
    roundedCorners: true,
    resizable: false,
    maximizable: false,
    minimizable: false,
    fullscreenable: false,
    alwaysOnTop: true,
    skipTaskbar: true,
    hasShadow: true,
    webPreferences: {
      preload: path.resolve(moduleDirectory, '../preload/preload.cjs'),
      contextIsolation: true,
      sandbox: true,
      nodeIntegration: false,
      webSecurity: true,
      webviewTag: false,
      spellcheck: false,
    },
    ...(capabilities.backgroundMaterial ? { backgroundMaterial: capabilities.backgroundMaterial } : {}),
  });

  const owned = mainWindow;
  const disposeContext = windowContexts.register(owned.webContents, 'launcher');
  owned.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  owned.webContents.on('will-attach-webview', (event) => event.preventDefault());
  owned.webContents.on('will-navigate', (event, url) => {
    if (!isAllowedRendererUrl(url, packagedRendererUrl, allowedDev)) event.preventDefault();
  });
  owned.webContents.on('will-redirect', (event, url) => {
    if (!isAllowedRendererUrl(url, packagedRendererUrl, allowedDev)) event.preventDefault();
  });
  owned.webContents.on('did-finish-load', () => {
    if (primaryFocusPending && currentState?.visible) void verifyAndRequestFocus();
  });
  owned.webContents.on('render-process-gone', (_event, details) => {
    writeLog('Renderer.Crashed', 'Renderer process exited; reloading the frontend.', { reason: details.reason, exitCode: details.exitCode });
    rendererReady = false;
    if (!owned.isDestroyed()) setTimeout(() => { if (!owned.isDestroyed()) void loadRenderer(owned, packagedRendererUrl); }, 250);
  });
  owned.webContents.on('did-fail-load', (_event, code, description, validatedUrl, isMainFrame) => {
    if (isMainFrame) writeLog('Renderer.LoadFailed', 'Renderer navigation failed.', { code, description, validatedUrl });
  });
  const contentSecurityPolicy = allowedDev
    ? "default-src 'self' http://127.0.0.1:5173; script-src 'self' 'unsafe-inline' http://127.0.0.1:5173; style-src 'self' 'unsafe-inline' http://127.0.0.1:5173; img-src 'self' data: blob: http://127.0.0.1:5173; font-src 'self' data: http://127.0.0.1:5173; connect-src 'self' http://127.0.0.1:5173 ws://127.0.0.1:5173; object-src 'none'; base-uri 'none'; form-action 'none'; frame-src 'none'"
    : "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' ws://127.0.0.1:5173; object-src 'none'; base-uri 'none'; form-action 'none'; frame-src 'none'";
  owned.webContents.session.webRequest.onHeadersReceived((details, callback) => {
    callback({
      responseHeaders: {
        ...details.responseHeaders,
        'Content-Security-Policy': [contentSecurityPolicy],
      },
    });
  });

  void loadRenderer(owned, packagedRendererUrl);
  owned.once('ready-to-show', () => {
    if (quitRequested || owned.isDestroyed()) return;
    const testStartupShow = testMode && process.env.AUTOMATOR_TEST_START_HIDDEN !== '1';
    const showAtStartup = testStartupShow || backendStatus === 'failed' || currentState?.hookInstalled === false
      || process.env.AUTOMATOR_SHOW_ON_START === '1' || !trayIconReady;
    writeLog('Panel.ReadyToShow', 'Electron signalled that the panel content can be displayed.', {
      testMode,
      showOnStart: process.env.AUTOMATOR_SHOW_ON_START === '1',
      trayIconReady,
    });
    if (showAtStartup) {
      if (backendStatus === 'ready') void showLauncher();
      else showFailureWindow();
    }
    if (!trayIconReady) writeLog('Tray.FallbackPanelShown', 'Tray registration failed; keeping the launcher panel visible.');
    if (showAtStartup) {
      setTimeout(() => {
        if (quitRequested || owned.isDestroyed()) return;
        if (owned.isVisible() === false) {
          if (backendStatus === 'ready') void showLauncher();
          else showFailureWindow();
        }
      }, 250);
    }
  });
  owned.on('show', () => writeLog('Panel.NativeShowEvent', 'Windows reported the panel as shown.', { visible: owned.isVisible() }));
  owned.on('show', maybeLogReady);
  owned.on('hide', () => writeLog('Panel.NativeHideEvent', 'Windows reported the panel as hidden.'));
  owned.on('blur', () => {
    const blurWindowHandle = getWindowHandleHex(owned);
    void backend?.request('launcher/verifyForeground', { windowHandleHex: blurWindowHandle })
      .then((foreground) => writeLog('Panel.NativeBlurObserved', 'Windows changed the launcher foreground activation state.', {
        windowHandle: blurWindowHandle,
        foregroundHwnd: (foreground as { foregroundHwnd?: string }).foregroundHwnd,
        browserWindowFocused: owned.isFocused(),
        visible: owned.isVisible(),
        mode: currentState?.mode,
      }))
      .catch((error) => writeLog('Panel.NativeBlurObserved', 'The launcher lost native focus and foreground diagnostics were unavailable.', {
        windowHandle: blurWindowHandle,
        error: String(error),
        browserWindowFocused: owned.isFocused(),
        visible: owned.isVisible(),
      }));
    if (quitRequested || nativeDialogActive || !currentState?.visible) return;
    if (workspaceWindow && !workspaceWindow.isDestroyed() && workspaceWindow.isFocused()) {
      void updateWindowContext(false);
      return;
    }
    setTimeout(() => {
      if (!quitRequested && mainWindow === owned && !owned.isDestroyed() && !owned.isFocused() && !nativeDialogActive && currentState?.visible
          && !(workspaceWindow && !workspaceWindow.isDestroyed() && workspaceWindow.isFocused()))
        void closeLauncher();
    }, 120);
  });
  owned.on('focus', () => {
    if (!quitRequested && currentState?.visible && rendererReady && !nativeDialogActive) void verifyAndRequestFocus();
  });
  owned.on('closed', () => {
    disposeContext();
    if (mainWindow === owned) mainWindow = undefined;
  });
}

async function loadRenderer(window: BrowserWindow, packagedRendererUrl: string, role: 'launcher' | 'workspace' = 'launcher'): Promise<void> {
  const url = new URL(rendererUrl ?? packagedRendererUrl);
  if (role === 'workspace') url.searchParams.set('surface', 'workspace');
  await window.loadURL(url.toString());
}

function isAllowedRendererUrl(url: string, packagedRendererUrl: string, dev: URL | undefined): boolean {
  if (dev) {
    try {
      const candidate = new URL(url);
      return candidate.origin === dev.origin && candidate.pathname === dev.pathname;
    } catch { return false; }
  }
  try {
    const candidate = new URL(url);
    const expected = new URL(packagedRendererUrl);
    return candidate.protocol === 'file:' && decodeURIComponent(candidate.pathname).toLowerCase() === decodeURIComponent(expected.pathname).toLowerCase();
  } catch { return false; }
}

async function initializeApp(): Promise<void> {
  if (!singleInstance || quitRequested) return;
  app.setAppUserModelId('com.automator.desktop');
  writeLog('App.Starting', 'Automator Electron host is starting.', {
    executable: process.execPath,
    portableExecutablePath: process.env.PORTABLE_EXECUTABLE_FILE ?? null,
    testMode,
  });
  registerIpc();
  try {
    await startBackend();
  } catch (error) {
    failBackend(error instanceof Error ? error.message : String(error));
    writeLog('App.InitializationFailed', 'Backend startup failed; the recovery window will remain available.', { error: backendFailure });
  }
  if (quitRequested) return;
  if (!testMode || process.env.AUTOMATOR_TEST_TRAY === '1') trayIconReady = createTray();
  createWindow();
  app.on('activate', () => void showLauncher());
}

if (singleInstance) {
  app.on('second-instance', () => void showLauncher());
  app.whenReady().then(initializeApp).catch((error: unknown) => {
    writeLog('App.InitializationFailed', 'Electron host initialization failed.', { error: String(error) });
    app.quit();
  });

  app.on('before-quit', (event) => {
    if (quitFinalized) return;
    event.preventDefault();
    if (quitRequested) return;
    quitRequested = true;
    writeLog('App.Stopping', 'Automator host is stopping its backend before quitting.');
    tray?.destroy();
    tray = undefined;
    trayIconReady = false;
    void stopBackend().catch((error) => writeLog('Backend.StopFailed', 'The backend did not stop cleanly.', { error: String(error) }))
      .finally(() => {
        quitFinalized = true;
        app.quit();
      });
  });
  app.on('window-all-closed', () => { /* The tray owns the desktop lifetime. */ });
} else {
  writeLog('Instance.DuplicateLaunch', 'A second launch was rejected by the single-instance lock.');
}

export {};
