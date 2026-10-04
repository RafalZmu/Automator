import { useEffect, useMemo, useState, useSyncExternalStore } from 'react';
import { Activity, ArrowUpRight, Search } from 'lucide-react';
import type { AutomationResult } from '../../contracts/rpc';
import { activityModuleNames, filterActivity, type RunActivityEntry } from '../../contracts/activity';
import { getTransientResult, subscribeTransientResults } from './transientResults';
import { ResultViewer } from './ResultViewer';
import './activity.css';

export type RunActivityViewProps = {
  entries: readonly RunActivityEntry[];
  loading?: boolean;
  error?: string;
  selectedRunId?: string | null;
  onNavigate: (entry: RunActivityEntry) => void;
  registeredActions?: readonly { id: string; version: number; command?: string | null }[];
  registeredActionsForModule?: (moduleId: string) => readonly { id: string; version: number; command?: string | null }[];
  onAction?: (entry: RunActivityEntry, action: AutomationResult['actions'][number]) => void | Promise<void>;
};
export function RunActivityView({ entries, loading, error, selectedRunId, onNavigate, registeredActions, registeredActionsForModule, onAction }: RunActivityViewProps) {
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<string | null>(null);
  useEffect(() => { if (selectedRunId !== undefined) setSelected(selectedRunId); }, [selectedRunId]);
  const matches = useMemo(() => filterActivity(entries, query), [entries, query]);
  const entry = entries.find((item) => item.id === selected);
  const transient = useSyncExternalStore(subscribeTransientResults, () => selected ? getTransientResult(selected) : undefined);
  const matchedResult = transient && entry && transient.moduleId === entry.moduleId && transient.actionId === entry.actionId ? transient.result : undefined;
  return <section className="run-activity-view" aria-label="Run activity">
    <div className="activity-heading"><Activity size={20} /><div><h1>Run activity</h1><p>Recent runs from across Automator</p></div></div>
    <label className="activity-search"><Search size={15} /><input type="search" aria-label="Search run activity" value={query} placeholder="Filter module, profile, status…" onChange={(event) => setQuery(event.target.value)} /></label>
    {error && <p role="alert">{error}</p>}
    {loading && <p role="status">Loading recent runs…</p>}
    <div className="activity-list">{matches.map((item) => <article className={`activity-row${selected === item.id ? ' is-selected' : ''}`} key={item.id}>
      <button type="button" className="activity-select" aria-pressed={selected === item.id} onClick={() => setSelected(item.id)}><strong>{activityModuleNames[item.moduleId]}</strong><span>{item.profileId ?? item.actionId} · {item.origin} · {new Date(item.startedUtc).toLocaleString()}</span></button>
      <span className={`activity-status is-${item.status}`}>{item.status}</span><span className="activity-duration">{(item.durationMilliseconds / 1000).toLocaleString(undefined, { maximumFractionDigits: 2 })}s</span>
      <button type="button" className="icon-button" aria-label={`Open ${activityModuleNames[item.moduleId]} for this run`} onClick={() => onNavigate(item)}><ArrowUpRight size={15} /></button>
    </article>)}</div>
    {!loading && matches.length === 0 && <p className="activity-empty">{query ? 'No runs match this search.' : 'Run an automation to see its status here.'}</p>}
    {entry && (matchedResult ? <ResultViewer result={matchedResult} registeredActions={registeredActionsForModule?.(entry.moduleId) ?? registeredActions} onAction={onAction ? (action) => onAction(entry, action) : undefined} />
      : <p className="activity-empty">This run retains status and timing only. Its output is no longer available in this window.</p>)}
  </section>;
}
