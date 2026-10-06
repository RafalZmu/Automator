import {
  PROTOCOL_VERSION,
  type AutomationModuleActionRequest,
  type ModuleSettingsUpdateRequest,
  type ModuleSettingsUpdateResult,
  type ModuleSettingsGetRequest,
  type ModuleSettingsGetResult,
  type AutomationResult,
  type SettingsImportResult,
  type AutomationHttpRequest,
  type BackendUiState,
  type GlobalVariableSnapshot,
} from '../contracts/rpc.ts';
import type { AutomationHttpIpcResponse } from '../contracts/automationHttpIpc.ts';
import type { RunActivitySnapshot } from '../contracts/activity.ts';
import type { AutomatorBridge, FocusRequest, HostInfo, InitialState } from './types.d';

const tabs: BackendUiState['tabs'] = [
  { slot: 1, id: 'launcher', title: 'Launcher', iconKey: 'sparkles', kind: 'launcher', searchEnabled: true, contractVersion: 1, settingsVersion: 1, actions: [
    { id: 'search', version: 1, command: 'launcher/setQuery', requiredCapabilities: [] },
    { id: 'selectBinding', version: 1, command: 'binding/select', requiredCapabilities: [] },
    { id: 'openCatalog', version: 1, command: 'launcher/openCatalog', requiredCapabilities: [] },
    { id: 'addCustom', version: 1, command: 'catalog/addCustom', requiredCapabilities: [] },
  ], capabilities: [] },
  { slot: 2, id: 'script-runner', title: 'Script Runner', iconKey: 'terminal', kind: 'script-runner' as const,
    searchEnabled: false, contractVersion: 1, settingsVersion: 1,
    actions: ['listProfiles', 'saveProfile', 'deleteProfile', 'runProfile'].map((id) => ({ id, version: 1, command: null, requiredCapabilities: [] })),
    capabilities: [{ id: 'storage.library', version: 1 }, { id: 'process.execute', version: 1 }] },
  ...Array.from({ length: 5 }, (_, index) => ({
    slot: index + 3, id: `reserved-${index + 3}`, title: `Tab ${index + 3}`, iconKey: 'grid', kind: 'reserved' as const,
    searchEnabled: false, contractVersion: 1, settingsVersion: 1, actions: [], capabilities: [],
  })),
  { slot: 8, id: 'website-launcher', title: 'Website Launcher', iconKey: 'globe', kind: 'website-launcher' as const,
    searchEnabled: true, contractVersion: 1, settingsVersion: 1,
    actions: [{ id: 'launchRow', version: 1, command: null, requiredCapabilities: [{ id: 'website.launch', version: 1 }] }],
    capabilities: [{ id: 'website.launch', version: 1 }] },
  { slot: 9, id: 'reserved-9', title: 'Tab 9', iconKey: 'grid', kind: 'reserved' as const,
    searchEnabled: false, contractVersion: 1, settingsVersion: 1, actions: [], capabilities: [] },
];

const emptyBinding = (id: string, name: string, targetPath: string, alias: string): BackendUiState['bindings'][number] => ({
  id, name, targetPath, alias, arguments: '', iconDataUrl: null,
});

const mockState: BackendUiState = {
  protocolVersion: PROTOCOL_VERSION,
  buildId: 'browser-preview',
  revision: 0,
  tabRegistryVersion: 3,
  moduleStates: tabs.map((tab): BackendUiState['moduleStates'][number] => ({ moduleId: tab.id, version: 3, values: tab.kind === 'reserved' ? { status: 'reserved' } : tab.kind === 'script-runner' ? { status: 'ready' } : {} })),
  visible: true,
  selectedTab: 1,
  query: '',
  previousForegroundHwnd: null,
  mode: 'launcher',
  tabs,
  bindings: [
    emptyBinding('browser', 'Microsoft Edge', 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe', 'ed'),
    emptyBinding('terminal', 'Windows Terminal', 'C:\\Program Files\\WindowsApps\\WindowsTerminal.exe', 'wt'),
    emptyBinding('editor', 'Visual Studio Code', 'C:\\Program Files\\Microsoft VS Code\\Code.exe', 'code'),
  ],
  catalogApps: [],
  catalogTotalCount: 0,
  aliasEditCandidate: null,
  hotkey: 'RightControl',
  theme: 'Light',
  startWithWindows: true,
  settingsValid: true,
  settingsError: null,
  missingBindings: [],
  catalogLoading: false,
  hookInstalled: false,
  busy: false,
  error: null,
  matchKind: 'None',
  matchedBindingId: null,
};

const host: HostInfo = {
  protocolVersion: PROTOCOL_VERSION,
  buildId: 'browser-preview',
  processId: 0,
  backendProcessId: null,
  testMode: true,
  backendState: 'ready',
  acrylicSupported: false,
  nativeCornersSupported: false,
  transparentWindow: false,
  trayIconReady: false,
  themeSource: 'light',
};

class BrowserPreviewBridge implements AutomatorBridge {
  private state = structuredClone(mockState);
  private stateListeners = new Set<(state: BackendUiState) => void>();
  private focusListeners = new Set<(request: FocusRequest) => void>();
  private failureListeners = new Set<(message: string) => void>();
  private hotkeyListeners = new Set<(key: string) => void>();
  private activityListeners = new Set<() => void>();
  private variableListeners = new Set<() => void>();
  private globalVariables: GlobalVariableSnapshot = { version: 1, values: {}, migrationConflicts: [] };

  async getInitialState(): Promise<InitialState> { return { state: structuredClone(this.state), host }; }
  async getHostInfo(): Promise<HostInfo> { return host; }
  async getWindowDiagnostics() { return null; }
  async reportRendererReady() { return true; }
  async openWorkspace() { return false; }
  async getWindowContext() { return { role: 'launcher' as const, selectedTab: this.state.selectedTab }; }
  async confirmPrimaryFocus() { return true; }
  async close() { return this.update({ visible: false }); }
  async selectTab(tab: number) { return this.update({ selectedTab: tab, query: '', mode: 'launcher', aliasEditCandidate: null, error: null }); }
  async setQuery(query: string) {
    const match = this.state.selectedTab === 1 && this.state.mode === 'launcher'
      ? this.state.bindings.filter((binding) => binding.alias && binding.alias.toLowerCase().startsWith(query.trim().toLowerCase()))
      : [];
    const exact = match.filter((binding) => binding.alias.toLowerCase() === query.trim().toLowerCase());
    return this.update({ query, matchedBindingId: exact.length === 1 ? exact[0].id : null, matchKind: exact.length === 1 ? 'Exact' : match.length ? 'Partial' : 'None' });
  }
  async openCatalog() {
    return this.update({ mode: 'catalog', catalogLoading: false, catalogApps: [
      emptyBinding('catalog-settings', 'Settings', 'C:\\Windows\\System32\\control.exe', ''),
      emptyBinding('catalog-notepad', 'Notepad', 'C:\\Windows\\System32\\notepad.exe', ''),
      emptyBinding('catalog-calculator', 'Calculator', 'C:\\Windows\\System32\\calc.exe', ''),
    ], catalogTotalCount: 3 });
  }
  async selectCatalogBinding(bindingId: string) {
    const candidate = this.state.catalogApps.find((binding) => binding.id === bindingId);
    return this.update({ aliasEditCandidate: candidate ?? null, mode: candidate ? 'aliasEditing' : 'catalog' });
  }
  async selectBinding(bindingId: string) {
    const candidate = this.state.bindings.find((binding) => binding.id === bindingId);
    return this.update({ aliasEditCandidate: candidate ?? null, mode: candidate ? 'aliasEditing' : 'launcher' });
  }
  async addCustomApp() { return null; }
  async saveAlias(bindingId: string, alias: string) {
    const candidate = this.state.aliasEditCandidate;
    if (!candidate || candidate.id !== bindingId) return this.update({ error: 'This app is no longer selected.' });
    const saved = { ...candidate, alias: alias.trim() };
    const bindings = [...this.state.bindings.filter((binding) => binding.id !== saved.id), saved];
    return this.update({ bindings, mode: 'launcher', aliasEditCandidate: null, selectedTab: 1, error: null });
  }
  async activateBinding() { return this.update({ visible: false, busy: false }); }
  async removeBinding(bindingId: string) {
    if (!this.state.bindings.some((binding) => binding.id === bindingId)) throw new Error('The selected binding no longer exists.');
    const bindings = this.state.bindings.filter((binding) => binding.id !== bindingId);
    return this.update({ bindings, matchKind: 'None', matchedBindingId: null, error: null });
  }
  async setMode(mode: BackendUiState['mode']) { return this.update({ mode, ...(mode === 'launcher' ? { aliasEditCandidate: null } : {}) }); }
  async updateSettings(settings: Pick<BackendUiState, 'hotkey' | 'theme' | 'startWithWindows' | 'bindings'>) {
    return this.update({ ...settings, mode: 'settings', settingsValid: true, settingsError: null });
  }
  async startHotkeyRecording() { return this.update({ mode: 'recordingHotkey' }); }
  async cancelHotkeyRecording() { return this.update({ mode: 'settings' }); }
  async importSettings(): Promise<SettingsImportResult | null> { return null; }
  async exportSettings() { return false; }
  async relinkBinding() { return null; }
  async openLogFolder() { return false; }
  async retryBackend() { return this.getInitialState(); }
  async getAutomationKeyboardEligibility() { return { eligible: false, revision: 0 }; }
  async subscribeAutomationKeyboard() { return { subscribed: false }; }
  async unsubscribeAutomationKeyboard() { return { unsubscribed: true }; }
  async automationHttpRequest(_moduleId: string, _requestId: string, _request: AutomationHttpRequest): Promise<AutomationHttpIpcResponse> {
    return {
      ok: false,
      error: { category: 'transportFailure', message: 'HTTP automation services are unavailable in browser preview.' },
    };
  }
  async cancelAutomationHttpRequest(_moduleId: string, _requestId: string) { return { canceled: false }; }
  async dispatchModuleAction(_request: AutomationModuleActionRequest): Promise<AutomationResult> {
    throw new Error('Module actions are unavailable in browser preview.');
  }
  async cancelAutomationModuleAction(_moduleId: string, _requestId: string) { return { canceled: false }; }
  async pickScriptFile(_interpreter: Parameters<AutomatorBridge['pickScriptFile']>[0]) { return null; }
  async pickPath(_kind: Parameters<AutomatorBridge['pickPath']>[0]) { return null; }
  async resolveDroppedFile(_file: File) { return ''; }
  async pickWorkingDirectory() { return null; }
  async pickBrowserProjectDirectory() { return null; }
  async saveWorkTimeCsv(_csvText: string) { return false; }
  async updateModuleSettings(request: ModuleSettingsUpdateRequest): Promise<ModuleSettingsUpdateResult> {
    return { saved: true, moduleId: request.moduleId, settingsVersion: request.settingsVersion };
  }
  async getModuleSettings(request: ModuleSettingsGetRequest): Promise<ModuleSettingsGetResult> {
    const tab = tabs.find((candidate) => candidate.id === request.moduleId);
    if (!tab || tab.contractVersion !== request.contractVersion || tab.settingsVersion !== request.settingsVersion)
      throw new Error('The module settings request is stale or unregistered.');
    return { ...request, value: {} };
  }
  async getGlobalVariables(): Promise<GlobalVariableSnapshot> { return structuredClone(this.globalVariables); }
  async setGlobalVariables(values: GlobalVariableSnapshot['values']): Promise<GlobalVariableSnapshot> {
    this.globalVariables = { version: 1, values: structuredClone(values), migrationConflicts: [] };
    for (const listener of this.variableListeners) listener();
    return structuredClone(this.globalVariables);
  }
  async getRunActivity(): Promise<RunActivitySnapshot> { return { contractVersion: 1, entries: [] }; }
  onStateChanged(listener: (state: BackendUiState) => void) { this.stateListeners.add(listener); return () => this.stateListeners.delete(listener); }
  onFocusPrimaryControl(listener: (request: FocusRequest) => void) { this.focusListeners.add(listener); return () => this.focusListeners.delete(listener); }
  onBackendFailure(listener: (message: string) => void) { this.failureListeners.add(listener); return () => this.failureListeners.delete(listener); }
  onHotkeyRecorded(listener: (key: string) => void) { this.hotkeyListeners.add(listener); return () => this.hotkeyListeners.delete(listener); }
  onAutomationKeyboardAvailability() { return () => {}; }
  onAutomationKeyboardInput() { return () => {}; }
  onRunActivityChanged(listener: () => void) { this.activityListeners.add(listener); return () => this.activityListeners.delete(listener); }
  onGlobalVariablesChanged(listener: () => void) { this.variableListeners.add(listener); return () => this.variableListeners.delete(listener); }

  private update(patch: Partial<BackendUiState>): BackendUiState {
    this.state = { ...this.state, ...patch, revision: this.state.revision + 1 };
    const snapshot = structuredClone(this.state);
    for (const listener of this.stateListeners) listener(snapshot);
    return snapshot;
  }
}

let previewBridge: BrowserPreviewBridge | undefined;

export function createBrowserPreviewBridge() {
  return new BrowserPreviewBridge();
}

export function getAutomatorBridge(): AutomatorBridge {
  if (window.automator) return window.automator;
  const preview = import.meta.env.DEV && window.location.protocol === 'http:'
    && window.location.hostname === '127.0.0.1' && new URLSearchParams(window.location.search).get('preview') === '1';
  if (preview) {
    previewBridge ??= createBrowserPreviewBridge();
    return previewBridge;
  }
  throw new Error('Automator preload bridge is unavailable. Restart the desktop app or open the explicit browser preview at ?preview=1.');
}
