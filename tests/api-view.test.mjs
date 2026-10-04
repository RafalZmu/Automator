import assert from 'node:assert/strict';
import { test } from 'node:test';
import {
  buildApiRequestBody,
  decodeApiResponse,
  parseApiCurlImport,
  readApiProfiles,
  validateApiProfile,
} from '../ui/contracts/api.ts';

const validProfile = {
  id: 'weather',
  name: 'Weather',
  method: 'GET',
  url: 'https://api.example.test/v1/current',
  allowLocalNetwork: false,
  headers: { Accept: 'application/json' },
  secretHeaders: { 'X-Api-Key': 'opaque-ref-1' },
  bodyTemplate: null,
  responseMode: 'json',
  timeoutSeconds: 30,
};

test('API profile reader keeps only the persisted safe profile fields', () => {
  const profiles = readApiProfiles({ profiles: [{ ...validProfile, secretValue: 'must-not-escape' }, { id: 'bad' }] });
  assert.equal(profiles.length, 1);
  assert.deepEqual(profiles[0], validProfile);
  assert.equal(JSON.stringify(profiles).includes('must-not-escape'), false);
});

test('API profiles derive host policy from the URL and retain profile-local default input', () => {
  const profile = { ...validProfile, defaultInput: { query: 'forecast', limit: 3 } };
  assert.deepEqual(validateApiProfile(profile), []);
  assert.deepEqual(readApiProfiles({ profiles: [{ ...profile, allowedHosts: ['legacy.example.test'] }] }), [profile]);
  assert.deepEqual(readApiProfiles({ profiles: [{ ...validProfile, defaultInput: { old: true } }] })[0].defaultInput, { old: true });
});

test('Chrome Copy as cURL parses a Bash request into an unsent profile draft and separates credential headers', () => {
  const imported = parseApiCurlImport([
    "curl 'https://api.example.test/v2/items?limit=5'",
    '-X PUT',
    "-H 'accept: application/json'",
    "-H 'Authorization: Bearer do-not-store'",
    "-H 'Cookie: session=do-not-store-either'",
    "--data-raw '{\"name\":\"blue chair\",\"enabled\":true}'",
  ].join(' '));

  assert.equal(imported.method, 'PUT');
  assert.equal(imported.url, 'https://api.example.test/v2/items?limit=5');
  assert.deepEqual(imported.headers, { accept: 'application/json' });
  assert.deepEqual(imported.secretValues, {
    Authorization: 'Bearer do-not-store',
    Cookie: 'session=do-not-store-either',
  });
  assert.deepEqual(imported.secretHeaders, ['Authorization', 'Cookie']);
  assert.deepEqual(imported.defaultInput, { name: 'blue chair', enabled: true });
  assert.equal(imported.bodyTemplate, null);
  assert.equal(JSON.stringify(imported.headers).includes('do-not-store'), false);
  assert.throws(() => parseApiCurlImport("curl 'https://api.example.test/' --data-raw '{\"access_token\":\"must-not-export\"}'"), /remove them before importing/i);
});

test('Chrome Copy as cURL parser accepts Windows cmd quoting and rejects shell substitutions', () => {
  const imported = parseApiCurlImport('curl ^"https://api.example.test/v1^" ^\r\n  -H ^"content-type: application/json^" ^\r\n  --data-raw ^"{^\\^"ok^\\^":true}^"');
  assert.equal(imported.method, 'POST');
  assert.deepEqual(imported.defaultInput, { ok: true });
  assert.throws(() => parseApiCurlImport("curl 'https://api.example.test/' -H \"X-Value: $(whoami)\""), /unsupported shell syntax/i);
  assert.throws(() => parseApiCurlImport('curl ^"https://api.example.test/v1^" -H ^"Authorization: %API_TOKEN%^"'), /unsupported shell syntax/i);
});

test('API request body substitutes input as raw JSON instead of quoted text', () => {
  assert.equal(
    buildApiRequestBody('{"payload":{{input}}}', { message: 'hello', count: 2 }),
    '{"payload":{"message":"hello","count":2}}',
  );
  assert.equal(buildApiRequestBody('static body', undefined), 'static body');
  assert.equal(buildApiRequestBody(null, { query: 'status' }), '{"query":"status"}');
  assert.equal(buildApiRequestBody(null, undefined), null);
});

test('API profile validation accepts URL-only host scope and keeps authentication out of ordinary headers', () => {
  assert.deepEqual(validateApiProfile(validProfile), []);
  assert.deepEqual(validateApiProfile({ ...validProfile, allowedHosts: ['*.example.test'] }), []);
  assert.ok(validateApiProfile({ ...validProfile, headers: { Authorization: 'Bearer token' } }).some((issue) => /secret header/i.test(issue)));
});

test('API response decoder returns structured JSON and preserves malformed JSON as text', () => {
  assert.deepEqual(decodeApiResponse('json', '{"ok":true}'), {
    bodyText: '', structuredOutput: { ok: true }, hasStructuredOutput: true, parseFailure: false,
  });
  assert.deepEqual(decodeApiResponse('json', '{broken'), {
    bodyText: '{broken', structuredOutput: null, hasStructuredOutput: false, parseFailure: true,
  });
  assert.deepEqual(decodeApiResponse('text', 'hello'), {
    bodyText: 'hello', structuredOutput: null, hasStructuredOutput: false, parseFailure: false,
  });
  assert.deepEqual(decodeApiResponse('json', 'null'), {
    bodyText: '', structuredOutput: null, hasStructuredOutput: true, parseFailure: false,
  });
});
