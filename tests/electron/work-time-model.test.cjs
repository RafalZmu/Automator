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

// Valid saved entries, one minute apart, numbered from firstIndex (ascending by stop time).
function workEntries(count, firstIndex = 0) {
  const base = Date.parse('2026-10-03T08:00:00Z');
  return Array.from({ length: count }, (_, offset) => {
    const index = firstIndex + offset;
    const startedUtc = new Date(base + index * 60_000).toISOString();
    const stoppedUtc = new Date(base + index * 60_000 + 30_000).toISOString();
    return { id: `entry-${index}`, description: 'Work', startedUtc, stoppedUtc, durationMilliseconds: 30_000, tags: [],
      segments: [{ startedUtc, stoppedUtc }] };
  });
}

test('work-time snapshot keeps the sent page and reads the stored total, defaulting to the page length', async () => {
  const { readWorkTimeSnapshot } = await import(modelUrl);
  const page = workEntries(250, 4750);
  const snapshot = readWorkTimeSnapshot({ timers: [], pendingEntries: [], history: page, historyTotal: 5000 });
  assert.equal(snapshot.history.length, 250);
  assert.equal(snapshot.history[0].id, 'entry-4750');
  assert.equal(snapshot.historyTotal, 5000);
  assert.equal(readWorkTimeSnapshot({ timers: [], pendingEntries: [], history: page })?.historyTotal, 250);
  assert.equal(readWorkTimeSnapshot({ timers: [], pendingEntries: [], history: page, historyTotal: 249 }), null,
    'the stored total cannot be smaller than the sent page');
});

test('work-time history pages validate entries and report offset and total', async () => {
  const { readWorkTimeHistoryPage } = await import(modelUrl);
  const page = readWorkTimeHistoryPage({ entries: workEntries(2, 10), total: 5000, offset: 0 });
  assert.equal(page.entries.length, 2);
  assert.equal(page.total, 5000);
  assert.equal(page.offset, 0);
  assert.deepEqual(readWorkTimeHistoryPage({ entries: [], total: 5000, offset: 5000 }).entries, []);
  assert.equal(readWorkTimeHistoryPage({ entries: [{ description: ' ' }], total: 1, offset: 0 }), null);
  assert.equal(readWorkTimeHistoryPage({ entries: [], total: 1.5, offset: 0 }), null);
  assert.equal(readWorkTimeHistoryPage({ total: 1, offset: 0 }), null);
});

test('merged work-time history removes duplicate ids, prefers the fresh entry, and orders oldest first', async () => {
  const { mergeWorkTimeHistory } = await import(modelUrl);
  const [oldest, middle, newest] = workEntries(3);
  const freshMiddle = { ...middle, description: 'Fresh' };
  const merged = mergeWorkTimeHistory([newest, freshMiddle], [middle, oldest]);
  assert.deepEqual(merged.map((entry) => entry.id), [oldest.id, middle.id, newest.id]);
  assert.equal(merged[1].description, 'Fresh');
  assert.deepEqual(mergeWorkTimeHistory([], []), []);
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

test('work-time descriptions reject whitespace-only values before saving', async () => {
  const { isWorkTimeDescriptionValid } = await import(modelUrl);
  assert.equal(isWorkTimeDescriptionValid(''), false);
  assert.equal(isWorkTimeDescriptionValid('   \t  '), false);
  assert.equal(isWorkTimeDescriptionValid('  timer description  '), true);
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

test('work-time manual entries convert local datetime fields and reject invalid intervals', async () => {
  const { resolveWorkTimeInterval, validateWorkTimeInterval, workTimeInputToIso } = await import(modelUrl);
  const now = new Date(2026, 9, 7, 12, 0, 0).getTime();

  assert.equal(workTimeInputToIso('2026-10-07T09:15'), new Date(2026, 9, 7, 9, 15).toISOString());
  assert.equal(workTimeInputToIso('2026-10-07T09:15:30'), new Date(2026, 9, 7, 9, 15, 30).toISOString());
  assert.equal(workTimeInputToIso('2026-02-31T09:15'), null, 'overflowing calendar days are rejected');
  assert.equal(workTimeInputToIso('2026-10-07 09:15'), null, 'only the datetime-local format is accepted');
  assert.equal(workTimeInputToIso(''), null);

  const start = new Date(2026, 9, 7, 9, 0, 0).toISOString();
  const stop = new Date(2026, 9, 7, 10, 30, 0).toISOString();
  assert.equal(validateWorkTimeInterval(start, stop, now), null);
  assert.equal(validateWorkTimeInterval(stop, start, now), 'The end time must be after the start time.');
  assert.equal(validateWorkTimeInterval(start, start, now), 'The end time must be after the start time.');
  assert.equal(validateWorkTimeInterval(start, new Date(2026, 9, 7, 13).toISOString(), now), 'The end time cannot be in the future.');
  assert.equal(validateWorkTimeInterval('not a date', stop, now), 'Enter a valid start and end time.');

  assert.deepEqual(resolveWorkTimeInterval('2026-10-07T09:00', '2026-10-07T10:30', null, now), { startedUtc: start, stoppedUtc: stop });
  assert.deepEqual(resolveWorkTimeInterval('2026-10-07T10:30', '2026-10-07T09:00', null, now), { error: 'The end time must be after the start time.' });
  assert.deepEqual(resolveWorkTimeInterval('2026-10-07T09:00', '', null, now), { error: 'Enter a valid start and end time.' });
});

test('work-time edits keep the stored instant when a time field is untouched', async () => {
  const { localDateTimeInputValue, resolveWorkTimeInterval } = await import(modelUrl);
  const now = new Date(2026, 9, 7, 12, 0, 0).getTime();
  const original = {
    startedUtc: new Date(2026, 9, 7, 9, 0, 0, 250).toISOString(),
    stoppedUtc: new Date(2026, 9, 7, 10, 0, 0, 750).toISOString(),
  };

  assert.equal(localDateTimeInputValue(original.startedUtc), '2026-10-07T09:00:00');
  assert.deepEqual(resolveWorkTimeInterval(localDateTimeInputValue(original.startedUtc), localDateTimeInputValue(original.stoppedUtc), original, now),
    original, 'milliseconds survive an edit that changes nothing about the times');

  const moved = resolveWorkTimeInterval('2026-10-07T09:30:00', localDateTimeInputValue(original.stoppedUtc), original, now);
  assert.deepEqual(moved, { startedUtc: new Date(2026, 9, 7, 9, 30, 0).toISOString(), stoppedUtc: original.stoppedUtc });
  assert.deepEqual(resolveWorkTimeInterval('2026-10-07T10:30:00', '2026-10-07T10:00:00', original, now),
    { error: 'The end time must be after the start time.' });
});

test('work-time manual duration ends now and starts the duration earlier', async () => {
  const { resolveWorkTimeDuration, workTimeDurationMilliseconds } = await import(modelUrl);
  const now = Date.parse('2026-10-07T12:00:00.000Z');

  assert.equal(workTimeDurationMilliseconds('1', '5'), 65 * 60_000);
  assert.equal(workTimeDurationMilliseconds('', '45'), 45 * 60_000, 'a blank field counts as zero');
  assert.equal(workTimeDurationMilliseconds('2', ''), 120 * 60_000);
  assert.deepEqual(resolveWorkTimeDuration('1', '5', now), {
    startedUtc: '2026-10-07T10:55:00.000Z',
    stoppedUtc: '2026-10-07T12:00:00.000Z',
  });

  for (const [hours, minutes] of [['', ''], ['0', '0'], ['1', '60'], ['-1', '5'], ['1.5', '0'], ['abc', '5'], ['100', '0']]) {
    assert.equal(workTimeDurationMilliseconds(hours, minutes), null, `${hours}h ${minutes}m is rejected`);
    assert.ok('error' in resolveWorkTimeDuration(hours, minutes, now));
  }
});
