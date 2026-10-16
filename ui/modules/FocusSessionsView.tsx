import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Activity, Check, Clock3, Download, History, Pause, Play, Trash2, X } from 'lucide-react';
import type { AutomationModuleActionRequest, BackendUiState } from '../../contracts/rpc';
import type { AutomationServices } from '../automationServices';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import { InlineConfirmButton } from '../components/InlineConfirmButton';
import { isWorkTimeToggleShortcut } from './workTimeShortcut';
import { WorkTimeTagsInput } from './WorkTimeTagsInput';
import {
  calculateWorkTimeReportTotals,
  calculateWorkTimeIntervalMilliseconds,
  createWorkTimeCsv,
  elapsedWorkTimeMilliseconds,
  filterWorkTimeEntries,
  formatWorkTimeDuration,
  getRecentWorkTimeTags,
  getWorkTimeTags,
  isWorkTimeDescriptionValid,
  localDateTimeInputValue,
  mergeWorkTimeHistory,
  parseWorkTimeTags,
  readWorkTimeHistoryPage,
  readWorkTimeSnapshot,
  resolveWorkTimeDuration,
  WORK_TIME_QUICK_ADD_MINUTES,
  workTimeQuickAddLabel,
  workTimeQuickAddMilliseconds,
  resolveWorkTimeInterval,
  WORK_TIME_HISTORY_PAGE_SIZE,
  workTimeEntryDurationMilliseconds,
  type WorkTimeEntry,
  type WorkTimeHistoryPage,
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
  // Older pages loaded with getHistory; any mutating action resets them to the snapshot's newest page.
  const [olderHistory, setOlderHistory] = useState<WorkTimeEntry[]>([]);
  const [startDescription, setStartDescription] = useState('');
  const [draftDescriptions, setDraftDescriptions] = useState<Record<string, string>>({});
  const [draftTags, setDraftTags] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const [now, setNow] = useState(Date.now());
  const [workingPeriodOnly, setWorkingPeriodOnly] = useState(false);
  const [reportFilter, setReportFilter] = useState<WorkTimeReportFilter>({ fromDate: '', toDate: '', tag: '' });
  const [editingEntryId, setEditingEntryId] = useState<string | null>(null);
  const [editStart, setEditStart] = useState('');
  const [editStop, setEditStop] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [editTags, setEditTags] = useState('');
  const [deleteCandidateId, setDeleteCandidateId] = useState<string | null>(null);
  const [manualHours, setManualHours] = useState('');
  const [manualMinutes, setManualMinutes] = useState('');
  const [manualDescription, setManualDescription] = useState('');
  const [manualTags, setManualTags] = useState('');
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
      setOlderHistory([]);
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
    if (!isWorkTimeDescriptionValid(description)) {
      setNotice('Enter a work description before saving.');
      return;
    }
    const tags = parseWorkTimeTags(String(form.get('tags') ?? ''));
    void dispatch('saveEntry', { id, description, tags }).then((saved) => {
      if (!saved) return;
      const forget = (current: Record<string, string>) => {
        const next = { ...current };
        delete next[id];
        return next;
      };
      setDraftDescriptions(forget);
      setDraftTags(forget);
    });
  };
  const history = useMemo(() => mergeWorkTimeHistory(snapshot?.history ?? [], olderHistory), [snapshot, olderHistory]);
  const reportEntries = useMemo(() => filterWorkTimeEntries(history, reportFilter).slice().reverse(), [history, reportFilter]);
  const reportTags = useMemo(() => getWorkTimeTags(history), [history]);
  const knownTags = useMemo(() => getRecentWorkTimeTags([...history, ...(snapshot?.pendingEntries ?? [])]), [history, snapshot]);
  const reportTotals = useMemo(() => calculateWorkTimeReportTotals(history, new Date(now), workingPeriodOnly), [history, now, workingPeriodOnly]);
  const hasSavedHistory = (snapshot?.historyTotal ?? 0) > 0;

  const fetchHistoryPage = async (offset: number): Promise<WorkTimeHistoryPage> => {
    const result = await services.modules.dispatch('getHistory', { offset, limit: WORK_TIME_HISTORY_PAGE_SIZE });
    if (result.status === 'error') throw new Error(result.message);
    const page = readWorkTimeHistoryPage(result.data);
    if (!page) throw new Error('Work Time returned invalid history data.');
    return page;
  };

  const loadOlderEntries = async () => {
    if (busy || !snapshot) return;
    setBusy('loadHistory');
    setNotice('');
    try {
      const page = await fetchHistoryPage(snapshot.history.length + olderHistory.length);
      if (!page.entries.length) throw new Error('No older work entries were returned.');
      if (alive.current) setOlderHistory((current) => mergeWorkTimeHistory(page.entries, current));
    } catch (error) {
      if (alive.current) setNotice(error instanceof Error ? error.message : 'Could not load older work entries.');
    } finally {
      if (alive.current) setBusy(null);
    }
  };

  const beginEdit = (entry: WorkTimeSnapshot['history'][number]) => {
    setEditingEntryId(entry.id);
    setEditStart(localDateTimeInputValue(entry.startedUtc));
    setEditStop(localDateTimeInputValue(entry.stoppedUtc));
    setEditDescription(entry.description);
    setEditTags(entry.tags.join(', '));
    setDeleteCandidateId(null);
  };

  const editingEntry = history.find((entry) => entry.id === editingEntryId) ?? null;
  const editInterval = editingEntry ? resolveWorkTimeInterval(editStart, editStop, editingEntry, now) : null;
  const editError = editInterval && 'error' in editInterval ? editInterval.error : '';
  const editCollapsesPauses = !!editingEntry && editingEntry.segments.length > 1 && !!editInterval && 'startedUtc' in editInterval
    && (editInterval.startedUtc !== editingEntry.startedUtc || editInterval.stoppedUtc !== editingEntry.stoppedUtc);

  const saveEdit = async () => {
    if (!editingEntry) return;
    const interval = resolveWorkTimeInterval(editStart, editStop, editingEntry);
    if ('error' in interval) {
      setNotice(interval.error);
      return;
    }
    const ok = await dispatch('updateEntry', {
      id: editingEntry.id,
      startedUtc: interval.startedUtc,
      stoppedUtc: interval.stoppedUtc,
      description: editDescription.trim(),
      tags: parseWorkTimeTags(editTags),
    });
    if (ok) setEditingEntryId(null);
  };

  const manualHasDuration = manualHours.trim() !== '' || manualMinutes.trim() !== '';
  const manualInterval = resolveWorkTimeDuration(manualHours, manualMinutes, now);
  const manualError = manualHasDuration && 'error' in manualInterval ? manualInterval.error : '';
  const manualDurationLabel = 'startedUtc' in manualInterval
    ? formatWorkTimeDuration(Date.parse(manualInterval.stoppedUtc) - Date.parse(manualInterval.startedUtc))
    : '';
  const manualDescriptionValue = manualDescription.trim();
  const submitManualEntry = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const interval = resolveWorkTimeDuration(manualHours, manualMinutes, Date.now());
    if ('error' in interval) {
      setNotice(interval.error);
      return;
    }
    const ok = await dispatch('createEntry', {
      startedUtc: interval.startedUtc,
      stoppedUtc: interval.stoppedUtc,
      description: manualDescriptionValue,
      tags: parseWorkTimeTags(manualTags),
    });
    if (!ok) return;
    setManualHours('');
    setManualMinutes('');
    setManualDescription('');
    setManualTags('');
  };

  const deleteEntry = async (id: string) => {
    const ok = await dispatch('deleteEntry', { id });
    if (ok) {
      setDeleteCandidateId(null);
      if (editingEntryId === id) setEditingEntryId(null);
    }
  };

  const exportReport = async () => {
    if (!snapshot || busy || !hasSavedHistory) return;
    setBusy('exportCsv');
    setNotice('');
    try {
      // The export covers every stored entry, so load the remaining pages before filtering.
      let older = olderHistory;
      while (snapshot.history.length + older.length < snapshot.historyTotal) {
        const page = await fetchHistoryPage(snapshot.history.length + older.length);
        const merged = mergeWorkTimeHistory(page.entries, older);
        if (merged.length === older.length) throw new Error('Could not load every saved work entry before exporting. Reload the log and try again.');
        older = merged;
      }
      if (alive.current) setOlderHistory(older);
      const entries = filterWorkTimeEntries(mergeWorkTimeHistory(snapshot.history, older), reportFilter).slice().reverse();
      if (!entries.length) {
        setNotice('No saved work matches these filters.');
        return;
      }
      const saved = await services.files.saveWorkTimeCsv(createWorkTimeCsv(entries, workingPeriodOnly));
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
    { id: 'add-work-entry-manually', label: 'Add work entry manually', keywords: ['register time without timer log past interval'], disabled: !!busy || !snapshot, run: () => { document.getElementById('work-time-manual-hours')?.focus(); } },
    ...(runningTimer ? [{ id: `pause-work-${runningTimer.id}`, label: `Pause ${runningTimer.description}`, keywords: ['pause timer'], disabled: !!busy, run: async () => { await dispatch('pause', { id: runningTimer.id }); } }] : []),
    ...((snapshot?.timers ?? []).filter((timer) => timer.status === 'paused').flatMap((timer) => [
      { id: `resume-work-${timer.id}`, label: `Resume ${timer.description}`, keywords: ['continue timer'], disabled: !!busy || !!runningTimer, run: async () => { await dispatch('resume', { id: timer.id }); } },
      { id: `end-work-${timer.id}`, label: `End ${timer.description}`, keywords: ['finish timer'], disabled: !!busy, run: async () => { await dispatch('end', { id: timer.id }); } },
      { id: `discard-work-${timer.id}`, label: `Discard ${timer.description}`, keywords: ['remove timer'], disabled: !!busy, confirmationPrompt: `Discard “${timer.description}”?`, run: async () => { await dispatch('discardTimer', { id: timer.id }); } },
    ])),
    ...((snapshot?.pendingEntries ?? []).map((entry) => ({ id: `discard-pending-${entry.id}`, label: `Discard unsaved ${entry.description || 'timer'}`, keywords: ['delete unsaved draft'], disabled: !!busy, confirmationPrompt: 'Discard this unsaved work interval?', run: async () => { await dispatch('discardPending', { id: entry.id }); } }))),
    ...(surface === 'workspace' ? [
      { id: 'export-work-time-csv', label: 'Export filtered work log as CSV', keywords: ['download report spreadsheet'], disabled: !!busy || !hasSavedHistory, run: exportReport },
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

      <section className="work-time-entry-card work-time-manual-card" aria-label="Add a work entry manually">
        <header><div><Clock3 size={14} /><h2>Add time without the timer</h2></div><span>{manualDurationLabel ? `${manualDurationLabel}, ending now` : 'Ends now'}</span></header>
        <form onSubmit={(event) => void submitManualEntry(event)}>
          <div className="work-time-manual-times">
            <label className="work-time-field">
              <span>Hours</span>
              <input id="work-time-manual-hours" aria-label="Manual entry hours" type="number" inputMode="numeric" min={0} max={99} step={1} placeholder="0" value={manualHours}
                disabled={!!busy} onChange={(event) => setManualHours(event.target.value)} />
            </label>
            <label className="work-time-field">
              <span>Minutes</span>
              <input aria-label="Manual entry minutes" type="number" inputMode="numeric" min={0} max={59} step={1} placeholder="0" value={manualMinutes}
                disabled={!!busy} onChange={(event) => setManualMinutes(event.target.value)} />
            </label>
          </div>
          {manualError && <p className="work-time-field-error" role="alert">{manualError}</p>}
          <label className="work-time-field">
            <span>Description <b>Required</b></span>
            <textarea aria-label="Manual entry description" value={manualDescription} maxLength={500} rows={2} placeholder="What did you work on?"
              disabled={!!busy} onChange={(event) => setManualDescription(event.target.value)} />
          </label>
          <div className="work-time-field">
            <span>Tags <b>Optional</b></span>
            <WorkTimeTagsInput ariaLabel="Manual entry tags" value={manualTags} knownTags={knownTags} disabled={!!busy} onChange={setManualTags} />
          </div>
          <div className="work-time-entry-actions">
            <button className="primary-button" type="submit" disabled={!!busy || !manualDescriptionValue || 'error' in manualInterval}><Check size={13} /> Add entry</button>
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
              {isRunning && <div className="work-time-quick-add" role="group" aria-label="Add time to the running timer">
                {WORK_TIME_QUICK_ADD_MINUTES.map((minutes) => <button className="secondary-button" type="button" key={minutes} disabled={!!busy}
                  aria-label={`Add ${minutes} ${minutes === 1 ? 'minute' : 'minutes'} to ${timer.description}`}
                  onClick={() => void dispatch('addTime', { id: timer.id, milliseconds: workTimeQuickAddMilliseconds(minutes) })}>{workTimeQuickAddLabel(minutes)}</button>)}
              </div>}
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
                  <InlineConfirmButton className="work-time-discard" label="Discard" confirmLabel={`Yes, discard ${timer.description}`} disabled={!!busy}
                    warning={`Discard timer “${timer.description}”?`} onConfirm={() => dispatch('discardTimer', { id: timer.id })}><Trash2 size={13} /> Discard</InlineConfirmButton>
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
            <textarea name="description" aria-label={`Work description ${entry.description || entry.id}`} value={draftDescriptions[entry.id] ?? entry.description}
              onChange={(event) => setDraftDescriptions((current) => ({ ...current, [entry.id]: event.target.value }))}
              maxLength={500} rows={2} placeholder="What did you work on?" disabled={!!busy} required />
          </label>
          <div className="work-time-field"><span>Tags <b>Optional</b></span>
            <WorkTimeTagsInput name="tags" ariaLabel={`Work tags ${entry.description || entry.id}`} value={draftTags[entry.id] ?? entry.tags.join(', ')} knownTags={knownTags} disabled={!!busy}
              onChange={(value) => setDraftTags((current) => ({ ...current, [entry.id]: value }))} />
          </div>
          <div className="work-time-entry-actions">
            <button className="primary-button" type="submit" disabled={!!busy || !isWorkTimeDescriptionValid(draftDescriptions[entry.id] ?? entry.description)}><Check size={13} /> Save entry</button>
            <InlineConfirmButton className="work-time-discard" label="Discard draft" confirmLabel="Yes, discard draft" disabled={!!busy}
                warning="Discard this unsaved work interval?" onConfirm={() => dispatch('discardPending', { id: entry.id })}><Trash2 size={13} /> Discard draft</InlineConfirmButton>
          </div>
        </form>
      </section>)}

      {surface === 'workspace' && <section className="work-time-reports" aria-label="Work time reports">
        <header className="work-time-reports-heading">
          <div><History size={15} /><div><h2>Work log</h2><p>Review, filter, and export your saved work.</p></div></div>
          <button className="secondary-button work-time-export" type="button" disabled={!!busy || !hasSavedHistory} onClick={() => void exportReport()}>
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
          ? <div className="work-time-history-empty"><span><History size={18} /></span><p>{history.length ? 'No saved work matches these filters.' : 'Saved intervals will appear here.'}</p></div>
          : <div className="work-time-report-list">{reportEntries.map((entry) => {
            const isEditing = editingEntryId === entry.id;
            const isDeleting = deleteCandidateId === entry.id;
            return <article className="work-time-report-row" key={entry.id}>
              {isEditing ? <form className="work-time-edit-form" onSubmit={(event) => { event.preventDefault(); void saveEdit(); }}>
                <label><span>Start</span><input aria-label="Edit work start time" type="datetime-local" step={1} value={editStart} onChange={(event) => setEditStart(event.target.value)} disabled={!!busy} /></label>
                <label><span>End</span><input aria-label="Edit work end time" type="datetime-local" step={1} value={editStop} onChange={(event) => setEditStop(event.target.value)} disabled={!!busy} /></label>
                <label><span>Description</span><input aria-label="Edit work description" maxLength={500} value={editDescription} onChange={(event) => setEditDescription(event.target.value)} disabled={!!busy} /></label>
                <div className="work-time-edit-tags"><span>Tags</span><WorkTimeTagsInput ariaLabel="Edit work tags" value={editTags} knownTags={knownTags} disabled={!!busy} hint={false} placeholder="" onChange={setEditTags} /></div>
                {editError && <p className="work-time-field-error work-time-edit-error" role="alert">{editError}</p>}
                {!editError && editCollapsesPauses && <p className="work-time-field-hint work-time-edit-error">Changing the start or end replaces this entry's pauses with one continuous interval.</p>}
                <div className="work-time-report-actions">
                  <button className="primary-button" type="submit" disabled={!!busy || !editDescription.trim() || !!editError}><Check size={12} /> Save work entry changes</button>
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
        {snapshot && history.length < snapshot.historyTotal && <div className="work-time-history-more">
          <span>Showing {history.length} of {snapshot.historyTotal} entries</span>
          <button className="secondary-button" type="button" disabled={!!busy} aria-label="Load older work entries" onClick={() => void loadOlderEntries()}>
            {busy === 'loadHistory' ? 'Loading…' : 'Load older entries'}
          </button>
        </div>}
      </section>}
    </>}
  </section>;
}
