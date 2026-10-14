// Builds a standalone, dependency-free interactive viewer for the impact graph.
//
//   node scripts/impact-graph-viewer.mjs            -> artifacts/impact-graph/index.html
//   node scripts/impact-graph-viewer.mjs --json out.json   (data only, for debugging)
//
// The viewer reads the same graph that scripts/impact-graph.mjs generates the Obsidian notes from
// (model.json + source-index.json), so run `node scripts/impact-graph.mjs generate` first if the
// source changed. Nothing here touches the Obsidian notes.
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadGraph } from './impact-graph.mjs';

const scriptPath = fileURLToPath(import.meta.url);
const repoRoot = path.resolve(path.dirname(scriptPath), '..');
const templatePath = path.join(path.dirname(scriptPath), 'impact-graph-viewer', 'template.html');

function classify(edge) {
  if (edge.kind === 'interaction') return 'interaction';
  const relation = edge.relation ?? '';
  if (relation.startsWith('called by')) return 'call';
  if (relation.startsWith('type')) return 'type';
  if (relation.startsWith('imports')) return 'import';
  return 'curated';
}

export function buildViewerData(graph) {
  const byId = new Map(graph.nodes.map((node) => [node.id, node]));
  const parent = new Map();
  const rank = new Map(); // prefer file->function over module->function
  for (const edge of graph.edges) {
    if (edge.kind !== 'contains' || !byId.has(edge.from) || !byId.has(edge.to)) continue;
    const from = byId.get(edge.from);
    const weight = from.layer === 'file' ? 2 : 1;
    if (!parent.has(edge.to) || weight > rank.get(edge.to)) {
      parent.set(edge.to, edge.from);
      rank.set(edge.to, weight);
    }
  }
  const root = graph.nodes.find((node) => node.kind === 'root')?.id ?? 'app';
  const fileByPath = new Map(graph.nodes.filter((n) => n.layer === 'file').map((n) => [n.sources?.[0], n.id]));
  for (const node of graph.nodes) {
    if (node.id === root || parent.has(node.id)) continue;
    const filePath = (node.sources?.[0] ?? '').split('#')[0];
    parent.set(node.id, fileByPath.get(filePath) ?? root);
  }

  // A few curated service modules are attached to the file that implements them. Show them next to
  // that file (inside the file's owning area) instead of nesting a module inside a file.
  for (const node of graph.nodes) {
    if (node.layer !== 'module') continue;
    const owner = byId.get(parent.get(node.id));
    if (owner?.layer === 'file') parent.set(node.id, parent.get(owner.id) ?? root);
  }

  const areaOf = (node) => {
    if (node.layer === 'module') return node.id;
    if (node.areaIds?.length) return node.areaIds[0];
    return null;
  };

  const index = new Map(graph.nodes.map((node, i) => [node.id, i]));
  const nodes = graph.nodes.map((node) => {
    const source = node.sources?.[0] ?? '';
    const [sourcePath, anchor] = source.split('#L');
    return {
      id: node.id,
      t: node.title,
      l: node.layer,
      k: node.kind,
      a: areaOf(node),
      g: node.language ?? null,
      x: node.isTest ? 1 : 0,
      p: node.path ?? sourcePath ?? '',
      n: node.startLine ?? (anchor ? Number(anchor) : 0),
      s: node.summary ?? '',
      par: parent.has(node.id) ? index.get(parent.get(node.id)) : -1
    };
  });

  const relations = [];
  const relationIndex = new Map();
  const edges = [];
  for (const edge of graph.edges) {
    if (edge.kind === 'contains') continue;
    const from = index.get(edge.from);
    const to = index.get(edge.to);
    if (from === undefined || to === undefined) continue;
    const key = `${classify(edge)}|${edge.relation ?? ''}`;
    if (!relationIndex.has(key)) {
      relationIndex.set(key, relations.length);
      relations.push({ type: classify(edge), label: edge.relation ?? '' });
    }
    edges.push([from, to, relationIndex.get(key), edge.evidence ?? '']);
  }
  return { root: index.get(root), nodes, edges, relations, generatedAt: new Date().toISOString() };
}

// Options let other repositories reuse the viewer: a page title, the folder used for "open in editor"
// links, and a localStorage prefix so two generated pages opened from file:// do not share settings.
export function renderViewer(data, { title = 'Automator impact graph', defaultRoot = 'C:/Automator', storagePrefix = 'impactgraph.' } = {}) {
  const template = readFileSync(templatePath, 'utf8');
  // '<' is escaped so a stray "</script>" inside a signature can never end the data block.
  const json = JSON.stringify(data).replace(/</g, '\\u003c');
  const escapedTitle = title.replace(/&/g, '&amp;').replace(/</g, '&lt;');
  return template
    .replaceAll('__GRAPH_TITLE__', () => escapedTitle)
    .replace('/*__STORAGE_PREFIX__*/"impactgraph."', () => JSON.stringify(storagePrefix))
    .replace('/*__DEFAULT_ROOT__*/"C:/Automator"', () => JSON.stringify(defaultRoot))
    .replace('/*__GRAPH_DATA__*/null', () => json);
}

if (process.argv[1] && path.resolve(process.argv[1]) === scriptPath) {
  const args = process.argv.slice(2);
  const data = buildViewerData(loadGraph());
  const jsonIndex = args.indexOf('--json');
  if (jsonIndex >= 0) {
    const out = path.resolve(args[jsonIndex + 1]);
    mkdirSync(path.dirname(out), { recursive: true });
    writeFileSync(out, JSON.stringify(data));
    console.log(`Wrote ${out}`);
  } else {
    const out = path.join(repoRoot, 'artifacts', 'impact-graph', 'index.html');
    mkdirSync(path.dirname(out), { recursive: true });
    writeFileSync(out, renderViewer(data), 'utf8');
    console.log(`Wrote ${path.relative(repoRoot, out)} (${data.nodes.length} nodes, ${data.edges.length} edges). Open it in a browser.`);
  }
}
