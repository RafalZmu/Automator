import fs from 'node:fs';
import path from 'node:path';

export type NotificationPrefs = { taskToasts: boolean };

const PREFS_VERSION = 1;

/** Reads the host-owned notification preferences. A missing or corrupt file means the defaults (popups on). */
export function loadNotificationPrefs(filePath: string): NotificationPrefs {
  try {
    const raw = JSON.parse(fs.readFileSync(filePath, 'utf8')) as Record<string, unknown>;
    if (raw && raw.version === PREFS_VERSION && typeof raw.taskToasts === 'boolean') return { taskToasts: raw.taskToasts };
  } catch { /* Fall back to defaults. */ }
  return { taskToasts: true };
}

export function saveNotificationPrefs(filePath: string, prefs: NotificationPrefs): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  const temporary = `${filePath}.${process.pid}.tmp`;
  fs.writeFileSync(temporary, JSON.stringify({ version: PREFS_VERSION, taskToasts: prefs.taskToasts }), 'utf8');
  fs.renameSync(temporary, filePath);
}
