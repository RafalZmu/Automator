import { Check, ChevronDown, ChevronRight, CircleAlert, FilePlus2, Code2, FolderOpen, Loader2, LoaderCircle, MonitorPlay, Play, Plus, RefreshCw, Square, Tag, Trash2, X } from 'lucide-react';
import { useCallback, useEffect, useRef, useState, useSyncExternalStore, type FormEvent } from 'react';
import type { BackendUiState } from '../../contracts/rpc';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import type { AutomationServices } from '../automationServices';
import {
  countPlaywrightOutcomes, failureReasonHints, failureReasonLabels, outcomeStatusLabels, readBrowserProfiles, readPlaywrightExplorerState, readPlaywrightRunHistory,
  validatePlaywrightSectionPath, type PlaywrightActiveRun, type PlaywrightExplorerState, type PlaywrightRunKind, type PlaywrightRunRecord,
  type BrowserAutomationProfile, type PlaywrightRunState, type PlaywrightTestIdentity, type PlaywrightTestOutcome,
} from '../contracts/browser';
import { SideHistoryContent, SideHistoryToggle, useHistorySidePanel } from '../components/SideHistoryPanel';
import { cancelBrowserRun, startBrowserRun, useBrowserRun } from './browserRunStore';

type ViewProps = {
  tab: BackendUiState['tabs'][number];
  moduleState: BackendUiState['moduleStates'][number] | undefined;
  services: AutomationServices;
  surface?: 'launcher' | 'workspace';
};

const styles = `
.playwright-view{gap:8px}.playwright-heading{display:flex;align-items:center;gap:9px;flex:none}.playwright-heading-copy{flex:1;min-width:0}.playwright-heading h1{margin:0;color:var(--ink);font-size:12px;font-weight:680}.playwright-heading p{margin:3px 0 0;color:var(--muted);font-size:9px}.playwright-project{flex:none;display:flex;align-items:center;gap:6px;padding:7px;border:1px solid var(--line);border-radius:9px;background:rgba(255,255,255,.28)}.window-shell[data-theme="dark"] .playwright-project{background:rgba(16,25,29,.28)}.playwright-project input{flex:1;min-width:0;height:28px;padding:5px 7px;border:1px solid var(--line);border-radius:7px;outline:0;color:var(--ink);background:var(--control);font:9px inherit}.playwright-project input:focus,.playwright-inline-editor input:focus{border-color:var(--accent);box-shadow:0 0 0 2px rgba(25,139,162,.1)}.playwright-project .secondary-button{flex:none}.playwright-info{display:flex;align-items:center;gap:6px;margin:0;padding:7px 9px;border:1px solid var(--line);border-radius:8px;color:var(--muted-strong);background:rgba(255,255,255,.27);font-size:9px;line-height:1.4}.playwright-info[data-error="true"]{color:var(--danger);background:rgba(255,240,238,.48)}.playwright-toolbar{display:flex;align-items:center;gap:6px;flex:none}.playwright-toolbar .spacer{flex:1}.playwright-toolbar select{height:29px;min-width:105px;padding:4px 7px;border:1px solid var(--line);border-radius:7px;color:var(--ink);background:var(--control);font:9px inherit}.playwright-runner{font-size:8px;color:var(--muted);white-space:nowrap}.playwright-list{flex:1;min-height:0;display:flex;flex-direction:column;gap:7px;overflow:auto;padding:1px 2px 3px 0;scrollbar-width:thin}.playwright-file{flex:none;border:1px solid var(--line);border-radius:10px;overflow:hidden;background:rgba(255,255,255,.26)}.window-shell[data-theme="dark"] .playwright-file{background:rgba(16,25,29,.22)}.playwright-file summary{display:flex;align-items:center;gap:6px;min-height:38px;padding:6px 8px;cursor:pointer;list-style:none}.playwright-file summary::-webkit-details-marker{display:none}.playwright-file-name{flex:1;min-width:0;overflow:hidden;color:var(--ink);font:9px "Cascadia Code",Consolas,monospace;text-overflow:ellipsis;white-space:nowrap}.playwright-file-count{color:var(--muted);font-size:8px;white-space:nowrap}.playwright-file-tools{display:flex;gap:3px}.playwright-test{display:flex;flex-direction:column;gap:4px;padding:7px 8px;border-top:1px solid var(--line)}.playwright-test-main{display:flex;align-items:center;gap:6px;min-width:0}.playwright-test-copy{flex:1;min-width:0}.playwright-test-title{overflow:hidden;color:var(--ink);font-size:9px;text-overflow:ellipsis;white-space:nowrap}.playwright-test-meta{margin-top:2px;color:var(--muted);font-size:7px}.playwright-test-actions{display:flex;align-items:center;gap:2px}.playwright-test-actions .icon-button{width:26px;height:26px}.playwright-tag-list{display:flex;flex-wrap:wrap;gap:4px}.playwright-tag-suggestion{cursor:pointer;font:inherit;font-size:9px}.playwright-tag-suggestion:hover{border-color:var(--accent)}.playwright-code{max-height:260px;margin:0;padding:6px 8px;overflow:auto;border:1px solid var(--line);border-radius:7px;color:var(--muted-strong);background:rgba(255,255,255,.3);font:10px/1.45 "Cascadia Code",Consolas,monospace;white-space:pre}.playwright-tag{padding:2px 6px;border:1px solid rgba(26,131,151,.18);border-radius:99px;color:var(--accent-strong);background:var(--accent-soft);font-size:7px}.playwright-inline-editor{display:flex;align-items:center;gap:5px}.playwright-inline-editor input{flex:1;min-width:80px;height:25px;padding:4px 6px;border:1px solid var(--line);border-radius:6px;color:var(--ink);background:var(--control);font:8px inherit}.playwright-inline-editor small{color:var(--muted);font-size:7px}.playwright-form{flex:none;display:flex;flex-direction:column;gap:7px;padding:8px;border:1px solid var(--line);border-radius:9px;background:rgba(255,255,255,.32)}.playwright-form header{display:flex;align-items:center;gap:6px;color:var(--ink);font-size:9px}.playwright-form header strong{flex:1}.playwright-form-fields{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr);gap:6px}.playwright-form label{display:flex;flex-direction:column;gap:3px;color:var(--muted);font-size:8px}.playwright-form label:first-child{grid-column:1/-1}.playwright-form input{min-width:0;height:28px;padding:5px 7px;border:1px solid var(--line);border-radius:7px;color:var(--ink);background:var(--control);font:9px inherit}.playwright-form-actions{display:flex;justify-content:flex-end;gap:5px}.playwright-empty{flex:1;min-height:100px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;color:var(--muted);text-align:center}.playwright-empty strong{color:var(--ink);font-size:10px}.playwright-empty p{max-width:320px;margin:0;font-size:8px;line-height:1.5}.playwright-stale{flex:none;display:flex;align-items:center;gap:7px;padding:7px;border:1px solid var(--line);border-radius:8px;color:var(--muted-strong);font-size:8px}.playwright-stale span{flex:1}.playwright-run-result{flex:0 1 auto;min-height:0;max-height:46%;display:flex;flex-direction:column;gap:7px;padding:9px;border:1px solid var(--line);border-radius:9px;background:rgba(255,255,255,.28);overflow:auto}.playwright-run-result header{display:flex;align-items:center;gap:8px;color:var(--ink)}.playwright-run-title{flex:1;min-width:0;display:flex;flex-direction:column;gap:2px}.playwright-run-title strong{overflow:hidden;color:var(--ink);font-size:12px;font-weight:650;text-overflow:ellipsis;white-space:nowrap}.playwright-run-title small{color:var(--muted);font-size:10px}.playwright-run-counts{display:flex;flex-wrap:wrap;gap:4px}.playwright-count{padding:2px 8px;border-radius:9px;color:var(--muted-strong);background:rgba(90,126,135,.14);font-size:10px;font-weight:650;white-space:nowrap}.playwright-count.is-passed{color:#1f7a5a;background:rgba(47,166,123,.16)}.playwright-count.is-failed{color:var(--danger,#b42318);background:rgba(180,35,24,.12)}.playwright-count.is-timedOut{color:#8a5300;background:rgba(214,139,20,.18)}.playwright-count.is-flaky{color:#5b3fb8;background:rgba(111,86,214,.14)}.playwright-outcome-list{display:flex;flex-direction:column;gap:6px}.playwright-outcome-row{display:flex;flex-direction:column;gap:4px;padding:8px 9px;border:1px solid var(--line);border-left:3px solid var(--danger,#b42318);border-radius:8px;background:rgba(255,255,255,.3)}.playwright-outcome-row[data-status="timedOut"]{border-left-color:#d68b14}.playwright-outcome-row[data-status="flaky"]{border-left-color:#6f56d6}.playwright-outcome-row[data-status="passed"],.playwright-outcome-row[data-status="skipped"]{border-left-color:var(--line)}.playwright-outcome-head{display:flex;align-items:center;gap:7px;min-width:0}.playwright-outcome-head strong{flex:1;min-width:0;overflow:hidden;color:var(--ink);font-size:11px;font-weight:650;text-overflow:ellipsis;white-space:nowrap}.playwright-outcome-status{flex:none;padding:1px 7px;border-radius:9px;color:var(--muted-strong);background:rgba(90,126,135,.14);font-size:10px;font-weight:650;white-space:nowrap}.playwright-outcome-status.is-failed,.playwright-outcome-status.is-timedOut{color:var(--danger,#b42318);background:rgba(180,35,24,.12)}.playwright-outcome-status.is-timedOut{color:#8a5300;background:rgba(214,139,20,.18)}.playwright-outcome-status.is-passed{color:#1f7a5a;background:rgba(47,166,123,.16)}.playwright-outcome-status.is-flaky{color:#5b3fb8;background:rgba(111,86,214,.14)}.playwright-outcome-where{margin:0;color:var(--muted-strong);font:10px/1.45 "Cascadia Code",Consolas,monospace;overflow-wrap:anywhere}.playwright-outcome-where code{font:inherit;color:var(--ink)}.playwright-outcome-hint{margin:0;color:var(--muted);font-size:10px;line-height:1.45}.playwright-snippet{color:var(--muted-strong);font-size:10px}.playwright-snippet summary,.playwright-quiet summary{cursor:pointer;padding:3px 0}.playwright-snippet pre{max-height:150px;margin:4px 0 0;padding:6px 8px;overflow:auto;border:1px solid var(--line);border-radius:7px;color:var(--muted-strong);background:rgba(255,255,255,.3);font:10px/1.45 "Cascadia Code",Consolas,monospace;white-space:pre}.playwright-quiet{color:var(--muted-strong);font-size:11px}.playwright-view .script-run-state{min-width:64px;padding:2px 8px;font-size:10px}.playwright-view .icon-button{flex:none}.playwright-view .icon-button:disabled{opacity:.45}.playwright-reason{flex:none;padding:2px 8px;border-radius:9px;color:var(--danger,#b42318);background:rgba(180,35,24,.1);font-size:10px;font-style:normal;font-weight:650;white-space:nowrap}.playwright-error{max-height:150px;margin:0;padding:6px 8px;overflow:auto;border:1px solid var(--line);border-radius:7px;color:var(--muted-strong);background:rgba(255,255,255,.3);font:10px/1.45 "Cascadia Code",Consolas,monospace;white-space:pre-wrap;word-break:break-word}.playwright-history{flex:none;min-height:0;display:flex;flex-direction:column;gap:6px;padding:8px 9px;border:1px solid var(--line);border-radius:9px;background:rgba(255,255,255,.28)}.playwright-history[data-open="true"]{flex:0 1 55%;max-height:55%}.playwright-history-body{flex:1;min-height:0;overflow:auto}.playwright-history header{display:flex;align-items:center;gap:8px;color:var(--ink)}.playwright-history-toggle{flex:1;min-width:0;display:flex;align-items:center;gap:7px;padding:4px 2px;border:0;background:transparent;color:var(--ink);font:inherit;text-align:left;cursor:pointer}.playwright-history-toggle strong{font-size:12px;font-weight:650;white-space:nowrap}.playwright-history-toggle small{flex:1;min-width:0;overflow:hidden;color:var(--muted);font-size:10px;text-overflow:ellipsis;white-space:nowrap}.playwright-history ul{display:flex;flex-direction:column;gap:3px;margin:0;padding:0;list-style:none}.playwright-history-item{width:100%;display:flex;align-items:center;flex-wrap:wrap;gap:8px;padding:7px 9px;border:1px solid transparent;border-radius:7px;color:var(--ink);background:transparent;font:11px inherit;text-align:left;cursor:pointer}.playwright-history-item:hover,.playwright-history-item[aria-pressed="true"]{border-color:var(--line);background:rgba(25,139,162,.08)}.playwright-history-item .playwright-history-copy{flex:1;min-width:0;display:flex;flex-direction:column;gap:2px}.playwright-history-item strong{min-width:0;overflow:hidden;font-size:11px;font-weight:620;text-overflow:ellipsis;white-space:nowrap}.playwright-history-item small{color:var(--muted);font-size:10px;white-space:nowrap}.playwright-history-item .playwright-run-counts{flex-basis:100%}
@media(max-width:430px){.playwright-project{flex-wrap:wrap}.playwright-project input{flex-basis:100%}.playwright-toolbar{flex-wrap:wrap}.playwright-form-fields{grid-template-columns:1fr}.playwright-form label:first-child{grid-column:auto}}.side-panel-inner .playwright-history-item{width:100%;display:flex;align-items:center;flex-wrap:wrap;gap:8px;padding:7px 9px;border:1px solid transparent;border-radius:7px;color:var(--ink);background:transparent;font:11px inherit;text-align:left;cursor:pointer}.side-panel-inner .playwright-history-item:hover,.side-panel-inner .playwright-history-item[aria-pressed="true"]{border-color:var(--line);background:rgba(25,139,162,.08)}.side-panel-inner .playwright-history-copy{flex:1;min-width:0;display:flex;flex-direction:column;gap:2px}.side-panel-inner .playwright-history-item strong{min-width:0;overflow:hidden;font-size:11px;font-weight:620;text-overflow:ellipsis;white-space:nowrap}.side-panel-inner .playwright-history-item small{color:var(--muted);font-size:10px;white-space:nowrap}.side-panel-inner .playwright-run-counts{flex-basis:100%}
`;

const stateLabels: Record<PlaywrightRunState, string> = {
  success: 'Passed', failed: 'Failed', timedOut: 'Timed out', canceled: 'Canceled', error: 'Error',
};

function formatDuration(milliseconds: number): string {
  return milliseconds < 1000 ? `${milliseconds} ms` : `${(milliseconds / 1000).toFixed(1)} s`;
}

function formatRunTime(value: string | number): string {
  return new Date(value).toLocaleString([], { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

// UI mode outlives this view (the compact window can unmount it), so its open state is kept at module level.
let uiModeActive = false;
const uiModeListeners = new Set<() => void>();
const setUiModeActive = (value: boolean) => { uiModeActive = value; for (const listener of uiModeListeners) listener(); };
const useUiModeActive = () => useSyncExternalStore((listener) => { uiModeListeners.add(listener); return () => { uiModeListeners.delete(listener); }; }, () => uiModeActive);

const historyOpenKey = 'automator.playwrightRunHistoryOpen';


/** Failing outcomes first, the most urgent at the top. */
const outcomeOrder: Record<string, number> = { timedOut: 0, failed: 1, interrupted: 2, flaky: 3, passed: 4, skipped: 5 };

function RunCountChips({ run }: { run: PlaywrightRunRecord }) {
  const counts = countPlaywrightOutcomes(run);
  const chips: Array<{ key: keyof typeof counts; text: string }> = [
    { key: 'passed', text: 'passed' }, { key: 'flaky', text: 'flaky' }, { key: 'failed', text: 'failed' },
    { key: 'timedOut', text: 'timed out' }, { key: 'skipped', text: 'skipped' }, { key: 'interrupted', text: 'interrupted' },
  ];
  const shown = chips.filter((chip) => counts[chip.key] > 0);
  if (shown.length === 0) return null;
  return <span className="playwright-run-counts">
    {shown.map((chip) => <span key={chip.key} className={`playwright-count is-${chip.key}`}>{counts[chip.key]} {chip.text}</span>)}
  </span>;
}

function OutcomeRow({ test }: { test: PlaywrightTestOutcome }) {
  const reason = test.reason;
  return <article className="playwright-outcome-row" data-status={test.status}>
    <div className="playwright-outcome-head">
      <span className={`playwright-outcome-status is-${test.status}`}>{outcomeStatusLabels[test.status] ?? test.status}</span>
      <strong title={test.title}>{test.title}</strong>
      {reason && <span className="playwright-reason" data-reason={reason}>{failureReasonLabels[reason]}</span>}
    </div>
    <p className="playwright-outcome-where">
      {test.project ? `[${test.project}] ` : ''}{test.file}:{test.failureLine ?? test.line}
      {test.step ? <> · at <code>{test.step}</code></> : null}
      {test.attempts > 1 ? ` · ${test.attempts} attempts` : ''}
    </p>
    {reason && <p className="playwright-outcome-hint">{failureReasonHints[reason]}</p>}
    {test.status === 'flaky' && <p className="playwright-outcome-hint">Passed on a retry. The error below is from the attempt that failed.</p>}
    {test.error && <pre className="playwright-error">{test.error}</pre>}
    {test.snippet && <details className="playwright-snippet"><summary>Code around the failure</summary><pre>{test.snippet}</pre></details>}
  </article>;
}

function ElapsedTime({ since }: { since: number }) {
  const [now, setNow] = useState(Date.now());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);
  return <>{Math.max(0, Math.floor((now - since) / 1000))} s</>;
}

function displayTestTitle(test: PlaywrightTestIdentity): string {
  return test.titlePath.join(' › ');
}

function safeTagName(value: string): string {
  return value.trim().replace(/[^A-Za-z0-9 ._-]+/g, '').slice(0, 48);
}

export function BrowserAutomationView({ tab, services, surface }: ViewProps) {
  const [explorer, setExplorer] = useState<PlaywrightExplorerState | null>(null);
  const [projectDraft, setProjectDraft] = useState('');
  const [notice, setNotice] = useState('');
  const [noticeIsError, setNoticeIsError] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [form, setForm] = useState<{ file: string; testName: string } | null>(null);
  const [formKind, setFormKind] = useState<'section' | 'test'>('section');
  const [tagEditorId, setTagEditorId] = useState<string | null>(null);
  const [tagDraft, setTagDraft] = useState('');
  const [selectedTag, setSelectedTag] = useState('');
  const [codeView, setCodeView] = useState<{ testId: string; startLine: number; code: string } | null>(null);
  const [history, setHistory] = useState<PlaywrightRunRecord[]>([]);
  const [backendActive, setBackendActive] = useState<PlaywrightActiveRun[]>([]);
  const [shownRunId, setShownRunId] = useState<string | null>(null);
  const uiModeOpen = useUiModeActive();
  const [profiles, setProfiles] = useState<BrowserAutomationProfile[]>([]);
  const { active: activeRun, last: lastRun } = useBrowserRun();
  const sidePanel = useHistorySidePanel(surface, historyOpenKey);
  const { open: historyOpen, setOpen: setHistoryOpen, panelMode: sidePanelMode } = sidePanel;
  const mounted = useRef(true);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);

  const showResult = useCallback((result: { status: string; message: string; data: unknown }) => {
    if (result.status === 'error') throw new Error(result.message);
    setNotice(result.message);
    setNoticeIsError(result.status === 'warning' && /could not|failed|unavailable|not found|exceeded/i.test(result.message));
    return result.data;
  }, []);

  const loadProfiles = useCallback(async () => {
    try {
      const result = await services.modules.dispatch('listProfiles', {});
      if (mounted.current && result.status !== 'error') setProfiles(readBrowserProfiles(result.data));
    } catch { /* The profile list is optional; the explorer keeps working without it. */ }
  }, [services]);
  useEffect(() => { void loadProfiles(); }, [loadProfiles]);

  const setAiAccess = async (profile: BrowserAutomationProfile, aiAccess: boolean) => {
    setBusy('profile');
    try {
      const result = await services.modules.dispatch('saveProfile', { ...profile, aiAccess });
      if (result.status === 'error') throw new Error(result.message);
      await loadProfiles();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not update the browser profile.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const loadState = useCallback(async () => {
    let discovering = false;
    try {
      const result = await services.modules.dispatch('getExplorerState', {});
      const data = result.data && typeof result.data === 'object' ? result.data as Record<string, unknown> : {};
      const parsed = readPlaywrightExplorerState(data.state);
      if (!parsed) throw new Error('Playwright Explorer returned invalid saved state.');
      setExplorer(parsed);
      setProjectDraft(parsed.projectRoot ?? '');
      setSelectedTag((current) => current && parsed.tags.includes(current) ? current : parsed.tags[0] ?? '');
      if (result.status !== 'error') {
        setNotice(parsed.projectRoot && parsed.runner.available ? 'Loading tests…' : parsed.runner.message);
        setNoticeIsError(false);
      }
      if (parsed.projectRoot && parsed.runner.available) {
        discovering = true;
        setBusy('discover');
        const discoveryResult = await services.modules.dispatch('discoverTests', {});
        const discoveryData = showResult(discoveryResult);
        const payload = discoveryData && typeof discoveryData === 'object' ? discoveryData as Record<string, unknown> : {};
        const discovered = readPlaywrightExplorerState(payload.state);
        if (!discovered) throw new Error('Playwright test discovery returned invalid data.');
        setExplorer(discovered);
        setProjectDraft(discovered.projectRoot ?? '');
        setSelectedTag((current) => current && discovered.tags.includes(current) ? current : discovered.tags[0] ?? '');
      }
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not load the Playwright Test Explorer.');
      setNoticeIsError(true);
    } finally {
      if (discovering) setBusy((current) => current === 'discover' ? null : current);
    }
  }, [services, showResult]);

  const refreshTests = useCallback(async () => {
    if (busy) return;
    setBusy('discover');
    try {
      const result = await services.modules.dispatch('discoverTests', {});
      const data = showResult(result);
      const payload = data && typeof data === 'object' ? data as Record<string, unknown> : {};
      const parsed = readPlaywrightExplorerState(payload.state);
      if (!parsed) throw new Error('Playwright test discovery returned invalid data.');
      setExplorer(parsed);
      setProjectDraft(parsed.projectRoot ?? '');
      setSelectedTag((current) => current && parsed.tags.includes(current) ? current : parsed.tags[0] ?? '');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not discover Playwright tests.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  }, [busy, services, showResult]);

  const loadHistory = useCallback(async () => {
    try {
      const result = await services.modules.dispatch('listRunHistory', {});
      const parsed = readPlaywrightRunHistory(result.data);
      if (!parsed || !mounted.current) return;
      setHistory(parsed.runs);
      setBackendActive(parsed.active);
    } catch {
      /* History is informational; the explorer keeps working without it. */
    }
  }, [services]);

  useEffect(() => { void loadState(); }, [loadState]);
  useEffect(() => { void loadHistory(); }, [loadHistory, lastRun?.id]);
  // A run started by another window or an agent is only visible through the backend, so follow it until it ends.
  const foreignRunning = !activeRun && backendActive.length > 0;
  useEffect(() => {
    if (!foreignRunning) return undefined;
    const timer = window.setInterval(() => void loadHistory(), 2000);
    return () => window.clearInterval(timer);
  }, [foreignRunning, loadHistory]);
  useEffect(() => {
    if (lastRun) { setNotice(lastRun.message); setNoticeIsError(lastRun.state === 'error'); }
  }, [lastRun?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  const clearHistory = async () => {
    if (busy) return;
    try {
      showResult(await services.modules.dispatch('deleteRunHistory', {}));
      setShownRunId(null);
      await loadHistory();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not clear the run history.');
      setNoticeIsError(true);
    }
  };

  const chooseProject = async () => {
    if (busy) return;
    try {
      const path = await services.files.pickBrowserProjectDirectory();
      if (path) setProjectDraft(path);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not open the project folder picker.');
      setNoticeIsError(true);
    }
  };

  const saveProject = async (event?: FormEvent) => {
    event?.preventDefault();
    if (!projectDraft.trim() || busy) return;
    setBusy('project');
    try {
      const result = await services.modules.dispatch('setProject', { projectRoot: projectDraft.trim() });
      const data = showResult(result) as Record<string, unknown>;
      const parsed = readPlaywrightExplorerState(data.state);
      if (!parsed) throw new Error('The saved project state was invalid.');
      setExplorer(parsed);
      setProjectDraft(parsed.projectRoot ?? '');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save the selected project.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const openCreateSection = () => {
    setFormKind('section');
    setForm({ file: 'tests/new.spec.ts', testName: 'new test' });
  };

  const openAppendTest = (file: string) => {
    setFormKind('test');
    setForm({ file, testName: 'new test' });
  };

  const saveTestFile = async (event: FormEvent) => {
    event.preventDefault();
    if (!form || busy) return;
    const normalized = validatePlaywrightSectionPath(form.file);
    if (!normalized) {
      setNotice('Use a project-relative *.spec.ts or *.test.ts file path without `..` segments.');
      setNoticeIsError(true);
      return;
    }
    setBusy('file');
    try {
      const result = await services.modules.dispatch(formKind === 'section' ? 'createSection' : 'appendTest', {
        file: normalized, testName: form.testName.trim(),
      });
      showResult(result);
      setForm(null);
      await refreshTests();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not update the Playwright test file.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const editTags = (test: PlaywrightTestIdentity) => {
    setTagEditorId(test.id);
    setTagDraft(test.tags.join(', '));
  };

  const tagSuggestions = (): string[] => {
    const parts = tagDraft.split(',');
    const typed = safeTagName(parts[parts.length - 1] ?? '').toLowerCase();
    const chosen = new Set(parts.slice(0, -1).map((part) => safeTagName(part).toLowerCase()));
    return (explorer?.tags ?? []).filter((tag) => !chosen.has(tag.toLowerCase()) && (!typed || (tag.toLowerCase().includes(typed) && tag.toLowerCase() !== typed))).slice(0, 12);
  };

  const pickSuggestion = (tag: string) => setTagDraft((draft) => {
    const parts = draft.split(',').map((part) => part.trim()).filter(Boolean);
    if (draft.trim() && !/,\s*$/.test(draft)) parts.pop();
    return [...parts, tag].join(', ') + ', ';
  });

  const toggleCode = async (test: PlaywrightTestIdentity) => {
    if (codeView?.testId === test.id) { setCodeView(null); return; }
    if (busy) return;
    setBusy(`code:${test.id}`);
    try {
      const result = await services.modules.dispatch('readTestSource', { testId: test.id });
      const data = showResult(result) as { startLine?: number; code?: string } | undefined;
      if (typeof data?.code === 'string') setCodeView({ testId: test.id, startLine: typeof data.startLine === 'number' ? data.startLine : test.line, code: data.code });
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not load the test code.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const saveTags = async (test: PlaywrightTestIdentity) => {
    if (busy) return;
    const tags = [...new Set(tagDraft.split(',').map(safeTagName).filter(Boolean))].slice(0, 16);
    setBusy(`tags:${test.id}`);
    try {
      const result = await services.modules.dispatch('saveTestTags', { testId: test.id, tags });
      showResult(result);
      setExplorer((current) => {
        if (!current) return current;
        const files = current.files.map((file) => ({ ...file, tests: file.tests.map((candidate) => candidate.id === test.id ? { ...candidate, tags } : candidate) }));
        const knownTags = [...new Set(files.flatMap((file) => file.tests.flatMap((candidate) => candidate.tags)))].sort((a, b) => a.localeCompare(b));
        return { ...current, files, tags: knownTags };
      });
      setTagEditorId(null);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save test tags.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const run = async (kind: PlaywrightRunKind, target: string, label: string) => {
    if (busy || activeRun || foreignRunning) return;
    // Dispatch through the bridge, not `services`: services are disposed when the tab changes, which would abort the run.
    const bridge = window.automator;
    const action = tab.actions.find((candidate) => candidate.id === 'runTests');
    if (!bridge || !action) {
      setNotice('Running Playwright tests is not available in this window.');
      setNoticeIsError(true);
      return;
    }
    setShownRunId(null);
    setNotice('');
    try {
      await startBrowserRun({ bridge, moduleId: tab.id, contractVersion: tab.contractVersion, action }, kind, label,
        { [kind === 'file' ? 'file' : kind === 'test' ? 'testId' : 'tag']: target });
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Playwright run failed.');
      setNoticeIsError(true);
    } finally {
      if (mounted.current) void loadHistory();
    }
  };

  const openUiMode = async () => {
    if (busy || activeRun || foreignRunning || uiModeOpen) return;
    // Like runs, dispatch through the bridge: the backend keeps UI mode alive until its window closes, even if this tab unmounts.
    const bridge = window.automator;
    const action = tab.actions.find((candidate) => candidate.id === 'openUiMode');
    if (!bridge || !action) {
      setNotice('Playwright UI mode is not available in this window.');
      setNoticeIsError(true);
      return;
    }
    setUiModeActive(true);
    setNotice('Playwright UI mode is open in its own window. Close it to run tests from here again.');
    setNoticeIsError(false);
    try {
      const result = await bridge.dispatchModuleAction({
        requestId: globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`,
        contractVersion: tab.contractVersion, moduleId: tab.id, actionId: action.id, actionVersion: action.version, input: {},
      });
      if (!mounted.current) return;
      setNotice(result.message);
      setNoticeIsError(result.status === 'error');
    } catch (error) {
      if (!mounted.current) return;
      setNotice(error instanceof Error ? error.message : 'Playwright UI mode failed.');
      setNoticeIsError(true);
    } finally {
      setUiModeActive(false);
    }
  };

  const removeStaleTags = async () => {
    if (busy || !explorer?.staleTags.length) return;
    setBusy('stale');
    try {
      const result = await services.modules.dispatch('removeStaleTags', {});
      showResult(result);
      await refreshTests();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not remove stale test tags.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const locked = !!busy || !!activeRun || foreignRunning || uiModeOpen;
  const commands = [
    ...(activeRun ? [{ id: 'cancel-playwright-run', label: 'Cancel running Playwright run', keywords: ['stop', 'terminate'], disabled: activeRun.canceling, run: () => cancelBrowserRun() }] : []),
    { id: 'open-ui-mode', label: 'Open Playwright tests in UI mode', keywords: ['ui', 'debug', 'watch'], disabled: !explorer?.projectRoot || !explorer.runner.available || locked, run: openUiMode },
    { id: 'refresh-tests', label: 'Refresh Playwright tests', keywords: ['discover', 'reload'], disabled: !explorer?.projectRoot || !!busy, run: refreshTests },
    { id: 'choose-project', label: 'Choose Playwright project folder', keywords: ['folder', 'directory'], disabled: !!busy, run: chooseProject },
    { id: 'create-section', label: 'Create Playwright test section', keywords: ['new file', 'spec'], disabled: !explorer?.projectRoot || !!busy, run: openCreateSection },
    ...((explorer?.files ?? []).flatMap((file) => [
      { id: `run-file:${file.path}`, label: `Run section ${file.path}`, keywords: ['playwright test file'], disabled: locked, confirmationPrompt: `Run all Playwright tests in ${file.path}?`, run: () => run('file', file.path, file.path) },
      { id: `append:${file.path}`, label: `Add test to ${file.path}`, keywords: ['new test'], disabled: !!busy, run: () => openAppendTest(file.path) },
      ...file.tests.map((test) => ({ id: `run-test:${test.id}`, label: `Run ${displayTestTitle(test)}`, keywords: ['playwright test', file.path], disabled: locked, confirmationPrompt: `Run “${displayTestTitle(test)}”?`, run: () => run('test', test.id, displayTestTitle(test)) })),
    ])),
    ...(explorer?.tags ?? []).map((tag) => ({ id: `run-tag:${tag}`, label: `Run tag group ${tag}`, keywords: ['playwright tests'], disabled: locked, confirmationPrompt: `Run Playwright tests tagged “${tag}”?`, run: () => run('tag', tag, `tag ${tag}`) })),
    { id: 'remove-stale-tags', label: 'Remove stale Playwright test tags', keywords: ['cleanup'], disabled: !explorer?.staleTags.length || !!busy, confirmationPrompt: 'Remove saved tags for tests that are no longer discovered?', run: removeStaleTags },
  ];
  useRegisterTabCommands(tab.id, commands);

  // A finished run is shown in the side panel, so reveal it when the panel is hidden.
  const finishedRunId = lastRun?.id;
  const seenRunId = useRef(finishedRunId);
  useEffect(() => {
    if (finishedRunId && finishedRunId !== seenRunId.current && sidePanelMode) setHistoryOpen(true);
    seenRunId.current = finishedRunId;
  }, [finishedRunId, sidePanelMode, setHistoryOpen]);
  const shownRun = shownRunId === 'none' ? undefined : history.find((item) => item.id === (shownRunId ?? history[0]?.id));
  const shownOutcomes = {
    attention: (shownRun?.tests ?? []).filter((test) => test.status !== 'passed' && test.status !== 'skipped')
      .sort((first, second) => (outcomeOrder[first.status] ?? 9) - (outcomeOrder[second.status] ?? 9)),
    quiet: (shownRun?.tests ?? []).filter((test) => test.status === 'passed' || test.status === 'skipped'),
  };
  const historySummary = history.length === 0 ? 'No runs yet'
    : `${history.length} saved · last ${stateLabels[history[0].state]}, ${formatRunTime(history[0].startedUtc)}`;
  const outcomesByLocation = new Map((shownRun?.tests ?? []).map((outcome) => [`${outcome.file.toLowerCase()}:${outcome.line}:${outcome.title.toLowerCase()}`, outcomeStatusLabels[outcome.status] ?? outcome.status]));
  const staleTagCount = explorer?.staleTags.length ?? 0;

  const clearButton = <button className="secondary-button" type="button" disabled={!!busy || history.length === 0} onClick={() => void clearHistory()}><Trash2 size={11} /> Clear history</button>;
  const historyList = <>
    <ul>{history.map((item) => <li key={item.id}>
      <button type="button" className="playwright-history-item" aria-pressed={shownRun?.id === item.id} onClick={() => setShownRunId(item.id)}>
        <span className={`script-run-state is-${item.state}`}>{stateLabels[item.state]}</span>
        <span className="playwright-history-copy">
          <strong title={item.target}>{item.target}</strong>
          <small>{formatRunTime(item.startedUtc)} · {formatDuration(item.durationMilliseconds)}</small>
        </span>
        <RunCountChips run={item} />
      </button>
    </li>)}</ul>
    {history.length === 0 && <p className="playwright-info">No runs yet. Runs are saved here and kept after Automator restarts.</p>}
  </>;
  const runResult = shownRun && <section className="playwright-run-result" aria-label="Playwright run results">
      <header>
        <span className={`script-run-state is-${shownRun.state}`}>{stateLabels[shownRun.state]}</span>
        <div className="playwright-run-title">
          <strong title={shownRun.target}>{shownRun.id === history[0]?.id ? 'Latest run' : 'Past run'} · {shownRun.target}</strong>
          <small>{formatRunTime(shownRun.startedUtc)} · {formatDuration(shownRun.durationMilliseconds)}</small>
        </div>
        {!sidePanelMode && <button className="icon-button" aria-label="Hide run results" onClick={() => setShownRunId('none')}><X size={12} /></button>}
      </header>
      <RunCountChips run={shownRun} />
      <p className="playwright-info" data-error={shownRun.state !== 'success'}>{shownRun.message}{shownRun.outputTruncated ? ' Test runner output was truncated; the run itself continued.' : ''}</p>
      {shownOutcomes.attention.length > 0 && <div className="playwright-outcome-list">
        {shownOutcomes.attention.map((test, index) => <OutcomeRow key={`${test.file}:${test.line}:${test.project}:${index}`} test={test} />)}
      </div>}
      {shownOutcomes.quiet.length > 0 && <details className="playwright-quiet">
        <summary>{shownOutcomes.quiet.length} passed or skipped</summary>
        <div className="playwright-outcome-list">
          {shownOutcomes.quiet.map((test, index) => <OutcomeRow key={`${test.file}:${test.line}:${test.project}:quiet-${index}`} test={test} />)}
        </div>
      </details>}
      {shownRun.tests.length === 0 && <p className="playwright-info">No per-test results were saved for this run.</p>}
    </section>;
  return <section className="module-view playwright-view" aria-label="Playwright Test Explorer">
    <style>{styles}</style>
    <header className="playwright-heading">
      <span className="script-module-icon"><FilePlus2 size={17} /></span>
      <div className="playwright-heading-copy"><h1>Playwright Test Explorer</h1><p>Agent-authored test files, run and grouped with Automator tags</p></div>
      {explorer?.runner.version && <span className="playwright-runner">Playwright {explorer.runner.version}</span>}
    </header>

    <form className="playwright-project" onSubmit={(event) => void saveProject(event)}>
      <FolderOpen size={14} />
      <input aria-label="Playwright project folder" value={projectDraft} onChange={(event) => setProjectDraft(event.target.value)} placeholder="Select a project folder containing node_modules/@playwright/test" />
      <button className="secondary-button" type="button" disabled={!!busy} onClick={() => void chooseProject()}>Browse</button>
      <button className="primary-button" disabled={!!busy || !projectDraft.trim()}>{busy === 'project' ? <LoaderCircle size={12} /> : <Check size={12} />} Use folder</button>
    </form>

    {notice && <p className="playwright-info" data-error={noticeIsError} role={noticeIsError ? 'alert' : 'status'}>{noticeIsError ? <CircleAlert size={12} /> : <Check size={12} />}{notice}</p>}
    {explorer?.projectRoot && !explorer.runner.available && <p className="playwright-info" data-error="true" role="alert">{explorer.runner.message}</p>}

    {(activeRun || foreignRunning) && <section className="script-running" role="status" aria-label="Playwright run in progress">
      <Loader2 size={15} className="script-running-spinner" />
      <div className="script-running-copy"><strong>{activeRun?.target ?? backendActive[0].target}</strong><span>{activeRun?.canceling ? 'Canceling…' : 'Running'} · <ElapsedTime since={activeRun?.startedAt ?? Date.parse(backendActive[0].startedUtc)} />{!activeRun && ' · started elsewhere'}</span></div>
      {activeRun && <button className="secondary-button" type="button" disabled={activeRun.canceling} onClick={() => void cancelBrowserRun()}><Square size={12} /> Cancel run</button>}
    </section>}

    {profiles.length > 0 && <section className="playwright-info" aria-label="Browser profiles for AI agents" style={{ flexDirection: 'column', alignItems: 'stretch' }}>
      <strong>Browser profiles AI can use</strong>
      {profiles.map((profile) => <label key={profile.id} style={{ display: 'flex', gap: 6, alignItems: 'center' }}>
        <input type="checkbox" checked={profile.aiAccess === true} disabled={!!busy} onChange={(event) => void setAiAccess(profile, event.target.checked)} />
        <span>{profile.name}</span>
      </label>)}
    </section>}

    {form && <form className="playwright-form" onSubmit={(event) => void saveTestFile(event)}>
      <header><FilePlus2 size={13} /><strong>{formKind === 'section' ? 'Create a test section' : 'Add a test to this section'}</strong></header>
      <div className="playwright-form-fields">
        <label><span>Test file in the project</span><input required maxLength={512} value={form.file} disabled={formKind === 'test'} onChange={(event) => setForm({ ...form, file: event.target.value })} placeholder="tests/auth/login.spec.ts" /></label>
        <label><span>Test name</span><input required maxLength={256} value={form.testName} onChange={(event) => setForm({ ...form, testName: event.target.value })} placeholder="user can sign in" /></label>
      </div>
      <div className="playwright-form-actions"><button className="secondary-button" type="button" disabled={!!busy} onClick={() => setForm(null)}>Cancel</button><button className="primary-button" disabled={!!busy}>{busy === 'file' ? <LoaderCircle size={12} /> : <Plus size={12} />} {formKind === 'section' ? 'Create file' : 'Add test'}</button></div>
    </form>}

    {explorer && staleTagCount > 0 && <div className="playwright-stale"><Tag size={12} /><span>{staleTagCount} tag record{staleTagCount === 1 ? '' : 's'} refer to tests no longer found after refresh.</span><button className="secondary-button" type="button" disabled={!!busy} onClick={() => void removeStaleTags()}><Trash2 size={11} /> Clean up</button></div>}

    <div className="playwright-toolbar">
      <button className="secondary-button" type="button" disabled={!!busy || !explorer?.projectRoot} onClick={() => void refreshTests()}>{busy === 'discover' ? <LoaderCircle size={12} /> : <RefreshCw size={12} />} Refresh tests</button>
      <button className="secondary-button" type="button" disabled={!!busy || !explorer?.projectRoot} onClick={openCreateSection}><Plus size={12} /> New section</button>
      <button className="secondary-button" type="button" title="Run `playwright test --ui` in its own window" disabled={locked || !explorer?.projectRoot || !explorer.runner.available} onClick={() => void openUiMode()}>{uiModeOpen ? <LoaderCircle size={12} /> : <MonitorPlay size={12} />} UI mode</button>
      <span className="spacer" />
      <select aria-label="Tag group" value={selectedTag} onChange={(event) => setSelectedTag(event.target.value)} disabled={!explorer?.tags.length}>
        {explorer?.tags.length ? explorer.tags.map((tag) => <option key={tag} value={tag}>{tag}</option>) : <option value="">No tag groups</option>}
      </select>
      <button className="secondary-button" type="button" disabled={locked || !selectedTag} onClick={() => void run('tag', selectedTag, `tag ${selectedTag}`)}><Play size={11} /> Run tag</button>
    </div>

    <div className="playwright-list" aria-live="polite">
      {!explorer?.projectRoot && <div className="playwright-empty"><FolderOpen size={24} /><strong>Select a Playwright project</strong><p>Choose a project folder containing its own Playwright Test runner. Automator will not install or modify dependencies.</p></div>}
      {explorer?.projectRoot && !explorer.runner.available && <div className="playwright-empty"><CircleAlert size={24} /><strong>Playwright Test is not set up here</strong><p>Install the runner in this project yourself, then refresh. Existing project tests remain untouched.</p></div>}
      {explorer?.projectRoot && explorer.runner.available && explorer.files.length === 0 && <div className="playwright-empty"><FilePlus2 size={24} /><strong>No tests discovered</strong><p>Create a test section here or add test files with your agent, then refresh.</p><button className="secondary-button" type="button" disabled={!!busy} onClick={openCreateSection}><Plus size={11} /> Create section</button></div>}
      {explorer?.files.map((file) => <details className="playwright-file" key={file.path} open>
        <summary><FilePlus2 size={13} /><span className="playwright-file-name" title={file.path}>{file.path}</span><span className="playwright-file-count">{file.tests.length} test{file.tests.length === 1 ? '' : 's'}</span>
          <span className="playwright-file-tools"><button className="icon-button" type="button" aria-label={`Run section ${file.path}`} title="Run section" disabled={locked} onClick={(event) => { event.preventDefault(); event.stopPropagation(); void run('file', file.path, file.path); }}><Play size={12} /></button><button className="icon-button" type="button" aria-label={`Add test to ${file.path}`} title="Add test" disabled={!!busy} onClick={(event) => { event.preventDefault(); event.stopPropagation(); openAppendTest(file.path); }}><Plus size={12} /></button></span>
        </summary>
        {file.tests.map((test) => {
          const outcomeKey = `${test.file.toLowerCase()}:${test.line}:${test.titlePath.at(-1)?.toLowerCase() ?? ''}`;
          const outcome = sidePanelMode ? undefined : outcomesByLocation.get(outcomeKey);
          return <article className="playwright-test" key={test.id}>
            <div className="playwright-test-main"><div className="playwright-test-copy"><div className="playwright-test-title" title={displayTestTitle(test)}>{displayTestTitle(test)}</div><div className="playwright-test-meta">{test.project ? `[${test.project}] · ` : ''}line {test.line}{outcome ? ` · ${outcome}` : ''}</div></div>
              <span className="playwright-test-actions"><button className="icon-button" type="button" title="Show test code" aria-label={`Show code for ${displayTestTitle(test)}`} aria-expanded={codeView?.testId === test.id} disabled={!!busy} onClick={() => void toggleCode(test)}><Code2 size={12} /></button><button className="icon-button" type="button" title="Edit tags" aria-label={`Edit tags for ${displayTestTitle(test)}`} disabled={!!busy} onClick={() => editTags(test)}><Tag size={12} /></button><button className="icon-button" type="button" title="Run test" aria-label={`Run ${displayTestTitle(test)}`} disabled={locked} onClick={() => void run('test', test.id, displayTestTitle(test))}><Play size={12} /></button></span>
            </div>
            {test.tags.length > 0 && <div className="playwright-tag-list">{test.tags.map((tag) => <span className="playwright-tag" key={tag}>{tag}</span>)}</div>}
            {tagEditorId === test.id && <div className="playwright-inline-editor"><Tag size={11} /><input aria-label={`Tags for ${displayTestTitle(test)}`} value={tagDraft} onChange={(event) => setTagDraft(event.target.value)} placeholder="smoke, login" /><small>comma separated</small><button className="icon-button" type="button" aria-label="Save tags" disabled={!!busy} onClick={() => void saveTags(test)}><Check size={12} /></button><button className="icon-button" type="button" aria-label="Cancel tag editing" disabled={!!busy} onClick={() => setTagEditorId(null)}>×</button></div>}
            {tagEditorId === test.id && tagSuggestions().length > 0 && <div className="playwright-tag-list" aria-label="Suggested tags">{tagSuggestions().map((tag) => <button className="playwright-tag playwright-tag-suggestion" type="button" key={tag} onClick={() => pickSuggestion(tag)}>+ {tag}</button>)}</div>}
            {codeView?.testId === test.id && <pre className="playwright-code" aria-label={`Code for ${displayTestTitle(test)}`}><code>{codeView.code.split('\n').map((text, index) => `${String(codeView.startLine + index).padStart(4)}  ${text}`).join('\n')}</code></pre>}
          </article>;
        })}
      </details>)}
    </div>

    {!sidePanelMode && runResult}

    {sidePanelMode
      ? <SideHistoryToggle panel={sidePanel} title="Run history" />
      : <section className="playwright-history" data-open={historyOpen} aria-label="Playwright run history">
        <header>
          <button type="button" className="playwright-history-toggle" aria-expanded={historyOpen} aria-controls="playwright-history-body" onClick={() => setHistoryOpen((open) => !open)}>
            {historyOpen ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
            <strong>Run history</strong>
            <small>{historySummary}</small>
          </button>
          {clearButton}
        </header>
        {historyOpen && <div className="playwright-history-body" id="playwright-history-body">{historyList}</div>}
      </section>}
    {sidePanelMode && <SideHistoryContent panel={sidePanel} title="Playwright run history" actions={clearButton}><div className="side-panel-list">{historyList}</div>{runResult && <div className="side-panel-detail">{runResult}</div>}</SideHistoryContent>}
  </section>;
}
