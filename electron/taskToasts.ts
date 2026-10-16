import { activityModuleNames, type RunActivityEntry } from '../contracts/activity.ts';

export const TOAST_WIDTH = 340;
export const TOAST_HEIGHT = 84;
export const TOAST_MARGIN = 16;
export const TOAST_GAP = 8;
export const TOAST_MAX_VISIBLE = 3;
export const TOAST_DURATION_MS = 5_000;

export type ToastTone = 'success' | 'warning' | 'error' | 'information';
export type ToastContent = { title: string; detail: string; tone: ToastTone };

/**
 * Tracks which run ids have already been seen so only runs that finished after the last snapshot raise a toast.
 * The first snapshot is only remembered, so restarting Automator does not replay history.
 */
export function createToastTracker() {
  const seen = new Set<string>();
  let seeded = false;
  return {
    /** Returns entries not seen before, oldest first. */
    next(entries: readonly RunActivityEntry[]): RunActivityEntry[] {
      const fresh = entries.filter((entry) => !seen.has(entry.id));
      for (const entry of entries) seen.add(entry.id);
      if (!seeded) { seeded = true; return []; }
      return fresh.sort((a, b) => Date.parse(a.finishedUtc) - Date.parse(b.finishedUtc));
    },
  };
}

/** Fixed, metadata-only text: never includes provider messages or result data. */
export function toastContent(entry: RunActivityEntry): ToastContent {
  const outcome = entry.status === 'success' ? 'Succeeded' : entry.status === 'error' ? 'Failed'
    : entry.status === 'warning' ? 'Finished with a warning' : 'Finished';
  return {
    title: entry.profileId ?? entry.actionId,
    detail: `${outcome} · ${activityModuleNames[entry.moduleId]}`,
    tone: entry.status,
  };
}

/** Bottom-right placement; index 0 is the lowest toast and later ones stack upward. */
export function computeToastBounds(workArea: { x: number; y: number; width: number; height: number }, index: number) {
  return {
    x: workArea.x + workArea.width - TOAST_WIDTH - TOAST_MARGIN,
    y: workArea.y + workArea.height - TOAST_HEIGHT - TOAST_MARGIN - index * (TOAST_HEIGHT + TOAST_GAP),
    width: TOAST_WIDTH,
    height: TOAST_HEIGHT,
  };
}

const escapeHtml = (value: string) => value.replace(/[&<>"']/g, (char) => (
  { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char] as string));

const toneColor: Record<ToastTone, string> = { success: '#2e9e5b', warning: '#d9962b', error: '#d64545', information: '#3b82d9' };

/** Self-contained page for the toast window: no scripts, so it needs no preload or renderer bundle. */
export function toastHtml(content: ToastContent, dark: boolean): string {
  const [background, text, muted] = dark ? ['#202225', '#f2f3f5', '#a9adb3'] : ['#ffffff', '#1f2328', '#59636e'];
  return `<!doctype html><html><head><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'"><style>
html,body{margin:0;height:100%;overflow:hidden;background:transparent;font:13px 'Segoe UI',system-ui,sans-serif}
.toast{box-sizing:border-box;height:100%;display:flex;flex-direction:column;justify-content:center;gap:4px;padding:0 16px 0 20px;border-radius:10px;background:${background};color:${text};border-left:6px solid ${toneColor[content.tone]};box-shadow:inset 0 0 0 1px rgba(128,128,128,.3)}
strong{font-size:14px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}span{color:${muted}}
</style></head><body><div class="toast" role="status"><strong>${escapeHtml(content.title)}</strong><span>${escapeHtml(content.detail)}</span></div></body></html>`;
}
