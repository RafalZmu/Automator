// Builds the same interactive impact graph as artifacts/impact-graph/index.html, but for the Dysponent2
// repository. Nothing is written into the Dysponent2 checkout; the page lands in Automator's artifacts.
//
//   node scripts/dysponent2-impact-graph.mjs
//   node scripts/dysponent2-impact-graph.mjs --root "C:/rekord/ProgramyKod/dysponent2" --out artifacts/impact-graph-dysponent2
//
// Unlike Automator, Dysponent2 has no hand-curated architecture model. Its areas are derived from the
// folder layout: each Dysponent2.App zone folder, each Angular feature folder, and each other project
// becomes one area (Area 1 node), and the source indexer supplies files, functions and call/import edges.
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { buildSourceIndex } from './impact-graph-source/index.mjs';
import { buildViewerData, renderViewer } from './impact-graph-viewer.mjs';

const scriptPath = fileURLToPath(import.meta.url);
const automatorRoot = path.resolve(path.dirname(scriptPath), '..');
const args = process.argv.slice(2);
const option = (name, fallback) => (args.includes(name) ? args[args.indexOf(name) + 1] : fallback);
const repoRoot = path.resolve(option('--root', 'C:/rekord/ProgramyKod/dysponent2'));
const outDir = path.resolve(automatorRoot, option('--out', 'artifacts/impact-graph-dysponent2'));

const slug = (value) => value.replace(/([a-z0-9])([A-Z])/g, '$1-$2').replace(/[^a-zA-Z0-9]+/g, '-').toLowerCase().replace(/^-|-$/g, '');
const testPattern = /(^|\/)(?:[^/]*\.(?:Tests|Specs)|ApiE2ETests|dysponent2-panel-e2e)\/|\.(?:spec|test)\.tsx?$/;

// Maps a tracked source path to its architectural area. Returns the area key, title, kind and the folder
// that represents the area in the graph (used as the module's source anchor).
function areaFor(relPath) {
  const parts = relPath.split('/');
  if (parts[0] === 'src' && parts[1] === 'Dysponent2.App') {
    if (parts.length === 3) return { key: 'app-root', title: 'Dysponent2.App root', kind: 'shared-service', dir: 'src/Dysponent2.App' };
    const zone = parts[2];
    return { key: `app-${slug(zone)}`, title: zone, kind: zone.endsWith('Zone') ? 'feature' : 'shared-service', dir: `src/Dysponent2.App/${zone}` };
  }
  if (parts[0] === 'src' && parts[1] === 'dysponent2-panel-spa') {
    if (parts[2] === 'src' && parts[3] === 'app' && parts.length > 5) {
      const feature = parts[4];
      return { key: `spa-${slug(feature)}`, title: `SPA ${feature}`, kind: 'runtime', dir: `src/dysponent2-panel-spa/src/app/${feature}` };
    }
    return { key: 'spa-root', title: 'SPA root', kind: 'runtime', dir: 'src/dysponent2-panel-spa' };
  }
  if (parts[0] === 'src' && parts.length > 2) {
    const project = parts[1];
    const kind = testPattern.test(relPath) ? 'test' : /Infrastructure$/.test(project) ? 'adapter' : /Foundation|Common|Raport/.test(project) ? 'shared-service' : 'runtime';
    return { key: `project-${slug(project)}`, title: project, kind, dir: `src/${project}` };
  }
  return { key: `dir-${slug(parts[0] ?? 'root')}`, title: parts[0] ?? 'root', kind: 'other', dir: parts[0] ?? '' };
}

const index = await buildSourceIndex({ root: repoRoot, tsconfigs: ['src/dysponent2-panel-spa/tsconfig.json', 'src/dysponent2-panel-e2e/tsconfig.json'] });

const moduleByKey = new Map();
const fileArea = new Map(); // file id -> module id
const files = index.files.map((file) => {
  const area = areaFor(file.path);
  const moduleId = `area-${area.key}`;
  if (!moduleByKey.has(moduleId)) moduleByKey.set(moduleId, { ...area, id: moduleId, fileCount: 0 });
  moduleByKey.get(moduleId).fileCount += 1;
  fileArea.set(file.id, moduleId);
  return { ...file, language: file.language, areaIds: [moduleId], isTest: testPattern.test(file.path) };
});

const fileById = new Map(files.map((f) => [f.id, f]));
const rootId = 'dysponent2';
const nodes = [
  { id: rootId, title: 'Dysponent2 application', layer: 'module', kind: 'root', summary: 'Dysponent2 application: ASP.NET backend, Angular panel, and supporting .NET integration projects.', sources: ['README.md'], areaIds: [] },
  ...[...moduleByKey.values()].sort((a, b) => a.id.localeCompare(b.id)).map((area) => ({
    id: area.id, title: area.title, layer: 'module', kind: area.kind,
    summary: `${area.fileCount} tracked source files under ${area.dir}.`, sources: [area.dir], areaIds: [],
  })),
  ...files.map((file) => ({
    id: file.id, title: path.posix.basename(file.path), layer: 'file', kind: file.language,
    summary: `Source file ${file.path}.`, sources: [file.path], language: file.language, areaIds: file.areaIds, isTest: file.isTest,
  })),
  ...index.functions.map((fn) => {
    const file = fileById.get(fn.fileId);
    return {
      id: fn.id, title: fn.qualifiedName, layer: 'function', kind: fn.kind, summary: fn.signature,
      sources: [`${fn.path}#L${fn.startLine}`], signature: fn.signature, path: fn.path, startLine: fn.startLine, endLine: fn.endLine,
      areaIds: file?.areaIds ?? [], isTest: file?.isTest ?? false, language: file?.language,
    };
  }),
];

const edges = [];
for (const module of moduleByKey.values()) edges.push({ from: rootId, to: module.id, kind: 'contains', relation: 'contains area' });
for (const file of files) edges.push({ from: file.areaIds[0], to: file.id, kind: 'contains', relation: 'owns source file' });
for (const fn of index.functions) edges.push({ from: fn.fileId, to: fn.id, kind: 'contains', relation: 'declares function', evidence: `${fn.path}#L${fn.startLine}` });
for (const edge of index.edges) edges.push(edge);

// EF Core applies IEntityTypeConfiguration builders reflectively, so call analysis never sees their Configure
// methods being used. The entity attribute names its builder explicitly, so that link is taken from the source
// text. Only attributes naming a builder with exactly one matching Configure method are linked.
const efAttribute = /\[EntityTypeConfiguration(?:<\s*(\w+)\s*,\s*\w+\s*>|\(typeof\((\w+)\)\))\]/g;
const configureByBuilder = new Map();
for (const fn of index.functions) {
  const match = fn.qualifiedName.match(/\.(\w+)\.Configure\(/);
  if (!match) continue;
  configureByBuilder.set(match[1], [...(configureByBuilder.get(match[1]) ?? []), fn.id]);
}
let efLinks = 0;
const efUnresolved = [];
for (const file of index.files.filter((f) => f.language === 'csharp')) {
  const text = readFileSync(path.join(repoRoot, file.path), 'utf8');
  for (const match of text.matchAll(efAttribute)) {
    const builder = match[1] ?? match[2];
    const line = text.slice(0, match.index).split('\n').length;
    const targets = configureByBuilder.get(builder) ?? [];
    if (targets.length !== 1) {
      efUnresolved.push(`${file.path}:${line} ${builder} (${targets.length} Configure methods)`);
      continue;
    }
    edges.push({ from: targets[0], to: file.id, kind: 'impact', relation: 'EF configuration attribute', evidence: `${file.path}:${line}` });
    efLinks += 1;
  }
}
console.log(`EF configuration links: ${efLinks}; unresolved: ${efUnresolved.length}${efUnresolved.length ? ` (${efUnresolved.join('; ')})` : ''}`);

// Module-level view: roll file-level dependencies up to the areas they connect, so the top of the graph
// shows which zones depend on which, not only the individual files.
const areaLinks = new Map();
for (const edge of index.edges) {
  if (!fileById.has(edge.from) || !fileById.has(edge.to)) continue;
  const from = fileArea.get(edge.from);
  const to = fileArea.get(edge.to);
  if (!from || !to || from === to) continue;
  const key = `${from}|${to}`;
  areaLinks.set(key, (areaLinks.get(key) ?? 0) + 1);
}
for (const [key, count] of areaLinks) {
  const [from, to] = key.split('|');
  edges.push({ from, to, kind: 'impact', relation: 'aggregated file dependency', evidence: `${count} file-level dependencies` });
}

const graph = { nodes, edges, diagnostics: index.diagnostics.filter((d) => d.code !== 'unmapped-area') };
const data = buildViewerData(graph);
const html = renderViewer(data, {
  title: 'Dysponent2 impact graph',
  defaultRoot: repoRoot.replaceAll('\\', '/'),
  storagePrefix: 'impactgraph-dysponent2.',
});

mkdirSync(outDir, { recursive: true });
writeFileSync(path.join(outDir, 'index.html'), html, 'utf8');
const diagnostics = graph.diagnostics.length;
console.log(`Wrote ${path.relative(automatorRoot, path.join(outDir, 'index.html'))} (${data.nodes.length} nodes, ${data.edges.length} edges, ${moduleByKey.size} areas, ${diagnostics} indexer diagnostics).`);
