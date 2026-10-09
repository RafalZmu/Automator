const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');
const modelUrl = pathToFileURL(path.join(workspace, 'ui', 'modules', 'workTimeViewModel.ts')).href;

test('work-time snapshot validates multiple running and paused timers, drafts, and saved entries', async () => {
  const { readWorkTimeSnapshot } = await import(modelUrl);
  const segment = { startedUtc: '2026-10-03T12:00:00Z', stoppedUtc: '2026-10-03T12:00:30Z' };
  const valid = {
    timers: [{ id: 'work-1', description: 'A', startedUtc: '2026-10-03T12:00:00Z', status: 'running', elapsedMilliseconds: 30_000,
      sampledUtc: '2026-10-03T12:00:30Z', segments: [{ ...segment, stoppedUtc: null }] }],
    pendingEntries: [],
    history: [],
  };
  assert.equal(readWorkTimeSnapshot(valid)?.timers[0].id, 'work-1');
  assert.equal(readWorkTimeSnapshot({ ...valid, timers: [valid.timers[0], { ...valid.timers[0], id: 'work-2', status: 'running' }] }), null,
    'only one timer can be running');
  assert.equal(readWorkTimeSnapshot({ ...valid, timers: [{ ...valid.timers[0], elapsedMilliseconds: -1 }] }), null);
  assert.equal(readWorkTimeSnapshot({ ...valid, timers: [{ ...valid.timers[0], status: 'paused' }] }), null,
    'paused timers cannot have an open segment');
  assert.equal(readWorkTimeSnapshot({ ...valid, history: [{ description: ' ' }] }), null);
});

test('work-time snapshot accepts undescribed ended drafts but rejects them as saved history', async () => {
  const { readWorkTimeSnapshot } = await import(modelUrl);
  const pending = {
    id: 'work-pending',
    startedUtc: '2026-10-03T12:00:00Z',
    stoppedUtc: '2026-10-03T12:00:20Z',
    durationMilliseconds: 20_000,
    description: '',
    tags: [],
    segments: [{ startedUtc: '2026-10-03T12:00:00Z', stoppedUtc: '2026-10-03T12:00:20Z' }],
  };

  const snapshot = readWorkTimeSnapshot({ timers: [], pendingEntries: [pending], history: [] });
  assert.deepEqual(snapshot?.pendingEntries, [pending]);
  assert.equal(readWorkTimeSnapshot({ timers: [], pendingEntries: [], history: [pending] }), null,
    'saved history still requires a description');
});

test('work-time display clock advances from a sampled backend duration', async () => {
  const { elapsedWorkTimeMilliseconds, formatWorkTimeDuration } = await import(modelUrl);
  const sample = {
    id: 'work-1', description: 'A', status: 'running', startedUtc: '2026-10-03T12:00:00Z', elapsedMilliseconds: 120_000,
    sampledUtc: '2026-10-03T12:02:00Z',
    segments: [{ startedUtc: '2026-10-03T12:00:00Z', stoppedUtc: null }],
  };
  assert.equal(elapsedWorkTimeMilliseconds(sample, Date.parse('2026-10-03T12:02:10Z')), 130_000);
  assert.equal(formatWorkTimeDuration(1_000), '1s');
  assert.equal(formatWorkTimeDuration(20_000), '20s');
  assert.equal(formatWorkTimeDuration(59_999), '59s');
  assert.equal(formatWorkTimeDuration(60_000), '1m');
  assert.equal(formatWorkTimeDuration(59 * 60_000), '59m');
  assert.equal(formatWorkTimeDuration(62 * 60_000), '1h 2m');
});

test('work-time working-period duration counts only running segments and excludes paused gaps', async () => {
  const { calculateWorkTimeIntervalMilliseconds } = await import(modelUrl);
  const interval = (start, stop, nextDay = false) => calculateWorkTimeIntervalMilliseconds(
    new Date(2026, 9, 6, ...start).toISOString(), new Date(2026, 9, 6 + Number(nextDay), ...stop).toISOString());

  assert.equal(interval([7, 30], [17, 0]), 8 * 60 * 60_000);
  assert.equal(interval([15, 30], [9, 30], true), 2 * 60 * 60_000);
  assert.equal(interval([17, 0], [7, 0]), 0);
  const { readWorkTimeSnapshot, workTimeEntryDurationMilliseconds } = await import(modelUrl);
  const split = {
    startedUtc: new Date(2026, 9, 6, 7, 30).toISOString(), stoppedUtc: new Date(2026, 9, 6, 17).toISOString(),
    durationMilliseconds: 2 * 60 * 60_000,
    segments: [
      { startedUtc: new Date(2026, 9, 6, 7, 30).toISOString(), stoppedUtc: new Date(2026, 9, 6, 9).toISOString() },
      { startedUtc: new Date(2026, 9, 6, 15).toISOString(), stoppedUtc: new Date(2026, 9, 6, 17).toISOString() },
    ],
  };
  assert.equal(workTimeEntryDurationMilliseconds(split, true), 2 * 60 * 60_000);
  const legacyUnknownBoundary = {
    id: 'legacy', startedUtc: new Date(2026, 9, 6, 9).toISOString(), stoppedUtc: new Date(2026, 9, 6, 15, 30).toISOString(),
    durationMilliseconds: 90 * 60_000, description: 'legacy', tags: [],
    segments: [{ startedUtc: new Date(2026, 9, 6, 15).toISOString(), stoppedUtc: new Date(2026, 9, 6, 15, 30).toISOString() }],
  };
  const migrated = readWorkTimeSnapshot({ timers: [], pendingEntries: [], history: [legacyUnknownBoundary] });
  assert.ok(migrated, 'known segments may cover only part of a preserved legacy elapsed duration');
  assert.equal(workTimeEntryDurationMilliseconds(migrated.history[0]), 90 * 60_000);
  assert.equal(workTimeEntryDurationMilliseconds(migrated.history[0], true), 30 * 60_000,
    'working-period reports count only the segment with known boundaries');
});

test('work-time reports calculate local daily and Monday-based weekly totals', async () => {
  const { calculateWorkTimeReportTotals } = await import(modelUrl);
  const entry = (id, started, durationMilliseconds, tags = []) => ({
    id,
    startedUtc: started.toISOString(),
    stoppedUtc: new Date(started.getTime() + durationMilliseconds).toISOString(),
    durationMilliseconds,
    description: id,
    tags,
    segments: [{ startedUtc: started.toISOString(), stoppedUtc: new Date(started.getTime() + durationMilliseconds).toISOString() }],
  });
  const now = new Date(2026, 9, 7, 12, 0, 0);
  const entries = [
    entry('today', new Date(2026, 9, 7, 9, 0, 0), 65_000),
    entry('monday', new Date(2026, 9, 5, 16, 0, 0), 120_000),
    entry('last-week', new Date(2026, 9, 4, 16, 0, 0), 30_000),
  ];

  assert.deepEqual(calculateWorkTimeReportTotals(entries, now), {
    todayMilliseconds: 65_000,
    weekMilliseconds: 185_000,
    todayEntries: 1,
    weekEntries: 2,
  });
});

test('work-time report totals and CSV can use working-period durations while preserving timestamps', async () => {
  const { calculateWorkTimeReportTotals, createWorkTimeCsv, workTimeEntryDurationMilliseconds } = await import(modelUrl);
  const entry = {
    id: 'overnight',
    startedUtc: new Date(2026, 9, 6, 15, 30).toISOString(),
    stoppedUtc: new Date(2026, 9, 7, 9, 30).toISOString(),
    durationMilliseconds: 18 * 60 * 60_000,
    description: 'overnight',
    tags: [],
    segments: [{ startedUtc: new Date(2026, 9, 6, 15, 30).toISOString(), stoppedUtc: new Date(2026, 9, 7, 9, 30).toISOString() }],
  };
  const totals = calculateWorkTimeReportTotals([entry], new Date(2026, 9, 7, 12), true);
  const csv = createWorkTimeCsv([entry], true);

  assert.equal(workTimeEntryDurationMilliseconds(entry, true), 2 * 60 * 60_000);
  assert.equal(totals.todayMilliseconds, 90 * 60_000, 'Wednesday receives only its own local working-period overlap');
  assert.equal(totals.weekMilliseconds, 2 * 60 * 60_000);
  assert.equal(totals.todayEntries, 1, 'today counts entries with working time on Wednesday');
  assert.equal(totals.weekEntries, 1, 'the week counts the spanning entry once');
  assert.match(csv, new RegExp(`${entry.startedUtc}.*${entry.stoppedUtc}.*"7200\.000","7200000"`));
  assert.equal(entry.durationMilliseconds, 18 * 60 * 60_000);
});

test('work-time reports filter inclusive local dates and tags without changing source entries', async () => {
  const { filterWorkTimeEntries, getWorkTimeTags } = await import(modelUrl);
  const entries = [
    { id: 'today', startedUtc: new Date(2026, 9, 7, 9).toISOString(), tags: ['Work', 'Planning'] },
    { id: 'monday', startedUtc: new Date(2026, 9, 5, 16).toISOString(), tags: ['work'] },
    { id: 'last-week', startedUtc: new Date(2026, 9, 4, 16).toISOString(), tags: ['work'] },
  ];

  const filtered = filterWorkTimeEntries(entries, { fromDate: '2026-10-05', toDate: '2026-10-07', tag: 'WORK' });
  assert.deepEqual(filtered.map((item) => item.id), ['today', 'monday']);
  assert.deepEqual(getWorkTimeTags(entries), ['Planning', 'Work']);
  assert.deepEqual(entries.map((item) => item.id), ['today', 'monday', 'last-week']);
});

test('work-time CSV export preserves exact milliseconds and escapes spreadsheet formulas and delimiters', async () => {
  const { createWorkTimeCsv } = await import(modelUrl);
  const entry = {
    id: 'work-1',
    startedUtc: '2026-10-07T07:00:00.000Z',
    stoppedUtc: '2026-10-07T07:00:01.234Z',
    durationMilliseconds: 1_234,
    description: '=SUM(1,"2")',
    tags: ['planning, review'],
    segments: [{ startedUtc: '2026-10-07T07:00:00.000Z', stoppedUtc: '2026-10-07T07:00:01.234Z' }],
  };

  assert.equal(createWorkTimeCsv([entry]), [
    '"id","startedUtc","stoppedUtc","durationSeconds","durationMilliseconds","description","tags"',
    '"work-1","2026-10-07T07:00:00.000Z","2026-10-07T07:00:01.234Z","1.234","1234","\'=SUM(1,""2"")","planning, review"',
  ].join('\r\n'));
});
