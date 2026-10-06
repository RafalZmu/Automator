export type WorkTimeActive = {
  id: string;
  startedUtc: string;
  elapsedMilliseconds: number;
  sampledUtc: string;
};

export type WorkTimePending = {
  id: string;
  startedUtc: string;
  stoppedUtc: string;
  durationMilliseconds: number;
  description: string;
  tags: string[];
};

export type WorkTimeEntry = WorkTimePending;

export type WorkTimeSnapshot = {
  active: WorkTimeActive | null;
  pending: WorkTimePending | null;
  history: WorkTimeEntry[];
};

type JsonObject = Record<string, unknown>;

function isObject(value: unknown): value is JsonObject {
  return Boolean(value && typeof value === 'object' && !Array.isArray(value));
}

function isDate(value: unknown): value is string {
  return typeof value === 'string' && Number.isFinite(Date.parse(value));
}

function isDuration(value: unknown): value is number {
  return typeof value === 'number' && Number.isSafeInteger(value) && value >= 0;
}

function readActive(value: unknown): WorkTimeActive | null | undefined {
  if (value === null) return null;
  if (!isObject(value) || typeof value.id !== 'string' || !value.id
      || !isDate(value.startedUtc) || !isDate(value.sampledUtc) || !isDuration(value.elapsedMilliseconds)) return undefined;
  return value as WorkTimeActive;
}

function readEntry(value: unknown, requireDescription = true): WorkTimeEntry | null {
  if (!isObject(value) || typeof value.id !== 'string' || !value.id
      || !isDate(value.startedUtc) || !isDate(value.stoppedUtc)
      || !isDuration(value.durationMilliseconds) || typeof value.description !== 'string'
      || (requireDescription && !value.description.trim()) || !Array.isArray(value.tags)
      || !value.tags.every((tag) => typeof tag === 'string' && Boolean(tag.trim()))) return null;
  return value as WorkTimeEntry;
}

export function readWorkTimeSnapshot(value: unknown): WorkTimeSnapshot | null {
  if (!isObject(value) || !Array.isArray(value.history)) return null;
  const active = readActive(value.active);
  const pending = value.pending === null ? null : readEntry(value.pending, false);
  if (active === undefined || (value.pending !== null && !pending) || (active && pending)) return null;
  if (!value.history.every((entry) => readEntry(entry) !== null)) return null;
  return { active, pending, history: value.history.map((entry) => readEntry(entry)!).slice(-500) };
}

export function elapsedWorkTimeMilliseconds(active: WorkTimeActive, now = Date.now()): number {
  const sampled = Date.parse(active.sampledUtc);
  return Math.max(0, active.elapsedMilliseconds + (Number.isFinite(sampled) ? now - sampled : 0));
}

function workTimeIntervalDailyOverlaps(startedUtc: string, stoppedUtc: string): Map<string, number> {
  const start = Date.parse(startedUtc);
  const stop = Date.parse(stoppedUtc);
  if (!Number.isFinite(start) || !Number.isFinite(stop) || stop <= start) return new Map();

  const overlaps = new Map<string, number>();
  const day = new Date(start);
  day.setHours(0, 0, 0, 0);
  while (day.getTime() < stop) {
    const windowStart = new Date(day);
    windowStart.setHours(8, 0, 0, 0);
    const windowStop = new Date(day);
    windowStop.setHours(16, 0, 0, 0);
    const overlap = Math.max(0, Math.min(stop, windowStop.getTime()) - Math.max(start, windowStart.getTime()));
    if (overlap > 0) overlaps.set(localDateKey(day), overlap);
    day.setDate(day.getDate() + 1);
  }
  return overlaps;
}

export function calculateWorkTimeIntervalMilliseconds(startedUtc: string, stoppedUtc: string): number {
  return [...workTimeIntervalDailyOverlaps(startedUtc, stoppedUtc).values()].reduce((total, overlap) => total + overlap, 0);
}

export function workTimeEntryDurationMilliseconds(entry: Pick<WorkTimeEntry, 'startedUtc' | 'stoppedUtc' | 'durationMilliseconds'>, workingPeriodOnly = false): number {
  return workingPeriodOnly
    ? calculateWorkTimeIntervalMilliseconds(entry.startedUtc, entry.stoppedUtc)
    : entry.durationMilliseconds;
}

export function formatWorkTimeDuration(milliseconds: number): string {
  const totalSeconds = Math.max(0, Math.floor(milliseconds / 1_000));
  if (totalSeconds < 60) return `${totalSeconds}s`;

  const totalMinutes = Math.floor(totalSeconds / 60);
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
}

export type WorkTimeReportFilter = { fromDate: string; toDate: string; tag: string };

function localDateKey(value: string | Date): string {
  const date = value instanceof Date ? value : new Date(value);
  if (!Number.isFinite(date.getTime())) return '';
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

export function calculateWorkTimeReportTotals(entries: readonly WorkTimeEntry[], now = new Date(), workingPeriodOnly = false): {
  todayMilliseconds: number;
  weekMilliseconds: number;
  todayEntries: number;
  weekEntries: number;
} {
  const today = localDateKey(now);
  const weekStartDate = new Date(now.getFullYear(), now.getMonth(), now.getDate() - ((now.getDay() + 6) % 7));
  const weekStart = localDateKey(weekStartDate);
  let todayMilliseconds = 0;
  let weekMilliseconds = 0;
  let todayEntries = 0;
  let weekEntries = 0;
  for (const entry of entries) {
    if (workingPeriodOnly) {
      let entryContributesToday = false;
      let entryContributesThisWeek = false;
      for (const [entryDate, duration] of workTimeIntervalDailyOverlaps(entry.startedUtc, entry.stoppedUtc)) {
        if (entryDate === today) {
          todayMilliseconds += duration;
          entryContributesToday = true;
        }
        if (entryDate >= weekStart && entryDate <= today) {
          weekMilliseconds += duration;
          entryContributesThisWeek = true;
        }
      }
      if (entryContributesToday) todayEntries += 1;
      if (entryContributesThisWeek) weekEntries += 1;
      continue;
    }
    const duration = workTimeEntryDurationMilliseconds(entry, workingPeriodOnly);
    const entryDate = localDateKey(entry.startedUtc);
    if (entryDate === today) {
      todayMilliseconds += duration;
      todayEntries += 1;
    }
    if (entryDate >= weekStart && entryDate <= today) {
      weekMilliseconds += duration;
      weekEntries += 1;
    }
  }
  return { todayMilliseconds, weekMilliseconds, todayEntries, weekEntries };
}

export function filterWorkTimeEntries<T extends Pick<WorkTimeEntry, 'startedUtc' | 'tags'>>(
  entries: readonly T[],
  filter: WorkTimeReportFilter,
): T[] {
  const selectedTag = filter.tag.trim().toLocaleLowerCase();
  return entries.filter((entry) => {
    const date = localDateKey(entry.startedUtc);
    return (!filter.fromDate || date >= filter.fromDate)
      && (!filter.toDate || date <= filter.toDate)
      && (!selectedTag || entry.tags.some((tag) => tag.toLocaleLowerCase() === selectedTag));
  });
}

export function getWorkTimeTags(entries: readonly Pick<WorkTimeEntry, 'tags'>[]): string[] {
  const tags = new Map<string, string>();
  for (const entry of entries) {
    for (const tag of entry.tags) {
      const key = tag.toLocaleLowerCase();
      if (!tags.has(key)) tags.set(key, tag);
    }
  }
  return [...tags.values()].sort((left, right) =>
    left.localeCompare(right, undefined, { sensitivity: 'base' }) || left.localeCompare(right));
}

function csvCell(value: string | number): string {
  const text = String(value);
  const safe = /^[\t\r\n ]*[=+\-@]/.test(text) ? `'${text}` : text;
  return `"${safe.replaceAll('"', '""')}"`;
}

export function createWorkTimeCsv(entries: readonly WorkTimeEntry[], workingPeriodOnly = false): string {
  const rows = [[
    'id', 'startedUtc', 'stoppedUtc', 'durationSeconds', 'durationMilliseconds', 'description', 'tags',
  ], ...entries.map((entry) => [
    entry.id,
    new Date(entry.startedUtc).toISOString(),
    new Date(entry.stoppedUtc).toISOString(),
    (workTimeEntryDurationMilliseconds(entry, workingPeriodOnly) / 1_000).toFixed(3),
    String(workTimeEntryDurationMilliseconds(entry, workingPeriodOnly)),
    entry.description,
    entry.tags.join(', '),
  ])];
  return rows.map((row) => row.map(csvCell).join(',')).join('\r\n');
}
