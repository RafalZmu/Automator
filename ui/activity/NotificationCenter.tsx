import { Bell, ArrowUpRight } from 'lucide-react';
import { activityModuleNames, activitySummary, type RunActivityEntry } from '../../contracts/activity';
import './activity.css';

export function NotificationCenter({ entries, onNavigate, onDismiss, emptyMessage = 'No recent run notifications.' }: {
  entries: readonly RunActivityEntry[];
  onNavigate: (entry: RunActivityEntry) => void;
  onDismiss?: () => void;
  emptyMessage?: string;
}) {
  return <section className="notification-center" aria-label="Notifications"><header><Bell size={17} /><strong>Notifications</strong>{onDismiss && <button className="secondary-button" type="button" onClick={onDismiss}>Clear</button>}</header>
    {entries.slice(0, 50).map((entry) => <button className="notification-row" type="button" key={entry.id} onClick={() => onNavigate(entry)}>
      <span><strong>{activitySummary(entry)}</strong><small>{entry.profileId ?? activityModuleNames[entry.moduleId]} · {new Date(entry.finishedUtc).toLocaleString()}</small></span><ArrowUpRight size={14} />
    </button>)}
    {entries.length === 0 && <p>{emptyMessage}</p>}
  </section>;
}
