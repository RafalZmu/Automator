import assert from 'node:assert/strict';
import { test } from 'node:test';
import {
  parsePlaywrightTestList,
  validatePlaywrightSectionPath,
  readBrowserActionResult,
  readBrowserProfiles,
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
