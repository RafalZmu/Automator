import { contextBridge, ipcRenderer, webUtils } from 'electron';
import {
  automationHttpIpcResponseSchema,
  createAutomationHttpIpcFailure,
} from '../contracts/automationHttpIpc.ts';
import {
  automationKeyboardEligibilityResultSchema,
  automationModuleActionRequestSchema,
  automationModuleActionCancelRequestSchema,
  moduleSettingsUpdateRequestSchema,
  moduleSettingsUpdateResultSchema,
  moduleSettingsGetRequestSchema,
  moduleSettingsGetResultSchema,
  automationResultSchema,
  globalVariableSnapshotSchema,
  backendNotificationSchema,
  fileExplorerLaunchRequestSchema,
  settingsImportResultSchema,
  type AutomationHttpRequest,
  type BackendUiState,
} from '../contracts/rpc';
import { runActivitySnapshotSchema } from '../contracts/activity';
import type { AutomatorBridge, FocusRequest, HostInfo, InitialState } from '../ui/types';

function listen<T>(channel: string, listener: (value: T) => void): () => void {
  const handler = (_event: Electron.IpcRendererEvent, value: T) => listener(value);
  ipcRenderer.on(channel, handler);
  return () => ipcRenderer.removeListener(channel, handler);
}

let fileExplorerSubscriberCount = 0;

const api: AutomatorBridge = Object.freeze({
  getInitialState: () => ipcRenderer.invoke('automator:get-initial-state') as Promise<InitialState>,
  getHostInfo: () => ipcRenderer.invoke('automator:get-host-info') as Promise<HostInfo>,
  getWindowDiagnostics: () => ipcRenderer.invoke('automator:get-window-diagnostics'),
  reportRendererReady: () => ipcRenderer.invoke('automator:renderer-ready'),
  openWorkspace: () => ipcRenderer.invoke('automator:open-workspace') as Promise<boolean>,
  getWindowContext: () => ipcRenderer.invoke('automator:get-window-context') as Promise<{ role: 'launcher' | 'workspace'; selectedTab: number }>,
  confirmPrimaryFocus: (nonce: string, mode: FocusRequest['mode'], focused: boolean) => ipcRenderer.invoke('automator:confirm-focus', { nonce, mode, focused }),
  close: () => ipcRenderer.invoke('automator:close'),
  selectTab: (tab: number) => ipcRenderer.invoke('automator:select-tab', { tab }),
  setQuery: (query: string) => ipcRenderer.invoke('automator:set-query', { query }),
  openCatalog: () => ipcRenderer.invoke('automator:open-catalog', {}),
  selectCatalogBinding: (bindingId: string) => ipcRenderer.invoke('automator:select-catalog-binding', { bindingId }),
  selectBinding: (bindingId: string) => ipcRenderer.invoke('automator:select-binding', { bindingId }),
  addCustomApp: () => ipcRenderer.invoke('automator:add-custom-app'),
  pickScriptFile: (interpreter: Parameters<AutomatorBridge['pickScriptFile']>[0]) => ipcRenderer.invoke('automator:pick-script-file', interpreter) as Promise<string | null>,
  pickPath: (kind: Parameters<AutomatorBridge['pickPath']>[0]) => ipcRenderer.invoke('automator:pick-path', kind) as Promise<string | null>,
  resolveDroppedFile: (file: File) => {
    try { return Promise.resolve(webUtils.getPathForFile(file)); }
    catch { return Promise.resolve(''); }
  },
  pickWorkingDirectory: () => ipcRenderer.invoke('automator:pick-working-directory') as Promise<string | null>,
  pickBrowserProjectDirectory: () => ipcRenderer.invoke('automator:pick-browser-project-directory') as Promise<string | null>,
  saveWorkTimeCsv: async (csvText: string) => {
    const result = await ipcRenderer.invoke('automator:save-work-time-csv', csvText);
    if (typeof result !== 'boolean') throw new Error('Automator returned an invalid Work Time CSV export result.');
    return result;
  },
  saveAlias: (bindingId: string, alias: string) => ipcRenderer.invoke('automator:save-alias', { bindingId, alias }),
  removeBinding: (bindingId: string) => ipcRenderer.invoke('automator:remove-binding', { bindingId }),
  activateBinding: (bindingId: string) => ipcRenderer.invoke('automator:activate-binding', { bindingId }),
  setMode: (mode: BackendUiState['mode']) => ipcRenderer.invoke('automator:set-mode', { mode }),
  updateSettings: (settings: Parameters<AutomatorBridge['updateSettings']>[0]) => ipcRenderer.invoke('automator:update-settings', {
    hotkey: settings.hotkey,
    theme: settings.theme,
    startWithWindows: settings.startWithWindows,
    bindings: settings.bindings.map(({ id, name, targetPath, alias, arguments: args }) => ({ id, name, targetPath, alias, arguments: args })),
  }),
  startHotkeyRecording: () => ipcRenderer.invoke('automator:start-hotkey-recording', {}),
  cancelHotkeyRecording: () => ipcRenderer.invoke('automator:cancel-hotkey-recording', {}),
  importSettings: async () => {
    const result = await ipcRenderer.invoke('automator:import-settings');
    if (result === null) return null;
    const parsed = settingsImportResultSchema.safeParse(result);
    if (!parsed.success) throw new Error('Automator returned an invalid backup import result.');
    return parsed.data;
  },
  exportSettings: () => ipcRenderer.invoke('automator:export-settings'),
  relinkBinding: (bindingId: string) => ipcRenderer.invoke('automator:relink-binding', bindingId),
  openLogFolder: () => ipcRenderer.invoke('automator:open-log-folder', {}),
  retryBackend: () => ipcRenderer.invoke('automator:retry-backend'),
  getAutomationKeyboardEligibility: async (moduleId: string) => {
    const result = await ipcRenderer.invoke('automator:automation-keyboard-eligibility', { moduleId });
    const parsed = automationKeyboardEligibilityResultSchema.safeParse(result);
    if (!parsed.success) throw new Error('Automator returned an invalid keyboard eligibility result.');
    return parsed.data;
  },
  subscribeAutomationKeyboard: async (moduleId: string) => {
    const result = await ipcRenderer.invoke('automator:automation-keyboard-subscribe', { moduleId });
    if (!result || typeof result.subscribed !== 'boolean') throw new Error('Automator returned an invalid keyboard subscription result.');
    return { subscribed: result.subscribed };
  },
  unsubscribeAutomationKeyboard: async (moduleId: string) => {
    const result = await ipcRenderer.invoke('automator:automation-keyboard-unsubscribe', { moduleId });
    if (!result || typeof result.unsubscribed !== 'boolean') throw new Error('Automator returned an invalid keyboard unsubscribe result.');
    return { unsubscribed: result.unsubscribed };
  },
  automationHttpRequest: async (moduleId: string, requestId: string, request: AutomationHttpRequest) => {
    const result = await ipcRenderer.invoke('automator:automation-http-request', { moduleId, requestId, request })
      .catch((error) => createAutomationHttpIpcFailure(error));
    const parsed = automationHttpIpcResponseSchema.safeParse(result);
    if (!parsed.success) {
      return createAutomationHttpIpcFailure({
        category: 'invalidResponse',
        message: 'Automator returned an invalid HTTP service response.',
      });
    }
    return parsed.data;
  },
  cancelAutomationHttpRequest: async (moduleId: string, requestId: string) => {
    const result = await ipcRenderer.invoke('automator:automation-http-cancel', { moduleId, requestId });
    if (!result || typeof result.canceled !== 'boolean') throw new Error('Automator returned an invalid HTTP cancellation result.');
    return { canceled: result.canceled };
  },
  dispatchModuleAction: async (request: Parameters<AutomatorBridge['dispatchModuleAction']>[0]) => {
    const parsedRequest = automationModuleActionRequestSchema.safeParse(request);
    if (!parsedRequest.success) throw new Error('The module action request is invalid.');
    const result = await ipcRenderer.invoke('automator:module-action', parsedRequest.data);
    const parsedResult = automationResultSchema.safeParse(result);
    if (!parsedResult.success) throw new Error('Automator returned an invalid module action result.');
    return parsedResult.data;
  },
  cancelAutomationModuleAction: async (moduleId: string, requestId: string) => {
    const request = automationModuleActionCancelRequestSchema.safeParse({ moduleId, requestId });
    if (!request.success) throw new Error('The module action cancellation request is invalid.');
    const result = await ipcRenderer.invoke('automator:module-action-cancel', request.data) as { canceled?: unknown };
    if (!result || typeof result.canceled !== 'boolean') throw new Error('Automator returned an invalid module action cancellation result.');
    return { canceled: result.canceled };
  },
  updateModuleSettings: async (request: Parameters<AutomatorBridge['updateModuleSettings']>[0]) => {
    const parsedRequest = moduleSettingsUpdateRequestSchema.safeParse(request);
    if (!parsedRequest.success) throw new Error('The module settings update request is invalid.');
    const result = await ipcRenderer.invoke('automator:module-settings-update', parsedRequest.data);
    const parsedResult = moduleSettingsUpdateResultSchema.safeParse(result);
    if (!parsedResult.success) throw new Error('Automator returned an invalid module settings update result.');
    return parsedResult.data;
  },
  getModuleSettings: async (request: Parameters<AutomatorBridge['getModuleSettings']>[0]) => {
    const parsedRequest = moduleSettingsGetRequestSchema.safeParse(request);
    if (!parsedRequest.success) throw new Error('The module settings read request is invalid.');
    const result = await ipcRenderer.invoke('automator:module-settings-get', parsedRequest.data);
    const parsedResult = moduleSettingsGetResultSchema.safeParse(result);
    if (!parsedResult.success) throw new Error('Automator returned an invalid module settings result.');
    return parsedResult.data;
  },
  getGlobalVariables: async () => {
    const result = await ipcRenderer.invoke('automator:variables-get');
    const parsed = globalVariableSnapshotSchema.safeParse(result);
    if (!parsed.success) throw new Error('Automator returned invalid global variables.');
    return parsed.data;
  },
  setGlobalVariables: async (values: Parameters<AutomatorBridge['setGlobalVariables']>[0]) => {
    const result = await ipcRenderer.invoke('automator:variables-set', values);
    const parsed = globalVariableSnapshotSchema.safeParse(result);
    if (!parsed.success) throw new Error('Automator returned invalid saved global variables.');
    return parsed.data;
  },
  getRunActivity: async () => {
    const result = await ipcRenderer.invoke('automator:activity-list');
    const parsed = runActivitySnapshotSchema.safeParse(result);
    if (!parsed.success) throw new Error('Automator returned invalid run activity.');
    return parsed.data;
  },
  onFileExplorerLaunch: (listener: Parameters<AutomatorBridge['onFileExplorerLaunch']>[0]) => {
    const unsubscribe = listen<unknown>('automator:file-explorer-launch', (value) => {
      const parsed = fileExplorerLaunchRequestSchema.safeParse(value);
      if (parsed.success) listener(parsed.data);
    });
    if (++fileExplorerSubscriberCount === 1) void ipcRenderer.invoke('automator:file-explorer-listener-ready');
    let subscribed = true;
    return () => {
      if (!subscribed) return;
      subscribed = false;
      unsubscribe();
      if (--fileExplorerSubscriberCount === 0) void ipcRenderer.invoke('automator:file-explorer-listener-closed');
    };
  },
  onStateChanged: (listener: Parameters<AutomatorBridge['onStateChanged']>[0]) => listen<BackendUiState>('automator:state-changed', listener),
  onFocusPrimaryControl: (listener: Parameters<AutomatorBridge['onFocusPrimaryControl']>[0]) => listen<FocusRequest>('automator:focus-primary-control', listener),
  onBackendFailure: (listener: Parameters<AutomatorBridge['onBackendFailure']>[0]) => listen<string>('automator:backend-failure', listener),
  onHotkeyRecorded: (listener: (key: string) => void) => listen<{ method: string; params: { key: string } }>('automator:backend-notification', (value) => {
    if (value?.method === 'hotkey/recorded' && typeof value.params?.key === 'string') listener(value.params.key);
  }),
  onAutomationKeyboardAvailability: (listener: Parameters<AutomatorBridge['onAutomationKeyboardAvailability']>[0]) => listen<unknown>('automator:backend-notification', (value) => {
    const parsed = backendNotificationSchema.safeParse(value);
    if (parsed.success && parsed.data.method === 'automation/keyboardAvailability') listener(parsed.data.params);
  }),
  onAutomationKeyboardInput: (listener: Parameters<AutomatorBridge['onAutomationKeyboardInput']>[0]) => listen<unknown>('automator:backend-notification', (value) => {
    const parsed = backendNotificationSchema.safeParse(value);
    if (parsed.success && parsed.data.method === 'automation/keyboardInput') listener(parsed.data.params);
  }),
  onRunActivityChanged: (listener: Parameters<AutomatorBridge['onRunActivityChanged']>[0]) => listen<unknown>('automator:backend-notification', (value) => {
    const parsed = backendNotificationSchema.safeParse(value);
    if (parsed.success && parsed.data.method === 'activity/changed') listener();
  }),
  onGlobalVariablesChanged: (listener: Parameters<AutomatorBridge['onGlobalVariablesChanged']>[0]) => listen<unknown>('automator:backend-notification', (value) => {
    const parsed = backendNotificationSchema.safeParse(value);
    if (parsed.success && parsed.data.method === 'variables/changed') listener();
  }),
});

contextBridge.exposeInMainWorld('automator', api);
