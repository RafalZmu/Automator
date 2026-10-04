import assert from 'node:assert/strict';
import { test } from 'node:test';
import { validateSchedule, readSchedulerSnapshot, describeRecurrence } from '../ui/contracts/scheduler.ts';
const interval = { id: 'daily-report', name: 'Report', workflowId: 'report', targetKind: 'workflow', profileId: 'report', enabled: true, runOnceAfterRestart: false,
  recurrence: { kind: 'interval', intervalMinutes: 60, localTime: null, daysOfWeek: null, intervalAnchorUtc: null } };
test('scheduler accepts interval/daily/weekly recurrence and rejects stale incompatible fields', () => {
  assert.deepEqual(validateSchedule(interval), []);
  assert.ok(validateSchedule({ ...interval, recurrence: { ...interval.recurrence, localTime: '09:00:00' } }).length);
  assert.ok(validateSchedule({ ...interval, recurrence: { ...interval.recurrence, intervalMinutes: 0 } }).length);
  const weekly = { ...interval, recurrence: { kind: 'weekly', intervalMinutes: null, intervalAnchorUtc: null, localTime: '09:00:00', daysOfWeek: ['monday', 'friday'] } };
  assert.deepEqual(validateSchedule(weekly), []);
  assert.ok(validateSchedule({ ...weekly, recurrence: { ...weekly.recurrence, daysOfWeek: ['monday', 'monday'] } }).length);
  assert.ok(validateSchedule({ ...weekly, recurrence: { ...weekly.recurrence, localTime: '24:00:00' } }).length);
});
test('scheduler snapshot tolerates optional catalogs and excludes arbitrary history output', () => {
  const snapshot = readSchedulerSnapshot({ schedules: [interval], running: true, history: [{ id: 'run-1', scheduleId: 'daily-report',
    startedUtc: '2026-10-03T10:00:00Z', finishedUtc: '2026-10-03T10:00:02Z', status: 'success', category: 'completed', durationMilliseconds: 2000,
    stdout: 'sensitive body', data: { secret: 'no' } }] });
  assert.equal(snapshot.schedules.length, 1);
  assert.deepEqual(snapshot.states, []);
  assert.deepEqual(snapshot.workflows, []);
  assert.deepEqual(snapshot.scripts, []);
  assert.equal(JSON.stringify(snapshot).includes('sensitive body'), false);
  assert.equal(JSON.stringify(snapshot).includes('secret'), false);
});
test('scheduler reader keeps missing workflow references for repair and exact next-run timestamps', () => {
  const timestamp = '2026-10-03T11:00:00+02:00';
  const snapshot = readSchedulerSnapshot({ schedules: [interval], history: [], running: true,
    states: [{ scheduleId: interval.id, nextRunAtUtc: timestamp, running: false }], workflows: [] });
  assert.equal(snapshot.schedules[0].targetKind, 'workflow');
  assert.equal(snapshot.schedules[0].profileId, 'report');
  assert.equal(snapshot.states[0].nextRunAtUtc, timestamp);
  assert.equal(describeRecurrence(interval.recurrence), 'Every 60 min');
});
test('scheduler reader accepts script profile targets while migrating old workflowId records', () => {
  const next = readSchedulerSnapshot({ schedules: [
    interval,
    { ...interval, id: 'nightly-script', targetKind: 'scriptProfile', profileId: 'nightly-build', workflowId: '' },
  ], history: [], running: true, workflows: [], scripts: [{ profileId: 'nightly-build', name: 'Nightly build' }] });
  assert.deepEqual(next.schedules.map(({ targetKind, profileId }) => [targetKind, profileId]), [
    ['workflow', 'report'], ['scriptProfile', 'nightly-build'],
  ]);
  assert.equal(next.scripts[0].name, 'Nightly build');
});

async function loadAgendaViewModel() {
  return import('../ui/modules/schedulerAgendaViewModel.ts').catch((error) => {
    if (error?.code === 'ERR_MODULE_NOT_FOUND' && String(error.message).includes('schedulerAgendaViewModel.ts')) return null;
    throw error;
  });
}

test('agenda sorts one upcoming local-time run per enabled schedule and preserves target and running state', async () => {
  const agenda = await loadAgendaViewModel();
  assert.ok(agenda?.buildUpcomingScheduleEntries, 'scheduler agenda view model is not implemented yet');

  const now = new Date(2026, 9, 4, 10, 0);
  const at = (hour, minute) => new Date(2026, 9, 4, hour, minute).toISOString();
  const snapshot = readSchedulerSnapshot({
    running: false,
    schedules: [
      { ...interval, id: 'report-later', name: 'Later report', profileId: 'report', enabled: true },
      { ...interval, id: 'script-build', name: 'Build', targetKind: 'scriptProfile', profileId: 'nightly-build', workflowId: '', enabled: true },
      { ...interval, id: 'report-first', name: 'Morning report', profileId: 'report', enabled: true },
      { ...interval, id: 'paused-job', name: 'Paused', profileId: 'report', enabled: false },
      { ...interval, id: 'past-job', name: 'Past', profileId: 'report', enabled: true },
    ],
    states: [
      { scheduleId: 'report-later', nextRunAtUtc: at(13, 0), running: false },
      { scheduleId: 'script-build', nextRunAtUtc: at(10, 45), running: true },
      { scheduleId: 'report-first', nextRunAtUtc: at(11, 15), running: false },
      { scheduleId: 'paused-job', nextRunAtUtc: at(12, 0), running: false },
      { scheduleId: 'past-job', nextRunAtUtc: at(9, 59), running: false },
    ],
    workflows: [{ profileId: 'report', name: 'Report workflow' }],
    scripts: [{ profileId: 'nightly-build', name: 'Nightly build' }],
    history: [],
  });

  const entries = agenda.buildUpcomingScheduleEntries(snapshot, now);
  assert.deepEqual(entries.map(({ scheduleId, timeLabel, targetLabel, currentlyRunning }) => [scheduleId, timeLabel, targetLabel, currentlyRunning]), [
    ['script-build', '10:45', 'Script profile · Nightly build', true],
    ['report-first', '11:15', 'Workflow · Report workflow', false],
    ['report-later', '13:00', 'Workflow · Report workflow', false],
  ]);
  assert.equal(entries.length, 3, 'agenda must use only the next run supplied by Scheduler, not generate recurrence instances');
});

test('calendar groups the next run on its local day and fills a Sunday-first month grid', async () => {
  const agenda = await loadAgendaViewModel();
  assert.ok(agenda?.buildUpcomingScheduleEntries, 'scheduler agenda view model is not implemented yet');
  assert.ok(agenda?.buildCalendarMonth, 'scheduler calendar month view model is not implemented yet');

  const runAt = new Date(2026, 9, 4, 9, 15);
  const snapshot = readSchedulerSnapshot({
    running: true,
    schedules: [{ ...interval, id: 'october-report', name: 'October report', profileId: 'report' }],
    states: [{ scheduleId: 'october-report', nextRunAtUtc: runAt.toISOString(), running: false }],
    workflows: [{ profileId: 'report', name: 'Report workflow' }],
    history: [],
  });
  const entries = agenda.buildUpcomingScheduleEntries(snapshot, new Date(2026, 9, 1));
  const month = agenda.buildCalendarMonth(new Date(2026, 9, 15), entries);

  assert.equal(month.days.length, 35);
  assert.equal(month.days[0].dateKey, '2026-09-27');
  assert.equal(month.days.at(-1).dateKey, '2026-10-31');
  const runDay = month.days.find(({ dateKey }) => dateKey === '2026-10-04');
  assert.equal(runDay.inMonth, true);
  assert.deepEqual(runDay.entries.map(({ scheduleId, timeLabel }) => [scheduleId, timeLabel]), [['october-report', '09:15']]);
  const adjacentDay = month.days.find(({ dateKey }) => dateKey === '2026-09-27');
  assert.equal(adjacentDay.inMonth, false);
  assert.deepEqual(adjacentDay.entries, []);
});
