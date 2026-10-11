import test from 'node:test';
import assert from 'node:assert/strict';
import path from 'node:path';
import { scanTypeScript } from '../../scripts/impact-graph-source/typescript.mjs';
import * as ts from 'typescript/unstable/sync';

const root = path.resolve('tests/impact-graph/fixtures/typescript');
const paths = ['entry.ts', 'dependency.ts'];
const result = scanTypeScript({ root, paths, ts });

function named(name) {
  return result.functions.find(item => item.qualifiedName === name);
}

test('indexes named functions, class methods, and named or assigned arrows', () => {
  for (const name of ['entryPoint', 'Service.run', 'Service.constructor', 'entryPoint.namedArrow', 'entryPoint.assignedArrow']) {
    assert.ok(named(name), `expected function ${name}`);
  }
});

test('labels anonymous callbacks and includes their call expressions', () => {
  const callback = result.functions.find(item => item.kind === 'callback');
  assert.ok(callback, 'expected anonymous callback record');
  assert.match(callback.qualifiedName, /map/);
  assert.ok(result.calls.some(call => call.callerKey === callback.symbolKey), 'expected callback body call');
});

test('resolves path aliases and re-export dependencies to source files', () => {
  assert.ok(result.dependencies.some(edge => edge.from === 'dependency.ts' && edge.to === 'entry.ts'));
  assert.ok(result.dependencies.some(edge => edge.from === 'dependency.ts' && edge.to === 'dependency.ts'));
});

test('resolves internal calls by symbol identity and ignores call-like comments or strings', () => {
  const entry = named('entryPoint');
  const dep = named('helper');
  assert.ok(entry && dep);
  const assigned = named('entryPoint.assignedArrow');
  assert.ok(assigned);
  assert.ok(result.calls.some(call => call.callerKey === assigned.symbolKey && call.calleeKey === dep.symbolKey), 're-export alias should resolve to the source declaration symbol');
  assert.ok(result.calls.some(call => call.callerKey === entry.symbolKey && call.calleeKey === dep.symbolKey), 'direct call should resolve to the function declaration symbol');
  assert.ok(result.calls.some(call => call.callerKey === entry.symbolKey && call.calleeKey === named('Service.constructor').symbolKey), 'new expression should resolve to the indexed constructor symbol');
  assert.ok(!result.calls.some(call => call.path === 'entry.ts' && call.line === 4), 'comment and string references must not create calls');
});

test('resolves overloaded calls to their selected declaration signatures', () => {
  const overloads = result.functions.filter(item => item.path === 'dependency.ts' && item.qualifiedName === 'select');
  assert.equal(overloads.length, 3, 'both overload signatures and the implementation should be indexed');
  const stringCall = result.calls.find(call => call.path === 'entry.ts' && call.line === 14);
  const numberCall = result.calls.find(call => call.path === 'entry.ts' && call.line === 15);
  assert.ok(stringCall && numberCall, 'expected both overload calls');
  assert.notEqual(stringCall.calleeKey, numberCall.calleeKey, 'distinct overloads must retain distinct declaration targets');
  const stringOverload = overloads.find(item => item.startLine === 5);
  const numberOverload = overloads.find(item => item.startLine === 6);
  assert.equal(stringCall.calleeKey, stringOverload.symbolKey);
  assert.equal(numberCall.calleeKey, numberOverload.symbolKey);
});

test('reports unresolved and external calls without guessing graph edges', () => {
  assert.ok(result.diagnostics.some(item => item.path === 'entry.ts' && item.code === 'unresolved-call'));
  assert.ok(result.diagnostics.some(item => item.path === 'entry.ts' && item.code === 'external-call'));
  assert.ok(!result.calls.some(call => call.path === 'entry.ts' && call.calleeKey.includes('external')));
});
