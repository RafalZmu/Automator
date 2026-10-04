import assert from 'node:assert/strict';
import { test } from 'node:test';
import { parseGlobalVariables } from '../ui/contracts/variables.ts';
test('global variables accept typed JSON arrays, objects and primitives', () => {
  const values = parseGlobalVariables([{ name: 'regions', json: '["eu", "apac"]' }, { name: 'config', json: '{"enabled":true}' }, { name: 'workflow.daily.count', json: '3' }]);
  assert.deepEqual(values.regions, ['eu', 'apac']);
  assert.deepEqual(values.config, { enabled: true });
  assert.equal(values['workflow.daily.count'], 3);
});
test('global variables reject duplicate, unsafe, malformed and oversized values', () => {
  assert.throws(() => parseGlobalVariables([{ name: 'same', json: '1' }, { name: ' same ', json: '2' }]), /Duplicate/);
  for (const name of ['__proto__', 'system.path', 'secret.token', 'bad/name']) assert.throws(() => parseGlobalVariables([{ name, json: 'null' }]), /reserved/);
  assert.throws(() => parseGlobalVariables([{ name: 'bad', json: 'plain text' }]), /valid JSON/);
  assert.throws(() => parseGlobalVariables([{ name: 'overflow', json: '1e309' }]), /finite/);
  assert.throws(() => parseGlobalVariables([{ name: 'large', json: JSON.stringify('x'.repeat(65536)) }]), /64 KiB/);
});
