export type ScheduleRecurrence = {
  kind: 'interval' | 'daily' | 'weekly';
  intervalMinutes: number | null;
  localTime: string | null;
  daysOfWeek: string[] | null;
  intervalAnchorUtc: string | null;
};
export type ScheduleTargetKind = 'workflow' | 'scriptProfile';
export type ScheduleDefinition = { id: string; name: string; targetKind: ScheduleTargetKind; profileId: string; enabled: boolean; recurrence: ScheduleRecurrence; runOnceAfterRestart: boolean };
export type ScheduleHistory = { id: string; scheduleId: string; startedUtc: string; finishedUtc: string | null; status: 'success' | 'information' | 'warning' | 'error'; category: string; durationMilliseconds: number };
export type SchedulerSnapshot = {
  schedules: ScheduleDefinition[]; history: ScheduleHistory[]; running: boolean;
  states: { scheduleId: string; nextRunAtUtc: string | null; running: boolean }[];
  workflows: { profileId: string; name: string }[];
  scripts: { profileId: string; name: string }[];
};
export const scheduleWeekdays = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];
const idPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;
function record(value: unknown): value is Record<string, unknown> { return value !== null && typeof value === 'object' && !Array.isArray(value); }
function date(value: unknown): value is string { return typeof value === 'string' && Number.isFinite(Date.parse(value)); }

export function validateSchedule(schedule: ScheduleDefinition): string[] {
  const errors: string[] = [];
  if (!idPattern.test(schedule.id) || !idPattern.test(schedule.profileId) || !['workflow', 'scriptProfile'].includes(schedule.targetKind)) errors.push('Select a saved workflow or script profile and use a short lowercase schedule key.');
  if (!schedule.name.trim() || schedule.name.trim().length > 128 || /[\x00-\x1f\x7f]/.test(schedule.name)) errors.push('Name is required and must be at most 128 characters.');
  const recurrence = schedule.recurrence;
  if (recurrence.kind === 'interval') {
    if (!Number.isInteger(recurrence.intervalMinutes) || recurrence.intervalMinutes! < 1 || recurrence.intervalMinutes! > 525600) errors.push('Interval must be between 1 and 525600 minutes.');
    if (recurrence.localTime !== null || recurrence.daysOfWeek !== null) errors.push('Interval recurrence must not contain a local time or weekdays.');
    if (recurrence.intervalAnchorUtc !== null && !date(recurrence.intervalAnchorUtc)) errors.push('Interval anchor must be a valid timestamp.');
  } else if (recurrence.kind === 'daily' || recurrence.kind === 'weekly') {
    if (typeof recurrence.localTime !== 'string' || !/^(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d(?:\.\d{1,7})?$/.test(recurrence.localTime)) errors.push('Choose a valid local time.');
    if (recurrence.intervalMinutes !== null || recurrence.intervalAnchorUtc !== null) errors.push('Daily and weekly recurrence must not contain interval fields.');
    if (recurrence.kind === 'daily' && recurrence.daysOfWeek !== null) errors.push('Daily recurrence must not contain weekdays.');
    if (recurrence.kind === 'weekly' && (!Array.isArray(recurrence.daysOfWeek) || recurrence.daysOfWeek.length < 1 || recurrence.daysOfWeek.length > 7
        || recurrence.daysOfWeek.some((day) => !scheduleWeekdays.includes(day)) || new Set(recurrence.daysOfWeek).size !== recurrence.daysOfWeek.length)) errors.push('Choose one or more unique weekdays.');
  } else errors.push('Choose interval, daily, or weekly recurrence.');
  return errors;
}

export function readSchedulerSnapshot(value: unknown): SchedulerSnapshot | null {
  if (!record(value) || !Array.isArray(value.schedules) || !Array.isArray(value.history) || typeof value.running !== 'boolean') return null;
  const schedules = value.schedules.slice(0, 256).flatMap((item) => {
    if (!record(item) || typeof item.id !== 'string' || typeof item.name !== 'string'
        || typeof item.enabled !== 'boolean' || typeof item.runOnceAfterRestart !== 'boolean' || !record(item.recurrence)) return [];
    const targetKind: ScheduleTargetKind = item.targetKind === 'scriptProfile' ? 'scriptProfile' : 'workflow';
    const profileId = typeof item.profileId === 'string' ? item.profileId : typeof item.workflowId === 'string' ? item.workflowId : '';
    if (typeof item.targetKind === 'string' && !['workflow', 'scriptProfile'].includes(item.targetKind)) return [];
    const r = item.recurrence;
    if (!['interval', 'daily', 'weekly'].includes(String(r.kind))) return [];
    const recurrence: ScheduleRecurrence = {
      kind: r.kind as ScheduleRecurrence['kind'], intervalMinutes: typeof r.intervalMinutes === 'number' ? r.intervalMinutes : null,
      localTime: typeof r.localTime === 'string' ? r.localTime : null,
      daysOfWeek: Array.isArray(r.daysOfWeek) && r.daysOfWeek.every((day) => typeof day === 'string') ? r.daysOfWeek : null,
      intervalAnchorUtc: typeof r.intervalAnchorUtc === 'string' ? r.intervalAnchorUtc : null,
    };
    const schedule: ScheduleDefinition = { id: item.id, name: item.name, targetKind, profileId, enabled: item.enabled, runOnceAfterRestart: item.runOnceAfterRestart, recurrence };
    return validateSchedule(schedule).length === 0 ? [schedule] : [];
  });
  const history = value.history.slice(-100).flatMap((item): ScheduleHistory[] => {
    if (!record(item) || typeof item.id !== 'string' || typeof item.scheduleId !== 'string' || !date(item.startedUtc)
        || !(item.finishedUtc === null || date(item.finishedUtc)) || !['success', 'information', 'warning', 'error'].includes(String(item.status))
        || typeof item.category !== 'string' || item.category.length > 128 || typeof item.durationMilliseconds !== 'number' || item.durationMilliseconds < 0) return [];
    return [{ id: item.id, scheduleId: item.scheduleId, startedUtc: item.startedUtc, finishedUtc: item.finishedUtc,
      status: item.status as ScheduleHistory['status'], category: item.category, durationMilliseconds: item.durationMilliseconds }];
  });
  const states = Array.isArray(value.states) ? value.states.flatMap((item) => record(item) && typeof item.scheduleId === 'string'
      && typeof item.running === 'boolean' && (item.nextRunAtUtc === null || date(item.nextRunAtUtc))
    ? [{ scheduleId: item.scheduleId, nextRunAtUtc: item.nextRunAtUtc, running: item.running }] : []) : [];
  const workflows = Array.isArray(value.workflows) ? value.workflows.flatMap((item) => record(item) && typeof item.profileId === 'string'
      && idPattern.test(item.profileId) && typeof item.name === 'string' && item.name.length <= 128
    ? [{ profileId: item.profileId, name: item.name }] : []) : [];
  const scripts = Array.isArray(value.scripts) ? value.scripts.flatMap((item) => record(item) && typeof item.profileId === 'string'
      && idPattern.test(item.profileId) && typeof item.name === 'string' && item.name.length <= 128
    ? [{ profileId: item.profileId, name: item.name }] : []) : [];
  return { schedules, history, states, workflows, scripts, running: value.running };
}

export function describeRecurrence(recurrence: ScheduleRecurrence): string {
  if (recurrence.kind === 'interval') return `Every ${recurrence.intervalMinutes} min`;
  const time = recurrence.localTime?.slice(0, 5) ?? '';
  return recurrence.kind === 'daily' ? `Daily at ${time}` : `${recurrence.daysOfWeek?.map((day) => day.slice(0, 3)).join(', ')} at ${time}`;
}
