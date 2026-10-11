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

function validateReference(reference, label, errors, root = repoRoot) {
  let [sourcePath, fragment] = reference.split('#', 2);
  if (!fragment) {
    const lineAnchor = sourcePath.match(/:(\d+)(?::(\d+))?$/);
    if (lineAnchor) {
      fragment = `L${lineAnchor[1]}${lineAnchor[2] ? `-L${lineAnchor[2]}` : ''}`;
      sourcePath = sourcePath.slice(0, -lineAnchor[0].length);
    }
  }
  const absolutePath = path.join(root, sourcePath);
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

export async function validateGraph(graph = loadGraph(), { root = repoRoot, notesRoot = path.join(root, 'docs/architecture/impact-graph/nodes') } = {}) {
  const errors = [];
  const byId = new Map(graph.nodes.map((node) => [node.id, node]));
  if (byId.size !== graph.nodes.length) errors.push('Merged graph node IDs must be unique.');
  for (const node of graph.nodes) for (const source of node.sources ?? []) validateReference(source, `source for ${node.id}`, errors, root);
  for (const edge of graph.edges) {
    if (!byId.has(edge.from)) errors.push(`Unknown edge source: ${edge.from}`);
    if (!byId.has(edge.to)) errors.push(`Unknown edge target: ${edge.to}`);
    for (const evidence of (edge.evidence ?? '').split(';').map((part) => part.trim()).filter(Boolean)) validateReference(evidence, `edge evidence for ${edge.from} -> ${edge.to}`, errors, root);
  }

  const folderFor = (node) => node.layer === 'file' ? 'files' : node.layer === 'function' ? 'functions' : '';
  const wanted = new Set();
  for (const node of graph.nodes) {
    const output = path.join(notesRoot, folderFor(node), `${node.id}.md`);
    wanted.add(path.resolve(output));
    if (!existsSync(output)) errors.push(`Missing generated note for ${node.id}: ${path.relative(root, output)}`);
    else if (!readFileSync(output, 'utf8').includes(generatorMarker)) errors.push(`Generated note is not generator-owned: ${path.relative(root, output)}`);
  }
  for (const folder of ['', 'files', 'functions']) {
    const dir = path.join(notesRoot, folder);
    if (!existsSync(dir)) continue;
    for (const name of await readdir(dir)) {
      const output = path.resolve(dir, name);
      if (!name.endsWith('.md') || wanted.has(output)) continue;
      if (readFileSync(output, 'utf8').includes(generatorMarker)) errors.push(`Stale generated note has no graph node: ${path.relative(root, output)}`);
    }
  }
  const reportsRoot = path.join(root, 'docs/architecture/impact-graph/reports');
  if (existsSync(reportsRoot)) for (const name of await readdir(reportsRoot)) {
    if (!name.endsWith('-impact.md')) continue;
    const report = path.join(reportsRoot, name);
    const contents = readFileSync(report, 'utf8');
    if (!contents.includes(generatorMarker)) continue;
    const id = name.slice(0, -'-impact.md'.length);
    if (!byId.has(id)) errors.push(`Stale generated impact report has no graph node: ${path.relative(root, report)}`);
    else {
      const expected = buildImpactReport(graph, id);
      const expectedContents = renderImpactReportNote(expected.changed, expected.report, graph);
      if (contents !== expectedContents) errors.push(`Generated impact report does not match the current graph: ${path.relative(root, report)}. Refresh it with 'node scripts/impact-graph.mjs impact ${id} --note'.`);
    }
  }
  return errors;
}

function yamlString(value) {
  return JSON.stringify(String(value));
}

function wikiLink(id, graphNodes = nodeById) {
  const node = graphNodes.get(id);
  const folder = node.layer === 'file' ? '/files' : node.layer === 'function' ? '/functions' : '';
  return `[[${vaultRelativeNotesRoot}${folder}/${id}|${node.title}]]`;
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

export function findNode(query, graph = { nodes: model.nodes }) {
  const nodeMap = new Map(graph.nodes.map((node) => [node.id, node]));
  const normalized = query.trim().toLocaleLowerCase();
  const exactId = nodeMap.get(query);
  if (exactId) return exactId;
  const pathMatches = graph.nodes.filter((node) => node.layer === 'file' && (node.sources ?? []).some((source) =>
    source.split('#', 1)[0].toLocaleLowerCase() === normalized));
  if (pathMatches.length === 1) return pathMatches[0];
  if (pathMatches.length > 1) throw new Error(`Ambiguous file path '${query}'. Use one of: ${pathMatches.map((node) => node.id).join(', ')}`);
  const functions = graph.nodes.filter((node) => node.layer === 'function' && node.title.toLocaleLowerCase() === normalized);
  if (functions.length === 1) return functions[0];
  if (functions.length > 1) throw new Error(`Ambiguous function '${query}'. Use one of: ${functions.map((node) => node.id).join(', ')}`);
  const matches = graph.nodes.filter((node) => node.layer === 'module' && node.title.toLocaleLowerCase() === normalized);
  if (matches.length === 1) return matches[0];
  if (matches.length > 1) {
    throw new Error(`Ambiguous node '${query}'. Use one of: ${matches.map((node) => node.id).join(', ')}`);
  }
  throw new Error(`No graph node matches '${query}'. Use 'list' to see IDs.`);
}

export function buildImpactReport(graph, query) {
  const graphNodes = new Map(graph.nodes.map((node) => [node.id, node]));
  const changed = findNode(query, graph);
  const ownership = new Map();
  for (const edge of graph.edges) if (edge.kind === 'contains') {
    if (!ownership.has(edge.from)) ownership.set(edge.from, []);
    ownership.get(edge.from).push(edge.to);
  }
  const seeds = new Set([changed.id]);
  if (changed.layer === 'module' || changed.layer === 'file') {
    const queue = [changed.id];
    for (let i = 0; i < queue.length; i += 1) for (const id of ownership.get(queue[i]) ?? []) {
      if (!seeds.has(id)) { seeds.add(id); queue.push(id); }
    }
  }
  const impactEdges = graph.edges.filter((edge) => edge.kind === 'impact');
  const outgoing = new Map();
  for (const edge of impactEdges) {
    if (!outgoing.has(edge.from)) outgoing.set(edge.from, []);
    outgoing.get(edge.from).push(edge);
  }

  const distance = new Map([...seeds].map((id) => [id, 0]));
  const pathEdges = new Map();
  const queue = [...seeds];
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
    .filter(([id, hops]) => !seeds.has(id) && hops > 0)
    .sort((a, b) => a[1] - b[1] || graphNodes.get(a[0]).title.localeCompare(graphNodes.get(b[0]).title));
  const direct = affected.filter(([, hops]) => hops === 1);
  const transitive = affected.filter(([, hops]) => hops > 1);

  const renderGroup = (items) => items.length === 0
    ? '- None recorded.'
    : items.map(([id]) => {
      const route = pathEdges.get(id) ?? [];
      const explanation = route.map((edge) => edge.relation).filter(Boolean).join(' → ');
      const evidence = [...new Set(route.map((edge) => edge.evidence).filter(Boolean))].join('; ');
      return `- ${wikiLink(id, graphNodes)}${explanation ? ` — ${explanation}` : ''}${evidence ? `\n  Evidence: \`${evidence}\`` : ''}`;
    }).join('\n');

  const noImpactHint = affected.length === 0
    ? changed.kind === 'root'
      ? 'This is the visual application root, so it has no impact edges. Query a specific module or method for a useful change-impact path.'
      : 'No outgoing dependents are recorded for this node in the current graph.'
    : '';
  const report = [
    `# Change impact: ${changed.title}`,
    '',
    `Starting node: ${wikiLink(changed.id, graphNodes)} (${changed.sources?.join(', ') ?? 'no source anchor'})`,
    `Traversal: ${changed.layer === 'module' ? 'owned files and functions, then dependency impact edges' : changed.layer === 'file' ? 'file consumers and functions declared in the file, then caller impact edges' : 'function callers'}`,
    '',
    `Graph edges indicate possible impact propagation. The merged graph currently covers ${graph.nodes.length} nodes; static analysis may miss runtime behavior.`,
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

  return { changed, direct: direct.map(([id]) => graphNodes.get(id)), transitive: transitive.map(([id]) => graphNodes.get(id)), report };
}

function reportTagsFor(changed, graph) {
  const areas = changed.layer === 'module' ? [areaTagForModule(changed)] : (changed.areaIds ?? []);
  return [...new Set(['impact-graph', 'impact-graph/report', `impact-graph/source-layer/${changed.layer}`, `impact-graph/source-kind/${changed.kind}`, ...areas.map((area) => `impact-graph/area/${area}`)])];
}

export function renderImpactReportNote(changed, report, graph) {
  return [
    '---',
    `graph_id: ${yamlString(`${changed.id}-impact`)}`,
    'graph_layer: "impact-report"',
    'graph_kind: "report"',
    `tags:\n${reportTagsFor(changed, graph).map((tag) => `  - ${yamlString(tag)}`).join('\n')}`,
    `generated_by: ${yamlString('scripts/impact-graph.mjs')}`,
    '---',
    '',
    report,
    ''
  ].join('\n');
}

export function writeImpactReportNote(changed, report, graph, reportsRoot = path.join(repoRoot, 'docs', 'architecture', 'impact-graph', 'reports')) {
  mkdirSync(reportsRoot, { recursive: true });
  const reportPath = path.join(reportsRoot, `${changed.id}-impact.md`);
  if (existsSync(reportPath) && !readFileSync(reportPath, 'utf8').includes(generatorMarker)) {
    throw new Error(`Refusing to overwrite a report not owned by this generator: ${reportPath}`);
  }
  writeFileSync(reportPath, renderImpactReportNote(changed, report, graph), 'utf8');
  return reportPath;
}

function impactReport(query, writeNote) {
  const graph = loadGraph();
  const { changed, report } = buildImpactReport(graph, query);
  console.log(report);
  if (writeNote) {
    const reportPath = writeImpactReportNote(changed, report, graph);
    console.log(`\nObsidian impact note: ${path.relative(repoRoot, reportPath)}`);
  }
}

function listNodes(layer) {
  const graph = loadGraph();
  for (const node of graph.nodes) if (!layer || layer === node.layer) console.log(`${node.id}\t${node.layer}\t${node.title}`);
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
  else if (command === 'list') {
    const layer = args[0];
    if (layer && !['module', 'file', 'function'].includes(layer)) throw new Error(`Unknown list layer '${layer}'. Use module, file, or function.`);
    listNodes(layer);
  }
  else if (command === 'validate') {
    const errors = await validateGraph(loadGraph());
    if (errors.length) throw new Error(errors.join('\n'));
    console.log('Impact graph is valid.');
  }
  else {
    console.log('Usage:');
    console.log('  node scripts/impact-graph.mjs generate');
    console.log('  node scripts/impact-graph.mjs impact <node-id|file-path|qualified-function-name> [--note]');
    console.log('  node scripts/impact-graph.mjs list [module|file|function]');
    console.log('  node scripts/impact-graph.mjs validate');
  }
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}
