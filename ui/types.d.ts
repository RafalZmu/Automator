import type {
  AutomationHttpRequest,
  AutomationModuleActionRequest,
  GlobalVariableSnapshot,
  ModuleSettingsUpdateRequest,
  ModuleSettingsUpdateResult,
  ModuleSettingsGetRequest,
  ModuleSettingsGetResult,
  AutomationResult,
  SettingsImportResult,
  AutomationKeyboardAvailabilityNotification,
  AutomationKeyboardEligibilityResult,
  AutomationKeyboardInputNotification,
  BackendUiState,
} from '../contracts/rpc';
import type { RunActivitySnapshot } from '../contracts/activity';
import type { ScriptRunnerInterpreter } from '../contracts/scriptRunner';
import type { AutomationHttpIpcResponse } from '../contracts/automationHttpIpc';

export {};

export type FocusRequest = { nonce: string; mode: BackendUiState['mode'] };
export type HostInfo = {
  protocolVersion: number;
  buildId: string;
  processId: number;
  backendProcessId: number | null;
  testMode: boolean;
  backendState: 'ready' | 'starting' | 'failed';
  acrylicSupported: boolean;
  nativeCornersSupported: boolean;
  transparentWindow: boolean;
  trayIconReady: boolean;
  themeSource: 'light' | 'dark' | 'system';
};

export type InitialState = { state: BackendUiState | null; host: HostInfo };

export interface AutomatorBridge {
  getInitialState(): Promise<InitialState>;
  getHostInfo(): Promise<HostInfo>;
  getWindowDiagnostics(): Promise<{
    windowHandleHex: string;
    browserWindowFocused: boolean;
    webContentsFocused: boolean;
    visible: boolean;
    foregroundHwndHex: string | null;
    buildId: string;
  } | null>;
  reportRendererReady(): Promise<boolean>;
  openWorkspace(): Promise<boolean>;
  getWindowContext(): Promise<{ role: 'launcher' | 'workspace'; selectedTab: number }>;
  confirmPrimaryFocus(nonce: string, mode: FocusRequest['mode'], focused: boolean): Promise<boolean>;
  close(): Promise<BackendUiState>;
  selectTab(tab: number): Promise<BackendUiState>;
  setQuery(query: string): Promise<BackendUiState>;
  openCatalog(): Promise<BackendUiState>;
  selectCatalogBinding(bindingId: string): Promise<BackendUiState>;
  selectBinding(bindingId: string): Promise<BackendUiState>;
  addCustomApp(): Promise<BackendUiState | null>;
  saveAlias(bindingId: string, alias: string): Promise<BackendUiState>;
  removeBinding(bindingId: string): Promise<BackendUiState>;
  activateBinding(bindingId: string): Promise<BackendUiState>;
  setMode(mode: BackendUiState['mode']): Promise<BackendUiState>;
  updateSettings(settings: Pick<BackendUiState, 'hotkey' | 'theme' | 'startWithWindows' | 'bindings'>): Promise<BackendUiState>;
  startHotkeyRecording(): Promise<BackendUiState>;
  cancelHotkeyRecording(): Promise<BackendUiState>;
  importSettings(): Promise<SettingsImportResult | null>;
  exportSettings(): Promise<boolean>;
  relinkBinding(bindingId: string): Promise<BackendUiState | null>;
  openLogFolder(): Promise<boolean>;
  retryBackend(): Promise<InitialState>;
  getAutomationKeyboardEligibility(moduleId: string): Promise<AutomationKeyboardEligibilityResult>;
  subscribeAutomationKeyboard(moduleId: string): Promise<{ subscribed: boolean }>;
  unsubscribeAutomationKeyboard(moduleId: string): Promise<{ unsubscribed: boolean }>;
  automationHttpRequest(moduleId: string, requestId: string, request: AutomationHttpRequest): Promise<AutomationHttpIpcResponse>;
  cancelAutomationHttpRequest(moduleId: string, requestId: string): Promise<{ canceled: boolean }>;
  dispatchModuleAction(request: AutomationModuleActionRequest): Promise<AutomationResult>;
  cancelAutomationModuleAction(moduleId: string, requestId: string): Promise<{ canceled: boolean }>;
  pickScriptFile(interpreter: ScriptRunnerInterpreter): Promise<string | null>;
  pickPath(kind: 'file' | 'directory'): Promise<string | null>;
  resolveDroppedFile(file: File): Promise<string>;
  pickBrowserProjectDirectory(): Promise<string | null>;
  pickWorkingDirectory(): Promise<string | null>;
  saveWorkTimeCsv(csvText: string): Promise<boolean>;
  updateModuleSettings(request: ModuleSettingsUpdateRequest): Promise<ModuleSettingsUpdateResult>;
  getModuleSettings(request: ModuleSettingsGetRequest): Promise<ModuleSettingsGetResult>;
  getGlobalVariables(): Promise<GlobalVariableSnapshot>;
  setGlobalVariables(values: GlobalVariableSnapshot['values']): Promise<GlobalVariableSnapshot>;
  onGlobalVariablesChanged(listener: () => void): () => void;
  getRunActivity(): Promise<RunActivitySnapshot>;
  onStateChanged(listener: (state: BackendUiState) => void): () => void;
  onFocusPrimaryControl(listener: (request: FocusRequest) => void): () => void;
  onBackendFailure(listener: (message: string) => void): () => void;
  onHotkeyRecorded(listener: (key: string) => void): () => void;
  onAutomationKeyboardAvailability(listener: (notification: AutomationKeyboardAvailabilityNotification) => void): () => void;
  onAutomationKeyboardInput(listener: (notification: AutomationKeyboardInputNotification) => void): () => void;
  onRunActivityChanged(listener: () => void): () => void;
}

declare global {
  interface Window {
    automator?: AutomatorBridge;
  }
}
