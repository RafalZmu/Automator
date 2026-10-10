import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Check, CircleAlert, Code2, Download, FilePlus2, FolderOpen, Play, RefreshCw, Save, Sparkles, Square } from 'lucide-react';
import type { AutomationServices } from '../automationServices';
import type { BackendUiState } from '../../contracts/rpc';
import {
  codexTaskDraftSchema,
  codexTaskDraftSummarySchema,
  codexTaskFormatSchema,
  codexTaskRunSchema,
  codexTaskStatusSchema,
  codexTaskSaveSchema,
  codexTaskRunInputSchema,
  type CodexTaskFormat,
} from '../../contracts/codex';
import './CodexView.css';

type Props = { tab: BackendUiState['tabs'][number]; services: AutomationServices };
type TaskDraft = ReturnType<typeof codexTaskDraftSchema.parse>;
type TaskStatus = ReturnType<typeof codexTaskStatusSchema.parse>;
const formats: Array<{ value: CodexTaskFormat | ''; label: string }> = [
  { value: '', label: 'Let Codex recommend' },
  { value: 'python', label: 'Python script' },
  { value: 'playwright', label: 'Playwright task' },
  { value: 'workflow', label: 'Workflow' },
];

function record(value: unknown): Record<string, unknown> {
  return value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : {};
}

function errorText(error: unknown): string {
  return error instanceof Error ? error.message : 'The Codex task could not be completed.';
}

function resultData(result: Awaited<ReturnType<AutomationServices['modules']['dispatch']>>): unknown {
  if (result.status === 'error') throw new Error(result.message || 'The Codex task failed.');
  return result.data;
}

export function CodexView({ tab, services }: Props) {
  const [status, setStatus] = useState<TaskStatus | null>(null);
  const [prompt, setPrompt] = useState('');
  const [scopeText, setScopeText] = useState('');
  const [preferredFormat, setPreferredFormat] = useState<CodexTaskFormat | ''>('');
  const [draft, setDraft] = useState<TaskDraft | null>(null);
  const [drafts, setDrafts] = useState<ReturnType<typeof codexTaskDraftSummarySchema.parse>[]>([]);
  const [taskInput, setTaskInput] = useState<Record<string, unknown>>({});
  const [notice, setNotice] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState<'status' | 'generate' | 'approve' | 'run' | 'save' | 'export' | 'scope' | null>(null);
  const [firstRunApproved, setFirstRunApproved] = useState(false);
  const [reviewedSavedTask, setReviewedSavedTask] = useState(false);
  const [acceptedProposedScope, setAcceptedProposedScope] = useState<string[]>([]);
  const [runSucceeded, setRunSucceeded] = useState(false);
  const [runOutput, setRunOutput] = useState<unknown>(null);
  const controller = useRef<AbortController | null>(null);
  const sensitiveEffects = useMemo(() => draft?.effects.filter((effect) => ['overwrite', 'delete', 'submit', 'send'].includes(effect.kind)) ?? [], [draft]);
  const scope = useMemo(() => scopeText.split(/\r?\n/).map((item) => item.trim()).filter(Boolean), [scopeText]);
  const proposedExpansion = draft?.proposedScope.filter((item) => !draft.scope.includes(item)) ?? [];

  const pickScopePath = async (kind: 'file' | 'directory') => {
    setBusy('scope'); setError('');
    try {
      const selectedPath = await services.files.pickCodexScopePath(kind);
      if (!selectedPath) return;
      setScopeText((current) => {
        const items = current.split(/\r?\n/).map((item) => item.trim()).filter(Boolean);
        return items.includes(selectedPath) ? current : [...items, selectedPath].join('\n');
      });
      setNotice('Selected path added to task scope.');
    } catch (cause) { setError(errorText(cause)); setNotice(''); }
    finally { setBusy(null); }
  };

  const reloadStatus = useCallback(async () => {
    setBusy('status'); setError('');
    try {
      const data = resultData(await services.modules.dispatch('getStatus', {}));
      setStatus(codexTaskStatusSchema.parse(data));
    } catch (cause) { setError(errorText(cause)); }
    finally { setBusy(null); }
  }, [services]);

  useEffect(() => { void reloadStatus(); }, [reloadStatus]);
  const reloadDrafts = useCallback(async () => {
    try {
      const data = resultData(await services.modules.dispatch('listDrafts', {}));
      if (!Array.isArray(data)) throw new Error('Codex draft list is invalid.');
      setDrafts(data.map((item) => codexTaskDraftSummarySchema.parse(item)));
    } catch (cause) { setError(errorText(cause)); }
  }, [services]);
  useEffect(() => { void reloadDrafts(); }, [reloadDrafts]);

  const loadDraft = async (id: string) => {
    if (!id) { setDraft(null); return; }
    setError('');
    try {
      const data = record(resultData(await services.modules.dispatch('getDraft', { id })));
      if (data.found === false) throw new Error('This draft is no longer available.');
      const parsed = codexTaskDraftSchema.parse(data);
      setDraft(parsed); setTaskInput(Object.fromEntries(parsed.inputs.map((item) => [item.name, ''])));
      setRunSucceeded(false); setRunOutput(null); setAcceptedProposedScope([]); setFirstRunApproved(false); setReviewedSavedTask(false);
      setNotice(parsed.planIsHistorical ? 'Saved task loaded. Its plan is historical; review the current source and scope.' : 'Draft loaded. Review the current source and scope before running.');
    } catch (cause) { setError(errorText(cause)); }
  };

  const generate = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!status?.available || !prompt.trim() || scope.length === 0) return;
    controller.current = new AbortController();
    setBusy('generate'); setError(''); setNotice('Asking Codex to prepare a task plan and source…');
    setDraft(null); setRunSucceeded(false); setRunOutput(null); setFirstRunApproved(false); setReviewedSavedTask(false); setAcceptedProposedScope([]);
    try {
      const input = { prompt: prompt.trim(), scope, ...(preferredFormat ? { preferredFormat } : {}) };
      const data = resultData(await services.modules.dispatch('generateDraft', input, { signal: controller.current.signal }));
      const parsed = codexTaskDraftSchema.parse(data);
      setDraft(parsed); setTaskInput(Object.fromEntries(parsed.inputs.map((item) => [item.name, ''])));
      await reloadDrafts();
      setNotice(`Draft ready. Codex recommends ${parsed.format}. Review the plan and source before approving a run.`);
    } catch (cause) { if (errorText(cause) !== 'The module action was aborted.') setError(errorText(cause)); setNotice(''); }
    finally { controller.current = null; setBusy(null); }
  };

  const approveFirstRun = async () => {
    if (!draft || proposedExpansion.length > 0) return;
    setBusy('approve'); setError('');
    try {
      const data = resultData(await services.modules.dispatch('approveChanges', { id: draft.id }));
      const updated = codexTaskDraftSchema.parse(data);
      setDraft(updated); setFirstRunApproved(true); setNotice('Reviewed and approved. You can now run this draft once.');
    } catch (cause) { setError(errorText(cause)); }
    finally { setBusy(null); }
  };

  const approveSavedReview = async () => {
    if (!draft?.reviewSourceHash) return;
    setBusy('approve'); setError('');
    try {
      const data = resultData(await services.modules.dispatch('approveChanges', { id: draft.id, expectedReviewSourceHash: draft.reviewSourceHash }));
      setDraft(codexTaskDraftSchema.parse(data)); setReviewedSavedTask(true); setFirstRunApproved(true); setNotice('The current saved task was reviewed and reapproved.');
    } catch (cause) { setError(errorText(cause)); }
    finally { setBusy(null); }
  };

  const runTask = async (effectConfirmed = false, target = draft) => {
    if (!target || (target.saved && !reviewedSavedTask)) return;
    if (target.proposedScope.some((item) => !target.scope.includes(item))) return;
    controller.current = new AbortController(); setBusy('run'); setError(''); setNotice('Running through the existing Automator task runner…'); setRunSucceeded(false); setRunOutput(null);
    try {
      const runInput = codexTaskRunInputSchema.parse({
        id: target.id,
        ...(effectConfirmed ? { effectConfirmed: true } : {}),
        ...(target.inputs.length ? { taskInput: target.inputs.reduce<Record<string, unknown>>((values, input) => {
          const raw = String(taskInput[input.name] ?? '');
          if (!raw.trim() && !input.required) return values;
          if (input.type === 'boolean') values[input.name] = raw === 'true';
          else if (input.type === 'number') {
            const parsed = Number(raw);
            if (!Number.isFinite(parsed)) throw new Error(`${input.name} must be a valid number.`);
            values[input.name] = parsed;
          } else if (input.type === 'json') {
            try { values[input.name] = JSON.parse(raw); } catch { throw new Error(`${input.name} must contain valid JSON.`); }
          } else values[input.name] = raw;
          return values;
        }, {}) } : {}),
      });
      const data = codexTaskRunSchema.parse(resultData(await services.modules.dispatch('runDraft', runInput, { signal: controller.current.signal })));
      setRunOutput(data.output ?? null); setRunSucceeded(data.succeeded); setNotice(data.message || (data.succeeded ? 'Task completed successfully.' : 'Task did not complete successfully.'));
      if (data.succeeded) setDraft((current) => current ? { ...current, runSucceeded: true } : current);
    } catch (cause) { if (errorText(cause) !== 'The module action was aborted.') setError(errorText(cause)); setNotice(''); }
    finally { controller.current = null; setBusy(null); }
  };

  const reviewAndRun = async () => {
    if (!draft || sensitiveEffects.length > 0 || proposedExpansion.length > 0) return;
    setBusy('approve'); setError('');
    try {
      const approved = codexTaskDraftSchema.parse(resultData(await services.modules.dispatch('approveChanges', { id: draft.id })));
      setDraft(approved); setFirstRunApproved(true); setNotice('Reviewed and approved. Starting the task now…');
      await runTask(false, approved);
    } catch (cause) { setError(errorText(cause)); }
    finally { if (!controller.current) setBusy(null); }
  };

  const regenerateWithAcceptedScope = async () => {
    if (!draft || acceptedProposedScope.length === 0) return;
    const expandedScope = [...new Set([...draft.scope, ...acceptedProposedScope])];
    if (expandedScope.length > 32) { setError('The expanded task scope exceeds the 32 item limit.'); return; }
    controller.current = new AbortController(); setBusy('generate'); setError(''); setNotice('Regenerating the plan for the accepted scope…');
    try {
      const input = { prompt: prompt.trim(), scope: expandedScope, ...(preferredFormat ? { preferredFormat } : {}) };
      const data = resultData(await services.modules.dispatch('generateDraft', input, { signal: controller.current.signal }));
      const regenerated = codexTaskDraftSchema.parse(data);
      setDraft(regenerated); setScopeText(expandedScope.join('\n')); setTaskInput(Object.fromEntries(regenerated.inputs.map((item) => [item.name, ''])));
      setAcceptedProposedScope([]); setFirstRunApproved(false); setReviewedSavedTask(false); setRunSucceeded(false); setRunOutput(null);
      await reloadDrafts(); setNotice('New draft generated for the accepted scope. Review and approve this version before running.');
    } catch (cause) { if (errorText(cause) !== 'The module action was aborted.') setError(errorText(cause)); setNotice(''); }
    finally { controller.current = null; setBusy(null); }
  };

  const saveDraft = async () => {
    if (!draft || !runSucceeded) return;
    setBusy('save'); setError('');
    try {
      const result = codexTaskSaveSchema.parse(resultData(await services.modules.dispatch('saveDraft', { id: draft.id })));
      if (!result.saved) throw new Error(result.message || 'The task was not saved.');
      setDraft((current) => current ? { ...current, saved: true } : current); await reloadDrafts(); setNotice(result.message);
    } catch (cause) { setError(errorText(cause)); }
    finally { setBusy(null); }
  };

  const exportDraft = async () => {
    if (!draft?.saved) return;
    setBusy('export'); setError('');
    try {
      const saved = await services.files.exportCodexDraft(draft.id);
      setNotice(saved ? 'Task source exported.' : 'Export canceled.');
    } catch (cause) { setError(errorText(cause)); }
    finally { setBusy(null); }
  };

  const data = record(runOutput);
  const effectsText = (effect: TaskDraft['effects'][number]) => `${effect.kind}: ${effect.description}${effect.target ? ` (${effect.target})` : ''}`;

  return <section className="module-view codex-view" aria-label="Codex task builder">
    <header className="codex-heading"><span className="codex-icon"><Sparkles size={17} /></span><div><h1>Codex</h1><p>Turn a repeatable task into a plan and inspectable automation</p></div><button className="secondary-button" type="button" disabled={busy !== null} onClick={() => void reloadStatus()}><RefreshCw size={13} /> Retry status</button></header>
    {error && <p role="alert" className="codex-alert"><CircleAlert size={14} />{error}</p>}
    {notice && <p role="status" className="codex-notice">{notice}</p>}

    {!status && <p className="codex-status">{busy === 'status' ? 'Checking the local Codex CLI…' : 'Codex CLI status is unavailable.'}</p>}
    {status && <section className={`codex-status ${status.available ? 'is-ready' : 'is-unavailable'}`} aria-label="Codex CLI readiness">
      <strong>{status.available ? 'Codex CLI ready' : status.state === 'signedOut' ? 'Codex CLI needs sign-in' : status.state === 'configurationError' ? 'Codex CLI configuration needs attention' : 'Codex CLI not found'}</strong>
      <span>{status.message}</span>{status.version && <small>{status.version}</small>}
      {!status.available && <button className="secondary-button" type="button" onClick={() => void reloadStatus()} disabled={busy !== null}>Retry</button>}
    </section>}
    {drafts.some((item) => item.saved) && <label className="codex-load-saved"><span>Review a saved task</span><select aria-label="Load saved task" value={draft?.saved ? draft.id : ''} onChange={(event) => void loadDraft(event.target.value)}><option value="">Choose saved task…</option>{drafts.filter((item) => item.saved).map((item) => <option key={item.id} value={item.id}>{item.plan.slice(0, 100)} · {item.format}</option>)}</select></label>}

    <form className="codex-request" onSubmit={(event) => void generate(event)}>
      <label><span>What should the task do?</span><textarea required maxLength={12000} value={prompt} onChange={(event) => setPrompt(event.target.value)} placeholder="For example: read the monthly CSV files, summarize totals, and create a report." /></label>
      <label><span>Files, folders, or sites in scope <small>Browse for local paths or enter one item per line; enter websites as URLs</small></span><textarea required maxLength={8192} value={scopeText} onChange={(event) => setScopeText(event.target.value)} placeholder={'C:\\Reports\\monthly\\\nhttps://example.com'} /></label>
      <div className="codex-scope-picker"><button className="secondary-button" type="button" disabled={busy !== null || scope.length >= 32} onClick={() => void pickScopePath('file')}><FilePlus2 size={13} /> Browse file</button><button className="secondary-button" type="button" disabled={busy !== null || scope.length >= 32} onClick={() => void pickScopePath('directory')}><FolderOpen size={13} /> Browse folder</button></div>
      <p className="codex-warning" role="note">The task description and selected scope are sent as text to the local Codex CLI. Drafting uses its existing sign-in and default model; Automator disables its known shell, browser, app, MCP, plugin, and search features and enables read-only mode. Tasks run through Automator only after you review and approve them.</p>
      <label><span>Format</span><select value={preferredFormat} onChange={(event) => setPreferredFormat(codexTaskFormatSchema.safeParse(event.target.value).success ? event.target.value as CodexTaskFormat : '')}>{formats.map((format) => <option key={format.value || 'auto'} value={format.value}>{format.label}</option>)}</select></label>
      {scope.length > 32 && <p className="codex-warning" role="alert">Choose 32 or fewer scope items before generating the task.</p>}
      <div className="codex-actions">{busy === 'generate' ? <button className="secondary-button" type="button" onClick={() => controller.current?.abort()}><Square size={13} /> Cancel generation</button> : <button className="primary-button" type="submit" disabled={!status?.available || busy !== null || !prompt.trim() || scope.length === 0 || scope.length > 32}><Sparkles size={13} /> Generate task draft</button>}</div>
    </form>

    {status && !status.available && <p className="codex-help">Install and configure the Codex CLI on this computer, then use Retry. Automator does not ask for a separate API key or login.</p>}
    {draft && <>
      <section className="codex-review" aria-label="Review generated task">
        <div className="codex-review-heading"><Code2 size={15} /><h2>Review before running</h2><span>Codex recommends {draft.format}</span></div>
        {draft.planIsHistorical && <p className="codex-warning" role="note">This plan is historical. The saved source or declared scope changed; review the current task below.</p>}
        <div className="codex-scope"><h3>Selected scope</h3><ul>{draft.scope.map((item) => <li key={item}>{item}</li>)}</ul>
          {proposedExpansion.length > 0 && <fieldset className="codex-scope-expansion"><legend>Codex proposes additional scope</legend><p>Accept each additional item you want this task to use. Then generate a new draft for the accepted scope and review that version before running.</p>{proposedExpansion.map((item) => <label key={item}><input type="checkbox" checked={acceptedProposedScope.includes(item)} onChange={(event) => setAcceptedProposedScope((current) => event.target.checked ? [...current, item] : current.filter((value) => value !== item))} />{item}</label>)}{new Set([...draft.scope, ...acceptedProposedScope]).size > 32 && <p role="alert">Choose 32 or fewer total scope items.</p>}<button className="secondary-button" type="button" disabled={busy !== null || acceptedProposedScope.length === 0 || new Set([...draft.scope, ...acceptedProposedScope]).size > 32} onClick={() => void regenerateWithAcceptedScope()}><RefreshCw size={13} /> Regenerate with accepted scope</button></fieldset>}
        </div>
        {draft.inputs.length > 0 && <fieldset className="codex-inputs"><legend>Inputs this task needs</legend>{draft.inputs.map((input) => <label key={input.name}><span>{input.name}{input.required ? ' *' : ''}<small>{input.description} · {input.type}</small></span>{input.type === 'boolean'
          ? <select required={input.required} value={String(taskInput[input.name] ?? '')} onChange={(event) => setTaskInput((current) => ({ ...current, [input.name]: event.target.value }))}><option value="">Choose…</option><option value="true">Yes</option><option value="false">No</option></select>
          : input.type === 'json' ? <textarea required={input.required} value={String(taskInput[input.name] ?? '')} onChange={(event) => setTaskInput((current) => ({ ...current, [input.name]: event.target.value }))} placeholder="Enter valid JSON" />
            : <input required={input.required} type={input.type === 'number' ? 'number' : 'text'} value={String(taskInput[input.name] ?? '')} onChange={(event) => setTaskInput((current) => ({ ...current, [input.name]: event.target.value }))} />}</label>)}</fieldset>}
        <div className="codex-effects"><h3>Planned effects</h3>{draft.effects.length ? <ul>{draft.effects.map((effect, index) => <li key={`${effect.kind}-${index}`}>{effectsText(effect)}</li>)}</ul> : <p>No effects were declared.</p>}</div>
        <p className="codex-trust">The selected scope is a review and consent boundary, not a technical sandbox. Generated code runs with your Windows user permissions and may access other resources available to that user.</p>
        <details className="codex-source" open><summary>Complete source ({draft.format})</summary><pre><code>{draft.source}</code></pre></details>
        {!draft.saved && !firstRunApproved && sensitiveEffects.length === 0 && <button className="primary-button" type="button" disabled={busy !== null || proposedExpansion.length > 0} onClick={() => void reviewAndRun()}><Check size={13} /><Play size={13} /> Review and run</button>}
        {!draft.saved && !firstRunApproved && sensitiveEffects.length > 0 && <button className="primary-button" type="button" disabled={busy !== null || proposedExpansion.length > 0} onClick={() => void approveFirstRun()}><Check size={13} /> Review and approve first run</button>}
        {draft.saved && <div className="codex-approval"><p>This saved task may have changed since its last review. The displayed plan can be historical.</p>{!reviewedSavedTask && <button className="secondary-button" type="button" disabled={busy !== null} onClick={() => void approveSavedReview()}>Review and approve current task</button>}{reviewedSavedTask && <span><Check size={13} /> Current task reapproved</span>}</div>}
        {firstRunApproved && <p className="codex-approved"><Check size={13} /> First run approved for the reviewed source and scope.</p>}
        {sensitiveEffects.length > 0 && <fieldset className="codex-effect-confirm"><legend>Additional effect confirmation</legend><p>This run includes effects that may overwrite, delete, or submit/send data. Confirm immediately before starting this task:</p><ul>{sensitiveEffects.map((effect, index) => <li key={`${effect.kind}-${index}`}>{effectsText(effect)}</li>)}</ul></fieldset>}
        <div className="codex-actions">
          {busy === 'run' ? <button className="secondary-button" type="button" onClick={() => controller.current?.abort()}><Square size={13} /> Cancel task</button> : firstRunApproved && <button className="primary-button" type="button" disabled={(draft.saved && !reviewedSavedTask) || proposedExpansion.length > 0 || busy !== null} onClick={() => void runTask(sensitiveEffects.length > 0)}><Play size={13} /> {sensitiveEffects.length ? 'Confirm effects and run' : 'Run task'}</button>}
          {runSucceeded && !draft.saved && <button className="secondary-button" type="button" disabled={busy !== null} onClick={() => void saveDraft()}><Save size={13} /> Save successful task</button>}
          {draft.saved && <button className="secondary-button" type="button" disabled={busy !== null} onClick={() => void exportDraft()}><Download size={13} /> Export source</button>}
        </div>
      </section>
      {(runOutput !== null || runSucceeded) && <section className="codex-run-result" aria-label="Task result"><h2>{runSucceeded ? 'Task completed' : 'Task output'}</h2>{typeof data.message === 'string' && <p>{data.message}</p>}<pre>{JSON.stringify(runOutput, null, 2) ?? 'No task output.'}</pre></section>}
    </>}
    {tab.kind !== 'codex' && <p className="codex-help">This view is registered for the Codex module.</p>}
  </section>;
}
