import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Activity, Check, Clock3, Download, History, Play, Tag, Trash2, X } from 'lucide-react';
import type { AutomationModuleActionRequest, BackendUiState } from '../../contracts/rpc';
import type { AutomationServices } from '../automationServices';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import {
  calculateWorkTimeReportTotals,
  createWorkTimeCsv,
  elapsedWorkTimeMilliseconds,
  filterWorkTimeEntries,
  formatWorkTimeDuration,
  getWorkTimeTags,
  readWorkTimeSnapshot,
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
  const [description, setDescription] = useState('');
  const [tags, setTags] = useState('');
  const [busy, setBusy] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const [now, setNow] = useState(Date.now());
  const [reportFilter, setReportFilter] = useState<WorkTimeReportFilter>({ fromDate: '', toDate: '', tag: '' });
  const [editingEntryId, setEditingEntryId] = useState<string | null>(null);
  const [editDescription, setEditDescription] = useState('');
  const [editTags, setEditTags] = useState('');
  const [deleteCandidateId, setDeleteCandidateId] = useState<string | null>(null);
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
      if (actionId === 'saveEntry' || actionId === 'discardPending') {
        setDescription('');
        setTags('');
      }
      return true;
    } catch (error) {
      if (alive.current) setNotice(error instanceof Error ? error.message : 'Work-time action failed.');
      return false;
    } finally {
      if (alive.current) setBusy(null);
    }
  };

  const cleanedDescription = description.trim();
  const tagValues = useMemo(() => tags.split(',').map((tag) => tag.trim()).filter(Boolean), [tags]);
  const reportEntries = useMemo(() => snapshot
    ? filterWorkTimeEntries(snapshot.history, reportFilter).slice().reverse()
    : [], [snapshot, reportFilter]);
  const reportTags = useMemo(() => getWorkTimeTags(snapshot?.history ?? []), [snapshot]);
  const reportTotals = useMemo(() => calculateWorkTimeReportTotals(snapshot?.history ?? [], new Date(now)), [snapshot, now]);

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
      const saved = await services.files.saveWorkTimeCsv(createWorkTimeCsv(reportEntries));
      setNotice(saved ? 'Filtered work-time report exported.' : 'CSV export cancelled.');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not export the work-time report.');
    } finally {
      if (alive.current) setBusy(null);
    }
  };

  const commands = useMemo(() => [
    { id: 'start-work', label: 'Start work timer', keywords: ['begin interval'], disabled: !!busy || !snapshot || !!snapshot.active || !!snapshot.pending, run: async () => { await dispatch('start'); } },
    { id: 'stop-work', label: 'Stop work timer', keywords: ['end interval'], disabled: !!busy || !snapshot?.active, run: async () => { await dispatch('stop'); } },
    { id: 'save-work-entry', label: 'Save work entry', keywords: ['description tags log'], disabled: !!busy || !snapshot?.pending || !cleanedDescription, run: async () => { await dispatch('saveEntry', { description: cleanedDescription, tags: tagValues }); } },
    { id: 'resume-work-entry', label: 'Resume pending entry', keywords: ['continue timer'], disabled: !!busy || !snapshot?.pending, run: async () => { await dispatch('resumePending'); } },
    { id: 'discard-work-entry', label: 'Discard pending entry', keywords: ['delete unsaved'], disabled: !!busy || !snapshot?.pending, confirmationPrompt: 'Discard the unsaved work interval?', run: async () => { await dispatch('discardPending'); } },
    ...(surface === 'workspace' ? [
      { id: 'export-work-time-csv', label: 'Export filtered work log as CSV', keywords: ['download report spreadsheet'], disabled: !!busy || reportEntries.length === 0, run: exportReport },
      ...reportEntries.flatMap((entry) => [
        { id: `edit-work-time-${entry.id}`, label: `Edit ${entry.description}`, keywords: ['change description tags'], disabled: !!busy, run: () => beginEdit(entry) },
        { id: `delete-work-time-${entry.id}`, label: `Delete ${entry.description}`, keywords: ['remove saved log entry'], disabled: !!busy, confirmationPrompt: `Delete “${entry.description}”?`, run: async () => { await deleteEntry(entry.id); } },
      ]),
    ] : []),
  ], [busy, snapshot, cleanedDescription, tagValues, services, surface, reportEntries, reportFilter, editDescription, editTags, editingEntryId]);
  useRegisterTabCommands(tab.id, commands);

  const submitEntry = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    void dispatch('saveEntry', { description: cleanedDescription, tags: tagValues });
  };

  const activeElapsed = snapshot?.active ? elapsedWorkTimeMilliseconds(snapshot.active, now) : 0;
  return <section className="module-view work-time-view" aria-label="Work Time Log" data-module-id={tab.id}>
    <header className="work-time-heading">
      <div className="work-time-heading-title">
        <span className="work-time-module-icon"><Activity size={17} /></span>
        <div><h1>Work Time Log</h1><p>Track a work interval, then describe what you did.</p></div>
      </div>
      <span className={`work-time-state ${snapshot?.active ? 'is-running' : snapshot?.pending ? 'is-pending' : ''}`}>
        <span className="work-time-state-dot" />{snapshot?.active ? 'Recording' : snapshot?.pending ? 'Needs details' : 'Ready'}
      </span>
    </header>

    {notice && <p className="work-time-notice" role="status">{notice}</p>}

    {!snapshot ? <div className="work-time-loading" role="status"><Clock3 size={20} /><p>Loading your work log…</p></div> : <>
      <section className="work-time-current-card" aria-label="Current work interval">
        {snapshot.active ? <>
          <div className="work-time-clock-block">
            <span className="work-time-eyebrow"><span className="work-time-live-dot" /> ACTIVE INTERVAL</span>
            <strong className="work-time-clock" aria-label={`${formatWorkTimeDuration(activeElapsed)} elapsed`}>{formatWorkTimeDuration(activeElapsed)}</strong>
            <span className="work-time-started">Started at {new Date(snapshot.active.startedUtc).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}</span>
          </div>
          <div className="work-time-current-copy">
            <h2>Work is being recorded</h2>
            <p>Stop when you finish. You can then add a description and optional tags before saving the entry.</p>
            <button className="primary-button work-time-stop" type="button" disabled={!!busy} onClick={() => void dispatch('stop')}>
              <X size={14} /> {busy === 'stop' ? 'Stopping…' : 'Stop work'}
            </button>
          </div>
        </> : snapshot.pending ? <>
          <div className="work-time-clock-block is-pending">
            <span className="work-time-eyebrow">UNSAVED INTERVAL</span>
            <strong className="work-time-clock">{formatWorkTimeDuration(snapshot.pending.durationMilliseconds)}</strong>
            <span className="work-time-started">{new Date(snapshot.pending.startedUtc).toLocaleString()}</span>
          </div>
          <div className="work-time-current-copy">
            <h2>Add details to save</h2>
            <p>This stopped interval is kept until you save it, resume it, or discard it.</p>
          </div>
        </> : <>
          <div className="work-time-clock-block is-ready">
            <span className="work-time-eyebrow">READY</span>
            <strong className="work-time-clock">Start</strong>
            <span className="work-time-started">The timer continues across Automator restarts.</span>
          </div>
          <div className="work-time-current-copy">
            <h2>Track your work</h2>
            <p>Start a timer when you begin. Stop it when you’re done and record a short description.</p>
            <button className="primary-button work-time-start" type="button" disabled={!!busy} onClick={() => void dispatch('start')}>
              <Play size={14} /> {busy === 'start' ? 'Starting…' : 'Start work'}
            </button>
          </div>
        </>}
      </section>

      {snapshot.pending && <section className="work-time-entry-card" aria-label="Save work interval details">
        <header><div><Check size={14} /><h2>Save this interval</h2></div><span>{formatWorkTimeDuration(snapshot.pending.durationMilliseconds)}</span></header>
        <form onSubmit={submitEntry}>
          <label className="work-time-field">
            <span>Description <b>Required</b></span>
            <textarea aria-label="Work description" value={description} maxLength={500} rows={2} placeholder="What did you work on?"
              disabled={!!busy} onChange={(event) => setDescription(event.target.value)} />
          </label>
          <label className="work-time-field">
            <span>Tags <b>Optional</b></span>
            <div className="work-time-tags-input"><Tag size={13} /><input aria-label="Work tags" value={tags} maxLength={500} placeholder="planning, release, bug fix"
              disabled={!!busy} onChange={(event) => setTags(event.target.value)} /><small>Separate with commas</small></div>
          </label>
          {description.length > 0 && description.length < 500 && <span className="work-time-character-count">{description.length}/500</span>}
          {description.length >= 500 && <span className="work-time-character-count">500/500</span>}
          <div className="work-time-entry-actions">
            <button className="primary-button" type="submit" disabled={!!busy || !cleanedDescription}><Check size={13} /> Save entry</button>
            <button className="secondary-button" type="button" disabled={!!busy} onClick={() => void dispatch('resumePending')}><Play size={12} /> Resume</button>
            <button className="work-time-discard" type="button" disabled={!!busy} onClick={() => { if (window.confirm('Discard this unsaved work interval?')) void dispatch('discardPending'); }}><Trash2 size={12} /> Discard</button>
          </div>
        </form>
      </section>}

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
                <span className="work-time-history-duration">{formatWorkTimeDuration(entry.durationMilliseconds)}</span>
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
