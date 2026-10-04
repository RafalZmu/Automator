import { CalendarClock, CalendarDays, ChevronLeft, ChevronRight, Clock3, List, Pencil, Play, Plus, Save, Trash2, X } from 'lucide-react';
import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import type { AutomationServices } from '../automationServices';
import type { AutomationModuleActionRequest, BackendUiState } from '../../contracts/rpc';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import { describeRecurrence, readSchedulerSnapshot, scheduleWeekdays, validateSchedule, type ScheduleDefinition, type ScheduleRecurrence, type ScheduleTargetKind, type SchedulerSnapshot } from '../contracts/scheduler';
import { buildCalendarMonth, buildUpcomingScheduleEntries } from './schedulerAgendaViewModel';

type ViewProps = { tab: BackendUiState['tabs'][number]; moduleState: BackendUiState['moduleStates'][number] | undefined; services: AutomationServices };
const emptySnapshot: SchedulerSnapshot = { schedules: [], history: [], states: [], workflows: [], scripts: [], running: false };
function blankSchedule(targetKind: ScheduleTargetKind = 'workflow', profileId = ''): ScheduleDefinition {
  return { id: `schedule-${Date.now().toString(36)}`, name: '', targetKind, profileId, enabled: true, runOnceAfterRestart: false,
    recurrence: { kind: 'interval', intervalMinutes: 60, localTime: null, daysOfWeek: null, intervalAnchorUtc: null } };
}
function switchRecurrence(kind: ScheduleRecurrence['kind']): ScheduleRecurrence {
  return { kind, intervalMinutes: kind === 'interval' ? 60 : null, localTime: kind === 'interval' ? null : '09:00:00',
    daysOfWeek: kind === 'weekly' ? ['monday'] : null, intervalAnchorUtc: null };
}
const styles = `
.scheduler-view{gap:8px}.scheduler-heading{justify-content:space-between;gap:10px;flex:none}.scheduler-list{display:flex;flex-direction:column;gap:7px;overflow:auto;min-height:0;flex:1}
.scheduler-card{padding:9px;border:1px solid var(--line);border-radius:11px;background:var(--control)}.scheduler-card-top{display:flex;gap:8px;align-items:center}.scheduler-copy{flex:1;min-width:0}.scheduler-copy strong{display:block;color:var(--ink);font-size:10px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.scheduler-copy small{color:var(--muted);font-size:8px}
.scheduler-card-top button{min-width:28px;min-height:28px}.scheduler-meta{display:flex;flex-wrap:wrap;gap:7px;margin-top:7px;color:var(--muted-strong);font-size:8px}.scheduler-meta span{display:flex;align-items:center;gap:3px}.scheduler-state{color:var(--accent-strong)}.scheduler-missing{color:var(--danger)!important}
.scheduler-check{display:flex;align-items:center;gap:7px;color:var(--muted-strong);font-size:9px}.scheduler-check input{width:13px;height:13px;accent-color:var(--accent)}.scheduler-days{display:flex;gap:3px;flex-wrap:wrap}.scheduler-day{padding:5px 7px;border:1px solid var(--line);border-radius:7px;color:var(--muted-strong);background:var(--control);font-size:8px}.scheduler-day[aria-pressed=true]{color:var(--accent-strong);background:var(--accent-soft);border-color:var(--accent)}
.scheduler-help{margin:0;color:var(--muted);font-size:8px;line-height:1.5}.scheduler-notice{margin:0;color:var(--muted-strong);font-size:9px}.scheduler-validation{margin:0;padding:7px 8px;color:var(--danger);font-size:9px;border:1px solid var(--line);border-radius:8px}.scheduler-history{flex:none;max-height:125px;overflow:auto;padding:8px;border:1px solid var(--line);border-radius:10px}.scheduler-history strong{color:var(--ink);font-size:9px}.scheduler-history-row{display:flex;align-items:center;justify-content:space-between;gap:8px;padding:5px 0;border-bottom:1px solid var(--line);color:var(--muted);font-size:8px}.scheduler-history-row:last-child{border:0}
.scheduler-upcoming{flex:none;display:flex;flex-direction:column;gap:6px;max-height:205px;min-height:0;padding:8px;overflow:auto;border:1px solid var(--line);border-radius:11px;background:rgba(255,255,255,.23)}.window-shell[data-theme="dark"] .scheduler-upcoming{background:rgba(14,23,27,.22)}
.scheduler-upcoming-heading,.scheduler-upcoming-heading>div,.scheduler-view-switch,.scheduler-calendar-heading{display:flex;align-items:center}.scheduler-upcoming-heading{justify-content:space-between;gap:8px}.scheduler-upcoming-heading h2{margin:0;color:var(--ink);font-size:10px;font-weight:670}.scheduler-upcoming-heading>div{gap:6px;color:var(--muted)}.scheduler-view-switch{gap:4px}.scheduler-view-switch button{min-height:25px;display:inline-flex;align-items:center;gap:4px;padding:0 7px;font-size:8px}.scheduler-view-switch button[aria-pressed=true]{color:var(--accent-strong);border-color:var(--accent);background:var(--accent-soft)}
.scheduler-agenda-list{display:flex;flex-direction:column;gap:4px;margin:0;padding:0;overflow:auto;list-style:none}.scheduler-agenda-item{min-width:0;display:grid;grid-template-columns:minmax(0,1fr) auto;gap:3px 8px;padding:6px 7px;border:1px solid var(--line);border-radius:8px;background:rgba(255,255,255,.28)}.window-shell[data-theme="dark"] .scheduler-agenda-item{background:rgba(255,255,255,.035)}.scheduler-agenda-item>time{grid-column:1/-1;color:var(--accent-strong);font-size:8px;font-variant-numeric:tabular-nums}.scheduler-agenda-item>strong{min-width:0;overflow:hidden;color:var(--ink);font-size:9px;text-overflow:ellipsis;white-space:nowrap}.scheduler-agenda-item>span{grid-column:1/-1;overflow:hidden;color:var(--muted);font-size:8px;text-overflow:ellipsis;white-space:nowrap}.scheduler-agenda-item .scheduler-running-chip{grid-column:2;grid-row:2;align-self:center;color:var(--accent-strong);font-size:7px;font-weight:650}
.scheduler-upcoming-empty,.scheduler-upcoming-loading{margin:0;padding:9px;color:var(--muted);font-size:8px;line-height:1.45}.scheduler-calendar{display:flex;flex-direction:column;gap:5px}.scheduler-calendar-heading{justify-content:space-between;gap:8px}.scheduler-calendar-heading h3{margin:0;color:var(--ink);font-size:9px;font-weight:650;text-align:center}.scheduler-calendar-heading button{width:24px;height:24px}.scheduler-calendar-weekdays,.scheduler-calendar-grid{display:grid;grid-template-columns:repeat(7,minmax(0,1fr));gap:3px;margin:0;padding:0;list-style:none}.scheduler-calendar-weekdays span{color:var(--muted);font-size:7px;font-weight:650;text-align:center}.scheduler-calendar-grid{max-height:145px;overflow:auto}.scheduler-calendar-day{min-width:0;min-height:37px;padding:3px;border:1px solid var(--line);border-radius:6px;background:rgba(255,255,255,.24)}.window-shell[data-theme="dark"] .scheduler-calendar-day{background:rgba(255,255,255,.025)}.scheduler-calendar-day[data-outside-month=true]{opacity:.48}.scheduler-calendar-day[data-today=true]{border-color:var(--accent)}.scheduler-calendar-day>time{display:block;color:var(--muted-strong);font-size:7px;font-variant-numeric:tabular-nums}.scheduler-calendar-events{display:flex;flex-direction:column;gap:2px;margin:2px 0 0;padding:0;list-style:none}.scheduler-calendar-run{display:flex;gap:2px;min-width:0;color:var(--accent-strong);font-size:6px;line-height:1.25}.scheduler-calendar-run time{flex:none;font-variant-numeric:tabular-nums}.scheduler-calendar-run span{min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.scheduler-calendar-empty{margin:0;color:var(--muted);font-size:8px}.scheduler-calendar-fallback{border-top:1px solid var(--line);padding-top:4px}.scheduler-calendar-fallback summary{color:var(--muted-strong);font-size:8px;font-weight:620;cursor:pointer}.scheduler-calendar-fallback .scheduler-agenda-list{max-height:92px;margin-top:5px}
.scheduler-calendar-events{max-height:48px;overflow:auto}.scheduler-calendar-run{font-size:7px}
`;

export function SchedulerView({ services }: ViewProps) {
  const [snapshot, setSnapshot] = useState<SchedulerSnapshot>(emptySnapshot);
  const [snapshotLoaded, setSnapshotLoaded] = useState(false);
  const [draft, setDraft] = useState<ScheduleDefinition | null>(null);
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState<string | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [upcomingView, setUpcomingView] = useState<'agenda' | 'calendar'>('agenda');
  const [calendarAnchor, setCalendarAnchor] = useState(() => new Date());
  const alive = useRef(true);
  const refreshing = useRef(false);
  const refresh = useCallback(async () => {
    if (refreshing.current) return;
    refreshing.current = true;
    try {
      const result = await services.modules.dispatch('getSnapshot', {});
      if (result.status === 'error') throw new Error(result.message);
      const data = readSchedulerSnapshot(result.data);
      if (!data) throw new Error('Scheduler returned invalid state.');
      if (alive.current) setSnapshot(data);
    } catch (error) { if (alive.current) setNotice(error instanceof Error ? error.message : 'Could not refresh Scheduler.'); }
    finally {
      refreshing.current = false;
      if (alive.current) setSnapshotLoaded(true);
    }
  }, [services]);
  useEffect(() => {
    alive.current = true;
    void refresh();
    const timer = window.setInterval(() => void refresh(), 5000);
    return () => { alive.current = false; window.clearInterval(timer); };
  }, [refresh]);

  const action = async (actionId: string, input: AutomationModuleActionRequest['input'], key: string) => {
    if (busy) return false;
    setBusy(key);
    try {
      const result = await services.modules.dispatch(actionId, input);
      if (!alive.current) return false;
      setNotice(result.message);
      await refresh();
      return result.status !== 'error';
    } catch (error) {
      if (alive.current) setNotice(error instanceof Error ? error.message : 'Scheduler action failed.');
      return false;
    } finally { if (alive.current) setBusy(null); }
  };
  const save = async (event: FormEvent) => {
    event.preventDefault();
    if (!draft) return;
    const issues = validateSchedule(draft);
    const catalog = draft.targetKind === 'workflow' ? snapshot.workflows : snapshot.scripts;
    if (!catalog.some((profile) => profile.profileId === draft.profileId)) issues.push(`Select an existing saved ${draft.targetKind === 'workflow' ? 'workflow' : 'script profile'}.`);
    setErrors(issues);
    if (!issues.length && await action('saveSchedule', { ...draft, workflowId: draft.targetKind === 'workflow' ? draft.profileId : '', name: draft.name.trim() }, draft.id)) setDraft(null);
  };
  const change = (patch: Partial<ScheduleDefinition>) => setDraft((current) => current ? { ...current, ...patch } : null);
  const recurrence = (patch: Partial<ScheduleRecurrence>) => setDraft((current) => current ? { ...current, recurrence: { ...current.recurrence, ...patch } } : null);
  const edit = (schedule: ScheduleDefinition) => { setErrors([]); setDraft({ ...schedule, recurrence: { ...schedule.recurrence, daysOfWeek: schedule.recurrence.daysOfWeek ? [...schedule.recurrence.daysOfWeek] : null } }); };
  const now = new Date();
  const upcomingRuns = buildUpcomingScheduleEntries(snapshot, now);
  const calendarMonth = buildCalendarMonth(calendarAnchor, upcomingRuns, now);
  const hasRunsInCalendarMonth = calendarMonth.days.some((day) => day.inMonth && day.entries.length > 0);
  const runningCount = snapshot.states.filter((state) => state.running).length;
  const shiftCalendarMonth = (offset: number) => setCalendarAnchor((current) => new Date(current.getFullYear(), current.getMonth() + offset, 1));
  const upcomingList = <ol className="scheduler-agenda-list" aria-label="Upcoming scheduled runs">
    {upcomingRuns.map((entry) => <li className="scheduler-agenda-item" key={entry.scheduleId}>
      <time dateTime={entry.scheduledAt.toISOString()}>{entry.dateLabel} · {entry.timeLabel}</time>
      <strong>{entry.scheduleName}</strong>
      {entry.currentlyRunning && <span className="scheduler-running-chip">Running now</span>}
      <span>{entry.targetLabel}</span>
    </li>)}
  </ol>;
  const commands = useMemo(() => [
    { id: 'scheduler:refresh', label: 'Refresh schedules', keywords: ['reload', 'update'], run: refresh },
    { id: 'scheduler:new', label: 'Create schedule', keywords: ['new', 'workflow', 'script'], disabled: !!busy || (snapshot.workflows.length === 0 && snapshot.scripts.length === 0), run: () => {
      setErrors([]);
      const targetKind: ScheduleTargetKind = snapshot.workflows.length ? 'workflow' : 'scriptProfile';
      const profileId = targetKind === 'workflow' ? snapshot.workflows[0]?.profileId : snapshot.scripts[0]?.profileId;
      setDraft(blankSchedule(targetKind, profileId));
    } },
    ...snapshot.schedules.map((schedule) => {
      const catalog = schedule.targetKind === 'workflow' ? snapshot.workflows : snapshot.scripts;
      const profile = catalog.find((item) => item.profileId === schedule.profileId);
      const state = snapshot.states.find((item) => item.scheduleId === schedule.id);
      return { id: `scheduler:run:${schedule.id}`, label: `Run ${schedule.name} now`, keywords: [schedule.profileId, profile?.name ?? '', schedule.targetKind],
        disabled: !!busy || !!state?.running || !profile, confirmationPrompt: `Run schedule “${schedule.name}” now?`,
        run: async () => { await action('runNow', { scheduleId: schedule.id }, schedule.id); } };
    }),
  ], [action, busy, refresh, snapshot]);
  useRegisterTabCommands('scheduler', commands);

  return <section className="module-view script-runner-view scheduler-view" aria-label="Scheduler">
    <style>{styles}</style>
    <header className="script-heading scheduler-heading">
      <div className="script-heading-title"><CalendarClock size={19} /><div><h1>Scheduler</h1><p>Saved workflows and scripts, on your local clock</p></div></div>
      <button className="secondary-button" disabled={!!busy || (snapshot.workflows.length === 0 && snapshot.scripts.length === 0)} onClick={() => { setErrors([]); const targetKind = snapshot.workflows.length ? 'workflow' : 'scriptProfile'; const profileId = targetKind === 'workflow' ? snapshot.workflows[0]?.profileId : snapshot.scripts[0]?.profileId; setDraft(blankSchedule(targetKind, profileId)); }}><Plus size={13} /> New</button>
    </header>
    <p className="scheduler-help">Runs while Automator is in the tray. Missed runs are skipped unless catch-up is enabled. Full exit stops scheduling.</p>
    {snapshotLoaded && <p role="status" className="scheduler-notice">{snapshot.running
      ? `Scheduler is running${runningCount ? ` · ${runningCount} schedule${runningCount === 1 ? '' : 's'} running now` : ''}.`
      : 'Scheduler is currently stopped.'}</p>}
    {notice && <p role="status" className="scheduler-notice">{notice}</p>}
    <section className="scheduler-upcoming" aria-labelledby="scheduler-upcoming-heading">
      <header className="scheduler-upcoming-heading">
        <div><CalendarClock size={14} /><h2 id="scheduler-upcoming-heading">Upcoming runs</h2></div>
        <div className="scheduler-view-switch" role="group" aria-label="Upcoming runs view">
          <button className="secondary-button" type="button" aria-pressed={upcomingView === 'agenda'} onClick={() => setUpcomingView('agenda')}><List size={11} /> Agenda</button>
          <button className="secondary-button" type="button" aria-pressed={upcomingView === 'calendar'} onClick={() => setUpcomingView('calendar')}><CalendarDays size={11} /> Calendar</button>
        </div>
      </header>
      {!snapshotLoaded ? <p role="status" className="scheduler-upcoming-loading">Loading upcoming runs…</p>
        : upcomingRuns.length === 0 ? <p role="status" className="scheduler-upcoming-empty">No upcoming runs. Enable a saved schedule with a future run time to see it here.</p>
          : upcomingView === 'agenda' ? upcomingList : <>
            <div className="scheduler-calendar">
              <div className="scheduler-calendar-heading">
                <button className="icon-button" type="button" aria-label="Previous month" onClick={() => shiftCalendarMonth(-1)}><ChevronLeft size={13} /></button>
                <h3 aria-live="polite" aria-atomic="true">{calendarMonth.monthLabel}</h3>
                <button className="icon-button" type="button" aria-label="Next month" onClick={() => shiftCalendarMonth(1)}><ChevronRight size={13} /></button>
              </div>
              <div className="scheduler-calendar-weekdays" aria-hidden="true">{calendarMonth.weekdayLabels.map((weekday, index) => <span key={index} title={weekday.full}>{weekday.short}</span>)}</div>
              <ol className="scheduler-calendar-grid" aria-label={`Days in ${calendarMonth.monthLabel}`}>
                {calendarMonth.days.map((day) => <li className="scheduler-calendar-day" key={day.dateKey} data-outside-month={!day.inMonth} data-today={day.isToday}>
                  <time dateTime={day.dateKey} aria-label={day.dateLabel} aria-current={day.isToday ? 'date' : undefined}>{day.dayNumber}</time>
                  {day.entries.length > 0 && <ol className="scheduler-calendar-events" aria-label={`Runs on ${day.dateLabel}`}>
                    {day.entries.map((entry) => <li className="scheduler-calendar-run" key={entry.scheduleId}>
                      <time dateTime={entry.scheduledAt.toISOString()}>{entry.timeLabel}</time><span title={entry.targetLabel}>{entry.scheduleName}</span>
                    </li>)}
                  </ol>}
                </li>)}
              </ol>
              {!hasRunsInCalendarMonth && <p role="status" className="scheduler-calendar-empty">No next runs in this month. The full list below includes later dates.</p>}
              <details className="scheduler-calendar-fallback" open={!hasRunsInCalendarMonth}>
                <summary>List all upcoming runs ({upcomingRuns.length})</summary>
                {upcomingList}
              </details>
            </div>
          </>}
    </section>
    {draft && <form className="script-editor" onSubmit={(event) => void save(event)}>
      <div className="script-editor-heading"><CalendarClock size={14} /><strong>Schedule</strong><button className="icon-button" type="button" aria-label="Close editor" disabled={!!busy} onClick={() => setDraft(null)}><X size={13} /></button></div>
      <div className="script-fields">
        <label><span>Name</span><input required maxLength={128} value={draft.name} onChange={(event) => change({ name: event.target.value })} /></label>
        <label><span>Run</span><select value={draft.targetKind} onChange={(event) => { const targetKind = event.target.value as ScheduleTargetKind; const catalog = targetKind === 'workflow' ? snapshot.workflows : snapshot.scripts; change({ targetKind, profileId: catalog[0]?.profileId ?? '' }); }}><option value="workflow">Workflow</option><option value="scriptProfile">Script profile</option></select></label>
        <label><span>{draft.targetKind === 'workflow' ? 'Workflow profile' : 'Script profile'}</span><select value={draft.profileId} onChange={(event) => change({ profileId: event.target.value })}>
          {(draft.targetKind === 'workflow' ? snapshot.workflows : snapshot.scripts).length === 0 && <option value="">No saved {draft.targetKind === 'workflow' ? 'workflows' : 'script profiles'}</option>}
          {(draft.targetKind === 'workflow' ? snapshot.workflows : snapshot.scripts).map((profile) => <option key={profile.profileId} value={profile.profileId}>{profile.name}</option>)}
        </select></label>
        <label><span>Recurrence</span><select value={draft.recurrence.kind} onChange={(event) => change({ recurrence: switchRecurrence(event.target.value as ScheduleRecurrence['kind']) })}><option value="interval">Interval</option><option value="daily">Daily</option><option value="weekly">Weekly</option></select></label>
        {draft.recurrence.kind === 'interval' ? <label><span>Interval in minutes</span><input type="number" min={1} max={525600} required value={draft.recurrence.intervalMinutes ?? 60} onChange={(event) => recurrence({ intervalMinutes: Number(event.target.value) })} /></label>
          : <label><span>Local time</span><input type="time" step={1} required value={draft.recurrence.localTime ?? '09:00:00'} onChange={(event) => recurrence({ localTime: event.target.value.length === 5 ? `${event.target.value}:00` : event.target.value })} /></label>}
      </div>
      {draft.recurrence.kind === 'weekly' && <div className="scheduler-days" role="group" aria-label="Weekdays">{scheduleWeekdays.map((day) => <button type="button" className="scheduler-day" key={day} aria-pressed={draft.recurrence.daysOfWeek?.includes(day) ?? false} onClick={() => recurrence({ daysOfWeek: draft.recurrence.daysOfWeek?.includes(day) ? draft.recurrence.daysOfWeek.filter((item) => item !== day) : [...(draft.recurrence.daysOfWeek ?? []), day] })}>{day.slice(0, 3)}</button>)}</div>}
      <label className="scheduler-check"><input type="checkbox" checked={draft.enabled} onChange={(event) => change({ enabled: event.target.checked })} /> Enabled</label>
      <label className="scheduler-check"><input type="checkbox" checked={draft.runOnceAfterRestart} onChange={(event) => change({ runOnceAfterRestart: event.target.checked })} /> Run once after restart if a run was missed</label>
      {errors.length > 0 && <div className="scheduler-validation" role="alert">{errors.map((error) => <p key={error}>{error}</p>)}</div>}
      <div className="script-editor-actions"><button className="secondary-button" type="button" disabled={!!busy} onClick={() => setDraft(null)}>Cancel</button><button className="primary-button" disabled={!!busy}><Save size={12} /> Save</button></div>
    </form>}
    <div className="scheduler-list">
      {snapshotLoaded && snapshot.schedules.length === 0 && !draft && <div className="script-empty"><CalendarClock className="empty-icon" size={30} /><strong>No schedules yet</strong><p>{snapshot.workflows.length || snapshot.scripts.length ? 'Create a schedule to run a saved workflow or script automatically.' : 'Save a workflow in tab 5 or a script profile in tab 2, then return to schedule it.'}</p></div>}
      {snapshot.schedules.map((schedule) => {
        const state = snapshot.states.find((item) => item.scheduleId === schedule.id);
        const targetCatalog = schedule.targetKind === 'workflow' ? snapshot.workflows : snapshot.scripts;
        const targetProfile = targetCatalog.find((item) => item.profileId === schedule.profileId);
        const targetLabel = schedule.targetKind === 'workflow' ? 'workflow' : 'script profile';
        const last = [...snapshot.history].reverse().find((item) => item.scheduleId === schedule.id);
        return <article className="scheduler-card" key={schedule.id}>
          <div className="scheduler-card-top"><CalendarClock size={17} /><div className="scheduler-copy"><strong>{schedule.name}</strong><small className={targetProfile ? '' : 'scheduler-missing'}>{targetProfile?.name ?? `Missing ${targetLabel}: ${schedule.profileId}`} · {describeRecurrence(schedule.recurrence)}</small></div>
            <button className="secondary-button" disabled={!!busy || !!state?.running} onClick={() => void action('setEnabled', { scheduleId: schedule.id, enabled: !schedule.enabled }, schedule.id)}>{schedule.enabled ? 'Pause' : 'Enable'}</button>
            <button className="icon-button" title="Run now" aria-label={`Run ${schedule.name} now`} disabled={!!busy || !!state?.running || !targetProfile} onClick={() => void action('runNow', { scheduleId: schedule.id }, schedule.id)}><Play size={13} /></button>
            <button className="icon-button" title="Edit" aria-label={`Edit ${schedule.name}`} disabled={!!busy || !!state?.running} onClick={() => edit(schedule)}><Pencil size={13} /></button>
            <button className="icon-button" title="Delete schedule" aria-label={`Delete ${schedule.name}`} disabled={!!busy || !!state?.running} onClick={() => void action('deleteSchedule', { scheduleId: schedule.id }, schedule.id)}><Trash2 size={13} /></button>
          </div>
          <div className="scheduler-meta"><span className="scheduler-state">{state?.running ? 'Running' : schedule.enabled ? 'Enabled' : 'Paused'}</span><span><Clock3 size={10} />{state?.nextRunAtUtc && schedule.enabled ? new Date(state.nextRunAtUtc).toLocaleString() : 'No upcoming run'}</span>{last && <span>Last: {last.category} · {last.durationMilliseconds} ms</span>}{schedule.runOnceAfterRestart && <span>Catch-up enabled</span>}</div>
        </article>;
      })}
    </div>
    {snapshot.history.length > 0 && <section className="scheduler-history" aria-label="Run history"><strong>Recent runs</strong>{[...snapshot.history].reverse().slice(0, 20).map((entry) => <div className="scheduler-history-row" key={entry.id}><span>{snapshot.schedules.find((item) => item.id === entry.scheduleId)?.name ?? entry.scheduleId} · {new Date(entry.startedUtc).toLocaleString()}</span><span>{entry.category} · {entry.durationMilliseconds} ms</span></div>)}</section>}
  </section>;
}
