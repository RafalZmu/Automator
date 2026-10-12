import { useEffect, useRef, type ReactNode } from 'react';
import { Keyboard, X } from 'lucide-react';
import { anywhereShortcuts, launcherShortcuts, type ShortcutReferenceRow, type ShortcutSurface } from './keyboardShortcuts';

export type TabShortcutEntry = { id: string; label: string; shortcut: string };

/** Each entry is one chord ("Ctrl+K") or one standalone key ("↑"). A chord renders its parts as separate keys. */
function KeyChord({ keys }: { keys: readonly string[] }) {
  return <span className="shortcut-keys">
    {keys.map((chord) => <span className="shortcut-chord" key={chord}>
      {chord.split('+').map((part, index) => <span key={`${chord}-${index}`}>
        {index > 0 && <span className="shortcut-plus" aria-hidden="true">+</span>}
        <kbd>{part}</kbd>
      </span>)}
    </span>)}
  </span>;
}

function ShortcutRows({ rows }: { rows: readonly ShortcutReferenceRow[] }) {
  return <dl className="shortcut-rows">
    {rows.map((row) => <div className="shortcut-row" key={`${row.keys.join(' ')}|${row.label}`}>
      <dt><KeyChord keys={row.keys} /></dt>
      <dd>{row.label}</dd>
    </div>)}
  </dl>;
}

function ShortcutGroup({ title, children }: { title: string; children: ReactNode }) {
  return <section className="shortcut-group">
    <h2>{title}</h2>
    {children}
  </section>;
}

/**
 * Keyboard reference shown in place of the active content. It is not a modal: the compact launcher's native
 * key hook owns Escape there, so an overlay would close the whole panel instead of itself.
 */
export function KeyboardShortcutsPanel({ surface, tabTitle, tabShortcuts, onClose }: {
  surface: ShortcutSurface;
  tabTitle: string | null;
  tabShortcuts: readonly TabShortcutEntry[];
  onClose: () => void;
}) {
  const headingRef = useRef<HTMLHeadingElement>(null);
  useEffect(() => { headingRef.current?.focus({ preventScroll: true }); }, []);

  return <section className="shortcut-panel" aria-labelledby="shortcut-panel-title">
    <div className="quick-actions-heading">
      <span className="quick-actions-mark"><Keyboard size={17} /></span>
      <div><h1 id="shortcut-panel-title" ref={headingRef} tabIndex={-1}>Keyboard shortcuts</h1><p>They work while Automator is focused. Typing in a text field is never intercepted.</p></div>
      <div className="quick-actions-heading-buttons">
        <button className="quick-actions-icon" type="button" aria-label="Close keyboard shortcuts" title="Close" onClick={onClose}><X size={15} /></button>
      </div>
    </div>
    <div className="shortcut-groups">
      <ShortcutGroup title="Anywhere"><ShortcutRows rows={anywhereShortcuts(surface)} /></ShortcutGroup>
      {surface === 'launcher' && <ShortcutGroup title="Launcher"><ShortcutRows rows={launcherShortcuts} /></ShortcutGroup>}
      <ShortcutGroup title={tabTitle ? `${tabTitle} tab` : 'This tab'}>
        {tabShortcuts.length > 0
          ? <ShortcutRows rows={tabShortcuts.map((entry) => ({ keys: [entry.shortcut], label: entry.label }))} />
          : <p className="shortcut-empty">No shortcuts in this tab yet. Press Ctrl+K to search its actions.</p>}
      </ShortcutGroup>
    </div>
  </section>;
}
