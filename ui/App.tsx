import * as Dialog from '@radix-ui/react-dialog';
import * as Tabs from '@radix-ui/react-tabs';
import {
  Activity, AlertCircle, AppWindow, ArrowDownToLine, ArrowUpFromLine, Bell, Check, ChevronRight, CircleDot,
  FolderOpen, Keyboard, Moon, Plus, Search, Settings2, Sparkles, Sun, X,
} from 'lucide-react';
import { AnimatePresence } from 'motion/react';
import { useCallback, useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react';
import { backendUiStateSchema, type AutomationResult, type BackendUiState, type GlobalVariableSnapshot, type FileExplorerLaunchRequest, type SettingsImportResult } from '../contracts/rpc';
import type { RunActivityEntry, RunActivitySnapshot } from '../contracts/activity';
import type { VariableJsonValue } from './contracts/variables';
import { readWebsiteLauncherQuickActions } from './contracts/websiteLauncher';
import { createAutomationServices } from './automationServices';
import type { AutomationServices } from './automationServices';
import { getAutomatorBridge } from './bridge';
import type { AutomatorBridge, FocusRequest, HostInfo, InitialState } from './types';
import { launcherViewRegistry } from './viewRegistry';
import { TabCommandBar } from './commands/TabCommandBar';
import { QuickActionsHome, type QuickAction } from './commands/QuickActionsHome';
import { useAllTabCommandCatalog, useTabCommandResolver } from './commands/TabCommandRegistry';
import type { TabCommand } from './commands/commandMatching';
import { GlobalVariablesContext, GlobalVariablesEditor } from './variables/GlobalVariables';
import { NotificationCenter } from './activity/NotificationCenter';
import { RunActivityView } from './activity/RunActivityView';
import { LatestModuleResult } from './activity/LatestModuleResult';
import { rememberTransientResult } from './activity/transientResults';
import { FileExplorerLaunchContext } from './FileExplorerLaunchContext';

type Binding = BackendUiState['bindings'][number];
type SettingsDraft = Pick<BackendUiState, 'hotkey' | 'theme' | 'startWithWindows' | 'bindings'>;

const quickActionBootstrap: readonly { scope: string; id: string; label: string; keywords: readonly string[]; category: string }[] = [
  { scope: 'script-runner', id: 'new-script-profile', label: 'Create script profile', keywords: ['python', 'powershell', 'bash', 'new'], category: 'Script Runner · action' },
  { scope: 'api', id: 'api:new-profile', label: 'New API profile', keywords: ['request', 'http', 'create'], category: 'API · action' },
  { scope: 'api', id: 'api:paste-curl', label: 'Paste cURL request', keywords: ['chrome', 'import', 'curl'], category: 'API · action' },
  { scope: 'browser-automation', id: 'choose-project', label: 'Choose Playwright project folder', keywords: ['browser', 'tests', 'directory'], category: 'Browser Automation · action' },
  { scope: 'browser-automation', id: 'create-section', label: 'Create Playwright test section', keywords: ['browser', 'new file', 'spec'], category: 'Browser Automation · action' },
  { scope: 'workflows', id: 'workflow:new', label: 'New workflow', keywords: ['create', 'automation', 'steps'], category: 'Workflows · action' },
  { scope: 'scheduler', id: 'scheduler:new', label: 'Create schedule', keywords: ['daily', 'weekly', 'script', 'workflow'], category: 'Scheduler · action' },
  { scope: 'focus-sessions', id: 'start-work', label: 'Start work timer', keywords: ['begin', 'time log'], category: 'Work Time Log · action' },
  { scope: 'focus-sessions', id: 'stop-work', label: 'Stop work timer', keywords: ['end', 'time log'], category: 'Work Time Log · action' },
];

function AppIcon({ binding }: { binding: Binding }) {
  const [broken, setBroken] = useState(false);
  return (
    <span className="app-icon" aria-hidden="true">
      {binding.iconDataUrl && !broken
        ? <img src={binding.iconDataUrl} alt="" onError={() => setBroken(true)} />
        : <span>{binding.name.trim().slice(0, 1).toLocaleUpperCase() || 'A'}</span>}
    </span>
  );
}

function AppRow({
  binding, onActivate, onEditAlias, onRemove, matched, isCatalog,
}: {
  binding: Binding;
  onActivate: () => void;
  onEditAlias: () => void;
  onRemove: () => void;
  matched: boolean;
  isCatalog: boolean;
}) {
  return (
    <article className="app-row" data-matched={matched}>
      <button className="app-row-main" type="button" onClick={onActivate} aria-label={isCatalog ? `Select ${binding.name} to assign an alias` : `Launch ${binding.name}`}>
        <AppIcon binding={binding} />
        <span className="app-copy">
          <span className="app-name">{binding.name}</span>
          <span className="app-target">{binding.targetPath}</span>
        </span>
      </button>
      <button className="alias-chip" type="button" onClick={onEditAlias} aria-label={binding.alias ? `Edit alias ${binding.alias} for ${binding.name}` : `Add alias for ${binding.name}`}>
        {binding.alias || <><Plus size={12} /> alias</>}
      </button>
      {!isCatalog && <button className="remove-binding" type="button" onClick={onRemove}
        aria-label={`Remove ${binding.name} from launcher`} title={`Remove ${binding.name} from launcher`}>
        <X size={14} aria-hidden="true" />
      </button>}
    </article>
  );
}

function SettingsDialog({
  bridge, state, open, onOpenChange, focusRef, reportError, portalContainer,
  loadGlobalVariables, saveGlobalVariables, onGlobalVariablesChanged,
}: {
  bridge: AutomatorBridge;
  state: BackendUiState;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  focusRef: React.RefObject<HTMLButtonElement | null>;
  reportError: (message: string) => void;
  portalContainer: HTMLDivElement | null;
  loadGlobalVariables: () => Promise<GlobalVariableSnapshot>;
  saveGlobalVariables: (values: Record<string, VariableJsonValue>) => Promise<GlobalVariableSnapshot>;
  onGlobalVariablesChanged: (snapshot: GlobalVariableSnapshot) => void;
}) {
  const [draft, setDraft] = useState<SettingsDraft>(() => ({
    hotkey: state.hotkey,
    theme: state.theme,
    startWithWindows: state.startWithWindows,
    bindings: state.bindings,
  }));
  const [saving, setSaving] = useState(false);
  const [notice, setNotice] = useState('');
  const [importReport, setImportReport] = useState<SettingsImportResult | null>(null);
  const [variableEditorRevision, setVariableEditorRevision] = useState(0);

  useEffect(() => {
    if (open) {
      setDraft({ hotkey: state.hotkey, theme: state.theme, startWithWindows: state.startWithWindows, bindings: state.bindings });
      setNotice('');
      setImportReport(null);
    }
  }, [open]);

  useEffect(() => {
    if (open && state.hotkey !== draft.hotkey) setDraft((value) => ({ ...value, hotkey: state.hotkey }));
  }, [open, state.hotkey, draft.hotkey]);

  const startRecording = async () => {
    setNotice('Press any key, including Right Ctrl or a modifier.');
    try { await bridge.startHotkeyRecording(); }
    catch (error) { reportError(errorMessage(error)); }
  };

  const save = async () => {
    setSaving(true);
    try {
      const next = await bridge.updateSettings(draft);
      setNotice('Settings saved.');
      await bridge.setMode('launcher');
      onOpenChange(false);
      return next;
    } catch (error) {
      reportError(errorMessage(error));
    } finally { setSaving(false); }
  };

  const importBackup = async () => {
    try {
      const report = await bridge.importSettings();
      if (!report) return;
      await loadGlobalVariables();
      setVariableEditorRevision((revision) => revision + 1);
      setImportReport(report);
      setNotice(report.includesAutomationLibrary
        ? `Backup imported with ${report.importedLibraryRecords} automation definition(s).`
        : 'Settings imported from a settings-only file.');
    } catch (error) { reportError(errorMessage(error)); }
  };

  const startWithWindowsId = 'startup-switch';
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal container={portalContainer ?? undefined}>
        <Dialog.Overlay className="dialog-overlay" />
      <Dialog.Content className="settings-panel" aria-describedby="settings-description" onEscapeKeyDown={(event) => {
        event.preventDefault();
        if (state.mode === 'recordingHotkey') void bridge.cancelHotkeyRecording();
        else void bridge.close();
      }}>
          <header className="settings-header">
            <div>
              <Dialog.Title className="settings-title">Settings</Dialog.Title>
              <Dialog.Description id="settings-description" className="settings-description">Choose how Automator opens and looks.</Dialog.Description>
            </div>
            <Dialog.Close className="icon-button dialog-close" aria-label="Close settings"><X size={17} /></Dialog.Close>
          </header>

          <div className="settings-scroll">
            <section className="settings-section">
              <div className="section-heading"><Keyboard size={15} /><h2>Opener key</h2></div>
              <div className="setting-row">
                <div className="setting-copy"><strong>Toggle Automator</strong><span>Press the same key again to hide it.</span></div>
                <button ref={focusRef} className="key-record-button" type="button" onClick={() => void startRecording()}>
                  <kbd>{state.mode === 'recordingHotkey' ? 'Listening…' : draft.hotkey}</kbd>
                  {state.mode === 'recordingHotkey' ? <CircleDot size={14} className="listening-icon" /> : <span>Record</span>}
                </button>
              </div>
              {state.mode === 'recordingHotkey' && (
                <div className="recording-inline">
                  <span>Press a key to use as your opener. Modifier keys are supported.</span>
                  <button type="button" onClick={() => { void bridge.cancelHotkeyRecording().then(() => setNotice('Key recording cancelled.')); }}>Cancel</button>
                </div>
              )}
              {notice && <p className="settings-notice" aria-live="polite">{notice}</p>}
            </section>

            <section className="settings-section">
              <div className="section-heading"><Sun size={15} /><h2>Appearance</h2></div>
              <div className="setting-row">
                <div className="setting-copy"><strong>Color theme</strong><span>Independent from the Windows theme.</span></div>
                <div className="theme-choice" role="group" aria-label="Color theme">
                  <button type="button" data-selected={draft.theme === 'Light'} onClick={() => setDraft((value) => ({ ...value, theme: 'Light' }))}><Sun size={14} /> Light</button>
                  <button type="button" data-selected={draft.theme === 'Dark'} onClick={() => setDraft((value) => ({ ...value, theme: 'Dark' }))}><Moon size={14} /> Dark</button>
                </div>
              </div>
            </section>

            <section className="settings-section">
              <GlobalVariablesEditor key={variableEditorRevision} load={loadGlobalVariables} save={saveGlobalVariables} onChanged={onGlobalVariablesChanged} />
            </section>

            <section className="settings-section">
              <div className="section-heading"><Keyboard size={15} /><h2>Startup</h2></div>
              <label className="setting-row switch-row" htmlFor={startWithWindowsId}>
                <span className="setting-copy"><strong>Start with Windows</strong><span>Open Automator in the notification area when you sign in.</span></span>
                <input id={startWithWindowsId} type="checkbox" checked={draft.startWithWindows} onChange={(event) => setDraft((value) => ({ ...value, startWithWindows: event.target.checked }))} />
              </label>
            </section>

            <section className="settings-section">
              <div className="section-heading"><Sparkles size={15} /><h2>Application aliases</h2></div>
              {draft.bindings.length === 0 ? <p className="settings-empty">No saved applications yet. Add one from the catalog.</p> : (
                <div className="settings-bindings">
                  {draft.bindings.map((binding) => {
                    const missing = state.missingBindings.some((item) => item.id === binding.id);
                    return (
                      <div className="settings-binding" key={binding.id}>
                        <AppIcon binding={binding} />
                        <div className="setting-copy"><strong>{binding.name}</strong><span>{binding.alias ? `Alias: ${binding.alias}` : 'No alias assigned'}{missing ? ' · target not found' : ''}</span></div>
                        {missing && <button className="relink-button" type="button" onClick={() => void bridge.relinkBinding(binding.id).then(() => setNotice(`${binding.name} relinked.`)).catch((error) => reportError(errorMessage(error)))}>Relink</button>}
                      </div>
                    );
                  })}
                </div>
              )}
            </section>

            <section className="settings-section settings-tools">
              <button type="button" onClick={() => void bridge.exportSettings().then((saved) => setNotice(saved ? 'Automator backup exported.' : 'Export cancelled.')).catch((error) => reportError(errorMessage(error)))}><ArrowUpFromLine size={15} /> Export backup</button>
              <button type="button" onClick={() => void importBackup()}><ArrowDownToLine size={15} /> Import backup</button>
              <button type="button" onClick={() => void bridge.openLogFolder().catch((error) => reportError(errorMessage(error)))}><FolderOpen size={15} /> Open logs</button>
              <p className="settings-backup-note">Backups include module profiles and schedules. API credentials and browser session data stay on this PC.</p>
              {importReport && importReport.warningCount > 0 && (
                <div className="settings-import-report" role="status" aria-live="polite">
                  <strong>{importReport.warningCount} item(s) need repair.</strong>
                  <details>
                    <summary>Review repair items{importReport.warningsTruncated ? ' (first 250 shown)' : ''}</summary>
                    <ul>{importReport.warnings.map((warning) => (
                      <li key={`${warning.moduleId}:${warning.recordId}:${warning.field}:${warning.code}`}>
                        <span>{warning.moduleId} · {warning.recordId} · {warning.field}</span>
                        {warning.message}
                      </li>
                    ))}</ul>
                  </details>
                </div>
              )}
            </section>
          </div>

          <footer className="settings-footer">
            {state.settingsError && <span className="settings-error"><AlertCircle size={14} /> {state.settingsError}</span>}
            <button className="secondary-button" type="button" onClick={() => onOpenChange(false)}>Cancel</button>
            <button className="primary-button" type="button" disabled={saving || !state.settingsValid} onClick={() => void save()}>
              <Check size={15} /> {saving ? 'Saving…' : 'Save changes'}
            </button>
          </footer>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

export function App() {
  const isWorkspaceSurface = new URLSearchParams(window.location.search).get('surface') === 'workspace';
  const [explorerLaunches, setExplorerLaunches] = useState<FileExplorerLaunchRequest[]>([]);
  const consumeExplorerLaunch = useCallback(() => setExplorerLaunches(current => current.slice(1)), []);
  const [bridge, setBridge] = useState<AutomatorBridge | null>(null);
  const [bridgeError, setBridgeError] = useState('');
  const [state, setState] = useState<BackendUiState | null>(null);
  const [host, setHost] = useState<HostInfo | null>(null);
  const [backendError, setBackendError] = useState('');
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [quickActionsHome, setQuickActionsHomeState] = useState(false);
  const [globalVariables, setGlobalVariables] = useState<GlobalVariableSnapshot>({ version: 1, values: {}, migrationConflicts: [] });
  const [websiteShortcuts, setWebsiteShortcuts] = useState<ReturnType<typeof readWebsiteLauncherQuickActions>>([]);
  const [runActivity, setRunActivity] = useState<RunActivitySnapshot>({ contractVersion: 1, entries: [] });
  const [activityOpen, setActivityOpen] = useState(false);
  const [notificationsOpen, setNotificationsOpen] = useState(false);
  const [selectedActivityRunId, setSelectedActivityRunId] = useState<string | null>(null);
  const [localQuery, setLocalQuery] = useState('');
  const [localAlias, setLocalAlias] = useState('');
  const [commandError, setCommandError] = useState('');
  const [retrying, setRetrying] = useState(false);
  const [automationServicesEntry, setAutomationServicesEntry] = useState<{ key: string; services: AutomationServices } | null>(null);
  const localQueryRef = useRef('');
  const searchRef = useRef<HTMLInputElement>(null);
  const catalogSearchRef = useRef<HTMLInputElement>(null);
  const aliasRef = useRef<HTMLInputElement>(null);
  const settingsFocusRef = useRef<HTMLButtonElement>(null);
  const [portalContainer, setPortalContainer] = useState<HTMLDivElement | null>(null);
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const currentStateRef = useRef<BackendUiState | null>(null);
  const bridgeRef = useRef<AutomatorBridge | null>(null);
  const quickActionsHomeRef = useRef(false);
  const commandCatalog = useAllTabCommandCatalog();
  const resolveLiveCommand = useTabCommandResolver();

  const setQuickActionsHome = useCallback((open: boolean) => {
    quickActionsHomeRef.current = open;
    setQuickActionsHomeState(open);
  }, []);

  const acceptState = useCallback((value: unknown) => {
    const parsed = backendUiStateSchema.safeParse(value);
    if (!parsed.success) {
      setCommandError(`The backend state could not be read: ${parsed.error.message}`);
      return;
    }
    const previous = currentStateRef.current;
    if (previous && previous.buildId === parsed.data.buildId && parsed.data.revision < previous.revision) return;
    currentStateRef.current = parsed.data;
    setState(parsed.data);
    const justOpened = parsed.data.visible && (!previous || !previous.visible);
    const selectedTabChanged = previous !== null && previous.selectedTab !== parsed.data.selectedTab;
    if (!isWorkspaceSurface && justOpened) setQuickActionsHome(true);
    else if (!parsed.data.visible || parsed.data.mode !== 'launcher' || selectedTabChanged) setQuickActionsHome(false);
    const contextChanged = !previous
      || previous.mode !== parsed.data.mode
      || previous.selectedTab !== parsed.data.selectedTab
      || (!previous.visible && parsed.data.visible);
    if (contextChanged || parsed.data.query === localQueryRef.current) {
      localQueryRef.current = parsed.data.query;
      setLocalQuery(parsed.data.query);
    }
    setSettingsOpen(parsed.data.mode === 'settings' || parsed.data.mode === 'recordingHotkey');
    if (parsed.data.aliasEditCandidate) setLocalAlias(parsed.data.aliasEditCandidate.alias);
  }, [isWorkspaceSurface, setQuickActionsHome]);

  useEffect(() => {
    try {
      const currentBridge = getAutomatorBridge();
      bridgeRef.current = currentBridge;
      setBridge(currentBridge);
      const unsubscribeExplorer = currentBridge.onFileExplorerLaunch(request => {
        setExplorerLaunches(current => [...current, request]);
        setQuickActionsHome(false);
        setActivityOpen(false);
        setSettingsOpen(false);
      });
      const unsubscribeState = currentBridge.onStateChanged(acceptState);
      const unsubscribeFailure = currentBridge.onBackendFailure((message) => {
        setBackendError(message);
        void currentBridge.getHostInfo().then(setHost);
      });
      const unsubscribeFocus = currentBridge.onFocusPrimaryControl((request) => focusPrimary(request));
      const unsubscribeHotkey = currentBridge.onHotkeyRecorded((key) => {
        const previous = currentStateRef.current;
        if (!previous) return;
        const next = { ...previous, hotkey: key, mode: 'settings' as const };
        currentStateRef.current = next;
        setState(next);
        setSettingsOpen(true);
        void currentBridge.updateSettings({ hotkey: key, theme: next.theme, startWithWindows: next.startWithWindows, bindings: next.bindings })
          .catch((error) => setCommandError(errorMessage(error)));
      });
      void currentBridge.getInitialState().then((initial: InitialState) => {
        setHost(initial.host);
        if (initial.state) {
          acceptState(initial.state);
          void currentBridge.reportRendererReady();
        } else {
          setBackendError('The backend is unavailable. Retry to restart its Windows services.');
          void currentBridge.reportRendererReady();
        }
      }).catch((error) => setBridgeError(errorMessage(error)));
      return () => { unsubscribeState(); unsubscribeFailure(); unsubscribeFocus(); unsubscribeHotkey(); unsubscribeExplorer(); };
    } catch (error) {
      setBridgeError(errorMessage(error));
      return undefined;
    }
  }, [acceptState]);

  useEffect(() => {
    if (!state) return;
    if (state.mode === 'settings' || state.mode === 'recordingHotkey') setSettingsOpen(true);
    else setSettingsOpen(false);
  }, [state?.mode]);

  useEffect(() => {
    if (!state) return undefined;
    const websiteState = state.moduleStates.find((item) => item.moduleId === 'website-launcher');
    setWebsiteShortcuts(readWebsiteLauncherQuickActions(websiteState?.values.globalActions));
  }, [state?.buildId, state?.revision]);

  useEffect(() => {
    const onChanged = (event: Event) => {
      const detail = (event as CustomEvent<unknown>).detail;
      setWebsiteShortcuts(readWebsiteLauncherQuickActions(detail));
    };
    window.addEventListener('automator:website-launcher-settings-changed', onChanged);
    return () => {
      window.removeEventListener('automator:website-launcher-settings-changed', onChanged);
    };
  }, []);

  const activeTab = state?.tabs.find((tab) => tab.slot === state.selectedTab) ?? state?.tabs[0];
  const moduleState = activeTab ? state?.moduleStates.find((item) => item.moduleId === activeTab.id) : undefined;
  const capabilitySignature = activeTab?.capabilities.map(({ id, version }) => `${id}@${version}`).join(',') ?? '';
  const serviceContextKey = bridge && activeTab ? `${activeTab.id}|${capabilitySignature}` : '';
  useEffect(() => {
    if (!bridge || !activeTab) {
      setAutomationServicesEntry(null);
      return undefined;
    }
    const services = createAutomationServices(activeTab.id, bridge, activeTab.capabilities.map(({ id }) => id), activeTab);
    const entry = { key: serviceContextKey, services };
    setAutomationServicesEntry(entry);
    return () => {
      void services.dispose();
    };
  }, [bridge, serviceContextKey]);
  const automationServices = automationServicesEntry?.key === serviceContextKey ? automationServicesEntry.services : null;
  const visibleBindings = useMemo(() => {
    if (!state) return [];
    const source = state.mode === 'catalog' ? state.catalogApps : state.bindings;
    const query = localQuery.trim().toLocaleLowerCase();
    return source.filter((binding) => !query || binding.name.toLocaleLowerCase().includes(query) || binding.alias.toLocaleLowerCase().includes(query));
  }, [state, localQuery]);
  const exactMatch = state?.matchKind === 'Exact' ? state.matchedBindingId : null;
  const mainListRef = useRef<HTMLDivElement>(null);

  const runCommand = useCallback(async (action: () => Promise<unknown>) => {
    setCommandError('');
    try {
      const result = await action();
      if (backendUiStateSchema.safeParse(result).success) acceptState(result);
      return result;
    } catch (error) {
      setCommandError(errorMessage(error));
      return null;
    }
  }, [acceptState]);

  const loadGlobalVariables = useCallback(async (): Promise<GlobalVariableSnapshot> => {
    if (!bridge) throw new Error('The Automator bridge is not ready.');
    const snapshot = await bridge.getGlobalVariables();
    setGlobalVariables(snapshot);
    return snapshot;
  }, [bridge]);

  const saveGlobalVariables = useCallback(async (values: Record<string, VariableJsonValue>): Promise<GlobalVariableSnapshot> => {
    if (!bridge) throw new Error('The Automator bridge is not ready.');
    const snapshot = await bridge.setGlobalVariables(values);
    setGlobalVariables(snapshot);
    return snapshot;
  }, [bridge]);

  const refreshRunActivity = useCallback(async (): Promise<RunActivitySnapshot> => {
    if (!bridge) throw new Error('The Automator bridge is not ready.');
    const snapshot = await bridge.getRunActivity();
    setRunActivity(snapshot);
    return snapshot;
  }, [bridge]);

  useEffect(() => {
    if (!bridge) return undefined;
    let active = true;
    const refresh = () => { void bridge.getRunActivity().then((snapshot) => { if (active) setRunActivity(snapshot); }).catch((error) => { if (active) setCommandError(errorMessage(error)); }); };
    const refreshVariables = () => { void bridge.getGlobalVariables().then((snapshot) => { if (active) setGlobalVariables(snapshot); }).catch((error) => { if (active) setCommandError(errorMessage(error)); }); };
    const unsubscribe = bridge.onRunActivityChanged(refresh);
    const unsubscribeVariables = bridge.onGlobalVariablesChanged(refreshVariables);
    refresh();
    refreshVariables();
    return () => { active = false; unsubscribe(); unsubscribeVariables(); };
  }, [bridge]);

  async function focusPrimary(request: FocusRequest) {
    let target: HTMLElement | null = null;
    const deadline = Date.now() + 750;
    let latest = currentStateRef.current;
    while (Date.now() < deadline) {
      latest = currentStateRef.current;
      if (!latest?.visible || latest.mode !== request.mode) break;
      if (request.mode === 'settings' || request.mode === 'recordingHotkey') target = settingsFocusRef.current;
      else if (request.mode === 'aliasEditing') target = aliasRef.current;
      else if (request.mode === 'catalog') target = catalogSearchRef.current;
      else if (quickActionsHomeRef.current && !isWorkspaceSurface) target = document.getElementById('quick-actions-search') as HTMLInputElement | null;
      else if (latest.selectedTab === 1) target = searchRef.current;
      else target = tabRefs.current[latest.selectedTab - 1] ?? null;
      if (target?.isConnected) break;
      target = null;
      await new Promise<void>((resolve) => setTimeout(resolve, 16));
    }
    latest = currentStateRef.current;
    if (!latest?.visible || latest.mode !== request.mode) {
      await bridgeRef.current?.confirmPrimaryFocus(request.nonce, request.mode, false);
      return;
    }
    target?.focus({ preventScroll: true });
    const confirmed = Boolean(target && document.activeElement === target);
    await bridgeRef.current?.confirmPrimaryFocus(request.nonce, request.mode, confirmed);
  }

  const changeTab = (tab: number) => {
    if (!bridge) return;
    setQuickActionsHome(false);
    localQueryRef.current = '';
    setLocalQuery('');
    if (tab === 1 && !isWorkspaceSurface) {
      requestAnimationFrame(() => searchRef.current?.focus({ preventScroll: true }));
    }
    void runCommand(() => bridge.selectTab(tab));
  };

  const updateQuery = (query: string) => {
    localQueryRef.current = query;
    setLocalQuery(query);
    if (bridge) void runCommand(() => bridge.setQuery(query));
  };

  const editAlias = (binding: Binding, catalog: boolean) => {
    if (!bridge) return;
    setLocalAlias(binding.alias);
    void runCommand(() => catalog ? bridge.selectCatalogBinding(binding.id) : bridge.selectBinding(binding.id));
  };

  const openSettings = () => {
    if (!bridge) return;
    setQuickActionsHome(false);
    setSettingsOpen(true);
    void runCommand(() => bridge.setMode('settings'));
  };

  const closeSettings = (open: boolean) => {
    setSettingsOpen(open);
    if (!open && bridge) {
      if (state?.mode === 'recordingHotkey') void runCommand(() => bridge.cancelHotkeyRecording());
      else void runCommand(() => bridge.setMode('launcher'));
    }
  };

  const retry = async () => {
    if (!bridge) return;
    setRetrying(true);
    setBackendError('');
    currentStateRef.current = null;
    localQueryRef.current = '';
    setState(null);
    setLocalQuery('');
    try {
      const initial = await bridge.retryBackend();
      setHost(initial.host);
      if (initial.state) acceptState(initial.state);
      else setBackendError('Backend retry completed without state.');
    } catch (error) { setBackendError(errorMessage(error)); }
    finally { setRetrying(false); }
  };

  const handleEscape = (event: KeyboardEvent<HTMLElement>) => {
    if (!state?.visible || !bridge) return;
    if (event.key === 'Escape') {
      event.preventDefault();
      event.stopPropagation();
      if (state.mode === 'recordingHotkey') void runCommand(() => bridge.cancelHotkeyRecording());
      else void runCommand(() => bridge.close());
      return;
    }
    const target = event.target;
    const isEditing = target instanceof HTMLElement
      && (target.isContentEditable || target instanceof HTMLInputElement || target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement);
    if (/^[1-9]$/.test(event.key) && !isEditing && !event.ctrlKey && !event.metaKey && !event.altKey) {
      event.preventDefault();
      changeTab(Number(event.key));
    }
  };

  if (bridgeError) {
    return <RecoveryView title="Automator bridge unavailable" message={bridgeError} preview={false} retry={undefined} />;
  }
  if (host?.backendState === 'failed' && bridge) {
    return <RecoveryView title="Automator needs to restart" message={backendError || 'The Windows services are unavailable.'} preview={host.buildId === 'browser-preview'} retry={() => { void retry(); }} />;
  }
  if (!state || !host || !bridge) {
    return <main className="window-shell loading-shell"><span className="loading-mark"><Sparkles size={18} /></span><p>Starting Automator…</p></main>;
  }

  const isCatalog = state.mode === 'catalog';
  const aliasCandidate = state.mode === 'aliasEditing' ? state.aliasEditCandidate : null;
  const isAliasEditing = Boolean(aliasCandidate);
  const showQuickActions = quickActionsHome && !isWorkspaceSurface && state.mode === 'launcher';
  const showSearch = state.selectedTab === 1 && !isAliasEditing && !showQuickActions;
  const launcherViewProps = automationServices && activeTab
    ? { tab: activeTab, moduleState, services: automationServices, surface: isWorkspaceSurface ? 'workspace' as const : 'launcher' as const }
    : null;
  const ActiveView = activeTab
    ? Object.hasOwn(launcherViewRegistry, activeTab.kind)
      ? launcherViewRegistry[activeTab.kind as keyof typeof launcherViewRegistry]
      : launcherViewRegistry.reserved
    : launcherViewRegistry.launcher;
  const quickActions: QuickAction[] = (() => {
    const noop = () => undefined;
    const bindings: QuickAction[] = state.bindings.map((binding) => ({
      id: `binding:${binding.id}`,
      label: binding.name,
      keywords: [binding.alias, 'application', 'launch'],
      kind: 'binding',
      scope: 'launcher',
      category: binding.alias ? `Application · alias ${binding.alias}` : 'Application',
      shortcut: binding.alias || undefined,
      targetId: binding.id,
      run: noop,
    }));
    const tabs: QuickAction[] = state.tabs.map((tab) => ({
      id: `tab:${tab.id}`,
      label: `Open ${tab.title}`,
      keywords: [`tab ${tab.slot}`, tab.title, tab.id, 'workspace'],
      kind: 'tab',
      scope: tab.id,
      category: `Workspace tab · ${tab.slot}`,
      shortcut: String(tab.slot),
      targetId: String(tab.slot),
      run: noop,
    }));
    const discoveredCommandIds = new Set(commandCatalog.map(({ scope, command }) => `${scope}:${command.id}`));
    const websiteBootstrap: typeof quickActionBootstrap = websiteShortcuts.map((row) => ({
      scope: 'website-launcher', id: `launch-${row.id}`, label: `Launch ${row.name}`,
      keywords: [row.alias.toLocaleLowerCase() + 'w', row.name, 'websites browser'],
      category: `Website Launcher · alias ${row.alias.toLocaleLowerCase()}w`,
    }));
    const bootstrappedCommands: QuickAction[] = [...quickActionBootstrap, ...websiteBootstrap].flatMap((descriptor) => {
      const tab = state.tabs.find((candidate) => candidate.id === descriptor.scope);
      if (!tab || discoveredCommandIds.has(`${descriptor.scope}:${descriptor.id}`)) return [];
      return [{
        id: `action:${descriptor.scope}:${descriptor.id}`,
        label: descriptor.label,
        keywords: [...descriptor.keywords, tab.title, tab.id],
        kind: 'module' as const,
        scope: descriptor.scope,
        category: `${tab.title} · action`,
        targetId: descriptor.id,
        run: noop,
      }];
    });
    const moduleCommands: QuickAction[] = [...bootstrappedCommands, ...commandCatalog.flatMap(({ scope, command }) => {
      const tab = state.tabs.find((candidate) => candidate.id === scope);
      if (!tab) return [];
      return [{
        id: `action:${scope}:${command.id}`,
        label: command.label,
        keywords: [...(command.keywords ?? []), tab.title, tab.id],
        disabled: command.disabled,
        confirmationPrompt: command.confirmationPrompt,
        kind: 'module' as const,
        scope,
        category: `${tab.title} · action`,
        targetId: command.id,
        run: noop,
      }];
    })];
    const systemActions: QuickAction[] = [
      { id: 'system:add-app', label: 'Add an application', keywords: ['browse', 'launcher', 'catalog'], kind: 'system', scope: 'system', category: 'Launcher', targetId: 'catalog', run: noop },
      { id: 'system:settings', label: 'Open Settings', keywords: ['preferences', 'appearance', 'hotkey'], kind: 'system', scope: 'system', category: 'Automator', targetId: 'settings', run: noop },
    ];
    return [...bindings, ...moduleCommands, ...tabs, ...systemActions];
  })();

  const waitForTabInput = async (tabId: string, timeoutMs = 2500): Promise<HTMLInputElement | null> => {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
      const input = tabId === 'launcher'
        ? searchRef.current
        : document.querySelector<HTMLInputElement>(`[data-tab-command-scope="${CSS.escape(tabId)}"] input`);
      if (input?.isConnected) return input;
      await new Promise<void>((resolve) => setTimeout(resolve, 25));
    }
    return null;
  };

  const runQuickAction = async (action: QuickAction): Promise<void> => {
    if (action.kind === 'binding') {
      const next = await bridge.activateBinding(action.targetId!);
      acceptState(next);
      return;
    }
    if (action.kind === 'system') {
      if (action.targetId === 'settings') openSettings();
      else {
        setQuickActionsHome(false);
        acceptState(await bridge.openCatalog());
      }
      return;
    }

    const tab = state.tabs.find((candidate) => candidate.id === action.scope);
    if (!tab) throw new Error('That workspace tab is no longer available.');
    setQuickActionsHome(false);
    localQueryRef.current = '';
    setLocalQuery('');
    acceptState(await bridge.selectTab(tab.slot));
    if (action.kind === 'tab') {
      const target = await waitForTabInput(tab.id);
      target?.focus({ preventScroll: true });
      if (tab.slot !== 1) window.dispatchEvent(new CustomEvent('automator:focus-tab-search', { detail: { scope: tab.id } }));
      return;
    }

    const deadline = Date.now() + 6000;
    let command: TabCommand | undefined;
    while (Date.now() < deadline) {
      command = resolveLiveCommand(action.scope, action.targetId!);
      if (command) break;
      await new Promise<void>((resolve) => setTimeout(resolve, 25));
    }
    if (!command) throw new Error('That module action is still loading. Open the tab and try again.');
    if (command.disabled) throw new Error('This action is not available in the current module state.');
    if (command.confirmationPrompt && !action.confirmationPrompt) {
      if (!window.confirm(command.confirmationPrompt)) return;
    }
    await command.run();
  };

  const showActivityEntry = (entry: RunActivityEntry) => {
    setSelectedActivityRunId(entry.id);
    setActivityOpen(true);
    setNotificationsOpen(false);
  };

  const navigateToActivityModule = async (entry: RunActivityEntry) => {
    const tab = state.tabs.find((candidate) => candidate.id === entry.moduleId);
    if (!tab) throw new Error('The module for this run is not available in this build.');
    setActivityOpen(false);
    setNotificationsOpen(false);
    setQuickActionsHome(false);
    acceptState(await bridge.selectTab(tab.slot));
  };

  const runActivityFollowUp = async (entry: RunActivityEntry, action: AutomationResult['actions'][number]) => {
    const tab = state.tabs.find((candidate) => candidate.id === entry.moduleId);
    const registered = tab?.actions.find((candidate) => candidate.id === action.id && candidate.version === action.version && !candidate.command);
    if (!tab || !registered) throw new Error('The follow-up action is no longer registered by this module.');
    acceptState(await bridge.selectTab(tab.slot));
    const deadline = Date.now() + 1500;
    let context = await bridge.getWindowContext();
    while (context.selectedTab !== tab.slot && Date.now() < deadline) {
      await new Promise<void>((resolve) => setTimeout(resolve, 25));
      context = await bridge.getWindowContext();
    }
    if (context.selectedTab !== tab.slot) throw new Error('The module did not become active in time.');
    const requestId = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
    const result = await bridge.dispatchModuleAction({
      requestId,
      contractVersion: tab.contractVersion,
      moduleId: tab.id,
      actionId: action.id,
      actionVersion: action.version,
      input: action.payload,
    });
    rememberTransientResult(requestId, tab.id, action.id, result);
    const refreshedActivity = await refreshRunActivity();
    if (refreshedActivity.entries.some((candidate) => candidate.id === requestId)) setSelectedActivityRunId(requestId);
  };

  const preview = host.buildId === 'browser-preview';
  const footer = state.mode === 'catalog'
    ? <span><kbd>/</kbd> filter catalog</span>
    : <span><kbd>1–9</kbd> switch tabs</span>;
  const listTitle = isCatalog ? 'Add an application' : exactMatch ? 'Alias match' : 'Your applications';
  const listDescription = isCatalog ? 'Choose an app to assign a unique letter alias.' : 'Choose an app to launch, or set an alias for quick access.';

  return (
    <GlobalVariablesContext.Provider value={globalVariables}>
    <main
      className="window-shell"
      aria-label="Automator launcher"
      data-theme={state.theme.toLowerCase()}
      data-material={!isWorkspaceSurface && host.acrylicSupported ? 'acrylic' : 'solid'}
      data-surface={isWorkspaceSurface ? 'workspace' : 'launcher'}
      data-native-corners={host.nativeCornersSupported}
      onKeyDownCapture={handleEscape}
    >
      <header className="topbar">
        <div className="brand-lockup">
          <span className="brand-mark"><Sparkles size={17} strokeWidth={2.2} /></span>
          <span className="brand-title">{isWorkspaceSurface ? 'Automator Workspace' : 'Automator'}</span>
          <span className="status-dot" data-ready={host.backendState === 'ready' && state.hookInstalled} aria-label={state.hookInstalled ? 'Ready' : preview ? 'Windows keyboard hook is unavailable in browser preview' : 'Keyboard opener unavailable'} />
          {preview && <span className="preview-badge">Preview</span>}
        </div>
        <div className="topbar-actions">
          {isWorkspaceSurface && <>
            <button className="icon-button" type="button" aria-label="Run activity" aria-pressed={activityOpen} title="Run activity" onClick={() => { setActivityOpen((value) => !value); setNotificationsOpen(false); }}><Activity size={16} /></button>
            <div className="notification-anchor">
              <button className="icon-button notification-trigger" type="button" aria-label="Notifications" aria-expanded={notificationsOpen} title="Notifications" onClick={() => { setNotificationsOpen((value) => !value); setActivityOpen(false); }}><Bell size={16} />{runActivity.entries.length > 0 && <span className="notification-count">{Math.min(runActivity.entries.length, 99)}</span>}</button>
              {notificationsOpen && <div className="notification-popover"><NotificationCenter entries={runActivity.entries} onNavigate={showActivityEntry} /></div>}
            </div>
          </>}
          {isWorkspaceSurface
            ? <button className="icon-button" type="button" aria-label="Hide Workspace" title="Hide Workspace" onClick={() => { if (bridge) void runCommand(() => bridge.close()); }}><X size={16} /></button>
            : <button className="icon-button" type="button" aria-label="Open Workspace" title="Open Workspace" onClick={() => { if (bridge) void runCommand(() => bridge.openWorkspace()); }}><AppWindow size={16} /></button>}
          {!isWorkspaceSurface && <button className="icon-button" type="button" aria-label="Open application catalog" title={state.selectedTab === 1 ? 'Add an app' : 'Switch to Launcher to add an app'} disabled={state.selectedTab !== 1} onClick={() => { if (bridge) void runCommand(() => bridge.openCatalog()); }}>
            <Plus size={17} />
          </button>}
          {!isWorkspaceSurface && <button className="icon-button" type="button" aria-label="Settings" title="Settings" onClick={openSettings}>
              <Settings2 size={17} />
            </button>}
        </div>
      </header>

      {backendError && <div className="backend-error" role="alert"><AlertCircle size={15} /><span>{backendError}</span><button type="button" onClick={() => void retry()} disabled={retrying}>{retrying ? 'Retrying…' : 'Retry'}</button></div>}
      {!preview && !state.hookInstalled && <div className="backend-error hook-warning" role="status"><AlertCircle size={15} /><span>The global opener is unavailable. Retry the Windows hook or change its key in settings.</span><button type="button" onClick={() => void retry()} disabled={retrying}>{retrying ? 'Retrying…' : 'Retry'}</button></div>}
      {preview && !state.hookInstalled && <div className="preview-hook-note" role="status">The Windows keyboard hook is unavailable in browser preview.</div>}

      <Tabs.Root className="tab-root" value={showQuickActions ? '' : String(state.selectedTab)} onValueChange={(value) => changeTab(Number(value))}>
        <Tabs.List className="tabs" aria-label="Workspace tabs">
          {state.tabs.filter((tab) => !isWorkspaceSurface || tab.slot > 1).map((tab) => (
            <Tabs.Trigger key={tab.id} className="tab-button" value={String(tab.slot)} aria-label={`${tab.title}, tab ${tab.slot}`} ref={(element) => { tabRefs.current[tab.slot - 1] = element; }}>
              <span className="tab-number">{tab.slot}</span>{tab.slot === 1 && <span className="tab-name">Launcher</span>}
            </Tabs.Trigger>
          ))}
        </Tabs.List>

        {!showQuickActions && !activityOpen && activeTab && activeTab.slot !== 1 && activeTab.kind !== 'reserved' && <TabCommandBar scope={activeTab.id} />}

        {isWorkspaceSurface && activityOpen ? <div className="activity-workspace-content"><RunActivityView
          entries={runActivity.entries}
          selectedRunId={selectedActivityRunId}
          onNavigate={(entry) => { void navigateToActivityModule(entry).catch((error) => setCommandError(errorMessage(error))); }}
          registeredActionsForModule={(moduleId) => state.tabs.find((tab) => tab.id === moduleId)?.actions ?? []}
          onAction={(entry, action) => runActivityFollowUp(entry, action)}
        />{activeTab && <LatestModuleResult moduleId={activeTab.id} moduleTitle={activeTab.title} />}</div> : <FileExplorerLaunchContext.Provider value={{ request: explorerLaunches[0] ?? null, consume: consumeExplorerLaunch }}><AnimatePresence mode="wait" initial={false}>
          {showQuickActions ? <QuickActionsHome key="quick-actions" actions={quickActions} onRun={runQuickAction}
            onAddApp={() => { setQuickActionsHome(false); void bridge.openCatalog().then(acceptState).catch((error) => setCommandError(errorMessage(error))); }}
            onOpenSettings={openSettings} onClose={() => { void runCommand(() => bridge.close()); }} />
          : launcherViewProps ? <ActiveView key={`${activeTab?.id}-${state.mode}`} {...launcherViewProps}>
            {showSearch && (
              <div className="search-area">
                <label className="search-box" htmlFor={isCatalog ? 'catalog-search' : 'search'}>
                  <Search className="search-icon" size={17} />
                  <input
                    ref={isCatalog ? catalogSearchRef : searchRef}
                    id={isCatalog ? 'catalog-search' : 'search'}
                    type="text"
                    aria-label={isCatalog ? 'Search application catalog' : 'Search applications'}
                    placeholder={isCatalog ? 'Search installed apps' : 'Search apps or type an alias'}
                    value={localQuery}
                    onChange={(event) => updateQuery(event.target.value)}
                    autoComplete="off"
                    spellCheck={false}
                  />
                  <span className="shortcut-hint">{isCatalog ? <X size={12} /> : '/'}</span>
                </label>
                <div className="list-heading">
                  <div><h1>{listTitle}</h1><p>{listDescription}</p></div>
                  {isCatalog && <button className="subtle-action" type="button" onClick={() => bridge.addCustomApp().then((value) => { if (value) acceptState(value); }).catch((error) => setCommandError(errorMessage(error)))}><FolderOpen size={14} /> Browse</button>}
                  {!isCatalog && <button className="subtle-action" type="button" onClick={() => bridge.openCatalog().then(acceptState).catch((error) => setCommandError(errorMessage(error)))}><Plus size={14} /> Add app</button>}
                </div>
              </div>
            )}

            {isCatalog && state.catalogTotalCount > state.catalogApps.length && (
              <div className="catalog-count" role="status">
                Showing {state.catalogApps.length} of {state.catalogTotalCount} installed apps. Search to narrow the full catalog.
              </div>
            )}

            {isAliasEditing && (
              <section className="alias-editor" aria-label="Assign application alias">
                <div className="alias-editor-heading">
                  <AppIcon binding={aliasCandidate!} />
                  <div><h1>Assign an alias</h1><p>{aliasCandidate!.name}</p></div>
                  <button className="icon-button" type="button" aria-label="Cancel alias editing" onClick={() => void bridge.setMode(isCatalog ? 'catalog' : 'launcher')}><X size={16} /></button>
                </div>
                <label className="alias-entry" htmlFor="alias-input">
                  <span>Unique letters only</span>
                  <input ref={aliasRef} id="alias-input" value={localAlias} maxLength={32} autoComplete="off" spellCheck={false} onChange={(event) => setLocalAlias(event.target.value.replace(/[^a-z]/gi, ''))} onKeyDown={(event) => { if (event.key === 'Enter') { event.preventDefault(); void runCommand(() => bridge.saveAlias(aliasCandidate!.id, localAlias)); } }} />
                </label>
                <p className="alias-hint">Type the alias into the launcher to start this application after a short pause.</p>
                <div className="alias-actions">
                  <button className="secondary-button" type="button" onClick={() => void runCommand(() => bridge.setMode(isCatalog ? 'catalog' : 'launcher'))}>Cancel</button>
                  <button className="primary-button" type="button" disabled={!/^[a-z]{1,32}$/i.test(localAlias)} onClick={() => void runCommand(() => bridge.saveAlias(aliasCandidate!.id, localAlias))}><Check size={15} /> Save alias</button>
                </div>
              </section>
            )}

            {!showSearch && !isAliasEditing && (
              <div className="reserved-spacer" aria-hidden="true" />
            )}

            {showSearch && (
              <div className="app-list" ref={mainListRef} aria-live="polite" aria-busy={state.catalogLoading}>
                {state.catalogLoading ? (
                  <div className="list-loading"><span /><span /><span /></div>
                ) : visibleBindings.length > 0 ? visibleBindings.map((binding) => (
                  <AppRow
                    key={binding.id}
                    binding={binding}
                    matched={binding.id === exactMatch}
                    isCatalog={isCatalog}
                    onActivate={() => isCatalog
                      ? editAlias(binding, true)
                      : void runCommand(() => bridge.activateBinding(binding.id))}
                    onEditAlias={() => editAlias(binding, isCatalog)}
                    onRemove={() => void runCommand(() => bridge.removeBinding(binding.id))}
                  />
                )) : (
                  <div className="empty-state">
                    <span className="empty-icon">{isCatalog ? <FolderOpen size={21} /> : <Sparkles size={21} />}</span>
                    <h2>{isCatalog ? (localQuery ? 'No matching applications' : 'Catalog is empty') : (state.bindings.length === 0 ? 'No applications yet' : 'No matching apps')}</h2>
                    <p>{isCatalog ? 'Try a different search or browse to an application shortcut.' : state.bindings.length === 0 ? 'Add an application and give it a short alias.' : 'Search by application name or alias.'}</p>
                    {!isCatalog && <button className="primary-button empty-cta" type="button" onClick={() => void bridge.openCatalog().then(acceptState).catch((error) => setCommandError(errorMessage(error)))}><Plus size={15} /> Add an app</button>}
                    {isCatalog && <button className="secondary-button empty-cta" type="button" onClick={() => void bridge.addCustomApp().then((value) => { if (value) acceptState(value); }).catch((error) => setCommandError(errorMessage(error)))}><FolderOpen size={15} /> Browse files</button>}
                  </div>
                )}
              </div>
            )}
          </ActiveView> : <div className="module-service-loading" role="status" aria-live="polite">Preparing module services…</div>}
        </AnimatePresence></FileExplorerLaunchContext.Provider>}
      </Tabs.Root>

      {commandError && <div className="command-error" role="alert"><AlertCircle size={14} /><span>{commandError}</span><button type="button" aria-label="Dismiss error" onClick={() => setCommandError('')}><X size={13} /></button></div>}

      <footer className="footer">
        {footer}
        {exactMatch && <span className="match-note"><Check size={12} /> Alias ready</span>}
        <span><kbd>Esc</kbd> close</span>
      </footer>

      <SettingsDialog
        bridge={bridge}
        state={state}
        open={settingsOpen}
        onOpenChange={closeSettings}
        focusRef={settingsFocusRef}
        reportError={setCommandError}
        portalContainer={portalContainer}
        loadGlobalVariables={loadGlobalVariables}
        saveGlobalVariables={saveGlobalVariables}
        onGlobalVariablesChanged={setGlobalVariables}
      />
      <div className="dialog-portal-root" ref={setPortalContainer} />
    </main>
    </GlobalVariablesContext.Provider>
  );
}

function RecoveryView({ title, message, preview, retry }: { title: string; message: string; preview: boolean; retry?: () => void }) {
  return (
    <main className="window-shell recovery-shell" data-theme="light">
      <span className="recovery-mark"><AlertCircle size={19} /></span>
      <h1>{title}</h1>
      <p>{message}</p>
      {preview && <span className="preview-badge">Preview</span>}
      {retry && <button className="primary-button" onClick={retry}><ChevronRight size={15} /> Retry</button>}
    </main>
  );
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}
