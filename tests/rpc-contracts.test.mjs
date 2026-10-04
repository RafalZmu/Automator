import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';
import { BUNDLED_TAB_VIEW_KINDS } from '../ui/viewKinds.ts';
import {
  automationHttpResultSchema,
  automationKeyboardEligibilityResultSchema,
  backendNotificationSchema,
  MAX_HTTP_REQUEST_BODY_BYTES,
  MAX_HTTP_RESPONSE_BODY_BYTES,
  MAX_MODULE_SETTINGS_BYTES,
  MAX_RPC_REQUEST_LINE_BYTES,
  MAX_RPC_RESPONSE_LINE_BYTES,
  automationModuleActionRequestSchema,
  automationModuleActionCancelRequestSchema,
  automationResultSchema,
  moduleSettingsUpdateRequestSchema,
  moduleSettingsUpdateResultSchema,
  moduleSettingsGetRequestSchema,
  moduleSettingsGetResultSchema,
  validateRpcRequest,
} from '../contracts/rpc.ts';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const fixture = JSON.parse(await readFile(path.join(root, 'contracts/rpc-contract-fixtures.json'), 'utf8'));
const viewKindFixture = JSON.parse(await readFile(path.join(root, 'contracts/bundled-tab-view-kinds.json'), 'utf8'));

test('renderer component registry declares the bundled tab view kinds from the shared contract', () => {
  assert.deepEqual([...BUNDLED_TAB_VIEW_KINDS], viewKindFixture.kinds);
});

test('typed automation notifications accept bounded title and body only', () => {
  const valid = { jsonrpc: '2.0', method: 'automation/notification', params: { title: 'Focus complete', body: 'Your break has started.' } };
  assert.equal(backendNotificationSchema.safeParse(valid).success, true);
  assert.equal(backendNotificationSchema.safeParse({ ...valid, params: { ...valid.params, body: 'x'.repeat(257) } }).success, false);
  assert.equal(backendNotificationSchema.safeParse({ ...valid, params: { ...valid.params, secret: 'no' } }).success, false);
});

test('module action requests and structured results enforce version and payload limits', () => {
  assert.equal(automationModuleActionRequestSchema.safeParse({
    requestId: 'test-run', contractVersion: 1, moduleId: 'fixture-module', actionId: 'summarize', actionVersion: 1, input: { count: 3 },
  }).success, true);
  const workspaceRequest = validateRpcRequest({
    jsonrpc: '2.0', id: 7, method: 'automation/moduleAction', params: {
      requestId: 'test-run', contractVersion: 1, moduleId: 'fixture-module', actionId: 'summarize', actionVersion: 1,
      input: {}, hostWindowContext: { role: 'workspace', selectedTab: 5 },
    },
  });
  assert.equal(workspaceRequest.params.hostWindowContext.selectedTab, 5);
  assert.equal(automationModuleActionRequestSchema.safeParse({
    requestId: 'test-run', contractVersion: 1, moduleId: 'fixture-module', actionId: 'summarize', actionVersion: 1,
    input: {}, hostWindowContext: { role: 'workspace', selectedTab: 10 },
  }).success, false);
  assert.equal(automationModuleActionRequestSchema.safeParse({
    requestId: 'test-run', contractVersion: 1, moduleId: 'fixture-module', actionId: 'summarize', actionVersion: 0, input: {},
  }).success, false);
  assert.equal(automationModuleActionRequestSchema.safeParse({
    requestId: 'test-run', contractVersion: 1, moduleId: 'fixture-module', actionId: 'summarize', actionVersion: 1, input: 'x'.repeat(64 * 1024),
  }).success, false);
  assert.equal(automationModuleActionRequestSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', actionId: 'summarize', actionVersion: 1, input: {},
  }).success, false);
  assert.equal(automationModuleActionCancelRequestSchema.safeParse({ moduleId: 'script-runner', requestId: 'test-run' }).success, true);
  assert.equal(automationModuleActionCancelRequestSchema.safeParse({ moduleId: 'Script Runner', requestId: '' }).success, false);
  assert.equal(automationResultSchema.safeParse({
    contractVersion: 1,
    status: 'success',
    message: 'Done',
    data: { count: 3 },
    actions: [{ id: 'open-report', label: 'Open report', version: 1, payload: { reportId: 'r1' } }],
  }).success, true);
  assert.equal(automationResultSchema.safeParse({
    contractVersion: 1, status: 'success', message: 'Done', data: {}, actions: [
      { id: 'open', label: 'Open', version: 1, payload: {} },
      { id: 'open', label: 'Open again', version: 1, payload: {} },
    ],
  }).success, false);
});

test('module settings writes enforce active module identifiers, schema versions and payload limits', () => {
  assert.equal(moduleSettingsUpdateRequestSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', settingsVersion: 2, value: { layout: 'compact' },
  }).success, true);
  assert.equal(moduleSettingsUpdateRequestSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', settingsVersion: 0, value: {},
  }).success, false);
  assert.equal(moduleSettingsUpdateRequestSchema.safeParse({
    contractVersion: 1, moduleId: 'Fixture_Module', settingsVersion: 2, value: {},
  }).success, false);
  assert.equal(moduleSettingsUpdateRequestSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', settingsVersion: 2, value: 'x'.repeat(MAX_MODULE_SETTINGS_BYTES),
  }).success, false);
  assert.equal(moduleSettingsUpdateResultSchema.safeParse({
    saved: true, moduleId: 'fixture-module', settingsVersion: 2,
  }).success, true);
  assert.equal(moduleSettingsGetRequestSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', settingsVersion: 2,
  }).success, true);
  assert.equal(moduleSettingsGetRequestSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', settingsVersion: 2,
    hostWindowContext: { role: 'workspace', selectedTab: 5 },
  }).success, true);
  assert.equal(moduleSettingsGetRequestSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', settingsVersion: 0,
  }).success, false);
  assert.equal(moduleSettingsGetResultSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', settingsVersion: 2, value: { layout: 'compact' },
  }).success, true);
  assert.equal(moduleSettingsGetResultSchema.safeParse({
    contractVersion: 1, moduleId: 'fixture-module', settingsVersion: 2, value: 'x'.repeat(MAX_MODULE_SETTINGS_BYTES),
  }).success, false);
});

test('shared C# and TypeScript request fixtures agree on valid and invalid parameters', () => {
  for (const { name, request } of fixture.validRequests) {
    assert.deepEqual(validateRpcRequest(request), request, `valid fixture ${name}`);
  }

  for (const { name, request, expectedCode } of fixture.invalidRequests) {
    assert.throws(() => validateRpcRequest(request), (error) => {
      assert.equal(error.code, expectedCode, `invalid fixture ${name}`);
      return true;
    });
  }
});

test('authorization fixtures are well-formed but fail active-module and capability checks', () => {
  for (const { name, request, activeModuleId, capability, declaredCapabilities, authorized } of fixture.authorizationCases) {
    assert.deepEqual(validateRpcRequest(request), request, `authorization fixture ${name}`);
    const params = request.params;
    const moduleIsActive = params.moduleId === activeModuleId;
    const capabilityIsDeclared = declaredCapabilities.includes(capability);
    assert.equal(moduleIsActive && capabilityIsDeclared, authorized, `authorization fixture ${name}`);
  }
});

test('control-character HTTP payloads fit escaped JSON-RPC request and response line caps', () => {
  const { controlCharacter, requestBodyBytes, responseBodyBytes } = fixture.wireLimits;
  const requestBody = controlCharacter.repeat(requestBodyBytes);
  assert.equal(Buffer.byteLength(requestBody, 'utf8'), MAX_HTTP_REQUEST_BODY_BYTES);
  const request = validateRpcRequest({
    jsonrpc: '2.0',
    id: 'wire-request',
    method: 'automation/httpRequest',
    params: {
      moduleId: 'launcher',
      requestId: 'wire-request',
      request: { uri: 'https://api.example.test/', method: 'POST', headers: {}, body: requestBody },
    },
  });
  assert.ok(Buffer.byteLength(JSON.stringify(request), 'utf8') <= MAX_RPC_REQUEST_LINE_BYTES);
  assert.throws(() => validateRpcRequest({
    ...request,
    params: { ...request.params, request: { ...request.params.request, body: requestBody + controlCharacter } },
  }));

  const result = {
    statusCode: 200,
    headers: {},
    contentType: 'text/plain',
    bodyText: controlCharacter.repeat(responseBodyBytes),
  };
  assert.equal(Buffer.byteLength(result.bodyText, 'utf8'), MAX_HTTP_RESPONSE_BODY_BYTES);
  assert.equal(automationHttpResultSchema.safeParse(result).success, true);
  assert.ok(Buffer.byteLength(JSON.stringify({ jsonrpc: '2.0', id: 1, result }), 'utf8') <= MAX_RPC_RESPONSE_LINE_BYTES);
  assert.equal(automationHttpResultSchema.safeParse({ ...result, bodyText: `${result.bodyText}${controlCharacter}` }).success, false);
});

test('keyboard input notifications validate their versioned event payload', () => {
  assert.equal(backendNotificationSchema.safeParse({
    jsonrpc: '2.0',
    method: 'automation/keyboardInput',
    params: {
      moduleId: 'launcher',
      event: { sequence: 7, code: 'KeyA', virtualKey: 65, isDown: true, isRepeat: false, modifiers: 3 },
    },
  }).success, true);
  assert.equal(backendNotificationSchema.safeParse({
    jsonrpc: '2.0',
    method: 'automation/keyboardInput',
    params: { moduleId: 'launcher', event: { sequence: -1 } },
  }).success, false);
});

test('keyboard availability notification supports revoke and regain', () => {
  const { keyboardAvailabilityNotification } = fixture;
  assert.equal(backendNotificationSchema.safeParse(keyboardAvailabilityNotification).success, true);
  assert.equal(automationKeyboardEligibilityResultSchema.safeParse({ eligible: true, revision: 1 }).success, true);
  assert.equal(automationKeyboardEligibilityResultSchema.safeParse({ eligible: true, revision: -1 }).success, false);
  assert.equal(backendNotificationSchema.safeParse({
    ...keyboardAvailabilityNotification,
    params: { moduleId: 'launcher', eligible: 'true' },
  }).success, false);
});
