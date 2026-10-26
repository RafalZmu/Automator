import fs from 'node:fs';
import path from 'node:path';

export type NotificationPrefs = { taskToasts: boolean; timerOverlay: boolean };

const PREFS_VERSION = 1;

/** Reads the host-owned notification preferences. A missing or corrupt file means the defaults (popups and timer overlay on). */
export function loadNotificationPrefs(filePath: string): NotificationPrefs {
  try {
    const raw = JSON.parse(fs.readFileSync(filePath, 'utf8')) as Record<string, unknown>;
    if (raw && raw.version === PREFS_VERSION && typeof raw.taskToasts === 'boolean') {
      return { taskToasts: raw.taskToasts, timerOverlay: typeof raw.timerOverlay === 'boolean' ? raw.timerOverlay : true };
    }
  } catch { /* Fall back to defaults. */ }
  return { taskToasts: true, timerOverlay: true };
}

export function saveNotificationPrefs(filePath: string, prefs: NotificationPrefs): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  const temporary = `${filePath}.${process.pid}.tmp`;
  fs.writeFileSync(temporary, JSON.stringify({ version: PREFS_VERSION, taskToasts: prefs.taskToasts, timerOverlay: prefs.timerOverlay }), 'utf8');
  fs.renameSync(temporary, filePath);
}
