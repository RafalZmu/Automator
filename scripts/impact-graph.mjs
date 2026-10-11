import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { readdir, unlink } from 'node:fs/promises';
import { stableFileId, stableFunctionId } from './impact-graph-source/schema.mjs';
import { buildSourceIndex } from './impact-graph-source/index.mjs';

const scriptPath = fileURLToPath(import.meta.url);
const repoRoot = path.resolve(path.dirname(scriptPath), '..');
const modelPath = path.join(repoRoot, 'docs', 'architecture', 'impact-graph', 'model.json');
const notesRoot = path.join(repoRoot, 'docs', 'architecture', 'impact-graph', 'nodes');
const vaultRelativeNotesRoot = 'architecture/impact-graph/nodes';
const model = JSON.parse(readFileSync(modelPath, 'utf8'));
const nodeById = new Map(model.nodes.map((node) => [node.id, node]));
const lineCountCache = new Map();

function fail(message) {
  console.error(message);
  process.exitCode = 1;
}

export function loadGraph({ root = repoRoot, sourceIndexPath = path.join(root, 'docs/architecture/impact-graph/source-index.json') } = {}) {
  const curated = JSON.parse(readFileSync(path.join(root, 'docs/architecture/impact-graph/model.json'), 'utf8'));
  const source = existsSync(sourceIndexPath) ? JSON.parse(readFileSync(sourceIndexPath, 'utf8')) : { files: [], functions: [], edges: [], diagnostics: [] };
  const legacy = existsSync(path.join(root, 'scripts/impact-graph-source/legacy-symbols.json')) ? JSON.parse(readFileSync(path.join(root, 'scripts/impact-graph-source/legacy-symbols.json'), 'utf8')) : [];
  const legacyBySymbol = new Map(legacy.map(x => [`${x.path}|${x.qualifiedName}|${x.signature}`, x.graphId]));
  const mappedLegacyIds = new Set(legacy.map(x=>x.graphId));
  const nodes = [...curated.nodes.filter(n=>n.layer!=='code'||!mappedLegacyIds.has(n.id)).map(n => n.layer === 'code' ? { ...n, layer: 'function' } : n)];
  const edges = [...curated.edges];
  const fileNodes = source.files.map(f => ({ id:f.id, title:path.posix.basename(f.path), layer:'file', kind:f.language, summary:`Source file ${f.path}.`, sources:[f.path], language:f.language, areaIds:f.areaIds, isTest:f.isTest }));
  const canonicalIdByIndexId = new Map(source.functions.map(f=>[f.id,legacyBySymbol.get(`${f.path}|${f.qualifiedName}|${f.signature}`)??f.id]));
  const functions = source.functions.map(f => {
    const owner = source.files.find(file => file.path === f.path);
    return { id:legacyBySymbol.get(`${f.path}|${f.qualifiedName}|${f.signature}`) ?? f.id, title:f.qualifiedName, layer:'function', kind:f.kind, summary:f.signature, sources:[`${f.path}#L${f.startLine}`], signature:f.signature, path:f.path, startLine:f.startLine, endLine:f.endLine, areaIds:owner?.areaIds??[], isTest:owner?.isTest??false, language:owner?.language };
  });
  nodes.push(...fileNodes, ...functions);
  const fileByPath = new Map(fileNodes.map(n => [n.sources[0],n]));
  for (const file of source.files) {
    const node=fileByPath.get(file.path);
    for (const area of file.areaIds) edges.push({from:area,to:node.id,kind:'contains',relation:'owns source file'});
  }
  for (const fn of source.functions) {
    const file=fileByPath.get(fn.path);
    const functionId=canonicalIdByIndexId.get(fn.id)??fn.id;
    if(file)edges.push({from:file.id,to:functionId,kind:'contains',relation:'declares function',evidence:`${fn.path}#L${fn.startLine}`});
  }
  for(const edge of source.edges) edges.push({...edge,from:canonicalIdByIndexId.get(edge.from)??edge.from,to:canonicalIdByIndexId.get(edge.to)??edge.to});
  return { nodes, edges, diagnostics:source.diagnostics };
}

const generatorMarker = 'generated_by: "scripts/impact-graph.mjs"';
export async function generateNotes({ graph = loadGraph(), notesRoot = path.join(repoRoot,'docs/architecture/impact-graph/nodes') } = {}) {
  const created=[], removed=[], wanted=new Set();
  const nodeMap=new Map(graph.nodes.map(n=>[n.id,n]));
  const layerFolder={file:'files',function:'functions'};
  for(const node of graph.nodes){
    const folder=layerFolder[node.layer];
    const out=path.join(notesRoot,folder??'',`${node.id}.md`); wanted.add(path.resolve(out));
    const tags=['impact-graph',`impact-graph/${node.layer}`,`impact-graph/kind/${node.kind}`];
    const areas=node.layer==='module'?[areaTagForModule(node)]:(node.areaIds??[]);
    for(const area of areas)tags.push(`impact-graph/area/${area}`);
    if(node.language)tags.push(`impact-graph/language/${node.language}`);
    if(node.isTest)tags.push('impact-graph/test');
    const outgoing=graph.edges.filter(e=>e.from===node.id).map(e=>`- ${e.kind}${e.relation?` (${e.relation})`:''}: [[${e.to}|${nodeMap.get(e.to)?.title??e.to}]]${e.evidence?` (${e.evidence})`:''}`).join('\n')||'- None recorded.';
    const body=['---',`graph_id: ${JSON.stringify(node.id)}`,`graph_layer: ${JSON.stringify(node.layer)}`,`graph_kind: ${JSON.stringify(node.kind)}`,`tags:\n${tags.map(t=>`  - ${JSON.stringify(t)}`).join('\n')}`,`generated_by: ${JSON.stringify('scripts/impact-graph.mjs')}`,'---','',`# ${node.title}`,'',node.summary??'', '', '## Connections','',outgoing,'','## Source anchors','',(node.sources??[]).map(s=>`- \`${s}\``).join('\n')||'- No source path recorded.',''].join('\n');
    if(existsSync(out)&&!readFileSync(out,'utf8').includes(generatorMarker))throw new Error(`Refusing to overwrite a note not owned by this generator: ${out}`);
    mkdirSync(path.dirname(out),{recursive:true});
    if(!existsSync(out)||readFileSync(out,'utf8')!==body){writeFileSync(out,body,'utf8');created.push(out);}
  }
  for(const folder of ['files','functions']){
    const dir=path.join(notesRoot,folder); if(!existsSync(dir))continue;
    for(const name of await readdir(dir)){const out=path.resolve(dir,name);if(!wanted.has(out)&&name.endsWith('.md')&&readFileSync(out,'utf8').includes(generatorMarker)){await unlink(out);removed.push(out);}}
  }
  const legacyPath=path.join(repoRoot,'scripts/impact-graph-source/legacy-symbols.json');
  if(existsSync(legacyPath))for(const entry of JSON.parse(readFileSync(legacyPath,'utf8'))){
    const oldNote=path.join(notesRoot,`${entry.graphId}.md`);
    if(existsSync(oldNote)){
      if(!readFileSync(oldNote,'utf8').includes(generatorMarker))throw new Error(`Refusing to remove a legacy note not owned by this generator: ${oldNote}`);
      await unlink(oldNote); removed.push(oldNote);
    }
  }
  return {written:created,removed,warnings:graph.diagnostics??[]};
}

function validateReference(reference, label, errors) {
  const [sourcePath, fragment] = reference.split('#', 2);
  const absolutePath = path.join(repoRoot, sourcePath);
  if (!existsSync(absolutePath)) {
    errors.push(`Missing ${label}: ${sourcePath}`);
    return;
  }
  const lineMatch = fragment?.match(/^L(\d+)(?:-L?(\d+))?$/);
  if (!lineMatch) return;
  let lineCount = lineCountCache.get(absolutePath);
  if (lineCount === undefined) {
    lineCount = readFileSync(absolutePath, 'utf8').split(/\r?\n/).length;
    lineCountCache.set(absolutePath, lineCount);
  }
  const endLine = Number(lineMatch[2] ?? lineMatch[1]);
  if (endLine > lineCount) errors.push(`Line anchor exceeds ${label} length: ${reference} (${lineCount} lines)`);
}

function validateModel() {
  const errors = [];
  if (nodeById.size !== model.nodes.length) errors.push('Node IDs must be unique.');

  for (const node of model.nodes) {
    if (!/^[a-z0-9-]+$/.test(node.id)) errors.push(`Invalid node ID: ${node.id}`);
    if (!['module', 'code'].includes(node.layer)) errors.push(`Invalid layer for ${node.id}: ${node.layer}`);
    for (const source of node.sources ?? []) validateReference(source, `source for ${node.id}`, errors);
  }

  for (const edge of model.edges) {
    if (!nodeById.has(edge.from)) errors.push(`Unknown edge source: ${edge.from}`);
    if (!nodeById.has(edge.to)) errors.push(`Unknown edge target: ${edge.to}`);
    if (!['contains', 'impact', 'interaction'].includes(edge.kind)) errors.push(`Invalid edge kind: ${edge.kind}`);
    for (const evidence of (edge.evidence ?? '').split(';').map((part) => part.trim()).filter(Boolean))
      validateReference(evidence, 'edge evidence', errors);
  }

  if (errors.length > 0) throw new Error(errors.join('\n'));
}

function yamlString(value) {
  return JSON.stringify(String(value));
}

function wikiLink(id) {
  const node = nodeById.get(id);
  return `[[${vaultRelativeNotesRoot}/${id}|${node.title}]]`;
}

function areaTagForModule(node) {
  if (node.kind === 'feature') return node.id;
  const areaByKind = {
    root: 'application',
    runtime: 'runtime',
    registry: 'runtime',
    'shared-service': 'shared-services',
    'shared-surface': 'shared-surfaces',
    adapter: 'infrastructure',
    contract: 'contracts'
  };
  return areaByKind[node.kind] ?? 'application';
}

function graphTagsFor(node, extraTags = []) {
  const ownerModules = model.edges
    .filter((edge) => edge.to === node.id && ['contains', 'impact'].includes(edge.kind))
    .map((edge) => nodeById.get(edge.from))
    .filter((owner) => owner?.layer === 'module');
  const areas = node.layer === 'module'
    ? [areaTagForModule(node)]
    : ownerModules.map(areaTagForModule);
  const uniqueAreas = [...new Set(areas)];
  if (uniqueAreas.length === 0) uniqueAreas.push(node.layer === 'code' ? 'implementation' : 'application');

  return [...new Set([
    'impact-graph',
    `impact-graph/${node.layer}`,
    `impact-graph/kind/${node.kind}`,
    ...uniqueAreas.map((area) => `impact-graph/area/${area}`),
    ...extraTags
  ])];
}

function reportTagsFor(changed) {
  const areaTags = graphTagsFor(changed).filter((tag) => tag.startsWith('impact-graph/area/'));
  return [...new Set([
    'impact-graph',
    'impact-graph/report',
    `impact-graph/source-layer/${changed.layer}`,
    `impact-graph/source-kind/${changed.kind}`,
    ...areaTags
  ])];
}

function noteFor(node) {
  const outgoing = model.edges.filter((edge) => edge.from === node.id);
  const connections = outgoing.length === 0
    ? '- No outgoing connections are recorded yet.'
    : outgoing.map((edge) => {
      const label = edge.kind === 'impact' ? 'May affect' : 'Contains';
      const detail = edge.relation ? ` — ${edge.relation}` : '';
      const evidence = edge.evidence ? ` (evidence: \`${edge.evidence}\`)` : '';
      return `- **${label}:** ${wikiLink(edge.to)}${detail}${evidence}`;
    }).join('\n');
  const sources = (node.sources ?? []).map((source) => `- \`${source}\``).join('\n') || '- No source path recorded.';

  return [
    '---',
    `graph_id: ${yamlString(node.id)}`,
    `graph_layer: ${yamlString(node.layer)}`,
    `graph_kind: ${yamlString(node.kind)}`,
    `tags:\n${graphTagsFor(node).map((tag) => `  - ${yamlString(tag)}`).join('\n')}`,
    `generated_by: ${yamlString('scripts/impact-graph.mjs')}`,
    '---',
    '',
    `# ${node.title}`,
    '',
    node.summary,
    '',
    '## Connections',
    '',
    connections,
    '',
    '## Source anchors',
    '',
    sources,
    '',
    '## Read this node for impact',
    '',
    '- Impact edges point from a changed dependency toward a consumer that may be affected.',
    '- Use Obsidian **Local Graph** to inspect nearby links and the **Backlinks** panel to see notes that point here.',
    '- For a direct and transitive list from this node, run `node scripts/impact-graph.mjs impact ' + node.id + '` from the repository root.',
    ''
  ].join('\n');
}

function generateModuleNotes() {
  mkdirSync(notesRoot, { recursive: true });
  for (const node of model.nodes) {
    const outputPath = path.join(notesRoot, `${node.id}.md`);
    if (existsSync(outputPath)) {
      const current = readFileSync(outputPath, 'utf8');
      if (!current.includes('generated_by: "scripts/impact-graph.mjs"')) {
        throw new Error(`Refusing to overwrite a note not owned by this generator: ${outputPath}`);
      }
    }
    writeFileSync(outputPath, noteFor(node), 'utf8');
  }
  console.log(`Generated ${model.nodes.length} Obsidian graph nodes under docs/architecture/impact-graph/nodes.`);
}

function findNode(query) {
  const normalized = query.trim().toLocaleLowerCase();
  const exactId = nodeById.get(query);
  if (exactId) return exactId;
  const matches = model.nodes.filter((node) => node.title.toLocaleLowerCase() === normalized);
  if (matches.length === 1) return matches[0];
  const pathMatches = model.nodes.filter((node) => (node.sources ?? []).some((source) =>
    source.split('#', 1)[0].toLocaleLowerCase() === normalized));
  if (pathMatches.length === 1) return pathMatches[0];
  if (matches.length > 1 || pathMatches.length > 1) {
    throw new Error(`Ambiguous node '${query}'. Use one of: ${[...matches, ...pathMatches].map((node) => node.id).join(', ')}`);
  }
  throw new Error(`No graph node matches '${query}'. Use 'list' to see IDs.`);
}

function impactReport(query, writeNote) {
  const changed = findNode(query);
  const impactEdges = model.edges.filter((edge) => {
    if (edge.kind !== 'impact') return false;
    const from = nodeById.get(edge.from);
    const to = nodeById.get(edge.to);
    if (changed.layer === 'module') {
      return from.layer === 'module' && (to.layer === 'module' || to.layer === 'code');
    }
    return from.layer === 'code' && to.layer === 'code';
  });
  const outgoing = new Map();
  for (const edge of impactEdges) {
    if (!outgoing.has(edge.from)) outgoing.set(edge.from, []);
    outgoing.get(edge.from).push(edge);
  }

  const distance = new Map([[changed.id, 0]]);
  const pathEdges = new Map();
  const queue = [changed.id];
  for (let index = 0; index < queue.length; index += 1) {
    const current = queue[index];
    for (const edge of outgoing.get(current) ?? []) {
      if (distance.has(edge.to)) continue;
      distance.set(edge.to, distance.get(current) + 1);
      pathEdges.set(edge.to, [...(pathEdges.get(current) ?? []), edge]);
      queue.push(edge.to);
    }
  }

  const affected = [...distance.entries()]
    .filter(([id, hops]) => id !== changed.id && hops > 0)
    .sort((a, b) => a[1] - b[1] || nodeById.get(a[0]).title.localeCompare(nodeById.get(b[0]).title));
  const direct = affected.filter(([, hops]) => hops === 1);
  const transitive = affected.filter(([, hops]) => hops > 1);

  const renderGroup = (items) => items.length === 0
    ? '- None recorded.'
    : items.map(([id]) => {
      const route = pathEdges.get(id) ?? [];
      const explanation = route.map((edge) => edge.relation).filter(Boolean).join(' → ');
      const evidence = [...new Set(route.map((edge) => edge.evidence).filter(Boolean))].join('; ');
      return `- ${wikiLink(id)}${explanation ? ` — ${explanation}` : ''}${evidence ? `\n  Evidence: \`${evidence}\`` : ''}`;
    }).join('\n');

  const noImpactHint = affected.length === 0
    ? changed.kind === 'root'
      ? 'This is the visual application root, so it has no impact edges. Query a specific module or method for a useful change-impact path.'
      : 'No outgoing dependents are recorded for this node in the current graph.'
    : '';
  const report = [
    `# Change impact: ${changed.title}`,
    '',
    `Starting node: ${wikiLink(changed.id)} (${changed.sources?.join(', ') ?? 'no source anchor'})`,
    `Traversal: ${changed.layer === 'module' ? 'module dependencies and directly linked code entry points' : 'the traced code call path'}`,
    '',
    `Graph edges indicate possible impact propagation. This curated graph currently covers ${model.nodes.length} nodes; it is not a whole-repository semantic call graph.`,
    ...(noImpactHint ? ['', noImpactHint] : []),
    '',
    `## Direct dependents (${direct.length})`,
    '',
    renderGroup(direct),
    '',
    `## Transitive dependents (${transitive.length})`,
    '',
    renderGroup(transitive),
    ''
  ].join('\n');

  console.log(report);
  if (writeNote) {
    const reportsRoot = path.join(repoRoot, 'docs', 'architecture', 'impact-graph', 'reports');
    mkdirSync(reportsRoot, { recursive: true });
    const reportPath = path.join(reportsRoot, `${changed.id}-impact.md`);
    if (existsSync(reportPath) && !readFileSync(reportPath, 'utf8').includes('generated_by: "scripts/impact-graph.mjs"')) {
      throw new Error(`Refusing to overwrite a report not owned by this generator: ${reportPath}`);
    }
    const reportNote = [
      '---',
      `graph_id: ${yamlString(`${changed.id}-impact`)}`,
      'graph_layer: "impact-report"',
      'graph_kind: "report"',
      `tags:\n${reportTagsFor(changed).map((tag) => `  - ${yamlString(tag)}`).join('\n')}`,
      `generated_by: ${yamlString('scripts/impact-graph.mjs')}`,
      '---',
      '',
      report,
      ''
    ].join('\n');
    writeFileSync(reportPath, reportNote, 'utf8');
    console.log(`\nObsidian impact note: ${path.relative(repoRoot, reportPath)}`);
  }
}

function listNodes() {
  for (const node of model.nodes) console.log(`${node.id}\t${node.layer}\t${node.title}`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === scriptPath) try {
  validateModel();
  const [command, ...args] = process.argv.slice(2);
  if (command === 'generate') {
    const outputPath = path.join(repoRoot, 'docs/architecture/impact-graph/source-index.json');
    await buildSourceIndex({ root: repoRoot, outputPath });
    await generateNotes({ graph: loadGraph() });
  }
  else if (command === 'impact' && args.length > 0) {
    const writeNote = args.includes('--note');
    const query = args.filter((arg) => arg !== '--note').join(' ');
    if (!query) throw new Error('Provide a node ID, title, or source path before --note.');
    impactReport(query, writeNote);
  }
  else if (command === 'list') listNodes();
  else {
    console.log('Usage:');
    console.log('  node scripts/impact-graph.mjs generate');
    console.log('  node scripts/impact-graph.mjs impact <node-id|title|source-path> [--note]');
    console.log('  node scripts/impact-graph.mjs list');
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}
