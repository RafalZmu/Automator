export type WorkTimeSegment = {
  startedUtc: string;
  stoppedUtc: string | null;
};

export type WorkTimeTimer = {
  id: string;
  description: string;
  startedUtc: string;
  status: 'running' | 'paused';
  elapsedMilliseconds: number;
  sampledUtc: string;
  segments: WorkTimeSegment[];
};

export type WorkTimeEntry = {
  id: string;
  startedUtc: string;
  stoppedUtc: string;
  durationMilliseconds: number;
  description: string;
  tags: string[];
  segments: WorkTimeSegment[];
};

export type WorkTimePending = WorkTimeEntry;

export type WorkTimeSnapshot = {
  timers: WorkTimeTimer[];
  pendingEntries: WorkTimePending[];
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

export function isWorkTimeDescriptionValid(description: string): boolean {
  return Boolean(description.trim());
}

function readSegments(value: unknown, allowOpen: boolean): WorkTimeSegment[] | null {
  if (!Array.isArray(value) || value.length === 0) return null;
  const segments: WorkTimeSegment[] = [];
  for (const item of value) {
    if (!isObject(item) || !isDate(item.startedUtc)
        || !(item.stoppedUtc === null || isDate(item.stoppedUtc))) return null;
    if (item.stoppedUtc !== null && Date.parse(item.stoppedUtc) < Date.parse(item.startedUtc)) return null;
    segments.push(item as WorkTimeSegment);
  }
  const open = segments.filter((segment) => segment.stoppedUtc === null);
  if ((!allowOpen && open.length) || (allowOpen && open.length > 1)
      || (open.length === 1 && segments.at(-1)?.stoppedUtc !== null)) return null;
  return segments;
}

function readTimer(value: unknown): WorkTimeTimer | null {
  if (!isObject(value) || typeof value.id !== 'string' || !value.id || typeof value.description !== 'string'
      || !value.description.trim() || !isDate(value.startedUtc) || !isDate(value.sampledUtc)
      || !isDuration(value.elapsedMilliseconds) || (value.status !== 'running' && value.status !== 'paused')) return null;
  const segments = readSegments(value.segments, value.status === 'running');
  if (!segments || (value.status === 'running' && segments.at(-1)?.stoppedUtc !== null)
      || (value.status === 'paused' && segments.at(-1)?.stoppedUtc === null)) return null;
  return { ...value, segments } as WorkTimeTimer;
}

function readEntry(value: unknown, requireDescription = true): WorkTimeEntry | null {
  if (!isObject(value) || typeof value.id !== 'string' || !value.id
      || !isDate(value.startedUtc) || !isDate(value.stoppedUtc)
      || !isDuration(value.durationMilliseconds) || typeof value.description !== 'string'
      || (requireDescription && !value.description.trim()) || !Array.isArray(value.tags)
      || !value.tags.every((tag) => typeof tag === 'string' && Boolean(tag.trim()))) return null;
  const segments = readSegments(value.segments, false);
  if (!segments || segments.some((segment) => Date.parse(segment.startedUtc) < Date.parse(value.startedUtc as string)
      || Date.parse(segment.stoppedUtc!) > Date.parse(value.stoppedUtc as string))) return null;
  return { ...value, segments } as WorkTimeEntry;
}

export function readWorkTimeSnapshot(value: unknown): WorkTimeSnapshot | null {
  if (!isObject(value) || !Array.isArray(value.timers) || !Array.isArray(value.pendingEntries) || !Array.isArray(value.history)) return null;
  const timers = value.timers.map(readTimer);
  const pendingEntries = value.pendingEntries.map((entry) => readEntry(entry, false));
  if (timers.some((timer) => !timer) || pendingEntries.some((entry) => !entry)
      || timers.filter((timer) => timer?.status === 'running').length > 1) return null;
  if (!value.history.every((entry) => readEntry(entry) !== null)) return null;
  const allIds = [...timers, ...pendingEntries, ...value.history.map((entry) => readEntry(entry)!)].map((item) => item!.id);
  if (new Set(allIds).size !== allIds.length) return null;
  return { timers: timers as WorkTimeTimer[], pendingEntries: pendingEntries as WorkTimePending[], history: value.history.map((entry) => readEntry(entry)!).slice(-500) };
}

export function elapsedWorkTimeMilliseconds(timer: WorkTimeTimer, now = Date.now()): number {
  const sampled = Date.parse(timer.sampledUtc);
  return Math.max(0, timer.elapsedMilliseconds + (timer.status === 'running' && Number.isFinite(sampled) ? now - sampled : 0));
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

function workTimeSegmentOverlaps(segments: readonly WorkTimeSegment[]): Map<string, number> {
  const overlaps = new Map<string, number>();
  for (const segment of segments) {
    if (!segment.stoppedUtc) continue;
    for (const [date, duration] of workTimeIntervalDailyOverlaps(segment.startedUtc, segment.stoppedUtc)) {
      overlaps.set(date, (overlaps.get(date) ?? 0) + duration);
    }
  }
  return overlaps;
}

export function workTimeEntryDurationMilliseconds(entry: Pick<WorkTimeEntry, 'startedUtc' | 'stoppedUtc' | 'durationMilliseconds' | 'segments'>, workingPeriodOnly = false): number {
  return workingPeriodOnly
    ? [...workTimeSegmentOverlaps(entry.segments).values()].reduce((total, overlap) => total + overlap, 0)
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
      for (const [entryDate, duration] of workTimeSegmentOverlaps(entry.segments)) {
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
