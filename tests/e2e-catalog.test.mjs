import assert from 'node:assert/strict';
import { test } from 'node:test';
import { buildTests, categories, e2eLayers, readPage, serialize } from '../scripts/e2e-catalog.mjs';

test('e2e catalog lists every end-to-end and API test once with a known category', () => {
  const tests = buildTests();
  assert.ok(tests.length > 30);
  assert.ok(tests.every((item) => e2eLayers.includes(item.layer)), "only end-to-end and API tests are listed");
  assert.equal(new Set(tests.map((item) => item.id)).size, tests.length);
  for (const item of tests) assert.ok(categories.includes(item.category), `${item.title} has unknown category ${item.category}`);
});

test('e2e catalog page data parses and keeps ideas with steps and expected results', () => {
  const { html, data } = readPage();
  assert.ok(html.includes('id="e2e-data"'));
  const copy = JSON.parse(serialize({ tests: [], ideas: [{ id: 'idea-1', title: 'x </script>', category: 'Launcher', steps: ['a'], expected: 'b' }] }));
  assert.deepEqual(copy.ideas[0].steps, ['a']);
  for (const idea of data.ideas) assert.ok(categories.includes(idea.category), `${idea.title} has unknown category`);
});
