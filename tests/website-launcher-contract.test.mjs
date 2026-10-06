import test from 'node:test';
import assert from 'node:assert/strict';
import { parseWebsiteLauncherSettings, readWebsiteLauncherQuickActions, validateWebsiteLauncherSettings } from '../ui/contracts/websiteLauncher.ts';

const validRow = {
  id: 'work', name: 'Work', alias: 'docs', groups: [
    { id: 'main', name: 'Main', websites: [{ id: 'wiki', name: 'Wiki', url: 'https://wiki.example.com' }] },
    { id: 'chat', name: 'Chat', websites: [{ id: 'chat-site', name: 'Chat', url: 'https://chat.example.com' }] },
  ],
};

test('parses grouped websites without changing their launch order', () => {
  assert.deepEqual(parseWebsiteLauncherSettings({ rows: [validRow] }), { rows: [validRow] });
});

test('allows an alias that may also belong to an app, but rejects duplicate website aliases', () => {
  assert.deepEqual(validateWebsiteLauncherSettings({ rows: [validRow] }), []);
  assert.match(validateWebsiteLauncherSettings({ rows: [validRow, { ...validRow, id: 'other' }] })[0], /alias/i);
});

test('accepts only absolute HTTP and HTTPS website URLs', () => {
  for (const url of ['file:///C:/secret.txt', 'javascript:alert(1)', 'https://user:password@example.com/', 'not a URL']) {
    const errors = validateWebsiteLauncherSettings({ rows: [{ ...validRow, groups: [{ ...validRow.groups[0], websites: [{ ...validRow.groups[0].websites[0], url }] }] }] });
    assert.ok(errors.some((error) => /http|https|URL/i.test(error)), `${url} should be rejected`);
  }
});

test('rejects missing groups, empty groups, and malformed row data', () => {
  assert.ok(validateWebsiteLauncherSettings({ rows: [{ ...validRow, groups: [] }] }).length > 0);
  assert.ok(validateWebsiteLauncherSettings({ rows: [{ ...validRow, groups: [{ ...validRow.groups[0], websites: [] }] }] }).length > 0);
  assert.throws(() => parseWebsiteLauncherSettings({ rows: 'not-an-array' }));
});

test('reads only safe shortcut labels and aliases from backend tab metadata', () => {
  assert.deepEqual(readWebsiteLauncherQuickActions('[{"id":"work","name":"Work","alias":"DOCS"}]'), [
    { id: 'work', name: 'Work', alias: 'docs' },
  ]);
  assert.deepEqual(readWebsiteLauncherQuickActions('[{"id":"work","name":"Work","alias":"docs"},{"id":"bad id","name":"Bad","alias":"bad"}]'), [
    { id: 'work', name: 'Work', alias: 'docs' },
  ]);
  assert.deepEqual(readWebsiteLauncherQuickActions('not-json'), []);
});
