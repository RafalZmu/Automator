import { useEffect, useMemo, useRef, useState, type KeyboardEvent } from 'react';
import { AppWindow, ListChecks, ArrowDown, ArrowUp, CornerDownLeft, Search, Settings2, Sparkles } from 'lucide-react';
import { groupOpenTasks, type OpenTasks } from '../contracts/notes';
import { canAutoRunTabCommand, matchQuickActions, resolveTabCommand, type TabCommand } from './commandMatching';

export type QuickAction = TabCommand & {
  kind: 'binding' | 'module' | 'tab' | 'system' | 'snippet';
  scope: string;
  category: string;
  shortcut?: string;
  targetId?: string;
};

export function QuickActionsHome({
  actions, openTasks, onRun, onAddApp, onOpenSettings, onClose,
}: {
  actions: readonly QuickAction[];
  openTasks: OpenTasks;
  onRun: (action: QuickAction) => Promise<void>;
  onAddApp: () => void;
  onOpenSettings: () => void;
  onClose: () => void;
}) {
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState(-1);
  const [armedId, setArmedId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);
  const matches = useMemo(() => matchQuickActions(actions, query), [actions, query]);
  const resolution = resolveTabCommand(matches);

  useEffect(() => {
    inputRef.current?.focus({ preventScroll: true });
  }, []);

  useEffect(() => {
    const trimmed = query.trim();
    if (!trimmed || resolution.kind !== 'unique' || !canAutoRunTabCommand(resolution.command)) return undefined;
    const command = resolution.command as QuickAction;
    const timer = window.setTimeout(() => { void execute(command); }, 420);
    return () => window.clearTimeout(timer);
  }, [query, resolution.kind, resolution.kind === 'unique' ? resolution.command.id : '', actions]);

  const execute = async (action: QuickAction) => {
    if (busyId) return;
    if (action.confirmationPrompt && armedId !== action.id) {
      setArmedId(action.id);
      return;
    }
    setBusyId(action.id);
    setError('');
    try {
      await onRun(action);
      setQuery('');
      setSelected(-1);
      setArmedId(null);
    } catch (failure) {
      setError(failure instanceof Error ? failure.message : 'The action could not be completed.');
    } finally {
      setBusyId(null);
    }
  };

  const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown' && matches.length > 0) {
      event.preventDefault();
      setSelected((value) => (value + 1) % matches.length);
      setArmedId(null);
    } else if (event.key === 'ArrowUp' && matches.length > 0) {
      event.preventDefault();
      setSelected((value) => value <= 0 ? matches.length - 1 : value - 1);
      setArmedId(null);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const action = selected >= 0 ? matches[selected] : resolution.kind === 'unique' ? resolution.command : undefined;
      if (action) void execute(action as QuickAction);
    } else if (event.key === '/' && !query.trim()) {
      event.preventDefault();
      onAddApp();
    } else if (event.key === 'Escape') {
      event.preventDefault();
      onClose();
    }
  };

  const armed = armedId ? matches.find((action) => action.id === armedId) : undefined;
  const notesTab = actions.find((action) => action.id === 'tab:notes');
  const showTasks = openTasks.tasks.length > 0;
  return <section className="quick-actions-home" data-tasks={showTasks} aria-label="Quick Actions">
    <div className="quick-actions-heading">
      <span className="quick-actions-mark"><Sparkles size={17} /></span>
      <div><h1>Quick Actions</h1><p>Search apps and actions across your workspace.</p></div>
      <div className="quick-actions-heading-buttons">
        <button className="quick-actions-icon" type="button" aria-label="Add an application" title="Add app" onClick={onAddApp}><AppWindow size={15} /></button>
        <button className="quick-actions-icon" type="button" aria-label="Open settings" title="Settings" onClick={onOpenSettings}><Settings2 size={15} /></button>
      </div>
    </div>
    <label className="quick-actions-search" htmlFor="quick-actions-search">
      <Search size={17} aria-hidden="true" />
      <input ref={inputRef} id="quick-actions-search" value={query} onChange={(event) => {
        setQuery(event.target.value); setSelected(-1); setArmedId(null); setError('');
      }} onKeyDown={onKeyDown} placeholder="Search apps, profiles, and actions…" autoComplete="off" spellCheck={false} aria-autocomplete="list" aria-controls="quick-actions-results" />
      <span className="quick-actions-key"><kbd>1–9</kbd> tabs</span>
    </label>
    <div id="quick-actions-results" className="quick-actions-results" role="listbox" aria-label="Quick action matches" aria-busy={Boolean(busyId)}>
      {query.trim() && matches.length === 0
        ? <div className="quick-actions-empty">No matching action. Try an app name, alias, or module.</div>
        : (query.trim() ? matches : matches.slice(0, 8)).map((action, index) => {
          const active = selected === index || (selected < 0 && resolution.kind === 'unique' && resolution.command.id === action.id);
          const armedAction = armed?.id === action.id;
          return <button key={action.id} type="button" role="option" aria-selected={active} className="quick-action-row" data-selected={active}
            onMouseEnter={() => { setSelected(index); setArmedId(null); }} onClick={() => void execute(action)} disabled={Boolean(busyId)}>
            <span className="quick-action-copy"><span className="quick-action-label">{action.label}</span><span className="quick-action-category">{action.category}</span></span>
            <span className="quick-action-side">{armedAction
              ? <span className="quick-action-confirm">Confirm</span>
              : action.shortcut ? <kbd>{action.shortcut}</kbd>
                : action.confirmationPrompt ? <span>Confirm</span>
                  : <CornerDownLeft size={13} />}</span>
          </button>;
        })}
      {!query.trim() && matches.length === 0 && <div className="quick-actions-empty">Add an app or choose one of the tabs to get started.</div>}
      {armed && <p className="quick-actions-confirm-note" role="status">{armed.confirmationPrompt} Press Enter again or select Confirm.</p>}
      {error && <p className="quick-actions-error" role="alert">{error}</p>}
      {query.trim() && matches.length > 1 && <p className="quick-actions-instruction"><ArrowUp size={12} /><ArrowDown size={12} /> Choose an action, then press Enter.</p>}
    </div>
    {showTasks && <div className="quick-actions-tasks" role="region" aria-label="Open tasks from Quick notes">
      <div className="quick-actions-tasks-heading"><ListChecks size={13} aria-hidden="true" /><span>Open tasks</span><span className="quick-actions-tasks-count">{openTasks.total}</span></div>
      <ul className="quick-actions-task-list">
        {groupOpenTasks(openTasks.tasks).map((group, groupIndex) => <li key={groupIndex} className="quick-actions-task-group">
          <div className="quick-actions-task-group-title">{group.note}</div>
          <ul className="quick-actions-task-items" aria-label={`Open tasks in ${group.note}`}>
            {group.tasks.map((text, index) => <li key={index}>
              <button type="button" className="quick-actions-task" disabled={!notesTab || Boolean(busyId)} title={`Open ${group.note} in Quick notes`}
                onClick={() => { if (notesTab) void execute(notesTab as QuickAction); }}>
                <span className="quick-actions-task-box" aria-hidden="true" /><span className="quick-actions-task-text">{text}</span>
              </button>
            </li>)}
          </ul>
        </li>)}
        {openTasks.total > openTasks.tasks.length && <li className="quick-actions-task-more">+{openTasks.total - openTasks.tasks.length} more in Quick notes</li>}
      </ul>
    </div>}
    <div className="quick-actions-footer"><span>Unique safe matches run automatically</span><span><kbd>/</kbd> add app <span aria-hidden="true">·</span> <kbd>Esc</kbd> close</span></div>
  </section>;
}
