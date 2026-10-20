import test from 'node:test';
import assert from 'node:assert/strict';
import { MAX_SNIPPETS, MAX_SNIPPET_VALUE_LENGTH, parseSnippetsSettings, readSnippetsSettings, validateSnippetsSettings } from '../ui/contracts/snippets.ts';

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
