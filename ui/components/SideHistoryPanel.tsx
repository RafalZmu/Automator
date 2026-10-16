import { PanelLeftClose, PanelLeftOpen } from 'lucide-react';
import { useEffect, useRef, useState, type ReactNode } from 'react';
import { createPortal } from 'react-dom';

function readOpen(key: string): boolean {
  try { return window.localStorage.getItem(key) === '1'; } catch { return false; }
}

function writeOpen(key: string, open: boolean): void {
  try { window.localStorage.setItem(key, open ? '1' : '0'); } catch { /* The toggle still works without storage. */ }
}

/**
 * Shared state for a tab's history panel. In the compact launcher window the panel sits to the left of the window and the host
 * widens the window (to the left) while it shows. `panelMode` is false in the workspace window and anywhere the host does not
 * resize, and the tab then keeps its own inline history. `available` is false while the tab has no history to show.
 */
export function useHistorySidePanel(surface: 'launcher' | 'workspace' | undefined, storageKey: string, available = true) {
  const [open, setOpen] = useState<boolean>(() => readOpen(storageKey));
  const [slot, setSlot] = useState<HTMLElement | null>(null);
  const [windowResizes, setWindowResizes] = useState(surface === 'launcher');
  const first = useRef(true);
  useEffect(() => { setSlot(document.getElementById('side-panel-slot')); }, []);
  useEffect(() => { writeOpen(storageKey, open); }, [storageKey, open]);
  const wanted = open && available;
  useEffect(() => {
    if (surface !== 'launcher') return;
    const bridge = window.automator;
    if (!bridge?.setSidePanel) { setWindowResizes(false); return; }
    const animate = !first.current;
    first.current = false;
    void bridge.setSidePanel(wanted, animate).then((resized) => { if (!resized) setWindowResizes(false); }).catch(() => setWindowResizes(false));
  }, [surface, wanted]);
  // The panel belongs to the tab, so the window narrows again when the tab goes away.
  useEffect(() => () => { if (surface === 'launcher') void window.automator?.setSidePanel?.(false)?.catch(() => undefined); }, [surface]);
  return { open, setOpen, slot, panelMode: surface === 'launcher' && windowResizes && !!slot };
}

type Panel = ReturnType<typeof useHistorySidePanel>;

/** The button in the tab that shows or hides the panel. */
export function SideHistoryToggle({ panel, title, summary }: { panel: Panel; title: string; summary?: string }) {
  return <section className="history-bar">
    <button type="button" className="history-bar-toggle" aria-pressed={panel.open} aria-expanded={panel.open} aria-controls="side-history-panel" onClick={() => panel.setOpen((open) => !open)}>
      {panel.open ? <PanelLeftClose size={14} /> : <PanelLeftOpen size={14} />}
      <strong>{title}</strong>
      {summary && <small>{summary}</small>}
    </button>
  </section>;
}

/** The panel itself, rendered into the slot at the left of the window. */
export function SideHistoryContent({ panel, title, actions, children }: { panel: Panel; title: string; actions?: ReactNode; children: ReactNode }) {
  if (!panel.slot) return null;
  return createPortal(
    <aside className="side-panel-inner" id="side-history-panel" role="region" aria-label={title} aria-hidden={!panel.open} inert={!panel.open}>
      <header>
        <strong>{title}</strong>
        {actions}
        <button className="icon-button" type="button" aria-label={`Hide ${title.toLowerCase()}`} title={`Hide ${title.toLowerCase()}`} onClick={() => panel.setOpen(false)}><PanelLeftClose size={14} /></button>
      </header>
      <div className="side-panel-body">{children}</div>
    </aside>, panel.slot);
}
