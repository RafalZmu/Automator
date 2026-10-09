import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Activity, Check, Clock3, Download, History, Pause, Play, Tag, Trash2, X } from 'lucide-react';
import type { AutomationModuleActionRequest, BackendUiState } from '../../contracts/rpc';
import type { AutomationServices } from '../automationServices';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import { isWorkTimeToggleShortcut } from './workTimeShortcut';
import {
  calculateWorkTimeReportTotals,
  calculateWorkTimeIntervalMilliseconds,
  createWorkTimeCsv,
  elapsedWorkTimeMilliseconds,
  filterWorkTimeEntries,
  formatWorkTimeDuration,
  getWorkTimeTags,
  readWorkTimeSnapshot,
  workTimeEntryDurationMilliseconds,
  type WorkTimeReportFilter,
  type WorkTimeSnapshot,
} from './workTimeViewModel';
import './FocusSessionsView.css';

type ViewProps = {
  tab: BackendUiState['tabs'][number];
  moduleState: BackendUiState['moduleStates'][number] | undefined;
  services: AutomationServices;
  surface: 'launcher' | 'workspace';
};

export function FocusSessionsView({ tab, services, surface }: ViewProps) {
  const [snapshot, setSnapshot] = useState<WorkTimeSnapshot | null>(null);
  const [startDescription, setStartDescription] = useState('');
  const [busy, setBusy] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const [now, setNow] = useState(Date.now());
  const [workingPeriodOnly, setWorkingPeriodOnly] = useState(false);
  const [reportFilter, setReportFilter] = useState<WorkTimeReportFilter>({ fromDate: '', toDate: '', tag: '' });
  const [editingEntryId, setEditingEntryId] = useState<string | null>(null);
  const [editDescription, setEditDescription] = useState('');
  const [editTags, setEditTags] = useState('');
  const [deleteCandidateId, setDeleteCandidateId] = useState<string | null>(null);
  const startDescriptionRef = useRef<HTMLInputElement>(null);
  const alive = useRef(true);

  const refresh = useCallback(async () => {
    const result = await services.modules.dispatch('getSnapshot', {});
    if (result.status === 'error') throw new Error(result.message);
    const next = readWorkTimeSnapshot(result.data);
    if (!next) throw new Error('Work Time returned invalid time-log data.');
    if (alive.current) setSnapshot(next);
  }, [services]);

  useEffect(() => {
    alive.current = true;
    void refresh().catch((error) => {
      if (alive.current) setNotice(error instanceof Error ? error.message : 'Could not load the work-time log.');
    });
    const ticker = window.setInterval(() => setNow(Date.now()), 1000);
    return () => {
      alive.current = false;
      window.clearInterval(ticker);
    };
  }, [refresh]);

  const dispatch = async (actionId: string, input: AutomationModuleActionRequest['input'] = {}): Promise<boolean> => {
    if (busy) return false;
    setBusy(actionId);
    setNotice('');
    try {
      const result = await services.modules.dispatch(actionId, input);
      if (result.status === 'error') throw new Error(result.message);
      const next = readWorkTimeSnapshot(result.data);
      if (!next) throw new Error('Work Time returned invalid time-log data.');
      if (!alive.current) return false;
      setSnapshot(next);
      setNotice(result.message);
      return true;
    } catch (error) {
      if (alive.current) setNotice(error instanceof Error ? error.message : 'Work-time action failed.');
      return false;
    } finally {
      if (alive.current) setBusy(null);
    }
  };

  const runningTimer = snapshot?.timers.find((timer) => timer.status === 'running') ?? null;
  const cleanedStartDescription = startDescription.trim();
  const startTimer = async () => {
    if (!cleanedStartDescription || runningTimer) return;
    if (await dispatch('start', { description: cleanedStartDescription })) setStartDescription('');
  };
  const toggleWorkTimer = async () => {
    if (busy || !snapshot) return;
    if (runningTimer) await dispatch('pause', { id: runningTimer.id });
    else startDescriptionRef.current?.focus();
  };
  useEffect(() => {
    const handleShortcut = (event: KeyboardEvent) => {
      if (!isWorkTimeToggleShortcut(event)) return;
      event.preventDefault();
      void toggleWorkTimer();
    };
    window.addEventListener('keydown', handleShortcut);
    return () => window.removeEventListener('keydown', handleShortcut);
  });
  const submitEntry = (event: FormEvent<HTMLFormElement>, id: string) => {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const description = String(form.get('description') ?? '').trim();
    const tags = String(form.get('tags') ?? '').split(',').map((tag) => tag.trim()).filter(Boolean);
    void dispatch('saveEntry', { id, description, tags });
  };
  const reportEntries = useMemo(() => snapshot
    ? filterWorkTimeEntries(snapshot.history, reportFilter).slice().reverse()
    : [], [snapshot, reportFilter]);
  const reportTags = useMemo(() => getWorkTimeTags(snapshot?.history ?? []), [snapshot]);
  const reportTotals = useMemo(() => calculateWorkTimeReportTotals(snapshot?.history ?? [], new Date(now), workingPeriodOnly), [snapshot, now, workingPeriodOnly]);

  const beginEdit = (entry: WorkTimeSnapshot['history'][number]) => {
    setEditingEntryId(entry.id);
    setEditDescription(entry.description);
    setEditTags(entry.tags.join(', '));
    setDeleteCandidateId(null);
  };

  const saveEdit = async () => {
    if (!editingEntryId) return;
    const id = editingEntryId;
    const ok = await dispatch('updateEntry', {
      id,
      description: editDescription.trim(),
      tags: editTags.split(',').map((tag) => tag.trim()).filter(Boolean),
    });
    if (ok) setEditingEntryId(null);
  };

  const deleteEntry = async (id: string) => {
    const ok = await dispatch('deleteEntry', { id });
    if (ok) {
      setDeleteCandidateId(null);
      if (editingEntryId === id) setEditingEntryId(null);
    }
  };

  const exportReport = async () => {
    if (!reportEntries.length || busy) return;
    setBusy('exportCsv');
    setNotice('');
    try {
      const saved = await services.files.saveWorkTimeCsv(createWorkTimeCsv(reportEntries, workingPeriodOnly));
      setNotice(saved ? 'Filtered work-time report exported.' : 'CSV export cancelled.');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not export the work-time report.');
    } finally {
      if (alive.current) setBusy(null);
    }
  };

  const commands = useMemo(() => [
    { id: 'toggle-work', label: 'Work timer shortcut', keywords: ['start pause shortcut s'], disabled: !!busy || !snapshot, run: toggleWorkTimer },
    { id: 'start-work', label: 'Start work timer', keywords: ['begin named timer'], disabled: !!busy || !snapshot || !!runningTimer || !cleanedStartDescription, run: startTimer },
    ...(runningTimer ? [{ id: `pause-work-${runningTimer.id}`, label: `Pause ${runningTimer.description}`, keywords: ['pause timer'], disabled: !!busy, run: async () => { await dispatch('pause', { id: runningTimer.id }); } }] : []),
    ...((snapshot?.timers ?? []).filter((timer) => timer.status === 'paused').flatMap((timer) => [
      { id: `resume-work-${timer.id}`, label: `Resume ${timer.description}`, keywords: ['continue timer'], disabled: !!busy || !!runningTimer, run: async () => { await dispatch('resume', { id: timer.id }); } },
      { id: `end-work-${timer.id}`, label: `End ${timer.description}`, keywords: ['finish timer'], disabled: !!busy, run: async () => { await dispatch('end', { id: timer.id }); } },
      { id: `discard-work-${timer.id}`, label: `Discard ${timer.description}`, keywords: ['remove timer'], disabled: !!busy, confirmationPrompt: `Discard “${timer.description}”?`, run: async () => { await dispatch('discardTimer', { id: timer.id }); } },
    ])),
    ...((snapshot?.pendingEntries ?? []).map((entry) => ({ id: `discard-pending-${entry.id}`, label: `Discard unsaved ${entry.description || 'timer'}`, keywords: ['delete unsaved draft'], disabled: !!busy, confirmationPrompt: 'Discard this unsaved work interval?', run: async () => { await dispatch('discardPending', { id: entry.id }); } }))),
    ...(surface === 'workspace' ? [
      { id: 'export-work-time-csv', label: 'Export filtered work log as CSV', keywords: ['download report spreadsheet'], disabled: !!busy || reportEntries.length === 0, run: exportReport },
      ...reportEntries.flatMap((entry) => [
        { id: `edit-work-time-${entry.id}`, label: `Edit ${entry.description}`, keywords: ['change description tags'], disabled: !!busy, run: () => beginEdit(entry) },
        { id: `delete-work-time-${entry.id}`, label: `Delete ${entry.description}`, keywords: ['remove saved log entry'], disabled: !!busy, confirmationPrompt: `Delete “${entry.description}”?`, run: async () => { await deleteEntry(entry.id); } },
      ]),
    ] : []),
  ], [busy, snapshot, cleanedStartDescription, services, surface, reportEntries, reportFilter, editDescription, editTags, editingEntryId, toggleWorkTimer, startTimer]);
  useRegisterTabCommands(tab.id, commands);

  const timerDuration = (timer: WorkTimeSnapshot['timers'][number]) => workingPeriodOnly
    ? timer.segments.reduce((duration, segment) => duration + calculateWorkTimeIntervalMilliseconds(segment.startedUtc, segment.stoppedUtc ?? new Date(now).toISOString()), 0)
    : elapsedWorkTimeMilliseconds(timer, now);
  return <section className="module-view work-time-view" aria-label="Work Time Log" data-module-id={tab.id}>
    <header className="work-time-heading">
      <div className="work-time-heading-title">
        <span className="work-time-module-icon"><Activity size={17} /></span>
        <div><h1>Work Time Log</h1><p>Track a work interval, then describe what you did.</p></div>
      </div>
      <span className={`work-time-state ${runningTimer ? 'is-running' : snapshot?.pendingEntries.length ? 'is-pending' : ''}`}>
        <span className="work-time-state-dot" />{runningTimer ? 'Recording' : snapshot?.pendingEntries.length ? 'Needs details' : 'Ready'}
      </span>
    </header>

    {notice && <p className="work-time-notice" role="status">{notice}</p>}

    {!snapshot ? <div className="work-time-loading" role="status"><Clock3 size={20} /><p>Loading your work log…</p></div> : <>
      <label className="work-time-working-period-toggle">
        <input aria-label="Count only working hours" type="checkbox" checked={workingPeriodOnly} onChange={(event) => setWorkingPeriodOnly(event.target.checked)} />
        <span><strong>Count only working hours</strong><small>08:00–16:00 local time each day</small></span>
      </label>
      <section className="work-time-entry-card" aria-label="New work timer">
        <header><div><Play size={14} /><h2>New timer</h2></div><span>{runningTimer ? 'Pause the running timer first' : 'Description required'}</span></header>
        <form onSubmit={(event) => { event.preventDefault(); void startTimer(); }}>
          <label className="work-time-field"><span>Work description <b>Required</b></span>
            <input ref={startDescriptionRef} className="work-time-start-input" aria-label="New timer description" value={startDescription} maxLength={500}
              placeholder="What are you working on?" disabled={!!busy} onChange={(event) => setStartDescription(event.target.value)} />
          </label>
          <div className="work-time-entry-actions">
            <button className="primary-button" type="submit" disabled={!!busy || !!runningTimer || !cleanedStartDescription}><Play size={13} /> {busy === 'start' ? 'Starting…' : 'Start timer'}</button>
          </div>
        </form>
      </section>

      {snapshot.timers.length > 0 && <section className="work-time-timer-list" aria-label="Work timers">
        {snapshot.timers.map((timer) => {
          const elapsed = timerDuration(timer);
          const isRunning = timer.status === 'running';
          return <article className={`work-time-current-card ${isRunning ? 'is-timer-running' : 'is-timer-paused'}`} key={timer.id} aria-label={`${isRunning ? 'Running' : 'Paused'} timer ${timer.description}`}>
            <div className={`work-time-clock-block ${isRunning ? '' : 'is-pending'}`}>
              <span className="work-time-eyebrow">{isRunning ? <><span className="work-time-live-dot" /> RUNNING</> : 'PAUSED'}</span>
              <strong className="work-time-clock" aria-label={`${formatWorkTimeDuration(elapsed)} elapsed`}>{formatWorkTimeDuration(elapsed)}</strong>
              <span className="work-time-started">Started {new Date(timer.startedUtc).toLocaleString()}</span>
            </div>
            <div className="work-time-current-copy">
              <h2>{timer.description}</h2>
              <p>{isRunning ? 'This timer is recording now. Pause it before starting another timer.' : 'This timer is saved and can be resumed later.'}</p>
              <div className="work-time-entry-actions">
                {isRunning
                  ? <>
                    <button className="primary-button" type="button" disabled={!!busy} onClick={() => void dispatch('pause', { id: timer.id })}><Pause size={13} /> {busy === 'pause' ? 'Pausing…' : 'Pause timer'}</button>
                    <button className="secondary-button" type="button" disabled={!!busy} onClick={() => void dispatch('end', { id: timer.id })}><X size={13} /> End timer</button>
                  </>
                  : <button className="primary-button" type="button" disabled={!!busy || !!runningTimer} onClick={() => void dispatch('resume', { id: timer.id })}><Play size={13} /> Resume timer</button>}
                {!isRunning && <>
                  <button className="secondary-button" type="button" disabled={!!busy} onClick={() => void dispatch('end', { id: timer.id })}><X size={13} /> End timer</button>
                  <button className="work-time-discard" type="button" disabled={!!busy} onClick={() => { if (window.confirm(`Discard timer “${timer.description}”?`)) void dispatch('discardTimer', { id: timer.id }); }}><Trash2 size={13} /> Discard</button>
                </>}
              </div>
            </div>
          </article>;
        })}
      </section>}

      {snapshot.pendingEntries.map((entry) => <section className="work-time-entry-card" aria-label={`Save work interval ${entry.description || 'Untitled timer'}`} key={entry.id}>
        <header><div><Check size={14} /><h2>Save {entry.description || 'Untitled timer'}</h2></div><span>{formatWorkTimeDuration(workTimeEntryDurationMilliseconds(entry, workingPeriodOnly))}</span></header>
        <form onSubmit={(event) => submitEntry(event, entry.id)}>
          <label className="work-time-field"><span>Description <b>Required</b></span>
            <textarea name="description" aria-label={`Work description ${entry.description || entry.id}`} defaultValue={entry.description} maxLength={500} rows={2} placeholder="What did you work on?" disabled={!!busy} />
          </label>
          <label className="work-time-field"><span>Tags <b>Optional</b></span>
            <div className="work-time-tags-input"><Tag size={13} /><input name="tags" aria-label={`Work tags ${entry.description || entry.id}`} defaultValue={entry.tags.join(', ')} maxLength={500} placeholder="planning, release, bug fix" disabled={!!busy} /><small>Separate with commas</small></div>
          </label>
          <div className="work-time-entry-actions">
            <button className="primary-button" type="submit" disabled={!!busy}><Check size={13} /> Save entry</button>
            <button className="work-time-discard" type="button" disabled={!!busy} onClick={() => { if (window.confirm('Discard this unsaved work interval?')) void dispatch('discardPending', { id: entry.id }); }}><Trash2 size={13} /> Discard draft</button>
          </div>
        </form>
      </section>)}

      {surface === 'workspace' && <section className="work-time-reports" aria-label="Work time reports">
        <header className="work-time-reports-heading">
          <div><History size={15} /><div><h2>Work log</h2><p>Review, filter, and export your saved work.</p></div></div>
          <button className="secondary-button work-time-export" type="button" disabled={!!busy || reportEntries.length === 0} onClick={() => void exportReport()}>
            <Download size={13} /> {busy === 'exportCsv' ? 'Preparing…' : 'Export filtered work log as CSV'}
          </button>
        </header>

        <div className="work-time-report-totals" aria-label="Work time totals">
          <article><span>Today</span><strong>{formatWorkTimeDuration(reportTotals.todayMilliseconds)}</strong><small>{reportTotals.todayEntries} {reportTotals.todayEntries === 1 ? 'entry' : 'entries'}</small></article>
          <article><span>This week</span><strong>{formatWorkTimeDuration(reportTotals.weekMilliseconds)}</strong><small>{reportTotals.weekEntries} {reportTotals.weekEntries === 1 ? 'entry' : 'entries'}</small></article>
        </div>

        <div className="work-time-report-filters" aria-label="Work log filters">
          <label><span>From</span><input aria-label="Filter work log from date" type="date" value={reportFilter.fromDate} onChange={(event) => setReportFilter((filter) => ({ ...filter, fromDate: event.target.value }))} /></label>
          <label><span>To</span><input aria-label="Filter work log to date" type="date" value={reportFilter.toDate} onChange={(event) => setReportFilter((filter) => ({ ...filter, toDate: event.target.value }))} /></label>
          <label className="work-time-tag-filter"><span>Tag</span><select aria-label="Filter work log by tag" value={reportFilter.tag} onChange={(event) => setReportFilter((filter) => ({ ...filter, tag: event.target.value }))}>
            <option value="">All tags</option>{reportTags.map((tag) => <option value={tag} key={tag}>{tag}</option>)}
          </select></label>
          <button className="work-time-clear-filters" type="button" disabled={!reportFilter.fromDate && !reportFilter.toDate && !reportFilter.tag} onClick={() => setReportFilter({ fromDate: '', toDate: '', tag: '' })}>Clear filters</button>
          <span className="work-time-report-count">{reportEntries.length} {reportEntries.length === 1 ? 'entry' : 'entries'}</span>
        </div>

        {reportEntries.length === 0
          ? <div className="work-time-history-empty"><span><History size={18} /></span><p>{snapshot.history.length ? 'No saved work matches these filters.' : 'Saved intervals will appear here.'}</p></div>
          : <div className="work-time-report-list">{reportEntries.map((entry) => {
            const isEditing = editingEntryId === entry.id;
            const isDeleting = deleteCandidateId === entry.id;
            return <article className="work-time-report-row" key={entry.id}>
              {isEditing ? <form className="work-time-edit-form" onSubmit={(event) => { event.preventDefault(); void saveEdit(); }}>
                <label><span>Description</span><input aria-label="Edit work description" maxLength={500} value={editDescription} onChange={(event) => setEditDescription(event.target.value)} disabled={!!busy} /></label>
                <label><span>Tags</span><input aria-label="Edit work tags" maxLength={500} value={editTags} onChange={(event) => setEditTags(event.target.value)} disabled={!!busy} /></label>
                <div className="work-time-report-actions">
                  <button className="primary-button" type="submit" disabled={!!busy || !editDescription.trim()}><Check size={12} /> Save work entry changes</button>
                  <button className="secondary-button" type="button" disabled={!!busy} onClick={() => setEditingEntryId(null)}>Cancel work entry changes</button>
                </div>
              </form> : <>
                <div className="work-time-report-main">
                  <strong>{entry.description}</strong>
                  <span>{new Date(entry.startedUtc).toLocaleDateString()} · {new Date(entry.startedUtc).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}–{new Date(entry.stoppedUtc).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}</span>
                  {entry.tags.length > 0 && <div className="work-time-tag-list">{entry.tags.map((tag) => <span className="work-time-tag" key={tag}>{tag}</span>)}</div>}
                </div>
                <span className="work-time-history-duration">{formatWorkTimeDuration(workTimeEntryDurationMilliseconds(entry, workingPeriodOnly))}</span>
                <div className="work-time-report-actions">
                  <button className="secondary-button" type="button" disabled={!!busy} aria-label={`Edit work log entry ${entry.description}`} onClick={() => beginEdit(entry)}>Edit</button>
                  <button className="work-time-discard" type="button" disabled={!!busy} aria-label={`Delete work log entry ${entry.description}`} onClick={() => { setDeleteCandidateId(entry.id); setEditingEntryId(null); }}><Trash2 size={12} /> Delete</button>
                </div>
                {isDeleting && <div className="work-time-delete-confirm" role="group" aria-label={`Confirm deletion of ${entry.description}`}>
                  <span>Delete this saved entry?</span>
                  <button className="work-time-discard" type="button" disabled={!!busy} onClick={() => void deleteEntry(entry.id)}>Confirm delete work entry</button>
                  <button className="secondary-button" type="button" disabled={!!busy} onClick={() => setDeleteCandidateId(null)}>Cancel</button>
                </div>}
              </>}
            </article>;
          })}</div>}
      </section>}
    </>}
  </section>;
}
