import { execFileSync } from 'node:child_process';
import { readFile, writeFile, mkdir } from 'node:fs/promises';
import os from 'node:os';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { stableFileId, stableFunctionId, classifyFile, unmappedDiagnostic, normalizeSourcePath } from './schema.mjs';
import ownership from './ownership.json' with { type: 'json' };
import * as ts from 'typescript/unstable/sync';
import { scanTypeScript } from './typescript.mjs';

// The indexer project ships with Automator, so it is addressed absolutely: the scanned repository may be elsewhere.
const indexerProject = fileURLToPath(new URL('./csharp/ImpactGraph.CSharpIndexer.csproj', import.meta.url));
const excluded = p => p.startsWith('scripts/impact-graph-source/') || p.startsWith('tests/impact-graph/fixtures/') || /(^|\/)(node_modules|bin|obj|dist|artifacts|coverage)\//i.test(p) || /\.g\.cs$/i.test(p);
export async function buildSourceIndex({ root, outputPath, tsconfigs = ['tsconfig.json'] }) {
  root = path.resolve(root);
  const tracked = execFileSync('git', ['ls-files', '-z'], { cwd: root, encoding: 'utf8' }).split('\0')
    .filter(p => /\.(cs|ts|tsx)$/.test(p) && !excluded(p)).map(normalizeSourcePath).sort();
  const tsPaths = tracked.filter(p => /\.(ts|tsx)$/.test(p));
  const csPaths = tracked.filter(p => p.endsWith('.cs'));
  // Each tsconfig owns the TypeScript files in its directory (Dysponent2 has two Angular/Playwright projects).
  const tsScans = tsconfigs.map(tsconfig => {
    const dir = path.posix.dirname(normalizeSourcePath(tsconfig));
    const paths = tsPaths.filter(p => dir === '.' || p.startsWith(`${dir}/`));
    return scanTypeScript({ root, paths, ts, tsconfig });
  });
  const tsResult = {
    files: tsScans.flatMap(r => r.files), functions: tsScans.flatMap(r => r.functions), calls: tsScans.flatMap(r => r.calls),
    dependencies: tsScans.flatMap(r => r.dependencies), diagnostics: tsScans.flatMap(r => r.diagnostics),
  };
  const missingTs = tsPaths.filter(p => !tsResult.files.some(f => normalizeSourcePath(f.path) === p));
  const csResult = { files: [], functions: [], calls: [], dependencies: [], diagnostics: [] };
  if (csPaths.length) {
    const projects = execFileSync('git', ['ls-files', '-z', '*.csproj'], { cwd:root, encoding:'utf8' }).split('\0').filter(p=>p && (p.startsWith('src/') || /^tests\/Automator\..*\.Specs\.csproj$/.test(p)));
    execFileSync('dotnet', ['build', indexerProject, '--nologo'], { cwd:root, encoding:'utf8', maxBuffer:64*1024*1024, stdio:['ignore','ignore','pipe'] });
    for (const [index, project] of projects.entries()) {
      const temp = path.join(os.tmpdir(), `impact-graph-csharp-${process.pid}-${index}.json`);
      try {
        execFileSync('dotnet', ['run', '--no-build', '--project', indexerProject, '--', '--root', root, '--output', temp, '--project', project], { cwd: root, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024, stdio:['ignore','ignore','pipe'] });
        const result = JSON.parse(await readFile(temp, 'utf8'));
        csResult.files.push(...result.files); csResult.functions.push(...result.functions); csResult.calls.push(...result.calls); csResult.dependencies.push(...result.dependencies); csResult.diagnostics.push(...result.diagnostics);
      } catch (error) {
        csResult.diagnostics.push({path:project,line:1,kind:'scanner-project-failed',message:error.stderr?.toString().split(/\r?\n/)[0]??String(error)});
      }
    }
    for (const key of ['files','functions','calls','dependencies','diagnostics']) csResult[key] = [...new Map(csResult[key].map(item=>[JSON.stringify(item),item])).values()];
    csResult.files=csResult.files.filter(p=>csPaths.includes(normalizeSourcePath(p)));
    csResult.functions=csResult.functions.filter(f=>csPaths.includes(normalizeSourcePath(f.path)));
    const validKeys=new Set(csResult.functions.map(f=>f.symbolKey));
    csResult.calls=csResult.calls.filter(c=>validKeys.has(c.callerKey)&&validKeys.has(c.calleeKey));
    csResult.dependencies=csResult.dependencies.filter(d=>csPaths.includes(normalizeSourcePath(d.fromPath))&&csPaths.includes(normalizeSourcePath(d.toPath)));
    csResult.diagnostics=csResult.diagnostics.filter(d=>csPaths.includes(normalizeSourcePath(d.path)));
  }
  const rawFiles = [...tsResult.files, ...csResult.files].map(f => normalizeSourcePath(typeof f === 'string' ? f : f.path));
  const missing = [...new Set([...missingTs, ...csPaths.filter(p => !rawFiles.includes(p))])].sort();
  const files = rawFiles.filter(p => tracked.includes(p)).map(p => {
    const { areaIds, isTest } = classifyFile(p, ownership);
    return { id: stableFileId(p), path: p, language: p.endsWith('.cs') ? 'csharp' : p.endsWith('.tsx') ? 'tsx' : 'typescript', areaIds, isTest };
  }).sort((a,b)=>a.path.localeCompare(b.path));
  const diagnostics = [...tsResult.diagnostics, ...csResult.diagnostics].map(d => ({ path: normalizeSourcePath(d.path), line: d.line ?? 1, code: d.code ?? d.kind ?? 'unresolved', message: d.message ?? d.kind ?? 'Unresolved source relationship.' }));
  for (const p of missing) diagnostics.push({ path:p, line:1, code:'scanner-omission', message:'Selected tracked source was not returned by its language scanner.' });
  for (const f of files) if (!f.areaIds.length) diagnostics.push(unmappedDiagnostic(f.path));
  const fnRows = [...tsResult.functions, ...csResult.functions];
  const functions = fnRows.map(f => ({ id: stableFunctionId(f.path, f.symbolKey), fileId: stableFileId(f.path), path:normalizeSourcePath(f.path), symbolKey:f.symbolKey, qualifiedName:f.qualifiedName, signature:f.signature, kind:f.kind, startLine:f.startLine, endLine:f.endLine })).sort((a,b)=>a.path.localeCompare(b.path)||a.startLine-b.startLine||a.id.localeCompare(b.id));
  const byKey = new Map(functions.map(f=>[f.symbolKey,f.id]));
  const edges = [];
  for (const d of tsResult.dependencies) edges.push({from:stableFileId(d.from),to:stableFileId(d.to),kind:'impact',relation:'imports or re-exports',evidence:d.evidence,confidence:'import'});
  for (const d of csResult.dependencies) edges.push({from:stableFileId(d.fromPath),to:stableFileId(d.toPath),kind:'impact',relation:d.relation,evidence:`${d.path}:${d.line}`,confidence:'symbol'});
  for (const c of [...tsResult.calls,...csResult.calls]) if(byKey.has(c.calleeKey)&&byKey.has(c.callerKey)) edges.push({from:byKey.get(c.calleeKey),to:byKey.get(c.callerKey),kind:'impact',relation:'called by',evidence:`${c.path}:${c.line}`,confidence:'symbol'});
  const result={schemaVersion:1,files,functions,edges:edges.sort((a,b)=>a.from.localeCompare(b.from)||a.to.localeCompare(b.to)||a.evidence.localeCompare(b.evidence)),diagnostics:diagnostics.sort((a,b)=>a.path.localeCompare(b.path)||a.line-b.line||a.code.localeCompare(b.code))};
  if(outputPath) { await mkdir(path.dirname(path.resolve(root,outputPath)),{recursive:true}); await writeFile(path.resolve(root,outputPath),JSON.stringify(result,null,2)+'\n'); }
  return result;
}
