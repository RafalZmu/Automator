import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, readFile, rm } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { buildImpactReport, validateGraph, writeImpactReportNote } from '../../scripts/impact-graph.mjs';

function graph() {
  const nodes = [
    { id: 'area', title: 'Area', layer: 'module', kind: 'feature', sources: [] },
    { id: 'file-a', title: 'a.ts', layer: 'file', kind: 'typescript', sources: ['src/a.ts'] },
    { id: 'file-b', title: 'b.ts', layer: 'file', kind: 'typescript', sources: ['src/b.ts'] },
    { id: 'file-c', title: 'c.ts', layer: 'file', kind: 'typescript', sources: ['src/c.ts'] },
    { id: 'fn-a', title: 'A.run', layer: 'function', kind: 'function', sources: ['src/a.ts#L1'], path: 'src/a.ts' },
    { id: 'fn-b', title: 'B.run', layer: 'function', kind: 'function', sources: ['src/b.ts#L2'], path: 'src/b.ts' },
    { id: 'fn-c', title: 'C.run', layer: 'function', kind: 'function', sources: ['src/c.ts#L3'], path: 'src/c.ts' }
  ];
  const edges = [
    { from: 'area', to: 'file-a', kind: 'contains', relation: 'owns file' },
    { from: 'file-a', to: 'fn-a', kind: 'contains', relation: 'declares function' },
    { from: 'file-b', to: 'fn-b', kind: 'contains', relation: 'declares function' },
    { from: 'file-c', to: 'fn-c', kind: 'contains', relation: 'declares function' },
    { from: 'file-a', to: 'file-b', kind: 'impact', relation: 'imports', evidence: 'src/a.ts:1' },
    { from: 'fn-a', to: 'fn-b', kind: 'impact', relation: 'calls', evidence: 'src/a.ts#L1' },
    { from: 'fn-b', to: 'fn-c', kind: 'impact', relation: 'calls', evidence: 'src/b.ts#L2' },
    { from: 'file-a', to: 'file-c', kind: 'impact', relation: 'type consumer', evidence: 'src/a.ts#L1' },
    { from: 'area', to: 'file-b', kind: 'impact', relation: 'module dependency', evidence: 'src/a.ts#L1' }
  ];
  return { nodes, edges, diagnostics: [] };
}

test('module query expands owned files and functions without reporting contains edges', () => {
  const result = buildImpactReport(graph(), 'area');
  assert.ok(result.direct.some((node) => node.id === 'file-b'));
  assert.ok(result.direct.some((node) => node.id === 'fn-b'));
  assert.ok(result.transitive.some((node) => node.id === 'fn-c'));
  assert.ok(!result.report.includes('owns file'));
});

test('file query reports import and type consumers and transitive callers using shortest paths', () => {
  const result = buildImpactReport(graph(), 'src/a.ts');
  assert.deepEqual(new Set(result.direct.map((node) => node.id)), new Set(['file-b', 'file-c', 'fn-b']));
  assert.ok(result.transitive.some((node) => node.id === 'fn-c'));
  assert.match(result.report, /imports/);
  assert.match(result.report, /src\/a\.ts#L1/);
  assert.match(result.report, /type consumer/);
});

test('function query accepts qualified name, and file path wins over a matching title', () => {
  assert.equal(buildImpactReport(graph(), 'A.run').changed.id, 'fn-a');
  const custom = graph();
  custom.nodes.find((node) => node.id === 'file-a').title = 'B.run';
  assert.equal(buildImpactReport(custom, 'src/a.ts').changed.id, 'file-a');
});

test('ambiguous qualified function names list their stable IDs', () => {
  const custom = graph();
  custom.nodes.push({ id: 'fn-a-overload', title: 'A.run', layer: 'function', kind: 'function', sources: ['src/a.ts#L9'] });
  assert.throws(() => buildImpactReport(custom, 'A.run'), /fn-a, fn-a-overload/);
});

test('--note writes a tagged owned report and refuses to overwrite a user report', async () => {
  const root = await mkdtemp(path.join(os.tmpdir(), 'impact-report-note-'));
  try {
    const data = graph();
    const report = buildImpactReport(data, 'src/a.ts');
    const output = await writeImpactReportNote(report.changed, report.report, data, root);
    const contents = await readFile(output, 'utf8');
    assert.match(contents, /generated_by: "scripts\/impact-graph\.mjs"/);
    assert.match(contents, /impact-graph\/report/);
    await writeFile(output, 'human report');
    assert.throws(() => writeImpactReportNote(report.changed, report.report, data, root), /Refusing to overwrite/);
    assert.equal(await readFile(output, 'utf8'), 'human report');
  } finally { await rm(root, { recursive: true, force: true }); }
});

test('validateGraph checks merged endpoints and generated node-note consistency without mutation', async () => {
  const root = await mkdtemp(path.join(os.tmpdir(), 'impact-validate-'));
  try {
    for (const file of ['src/a.ts', 'src/b.ts', 'src/c.ts']) {
      await mkdir(path.dirname(path.join(root, file)), { recursive: true });
      await writeFile(path.join(root, file), 'one\ntwo\nthree\n');
    }
    const notes = path.join(root, 'docs/architecture/impact-graph/nodes');
    const clean = graph();
    for (const node of clean.nodes) {
      const dir = node.layer === 'file' ? 'files' : node.layer === 'function' ? 'functions' : '';
      const target = path.join(notes, dir, `${node.id}.md`);
      await mkdir(path.dirname(target), { recursive: true });
      await writeFile(target, 'generated_by: "scripts/impact-graph.mjs"\n');
    }
    assert.deepEqual(await validateGraph(clean, { root, notesRoot: notes }), []);
    const stale = path.join(notes, 'files/stale.md');
    await writeFile(stale, 'generated_by: "scripts/impact-graph.mjs"\n');
    assert.ok((await validateGraph(clean, { root, notesRoot: notes })).some((error) => error.includes('Stale generated note')));
    await rm(stale);
    const invalid = { ...clean, edges: [...clean.edges, { from: 'absent', to: 'fn-c', kind: 'impact' }] };
    const errors = await validateGraph(invalid, { root, notesRoot: notes });
    assert.ok(errors.some((error) => error.includes('Unknown edge source: absent')));
    await writeFile(path.join(notes, 'functions/fn-c.md'), 'user note');
    assert.ok((await validateGraph(clean, { root, notesRoot: notes })).some((error) => error.includes('not generator-owned')));
  } finally { await rm(root, { recursive: true, force: true }); }
});
