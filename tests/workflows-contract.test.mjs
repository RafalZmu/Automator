import assert from 'node:assert/strict';
import { test } from 'node:test';
import { parseWorkflowVariableEntries, readWorkflowProfile, validateWorkflowProfile } from '../ui/contracts/workflows.ts';

function validWorkflow() {
  return {
    id: 'daily-report',
    name: 'Daily report',
    steps: [
      {
        id: 'fetch-data',
        moduleId: 'api',
        profileId: 'release-status',
        inputs: [{ targetJsonPointer: '/query', literal: { window: 'week' } }],
      },
      {
        id: 'summarize',
        moduleId: 'script-runner',
        profileId: 'summarize-json',
        inputs: [{ targetJsonPointer: '/records', sourceStepId: 'fetch-data', sourceJsonPointer: '/items' }],
      },
    ],
  };
}

test('workflow contracts accept typed JSON values from an earlier step', () => {
  assert.equal(validateWorkflowProfile(validWorkflow()), null);
});

test('workflow contracts accept saved variables with arrays and typed JSON values', () => {
  const workflow = validWorkflow();
  workflow.variables = { regions: ['eu', 'apac'], enabled: true, count: 3, settings: { mode: 'fast' } };
  assert.equal(validateWorkflowProfile(workflow), null);
});

test('workflow contracts treat missing variables as an empty object for legacy profiles', () => {
  const profile = validWorkflow();
  assert.equal(validateWorkflowProfile(profile), null);
  assert.deepEqual(readWorkflowProfile(profile)?.variables, {});
});

test('workflow contracts reject malformed variable objects and invalid variable keys', () => {
  const workflow = validWorkflow();
  workflow.variables = [];
  assert.match(validateWorkflowProfile(workflow) ?? '', /variables.*object/i);

  workflow.variables = { 'bad/key': 'value' };
  assert.match(validateWorkflowProfile(workflow) ?? '', /variable key/i);
});

test('workflow variable editor preserves JSON values and rejects duplicate or malformed entries', () => {
  const parsed = parseWorkflowVariableEntries([
    { key: 'regions', valueText: '["eu", "apac"]' },
    { key: 'enabled', valueText: 'true' },
  ]);
  assert.deepEqual({ ...parsed.variables }, { regions: ['eu', 'apac'], enabled: true });
  assert.match(parseWorkflowVariableEntries([
    { key: 'region', valueText: '"eu"' },
    { key: ' region ', valueText: '"apac"' },
  ]).error ?? '', /duplicate/i);
  assert.match(parseWorkflowVariableEntries([{ key: 'region', valueText: '[oops]' }]).error ?? '', /valid JSON/i);
  assert.match(parseWorkflowVariableEntries(Array.from({ length: 65 }, (_, index) => ({ key: `v${index}`, valueText: 'null' }))).error ?? '', /64 variables/i);
  assert.match(parseWorkflowVariableEntries([{ key: 'payload', valueText: `"${'x'.repeat(49 * 1024)}"` }]).error ?? '', /48 KiB/i);
});

test('workflow contracts preserve an explicit JSON null literal', () => {
  const workflow = validWorkflow();
  workflow.steps[0].inputs = [{ targetJsonPointer: '/optional', literal: null, literalPresent: true }];
  assert.equal(validateWorkflowProfile(workflow), null);
});

test('workflow contracts reject forward output references', () => {
  const workflow = validWorkflow();
  workflow.steps[0].inputs = [{ targetJsonPointer: '/query', sourceStepId: 'summarize', sourceJsonPointer: '' }];
  assert.match(validateWorkflowProfile(workflow) ?? '', /earlier step/i);
});

test('workflow contracts accept mappings from the variables root input', () => {
  const workflow = validWorkflow();
  workflow.steps[1].inputs = [{ targetJsonPointer: '/region', sourceStepId: '$input', sourceJsonPointer: '/region' }];
  assert.equal(validateWorkflowProfile(workflow), null);
});

test('workflow contracts reject JSON pointers with empty path segments', () => {
  const workflow = validWorkflow();
  workflow.steps[0].inputs = [{ targetJsonPointer: '/settings/', literal: true }];
  assert.match(validateWorkflowProfile(workflow) ?? '', /pointer/i);

  workflow.steps[0].inputs = [{ targetJsonPointer: '/settings//mode', literal: true }];
  assert.match(validateWorkflowProfile(workflow) ?? '', /pointer/i);
});

test('workflow contracts reject conflicting destination pointers', () => {
  const workflow = validWorkflow();
  workflow.steps[0].inputs = [
    { targetJsonPointer: '/config', literal: {} },
    { targetJsonPointer: '/config/mode', literal: 'fast' },
  ];
  assert.match(validateWorkflowProfile(workflow) ?? '', /conflict/i);
});

test('workflow contracts reject unsupported modules and malformed JSON pointers', () => {
  const workflow = validWorkflow();
  workflow.steps[0].moduleId = 'launcher';
  workflow.steps[0].inputs = [{ targetJsonPointer: '/bad~2key', literal: true }];
  assert.match(validateWorkflowProfile(workflow) ?? '', /module/i);

  workflow.steps[0].moduleId = 'api';
  assert.match(validateWorkflowProfile(workflow) ?? '', /pointer/i);
});

test('workflow contracts reject duplicate step ids and invalid binding shapes', () => {
  const workflow = validWorkflow();
  workflow.steps[1].id = workflow.steps[0].id;
  assert.match(validateWorkflowProfile(workflow) ?? '', /duplicate step/i);

  workflow.steps[1].id = 'summarize';
  workflow.steps[0].inputs = [{ targetJsonPointer: '/query', literal: null, sourceStepId: 'fetch-data', sourceJsonPointer: '' }];
  assert.match(validateWorkflowProfile(workflow) ?? '', /exactly one/i);
});

test('workflow contracts respect the RPC size limit for saved profiles', () => {
  const workflow = validWorkflow();
  workflow.steps[0].inputs = [{ targetJsonPointer: '/large', literal: 'x'.repeat(50 * 1024), literalPresent: true }];
  assert.match(validateWorkflowProfile(workflow) ?? '', /48 KiB/i);
});
