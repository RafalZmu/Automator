import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, readFile, writeFile, readdir, rm, mkdir } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import os from 'node:os';
import path from 'node:path';
import { loadGraph, generateNotes } from '../../scripts/impact-graph.mjs';
import ownership from '../../scripts/impact-graph-source/ownership.json' with { type: 'json' };
import { classifyFile } from '../../scripts/impact-graph-source/schema.mjs';
import legacySymbols from '../../scripts/impact-graph-source/legacy-symbols.json' with { type: 'json' };
import model from '../../docs/architecture/impact-graph/model.json' with { type: 'json' };

const tracked = execFileSync('git',['ls-files','-z'],{encoding:'utf8'}).split('\0').filter(p=>/\.(cs|ts|tsx)$/.test(p)&&!p.startsWith('scripts/impact-graph-source/')&&!p.startsWith('tests/impact-graph/fixtures/')&&!/(^|\/)(node_modules|bin|obj|dist|artifacts|coverage)\//i.test(p)&&!/\.g\.cs$/i.test(p));

test('merged graph includes three layers, curated trace IDs, User and SQLite', () => {
  const graph = loadGraph();
  assert.ok(graph.nodes.some(n => n.layer === 'file'));
  assert.ok(graph.nodes.some(n => n.layer === 'function'));
  assert.ok(graph.nodes.some(n => n.id === 'view-run' && n.layer === 'function'));
  assert.ok(graph.nodes.some(n => n.id === 'user'));
  assert.ok(graph.nodes.some(n => n.id === 'sqlite-database'));
  assert.ok(graph.edges.some(e => e.kind === 'interaction'));
  const fn=graph.nodes.find(n=>n.id==='view-run');
  assert.match(fn.sources.join(' '),/BrowserAutomationView\.tsx#L224/);
  assert.ok(graph.edges.some(e=>e.from==='sqlite-database'&&e.to==='library'));
  for(const entry of legacySymbols)assert.equal(graph.nodes.filter(n=>n.id===entry.graphId).length,1,entry.graphId + " should map to exactly one function node");
  for(const entry of model.nodes.filter(n=>n.layer==='code'))assert.equal(graph.nodes.filter(n=>n.id===entry.id&&n.layer==='function').length,1,`${entry.id} should normalize exactly once`);
});

test('source index covers selected tracked paths or keeps explicit omission diagnostics', async () => {
  const index=JSON.parse(await readFile('docs/architecture/impact-graph/source-index.json','utf8'));
  const indexed=new Set(index.files.map(f=>f.path));
  const omissions=new Set(index.diagnostics.filter(d=>d.code==='scanner-omission').map(d=>d.path));
  for(const file of tracked)assert.ok(indexed.has(file)||omissions.has(file),`${file} silently omitted`);
  assert.equal(index.files.length,157);
  assert.deepEqual(index.diagnostics.filter(d=>d.code==='scanner-omission'),[]);
});

test('file containment targets every distinct function sharing a source anchor', async () => {
  const root=await mkdtemp(path.join(os.tmpdir(),'impact-miniature-'));
  try{
    await mkdir(path.join(root,'docs/architecture/impact-graph'),{recursive:true});
    await mkdir(path.join(root,'scripts/impact-graph-source'),{recursive:true});
    await writeFile(path.join(root,'docs/architecture/impact-graph/model.json'),JSON.stringify({nodes:[{id:'area',title:'Area',layer:'module',kind:'feature',summary:'Area',sources:[]}],edges:[]}));
    await writeFile(path.join(root,'scripts/impact-graph-source/legacy-symbols.json'),'[]');
    await writeFile(path.join(root,'docs/architecture/impact-graph/source-index.json'),JSON.stringify({
      schemaVersion:1,
      files:[{id:'file-same-anchor',path:'src/shared.ts',language:'typescript',areaIds:['area'],isTest:false}],
      functions:[
        {id:'function-one',fileId:'file-same-anchor',path:'src/shared.ts',symbolKey:'symbol-one',qualifiedName:'Shared.run',signature:'run(value: string): void',kind:'function',startLine:12,endLine:14},
        {id:'function-two',fileId:'file-same-anchor',path:'src/shared.ts',symbolKey:'symbol-two',qualifiedName:'Shared.run',signature:'run(value: number): void',kind:'function',startLine:12,endLine:14}
      ],edges:[],diagnostics:[]
    }));
    const graph=loadGraph({root});
    const contained=new Set(graph.edges.filter(e=>e.from==='file-same-anchor'&&e.kind==='contains').map(e=>e.to));
    assert.deepEqual(contained,new Set(['function-one','function-two']));
  }finally{await rm(root,{recursive:true,force:true});}
});

test('file and function notes carry layer, ownership, language, test and source anchors', async () => {
  const graph=loadGraph();
  const file=graph.nodes.find(n=>n.layer==='file'&&n.sources[0]==='ui/modules/ApiView.tsx');
  assert.ok(file);
  const fn=graph.nodes.find(n=>n.layer==='function'&&n.sources[0].startsWith('ui/modules/ApiView.tsx#'));
  assert.ok(fn);
  assert.ok(graph.edges.some(e=>e.from===file.id&&e.to===fn.id&&e.kind==='contains'));
  assert.ok(graph.edges.some(e=>e.to===file.id&&e.kind==='contains'&&e.from==='api'));
  const root=await mkdtemp(path.join(os.tmpdir(),'impact-notes-'));
  try{
    await generateNotes({graph,notesRoot:root});
    const moduleNote=await readFile(path.join(root,'browser.md'),'utf8'); assert.ok(moduleNote.includes('impact-graph/area/browser'));
    const fileNote=await readFile(path.join(root,'files',`${file.id}.md`),'utf8');
    const fnNote=await readFile(path.join(root,'functions',`${fn.id}.md`),'utf8');
    assert.match(fileNote,/impact-graph\/file/); assert.match(fileNote,/impact-graph\/area\/api/); assert.match(fileNote,/impact-graph\/language\/tsx/); assert.match(fileNote,/ui\/modules\/ApiView\.tsx/);
    assert.match(fnNote,/impact-graph\/function/); assert.match(fnNote,/impact-graph\/area\/api/); assert.match(fnNote,/ApiView\.tsx#L/);
    const testFile=graph.nodes.find(n=>n.layer==='file'&&n.isTest);
    const testNote=await readFile(path.join(root,'files',`${testFile.id}.md`),'utf8'); assert.match(testNote,/impact-graph\/test/);
    const repeated=await generateNotes({graph,notesRoot:root});
    assert.deepEqual(repeated.written,[]);
  }finally{await rm(root,{recursive:true,force:true});}
});

test('refuses unowned collisions and removes only stale owned notes', async () => {
  const graph=loadGraph(), root=await mkdtemp(path.join(os.tmpdir(),'impact-safety-'));
  try{
    await generateNotes({graph,notesRoot:root});
    const fileDir=path.join(root,'files'),files=await readdir(fileDir),stale=path.join(fileDir,files[0]);
    const preserved=path.join(fileDir,'unrelated.md'); await writeFile(preserved,'human content');
    await writeFile(stale,'<!-- generated_by: "scripts/impact-graph.mjs" -->');
    const result=await generateNotes({graph:{nodes:[],edges:[]},notesRoot:root});
    assert.ok(result.removed.includes(stale)); assert.ok(result.removed.every(file=>file===stale||file.endsWith('.md'))); assert.equal(await readFile(preserved,'utf8'),'human content');
    const collision=path.join(fileDir,'collision.md'); await writeFile(collision,'human owned');
    await assert.rejects(()=>generateNotes({graph:{nodes:[{id:'collision',layer:'file',kind:'source',title:'Collision',summary:'',sources:['a.ts']}],edges:[]},notesRoot:root}),/Refusing to overwrite/);
    assert.equal(await readFile(collision,'utf8'),'human owned');
  }finally{await rm(root,{recursive:true,force:true});}
});
