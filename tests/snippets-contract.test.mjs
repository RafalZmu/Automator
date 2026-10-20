import test from 'node:test';
import assert from 'node:assert/strict';
import { MAX_SNIPPETS, MAX_SNIPPET_VALUE_LENGTH, parseSnippetsSettings, readSnippetShortcuts, readSnippetsSettings, validateSnippetsSettings } from '../ui/contracts/snippets.ts';

const plan = { id: 'plan', name: 'Plan command', alias: 'plan', value: '/plan' };
const nip = { id: 'nip', name: 'Company NIP', alias: 'nip2', value: '5260001246' };

test('parses snippets without changing their order or text', () => {
  const settings = { snippets: [plan, nip] };
  assert.deepEqual(parseSnippetsSettings(settings), settings);
});

test('keeps line breaks and tabs in snippet text but rejects other control characters', () => {
  assert.deepEqual(validateSnippetsSettings({ snippets: [{ ...plan, value: 'line one\r\n\tline two' }] }), []);
  for (const value of ['bell\u0007', 'nul\u0000', 'escape\u001b[0m']) {
    assert.ok(validateSnippetsSettings({ snippets: [{ ...plan, value }] }).some((error) => /text/i.test(error)), `${JSON.stringify(value)} should be rejected`);
  }
});

test('rejects blank snippet text and text over the length limit', () => {
  for (const value of ['', '   \n']) {
    assert.ok(validateSnippetsSettings({ snippets: [{ ...plan, value }] }).some((error) => /text/i.test(error)));
  }
  assert.deepEqual(validateSnippetsSettings({ snippets: [{ ...plan, value: 'a'.repeat(MAX_SNIPPET_VALUE_LENGTH) }] }), []);
  assert.ok(validateSnippetsSettings({ snippets: [{ ...plan, value: 'a'.repeat(MAX_SNIPPET_VALUE_LENGTH + 1) }] }).length > 0);
});

test('accepts letters and digits as a shortcut, allows none, and rejects other characters', () => {
  assert.deepEqual(validateSnippetsSettings({ snippets: [{ ...plan, alias: '' }, nip] }), []);
  for (const alias of ['/plan', 'two words', 'dash-ed', 'a'.repeat(33)]) {
    assert.ok(validateSnippetsSettings({ snippets: [{ ...plan, alias }] }).some((error) => /shortcut/i.test(error)), `${alias} should be rejected`);
  }
});

test('rejects a shortcut used by two snippets regardless of letter case but allows several without one', () => {
  assert.ok(validateSnippetsSettings({ snippets: [plan, { ...nip, alias: 'PLAN' }] }).some((error) => /shortcut/i.test(error)));
  assert.deepEqual(validateSnippetsSettings({ snippets: [{ ...plan, alias: '' }, { ...nip, alias: '' }] }), []);
});

test('requires a unique valid key and a name for each snippet', () => {
  assert.ok(validateSnippetsSettings({ snippets: [plan, { ...nip, id: 'plan' }] }).some((error) => /key/i.test(error)));
  assert.ok(validateSnippetsSettings({ snippets: [{ ...plan, id: 'bad id' }] }).some((error) => /key/i.test(error)));
  assert.ok(validateSnippetsSettings({ snippets: [{ ...plan, name: '  ' }] }).some((error) => /name/i.test(error)));
});

test('rejects malformed settings, too many snippets, and an oversized payload', () => {
  assert.throws(() => parseSnippetsSettings({ snippets: 'not-an-array' }));
  assert.throws(() => parseSnippetsSettings({}));
  const many = Array.from({ length: MAX_SNIPPETS + 1 }, (_, index) => ({ ...plan, id: `s${index}`, alias: `a${index}` }));
  assert.ok(validateSnippetsSettings({ snippets: many }).length > 0);
  const large = Array.from({ length: 20 }, (_, index) => ({ ...plan, id: `s${index}`, alias: `a${index}`, value: 'x'.repeat(MAX_SNIPPET_VALUE_LENGTH) }));
  assert.ok(validateSnippetsSettings({ snippets: large }).some((error) => /64 KiB/.test(error)));
});

test('reads an invalid saved list as empty instead of exposing partial data', () => {
  assert.deepEqual(readSnippetsSettings({ snippets: [plan, { ...nip, value: '' }] }), { snippets: [] });
  assert.deepEqual(readSnippetsSettings({}), { snippets: [] });
  assert.deepEqual(readSnippetsSettings(undefined), { snippets: [] });
});

const meta = (name, alias, chunks) => JSON.stringify({ name, alias, chunks });

test('rebuilds copyable snippets from the module state, joining chunked text', () => {
  const longText = `${'a'.repeat(2000)}${'b'.repeat(1500)}`;
  const shortcuts = readSnippetShortcuts({
    status: 'ready',
    'meta/plan': meta('Plan command', 'plan', 1), 'text/plan/0': '/plan',
    'meta/long': meta('Long text', 'LONG', 2), 'text/long/0': 'a'.repeat(2000), 'text/long/1': 'b'.repeat(1500),
  });
  assert.deepEqual(shortcuts, [
    { id: 'plan', name: 'Plan command', alias: 'plan', value: '/plan' },
    { id: 'long', name: 'Long text', alias: 'long', value: longText },
  ]);
  assert.deepEqual(readSnippetShortcuts(undefined), []);
  assert.deepEqual(readSnippetShortcuts({ status: 'ready' }), []);
});

test('skips state snippets that are incomplete, invalid, or repeat a shortcut without hiding the others', () => {
  const shortcuts = readSnippetShortcuts({
    'meta/good': meta('Good', 'good', 1), 'text/good/0': 'text',
    'meta/missing': meta('Missing chunk', 'missing', 2), 'text/missing/0': 'only the first half',
    'meta/not-json': 'not json', 'text/not-json/0': 'x',
    'meta/bad alias': meta('Bad alias', 'bad alias', 1), 'text/bad alias/0': 'x',
    'meta/noalias': meta('No alias', '', 1), 'text/noalias/0': 'x',
    'meta/control': meta('Control', 'control', 1), 'text/control/0': 'bell\u0007',
    'meta/blank': meta('Blank', 'blank', 1), 'text/blank/0': '   ',
    'meta/toomany': meta('Too many chunks', 'toomany', 99), 'text/toomany/0': 'x',
    'meta/dup': meta('Duplicate', 'GOOD', 1), 'text/dup/0': 'other',
    'meta/Bad Key': meta('Bad key', 'badkey', 1), 'text/Bad Key/0': 'x',
  });
  assert.deepEqual(shortcuts.map((shortcut) => shortcut.id), ['good']);
});
