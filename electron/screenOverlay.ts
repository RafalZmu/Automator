/** Always-on-top, click-through text indicators in the bottom-right corner of the screen. Timers are the first indicator. */
export const OVERLAY_WIDTH = 360;
export const OVERLAY_LINE_HEIGHT = 20;
export const OVERLAY_MARGIN = 12;
export const OVERLAY_MAX_ITEMS = 6;
export const OVERLAY_POLL_MS = 3_000;
export const OVERLAY_OPACITY = 0.65;
/** Temporary: paints the whole overlay window solid white so its position is easy to find. Set to false for the normal transparent look. */
export const OVERLAY_TEST_BACKGROUND = true;
const MAX_DESCRIPTION_LENGTH = 40;

export type OverlayItem = { id: string; text: string };
export type OverlayTimer = { id: string; description: string; status: string; elapsedMilliseconds: number; sampledUtc: string };

export function formatOverlayDuration(milliseconds: number): string {
  const total = Math.max(0, Math.floor(milliseconds / 1000));
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${Math.floor(total / 3600)}:${pad(Math.floor(total / 60) % 60)}:${pad(total % 60)}`;
}

/** Reads the timers out of a Work Time snapshot, ignoring anything malformed. */
export function readOverlayTimers(snapshot: unknown): OverlayTimer[] {
  const timers = (snapshot as { timers?: unknown } | null)?.timers;
  if (!Array.isArray(timers)) return [];
  return timers.flatMap((timer): OverlayTimer[] => {
    const value = timer as Partial<OverlayTimer> | null;
    if (!value || typeof value.id !== 'string' || typeof value.description !== 'string' || typeof value.status !== 'string'
        || typeof value.elapsedMilliseconds !== 'number' || typeof value.sampledUtc !== 'string') return [];
    return [value as OverlayTimer];
  });
}

/** One line for the running timer; nothing when no timer runs, so the overlay hides. Paused timers are not shown. */
export function timerOverlayItems(timers: readonly OverlayTimer[], now: number): OverlayItem[] {
  return timers.filter((timer) => timer.status === 'running').slice(0, OVERLAY_MAX_ITEMS).map((timer) => {
    const sampled = Date.parse(timer.sampledUtc);
    const elapsed = timer.elapsedMilliseconds + (Number.isFinite(sampled) ? Math.max(0, now - sampled) : 0);
    const description = timer.description.trim();
    const label = description.length > MAX_DESCRIPTION_LENGTH ? `${description.slice(0, MAX_DESCRIPTION_LENGTH - 1)}…` : description;
    return { id: timer.id, text: `${formatOverlayDuration(elapsed)}  ${label}`.trimEnd() };
  });
}

/** Bottom-right of the work area. The window always has room for every line, so it never resizes; the page bottom-aligns the text. */
export function computeOverlayBounds(workArea: { x: number; y: number; width: number; height: number }) {
  const height = OVERLAY_MAX_ITEMS * OVERLAY_LINE_HEIGHT;
  return {
    x: workArea.x + workArea.width - OVERLAY_WIDTH - OVERLAY_MARGIN,
    y: workArea.y + workArea.height - height - OVERLAY_MARGIN,
    width: OVERLAY_WIDTH,
    height,
  };
}

/** Page for the overlay window. Text is only ever set through textContent, so item text cannot inject markup. */
export function overlayHtml(dark: boolean): string {
  const [text, shadow] = OVERLAY_TEST_BACKGROUND ? ['#000000', 'transparent'] : dark ? ['#ffffff', 'rgba(0,0,0,.8)'] : ['#101418', 'rgba(255,255,255,.8)'];
  const [background, opacity] = OVERLAY_TEST_BACKGROUND ? ['#ffffff', 1] : ['transparent', OVERLAY_OPACITY];
  return `<!doctype html><html><head><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; script-src 'unsafe-inline'"><style>
html,body{margin:0;height:100%;overflow:hidden;background:${background};user-select:none;cursor:default}
body{display:flex;flex-direction:column;justify-content:flex-end;align-items:flex-end;font:600 13px/${OVERLAY_LINE_HEIGHT}px 'Segoe UI',system-ui,sans-serif;color:${text};opacity:${opacity};text-shadow:0 0 3px ${shadow},0 0 1px ${shadow}}
div{max-width:100%;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;font-variant-numeric:tabular-nums}
</style></head><body><script>window.setItems=function(items){document.body.replaceChildren.apply(document.body,items.map(function(item){var line=document.createElement('div');line.textContent=item;return line;}));};</script></body></html>`;
}
