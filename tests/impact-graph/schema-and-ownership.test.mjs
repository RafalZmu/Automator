import test from 'node:test';
import assert from 'node:assert/strict';
import { stableFileId, stableFunctionId, resolveAreas, classifyFile, unmappedDiagnostic } from '../../scripts/impact-graph-source/schema.mjs';
import ownership from '../../scripts/impact-graph-source/ownership.json' with { type: 'json' };

test('normalizes path separators for stable file IDs', () => {
  assert.equal(stableFileId('ui\\modules\\ApiView.tsx'), stableFileId('ui/modules/ApiView.tsx'));
  assert.match(stableFileId('ui/modules/ApiView.tsx'), /^file-api-view-[a-f0-9]{8}$/);
});

test('keeps overloaded symbols distinct in stable function IDs', () => {
  assert.notEqual(stableFunctionId('src/Foo.cs', 'M:Foo.Run(System.Int32)'), stableFunctionId('src/Foo.cs', 'M:Foo.Run(System.String)'));
});

test('feature and shared overrides take precedence over project defaults', () => {
  const rules = { defaults: [{ pattern: 'src/', areaIds: ['backend'] }], overrides: [{ pattern: 'src/Automator.Application/Automation/ApiModule.cs', areaIds: ['api'] }] };
  assert.deepEqual(resolveAreas('src/Automator.Application/Automation/ApiModule.cs', rules), ['api']);
});

test('keeps test tagging independent of area ownership', () => {
  const result = classifyFile('tests/Automator.Application.Specs/RunActivitySpecs.cs', ownership);
  assert.equal(result.isTest, true);
  assert.ok(result.areaIds.length > 0);
  assert.ok(!result.areaIds.includes('test'));
});

test('leaves an unmapped path unassigned and creates a stable diagnostic', () => {
  const path = 'future/NewFeature/Thing.cs';
  assert.deepEqual(resolveAreas(path, ownership), []);
  assert.deepEqual(classifyFile(path, ownership).areaIds, []);
  assert.deepEqual(unmappedDiagnostic(path), { path, line: 1, code: 'unmapped-area', message: `No ownership rule matched ${path}.` });
});
