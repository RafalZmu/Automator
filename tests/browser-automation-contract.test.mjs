import assert from 'node:assert/strict';
import { test } from 'node:test';
import {
  parsePlaywrightTestList,
  validatePlaywrightSectionPath,
  readBrowserActionResult,
  readBrowserProfiles,
  readPlaywrightRunHistory,
  validateBrowserAction,
  validateBrowserProfile,
} from '../ui/contracts/browser.ts';

const profile = {
  id: 'docs',
  name: 'Documentation',
  startUrl: 'https://docs.example.test/start',
  allowedHosts: ['docs.example.test'],
  allowLocalNetwork: false,
};

test('browser profile validation requires its start host in an exact allowlist', () => {
  assert.deepEqual(validateBrowserProfile(profile), []);
  assert.match(validateBrowserProfile({ ...profile, allowedHosts: ['*.example.test'] }).join(' '), /exact host/);
  assert.match(validateBrowserProfile({ ...profile, startUrl: 'file:///C:/secret.txt' }).join(' '), /HTTP or HTTPS/);
  assert.match(validateBrowserProfile({ ...profile, startUrl: 'https://other.example.test' }).join(' '), /allowed-host/);
});

test('browser actions reject unsupported, oversized, malformed, and out-of-policy inputs', () => {
  assert.deepEqual(validateBrowserAction({ kind: 'navigate', url: 'https://docs.example.test/next' }, profile), []);
  assert.match(validateBrowserAction({ kind: 'navigate', url: 'https://outside.example.test' }, profile).join(' '), /allowed-host/);
  assert.match(validateBrowserAction({ kind: 'evaluate', value: '1 + 1' }, profile).join(' '), /unsupported/);
  assert.match(validateBrowserAction({ kind: 'fill', locatorKind: 'label', locator: 'Email' }, profile).join(' '), /value/);
  assert.match(validateBrowserAction({ kind: 'click', locatorKind: 'text', locator: 'x'.repeat(2049) }, profile).join(' '), /Locator/);
});

test('browser result decoding accepts bounded known fields and ignores malformed links', () => {
  assert.deepEqual(readBrowserActionResult({
    currentUrl: 'https://docs.example.test/',
    title: 'Docs',
    text: 'Welcome',
    links: [{ text: 'Guide', url: 'https://docs.example.test/guide' }, { text: 5, url: 'javascript:alert(1)' }],
  }), {
    currentUrl: 'https://docs.example.test/',
    title: 'Docs',
    text: 'Welcome',
    links: [{ text: 'Guide', url: 'https://docs.example.test/guide' }],
  });
  assert.equal(readBrowserActionResult({ currentUrl: 'x'.repeat(9000) }), null);
  assert.deepEqual(readBrowserProfiles({ profiles: [profile, { ...profile, id: 'INVALID' }, null] }), [profile]);
});

test('Playwright list parsing groups exact project, file, location, and title identities', () => {
  const parsed = parsePlaywrightTestList([
    'Listing tests:',
    '  [chromium] › tests/login.spec.ts:12:5 › account › signs in',
    '  [firefox] › tests/login.spec.ts:12:5 › account › signs in',
    '  tests/profile.test.ts:4:1 › renders profile',
    'Total: 3 tests in 2 files',
  ].join('\n'));

  assert.equal(parsed.length, 3);
  assert.equal(parsed[0].project, 'chromium');
  assert.equal(parsed[0].file, 'tests/login.spec.ts');
  assert.equal(parsed[0].line, 12);
  assert.deepEqual(parsed[0].titlePath, ['account', 'signs in']);
  assert.equal(parsed[0].listEntry, '[chromium] › tests/login.spec.ts:12:5 › account › signs in');
  assert.notEqual(parsed[0].id, parsed[1].id);
});

test('Playwright section path validation allows test files under the project and rejects traversal', () => {
  assert.equal(validatePlaywrightSectionPath('tests/auth/login.spec.ts'), 'tests/auth/login.spec.ts');
  assert.equal(validatePlaywrightSectionPath('tests\u005c..\u005c..\u005csecret.spec.ts'), null);
  assert.equal(validatePlaywrightSectionPath('tests/notes.txt'), null);
  assert.equal(validatePlaywrightSectionPath('/outside.spec.ts'), null);
  assert.equal(validatePlaywrightSectionPath('tests/new.test.mts'), null);
});

test('Playwright run history decoding keeps bounded known fields and drops malformed rows', () => {
  const run = {
    id: 'a1', kind: 'file', target: 'tests/example.spec.ts', state: 'failed', message: 'x: 0 passed, 1 failed, 0 skipped.',
    startedUtc: '2026-10-14T10:00:00+00:00', finishedUtc: '2026-10-14T10:00:02+00:00', durationMilliseconds: 2000, exitCode: 1,
    summary: { passed: 0, failed: 1, skipped: 0, interrupted: 0 },
    tests: [{ title: 'one', file: 'tests/example.spec.ts', line: 3, project: null, status: 'failed', reason: 'assertion', error: 'Expected: 4' }, { title: 'two', file: 'f', line: 1, status: 'failed', reason: 'bogus' }, { title: 7 }],
    outputTruncated: false, projectRoot: 'C:\Playwright',
  };
  const parsed = readPlaywrightRunHistory({
    runs: [run, { ...run, id: 'b2', kind: 'shell' }, { ...run, id: 'c3', state: 'weird' }, { ...run, id: 'd4', finishedUtc: 'nope' }, null],
    active: [{ id: 'live', kind: 'tag', target: 'tag smoke', startedUtc: '2026-10-14T10:01:00+00:00' }, { id: 5 }],
  });
  assert.deepEqual(parsed.runs.map((item) => item.id), ['a1']);
  assert.equal(parsed.runs[0].tests.length, 2);
  assert.deepEqual([parsed.runs[0].tests[0].reason, parsed.runs[0].tests[0].error], ['assertion', 'Expected: 4']);
  assert.deepEqual([parsed.runs[0].tests[1].reason, parsed.runs[0].tests[1].error], [null, null]);
  assert.equal(parsed.runs[0].summary.failed, 1);
  assert.deepEqual(parsed.active.map((item) => item.id), ['live']);
  assert.equal(readPlaywrightRunHistory({ runs: 'bad' }), null);
});
