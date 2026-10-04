const assert = require('node:assert/strict');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');
const modelUrl = pathToFileURL(path.join(workspace, 'ui', 'modules', 'workTimeViewModel.ts')).href;

test('work-time snapshot validates active, pending, and saved entries', async () => {
  const { readWorkTimeSnapshot } = await import(modelUrl);
  const valid = {
    active: { id: 'work-1', startedUtc: '2026-10-03T12:00:00Z', elapsedMilliseconds: 30_000, sampledUtc: '2026-10-03T12:00:30Z' },
    pending: null,
    history: [],
  };
  assert.equal(readWorkTimeSnapshot(valid)?.active?.id, 'work-1');
  assert.equal(readWorkTimeSnapshot({ ...valid, pending: { id: 'work-2' } }), null, 'active and pending cannot coexist');
  assert.equal(readWorkTimeSnapshot({ ...valid, active: { ...valid.active, elapsedMilliseconds: -1 } }), null);
  assert.equal(readWorkTimeSnapshot({ ...valid, history: [{ description: ' ' }] }), null);
});

test('work-time snapshot accepts an undescribed pending interval but rejects it as saved history', async () => {
  const { readWorkTimeSnapshot } = await import(modelUrl);
  const pending = {
    id: 'work-pending',
    startedUtc: '2026-10-03T12:00:00Z',
    stoppedUtc: '2026-10-03T12:00:20Z',
    durationMilliseconds: 20_000,
    description: '',
    tags: [],
  };

  const snapshot = readWorkTimeSnapshot({ active: null, pending, history: [] });
  assert.deepEqual(snapshot?.pending, pending);
  assert.equal(readWorkTimeSnapshot({ active: null, pending: null, history: [pending] }), null,
    'saved history still requires a description');
});

test('work-time display clock advances from a sampled backend duration', async () => {
  const { elapsedWorkTimeMilliseconds, formatWorkTimeDuration } = await import(modelUrl);
  const sample = {
    id: 'work-1', startedUtc: '2026-10-03T12:00:00Z', elapsedMilliseconds: 120_000,
    sampledUtc: '2026-10-03T12:02:00Z',
  };
  assert.equal(elapsedWorkTimeMilliseconds(sample, Date.parse('2026-10-03T12:02:10Z')), 130_000);
  assert.equal(formatWorkTimeDuration(1_000), '1s');
  assert.equal(formatWorkTimeDuration(20_000), '20s');
  assert.equal(formatWorkTimeDuration(59_999), '59s');
  assert.equal(formatWorkTimeDuration(60_000), '1m');
  assert.equal(formatWorkTimeDuration(59 * 60_000), '59m');
  assert.equal(formatWorkTimeDuration(62 * 60_000), '1h 2m');
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
  };

  assert.equal(createWorkTimeCsv([entry]), [
    '"id","startedUtc","stoppedUtc","durationSeconds","durationMilliseconds","description","tags"',
    '"work-1","2026-10-07T07:00:00.000Z","2026-10-07T07:00:01.234Z","1.234","1234","\'=SUM(1,""2"")","planning, review"',
  ].join('\r\n'));
});
