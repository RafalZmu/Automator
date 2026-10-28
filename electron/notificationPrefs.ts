import fs from 'node:fs';
import path from 'node:path';

/** `overlayPosition` is the bottom-right corner of the timer overlay in screen coordinates; null means the default (bottom-right of the primary display). */
export type NotificationPrefs = { taskToasts: boolean; timerOverlay: boolean; overlayColor: string; overlayPosition: { x: number; y: number } | null };

const PREFS_VERSION = 1;
// Keep in step with DEFAULT_OVERLAY_COLOR in screenOverlay.ts (not imported so this file loads on its own in tests).
const DEFAULT_OVERLAY_COLOR = '#ffffff';

function defaults(): NotificationPrefs {
  return { taskToasts: true, timerOverlay: true, overlayColor: DEFAULT_OVERLAY_COLOR, overlayPosition: null };
}

/** Reads the host-owned notification preferences. A missing or corrupt file means the defaults (popups and timer overlay on). */
export function loadNotificationPrefs(filePath: string): NotificationPrefs {
  try {
    const raw = JSON.parse(fs.readFileSync(filePath, 'utf8')) as Record<string, unknown>;
    if (raw && raw.version === PREFS_VERSION && typeof raw.taskToasts === 'boolean') {
      const color = typeof raw.overlayColor === 'string' && /^#[0-9a-f]{6}$/i.test(raw.overlayColor) ? raw.overlayColor.toLowerCase() : DEFAULT_OVERLAY_COLOR;
      const x = raw.overlayX;
      const y = raw.overlayY;
      const position = typeof x === 'number' && typeof y === 'number' && Number.isFinite(x) && Number.isFinite(y) ? { x, y } : null;
      return { taskToasts: raw.taskToasts, timerOverlay: typeof raw.timerOverlay === 'boolean' ? raw.timerOverlay : true, overlayColor: color, overlayPosition: position };
    }
  } catch { /* Fall back to defaults. */ }
  return defaults();
}

export function saveNotificationPrefs(filePath: string, prefs: NotificationPrefs): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  const temporary = `${filePath}.${process.pid}.tmp`;
  fs.writeFileSync(temporary, JSON.stringify({
    version: PREFS_VERSION,
    taskToasts: prefs.taskToasts,
    timerOverlay: prefs.timerOverlay,
    overlayColor: prefs.overlayColor,
    overlayX: prefs.overlayPosition?.x,
    overlayY: prefs.overlayPosition?.y,
  }), 'utf8');
  fs.renameSync(temporary, filePath);
}
