/** Always-on-top text indicators on the screen: click-through, bottom-right of the primary display unless the user moved them. Timers are the first indicator. */
export const OVERLAY_FONT_SIZE = 20;
export const OVERLAY_LINE_HEIGHT = 28;
export const OVERLAY_PADDING_X = 10;
export const OVERLAY_PADDING_Y = 6;
/** Widest the window may grow; it is sized to the measured text and never exceeds this. */
export const OVERLAY_WIDTH = 560;
export const OVERLAY_MARGIN = 12;
export const OVERLAY_MAX_ITEMS = 6;
export const OVERLAY_POLL_MS = 3_000;
export const OVERLAY_TEXT_OPACITY = 0.9;
/** Shown while the user is moving the overlay and no timer is running, so there is something to grab. */
export const OVERLAY_MOVE_SAMPLE = '0:00:00  Drag to move';

export const DEFAULT_OVERLAY_COLOR = '#ffffff';
/** The bottom-right corner of the overlay, in screen (DIP) coordinates. The window is anchored here as its text grows and shrinks. */
export type OverlayPosition = { x: number; y: number };
type Area = { x: number; y: number; width: number; height: number };

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

export function isOverlayColor(value: unknown): value is string {
  return typeof value === 'string' && /^#[0-9a-f]{6}$/i.test(value);
}

export function normalizeOverlayColor(value: unknown): string {
  return isOverlayColor(value) ? value.toLowerCase() : DEFAULT_OVERLAY_COLOR;
}

export function isOverlayPosition(value: unknown): value is OverlayPosition {
  const position = value as Partial<OverlayPosition> | null;
  return !!position && typeof position.x === 'number' && typeof position.y === 'number' && Number.isFinite(position.x) && Number.isFinite(position.y);
}

/** A light halo behind dark text and a dark halo behind light text, so the clock stays readable over any desktop. */
export function overlayShadow(color: string): string {
  const [red, green, blue] = [1, 3, 5].map((offset) => parseInt(color.slice(offset, offset + 2), 16));
  return (0.2126 * red + 0.7152 * green + 0.0722 * blue) / 255 > 0.6 ? 'rgba(0,0,0,.9)' : 'rgba(255,255,255,.9)';
}

/** The bottom-right corner of a display's work area, inset by the margin. */
export function defaultOverlayAnchor(workArea: Area): OverlayPosition {
  return { x: workArea.x + workArea.width - OVERLAY_MARGIN, y: workArea.y + workArea.height - OVERLAY_MARGIN };
}

/**
 * Keeps a remembered position only while it still lies on a visible display (a monitor may have been unplugged); otherwise the default is used.
 * `displays` are full display bounds, so a corner dragged over the taskbar or to the screen edge is kept, not snapped back.
 */
export function resolveOverlayAnchor(saved: OverlayPosition | null, displays: readonly Area[], primaryWorkArea: Area): OverlayPosition {
  if (saved && displays.some((display) => saved.x >= display.x && saved.x <= display.x + display.width && saved.y >= display.y && saved.y <= display.y + display.height)) {
    return { x: Math.round(saved.x), y: Math.round(saved.y) };
  }
  return defaultOverlayAnchor(primaryWorkArea);
}

/** The window rectangle for a bottom-right anchor, sized to the measured text (the largest window when no size is known yet). */
export function computeOverlayBounds(anchor: OverlayPosition, size?: { width: number; height: number }) {
  const width = Math.min(OVERLAY_WIDTH, Math.max(1, Math.ceil(size?.width ?? OVERLAY_WIDTH)));
  const height = Math.min(maxOverlayHeight(), Math.max(1, Math.ceil(size?.height ?? maxOverlayHeight())));
  return { x: anchor.x - width, y: anchor.y - height, width, height };
}

/**
 * Where the overlay window should be, or null to leave it alone. Normally it sits at its anchor. While the user may be dragging it keeps wherever it is
 * (anchored at its bottom-right) and only follows size changes beyond a small tolerance (Electron rounds sizes on scaled displays, so exact comparisons
 * would make it creep). `force` (used by Reset) always goes to the anchor.
 */
export function overlayPlacement(options: { moving: boolean; force?: boolean; current: Area; anchor: OverlayPosition; size?: { width: number; height: number } }): Area | null {
  if (!options.moving || options.force) return computeOverlayBounds(options.anchor, options.size);
  const { current } = options;
  const next = computeOverlayBounds({ x: current.x + current.width, y: current.y + current.height }, options.size);
  return Math.abs(next.width - current.width) > 2 || Math.abs(next.height - current.height) > 2 ? next : null;
}

function maxOverlayHeight(): number {
  return OVERLAY_MAX_ITEMS * OVERLAY_LINE_HEIGHT + 2 * OVERLAY_PADDING_Y;
}

/** Page for the overlay window: text only, no background. It has no script of its own; the host fills it with `overlayItemsScript`. */
export function overlayHtml(color: string): string {
  return `<!doctype html><html><head><meta charset="utf-8"><title>Automator timer overlay</title><meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'"><style>
:root{--overlay-text:${color};--overlay-shadow:${overlayShadow(color)}}
html,body{margin:0;height:100%;overflow:hidden;background:transparent;user-select:none;cursor:default}
body{display:flex;flex-direction:column;justify-content:flex-end;align-items:flex-end}
#panel{box-sizing:border-box;width:max-content;padding:${OVERLAY_PADDING_Y}px ${OVERLAY_PADDING_X}px;color:var(--overlay-text);opacity:${OVERLAY_TEXT_OPACITY};font:600 ${OVERLAY_FONT_SIZE}px/${OVERLAY_LINE_HEIGHT}px 'Segoe UI',system-ui,sans-serif;text-shadow:0 0 3px var(--overlay-shadow),0 0 1px var(--overlay-shadow),0 1px 2px var(--overlay-shadow)}
#panel div{white-space:nowrap;text-align:right;font-variant-numeric:tabular-nums}
body.moving #panel{opacity:1;outline:2px dashed var(--overlay-text);outline-offset:-2px;background:rgba(128,128,128,.35);-webkit-app-region:drag;cursor:move}
</style></head><body><div id="panel"></div></body></html>`;
}

/** Script the host runs in the overlay page to replace its lines. Text goes in through textContent, so it cannot inject markup. Returns the natural size of the panel. */
export function overlayItemsScript(lines: readonly string[]): string {
  return `(function(lines){var panel=document.getElementById('panel');panel.replaceChildren.apply(panel,lines.map(function(text){var line=document.createElement('div');line.textContent=text;return line;}));var box=panel.getBoundingClientRect();return {text:document.body.innerText,width:Math.ceil(box.width),height:Math.ceil(box.height)};})(${JSON.stringify(lines)})`;
}

/** Script that recolours an already-open overlay page and switches its move mode (dashed outline, draggable) without recreating the window. */
export function overlayStyleScript(color: string, moving: boolean): string {
  return `(function(root){root.setProperty('--overlay-text',${JSON.stringify(color)});root.setProperty('--overlay-shadow',${JSON.stringify(overlayShadow(color))});document.body.classList.toggle('moving',${moving ? 'true' : 'false'});})(document.documentElement.style)`;
}
