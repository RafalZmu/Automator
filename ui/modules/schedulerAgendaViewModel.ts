import type { SchedulerSnapshot, ScheduleTargetKind } from '../contracts/scheduler';

export type UpcomingScheduleEntry = {
  scheduleId: string;
  scheduleName: string;
  targetLabel: string;
  targetKind: ScheduleTargetKind;
  scheduledAt: Date;
  dateKey: string;
  dateLabel: string;
  timeLabel: string;
  currentlyRunning: boolean;
};

export type SchedulerCalendarDay = {
  date: Date;
  dateKey: string;
  dateLabel: string;
  dayNumber: number;
  inMonth: boolean;
  isToday: boolean;
  entries: UpcomingScheduleEntry[];
};

export type SchedulerCalendarMonth = {
  monthLabel: string;
  weekdayLabels: { short: string; full: string }[];
  days: SchedulerCalendarDay[];
};

function localDateKey(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function localTimeLabel(date: Date): string {
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`;
}

function scheduleTargetLabel(snapshot: SchedulerSnapshot, targetKind: ScheduleTargetKind, profileId: string): string {
  const targetType = targetKind === 'workflow' ? 'Workflow' : 'Script profile';
  const catalog = targetKind === 'workflow' ? snapshot.workflows : snapshot.scripts;
  const profile = catalog.find((item) => item.profileId === profileId);
  return `${targetType} · ${profile?.name ?? `Missing ${targetType.toLowerCase()}: ${profileId}`}`;
}

/**
 * Presents only the next-run timestamps computed by Scheduler. It deliberately
 * does not synthesize later occurrences; interval, daily, weekly, and DST
 * behavior remains owned by the backend scheduler.
 */
export function buildUpcomingScheduleEntries(snapshot: SchedulerSnapshot, now = new Date()): UpcomingScheduleEntry[] {
  const nowMilliseconds = now.getTime();
  return snapshot.schedules.flatMap((schedule) => {
    if (!schedule.enabled) return [];
    const state = snapshot.states.find((item) => item.scheduleId === schedule.id);
    if (!state?.nextRunAtUtc) return [];
    const runTime = Date.parse(state.nextRunAtUtc);
    if (!Number.isFinite(runTime) || runTime < nowMilliseconds) return [];

    const scheduledAt = new Date(runTime);
    return [{
      scheduleId: schedule.id,
      scheduleName: schedule.name,
      targetKind: schedule.targetKind,
      targetLabel: scheduleTargetLabel(snapshot, schedule.targetKind, schedule.profileId),
      scheduledAt,
      dateKey: localDateKey(scheduledAt),
      dateLabel: scheduledAt.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' }),
      timeLabel: localTimeLabel(scheduledAt),
      currentlyRunning: state.running,
    }];
  }).sort((left, right) => left.scheduledAt.getTime() - right.scheduledAt.getTime()
    || left.scheduleName.localeCompare(right.scheduleName));
}

/** Build a Sunday-first calendar month using local dates and local date keys. */
export function buildCalendarMonth(anchorDate: Date, entries: UpcomingScheduleEntry[], now = new Date()): SchedulerCalendarMonth {
  const year = anchorDate.getFullYear();
  const month = anchorDate.getMonth();
  const firstDay = new Date(year, month, 1);
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const dayCount = Math.ceil((firstDay.getDay() + daysInMonth) / 7) * 7;
  const firstCell = new Date(year, month, 1 - firstDay.getDay());
  const entryMap = new Map<string, UpcomingScheduleEntry[]>();
  for (const entry of entries) {
    const dayEntries = entryMap.get(entry.dateKey) ?? [];
    dayEntries.push(entry);
    entryMap.set(entry.dateKey, dayEntries);
  }

  const weekdayLabels = Array.from({ length: 7 }, (_, index) => {
    const weekday = new Date(2023, 0, 1 + index);
    return {
      short: weekday.toLocaleDateString(undefined, { weekday: 'short' }),
      full: weekday.toLocaleDateString(undefined, { weekday: 'long' }),
    };
  });
  const todayKey = localDateKey(now);
  const days = Array.from({ length: dayCount }, (_, index) => {
    const date = new Date(firstCell);
    date.setDate(firstCell.getDate() + index);
    const dateKey = localDateKey(date);
    return {
      date,
      dateKey,
      dateLabel: date.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' }),
      dayNumber: date.getDate(),
      inMonth: date.getMonth() === month && date.getFullYear() === year,
      isToday: dateKey === todayKey,
      entries: entryMap.get(dateKey) ?? [],
    };
  });

  return {
    monthLabel: firstDay.toLocaleDateString(undefined, { month: 'long', year: 'numeric' }),
    weekdayLabels,
    days,
  };
}
