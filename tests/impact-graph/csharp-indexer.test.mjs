import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { mkdtempSync, rmSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import test from 'node:test';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const project = path.join(root, 'scripts/impact-graph-source/csharp/ImpactGraph.CSharpIndexer.csproj');
const fixture = path.join(root, 'tests/impact-graph/fixtures/csharp/GraphFixture.csproj');

test('indexes C# symbol-resolved calls, closures, and type dependencies', () => {
  const outputDir = mkdtempSync(path.join(os.tmpdir(), 'impact-csharp-'));
  const output = path.join(outputDir, 'index.json');
  try {
    execFileSync('dotnet', ['run', '--project', project, '--', '--root', root, '--output', output, '--project', fixture], { cwd: root, stdio: 'pipe' });
    const index = JSON.parse(execFileSync('node', ['-e', 'process.stdout.write(require("node:fs").readFileSync(process.argv[1], "utf8"))', output], { encoding: 'utf8' }));
    assert.deepEqual(Object.keys(index).sort(), ['calls', 'dependencies', 'diagnostics', 'files', 'functions']);
    assert.ok(index.files.every(file => !file.includes('/obj/') && !file.includes('/bin/')));
    const overloads = index.functions.filter(x => x.qualifiedName.startsWith('Fixture.Dependency.Run('));
    assert.equal(overloads.length, 2);
    assert.equal(new Set(overloads.map(x => x.symbolKey)).size, 2, 'overloads have different Roslyn identities');
    const local = index.functions.find(x => x.kind === 'local-function' && x.qualifiedName.endsWith('.Local()'));
    const lambda = index.functions.find(x => x.kind === 'lambda');
    const ctor = index.functions.find(x => x.kind === 'constructor' && x.qualifiedName === 'Fixture.Dependency.Dependency()');
    const use = index.functions.find(x => x.qualifiedName === 'Fixture.Caller.Use()');
    assert.ok(local && lambda && use && ctor, 'constructors, local functions and lambdas are indexed');
    const callTargets = new Set(index.calls.map(x => `${x.callerKey}->${x.calleeKey}`));
    assert.ok(index.calls.some(x => x.callerKey === use.symbolKey && overloads.some(o => o.symbolKey === x.calleeKey)), 'overload calls resolve');
    assert.ok(index.calls.some(x => x.callerKey === local.symbolKey && overloads.some(o => o.symbolKey === x.calleeKey)), 'local function calls resolve');
    assert.ok(index.calls.some(x => x.callerKey === lambda.symbolKey && overloads.some(o => o.symbolKey === x.calleeKey)), 'lambda calls resolve');
    assert.ok(index.calls.some(x => x.calleeKey === local.symbolKey), 'local invocation resolves');
    assert.ok(index.calls.some(x => x.calleeKey === ctor.symbolKey), 'constructor call resolves by Roslyn symbol identity');
    assert.ok(callTargets.size > 0);
    assert.ok(index.dependencies.some(x => x.fromPath.endsWith('Dependency.cs') && x.toPath.endsWith('Caller.cs')));
    assert.ok(index.diagnostics.some(x => /dynamic|unresolved/i.test(JSON.stringify(x))));
    assert.ok(index.calls.every(x => x.calleeKey && !/Missing/.test(x.calleeKey)), 'unresolved dynamic target is not fabricated');
    assert.ok(index.calls.every(x => x.line > 0 && x.path));
  } finally {
    rmSync(outputDir, { recursive: true, force: true });
  }
});
