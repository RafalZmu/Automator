import test from 'node:test';
import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { stableFileId, stableFunctionId, resolveAreas, classifyFile, unmappedDiagnostic } from '../../scripts/impact-graph-source/schema.mjs';
import ownership from '../../scripts/impact-graph-source/ownership.json' with { type: 'json' };
import model from '../../docs/architecture/impact-graph/model.json' with { type: 'json' };

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

test('maps every tracked application source file to existing Area 1 module IDs', () => {
  const trackedFiles = execFileSync('git', ['ls-files', '-z'], { encoding: 'utf8' })
    .split('\0')
    .filter(file => /\.(cs|ts|tsx)$/.test(file))
    .filter(file => !file.startsWith('scripts/impact-graph-source/'))
    .filter(file => !file.startsWith('tests/impact-graph/fixtures/'));
  const modules = new Set(model.nodes.filter(node => node.layer === 'module').map(node => node.id));

  for (const file of trackedFiles) {
    const areaIds = resolveAreas(file, ownership);
    assert.ok(areaIds.length > 0, `${file} should have an Area 1 owner`);
    for (const areaId of areaIds) assert.ok(modules.has(areaId), `${file} owner ${areaId} should be a module node`);
  }
});

test('maps feature views and shared coordinators to their intended module nodes', () => {
  const expected = new Map([
    ['ui/modules/ApiView.tsx', 'api'],
    ['ui/modules/BrowserAutomationView.tsx', 'browser'],
    ['ui/modules/FocusSessionsView.tsx', 'work-time'],
    ['ui/modules/SchedulerView.tsx', 'scheduler'],
    ['ui/modules/ScriptRunnerView.tsx', 'script'],
    ['ui/modules/WebsiteLauncherView.tsx', 'website'],
    ['ui/modules/WorkflowsView.tsx', 'workflows'],
    ['src/Automator.Application/Automation/AutomationWorkflowEngine.cs', 'workflow-engine'],
    ['src/Automator.Application/Automation/AutomationSchedulerCoordinator.cs', 'scheduler-coordinator'],
    ['src/Automator.Application/Automation/AutomationWorkTimeCoordinator.cs', 'worktime-coordinator'],
    ['src/Automator.Application/Automation/AutomationRunActivityService.cs', 'activity'],
  ]);
  for (const [file, expectedId] of expected) assert.deepEqual(resolveAreas(file, ownership), [expectedId], file);
});
