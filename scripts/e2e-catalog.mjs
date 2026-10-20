// Builds the automated-test list for docs/e2e/index.html.
//
//   node scripts/e2e-catalog.mjs          rewrite the "tests" part of the data block in the page
//   node scripts/e2e-catalog.mjs --check  exit 1 when the page lists different tests than the source
//   node scripts/e2e-catalog.mjs --missing <file>  write the tests that still lack steps or an expected result
//
// Test ideas typed into the page live in the same data block ("ideas") and are never touched here.
import { createHash } from 'node:crypto';
import { readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

export const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
export const pagePath = path.join(root, 'docs', 'e2e', 'index.html');

export const categories = [
  'Launcher', 'Script Runner', 'API', 'Browser Automation', 'Workflows', 'Scheduler', 'Work Time',
  'Website Launcher', 'Snippets', 'Codex', 'Global action search', 'Options', 'Activity and notifications',
  'Workspace window', 'Agent access (MCP)', 'Platform and shell',
];

const tabDirectories = {
  launcher: 'Launcher', 'script-runner': 'Script Runner', api: 'API', 'browser-automation': 'Browser Automation',
  workflows: 'Workflows', scheduler: 'Scheduler', 'work-time': 'Work Time', 'website-launcher': 'Website Launcher', snippets: 'Snippets', codex: 'Codex',
};

// First match wins. Matched against the repo-relative path with forward slashes.
const pathRules = [
  [/e2e-catalog/, 'Platform and shell'],
  [/mcp/i, 'Agent access (MCP)'],
  [/activity|task-toasts/i, 'Activity and notifications'],
  [/api-view/i, 'API'],
  [/browser|playwright/i, 'Browser Automation'],
  [/website-launcher/i, 'Website Launcher'],
  [/snippets/i, 'Snippets'],
  [/codex/i, 'Codex'],
  [/scheduler/i, 'Scheduler'],
  [/work-time|Focus\.Specs/i, 'Work Time'],
  [/workflows|Variables\.Specs|variables-contract/i, 'Workflows'],
  [/script-run|script-template|ScriptRunner|fdb-stale|template-asset|update-and-restart|path-field|automation-services/i, 'Script Runner'],
  [/command-matching|catalog-alias|explorer-action-ui|file-explorer|FileExplorer/i, 'Launcher'],
  [/native-|window-context|minimal-window|dev-renderer|backend-process|host-platform|rpc-contracts|reload-budget|vite-watch|packaged|Core\.Specs|Protocol\.Specs|Windows\.Specs|Infrastructure\.Specs|Scheduler/i, 'Platform and shell'],
];

// For the shared specs whose path says nothing, fall back to words in the test title.
const titleRules = [
  [/alias|catalog|launcher|tab key|digit|slash|panel|activation|hotkey|foreground/i, 'Launcher'],
  [/theme|opener|settings|configuration/i, 'Options'],
];

function* walk(directory) {
  for (const name of readdirSync(directory)) {
    if (name === 'node_modules' || name === 'bin' || name === 'obj') continue;
    const full = path.join(directory, name);
    if (statSync(full).isDirectory()) yield* walk(full);
    else yield full;
  }
}

const rel = (file) => path.relative(root, file).split(path.sep).join('/');
const line = (text, index) => text.slice(0, index).split('\n').length;

function titlesIn(file, text) {
  const found = [];
  if (/\.(?:cjs|mjs)$/.test(file)) {
    const pattern = /^\s*(?:test|it)\(\s*(['"`])((?:\\.|(?!\1)[^\\\n])*)\1(?!\s*,\s*\{[^}]*skip)/gm;
    for (const match of text.matchAll(pattern)) found.push({ title: match[2].replace(/\\(['"`])/g, '$1'), line: line(text, match.index) });
  } else if (/Program\.cs$/.test(file)) {
    const pattern = /^\s*\("((?:\\.|[^"\\])+)",\s*[\w.]+\)/gm;
    for (const match of text.matchAll(pattern)) found.push({ title: match[1].replace(/\\"/g, '"'), line: line(text, match.index) });
  } else if (/\.spec\.ps1$/.test(file)) {
    found.push({ title: path.basename(file, '.spec.ps1').replace(/-/g, ' ') + ' (PowerShell spec)', line: 1 });
  }
  return found;
}

function layerOf(file) {
  if (file.endsWith('.spec.ps1')) return 'PowerShell spec';
  if (file.endsWith('.package.cjs')) return 'Packaged app';
  if (file.startsWith('tests/electron/')) return 'Electron UI';
  if (file.endsWith('.cs')) return '.NET spec';
  return 'Contract / model';
}

function readTestDocs() {
  const byTitle = new Map();
  for (const [directory, category] of Object.entries(tabDirectories)) {
    const file = path.join(root, 'docs', 'tabs', directory, 'TESTS.md');
    let text;
    try { text = readFileSync(file, 'utf8'); } catch { continue; }
    for (const block of text.split(/^### /m).slice(1)) {
      const title = block.split('\n', 1)[0].trim();
      const steps = [...block.matchAll(/^\d+\.\s+(.+)$/gm)].map((match) => match[1].trim());
      const expected = block.match(/\*\*Expected result:\*\*\s*(.+)/)?.[1].trim() ?? '';
      const entry = byTitle.get(title) ?? { categories: new Set(), steps, expected };
      entry.categories.add(category);
      byTitle.set(title, entry);
    }
  }
  return byTitle;
}

// Steps and expected results written by hand (or by Claude) for tests that no TESTS.md describes live in the page itself.
export function buildTests(previous = []) {
  const docs = readTestDocs();
  const kept = new Map(previous.map((test) => [test.id, test]));
  const files = [...walk(path.join(root, 'tests'))].map(rel).sort();
  const tests = [];
  for (const file of files) {
    const text = readFileSync(path.join(root, file), 'utf8');
    for (const { title, line: sourceLine } of titlesIn(file, text)) {
      const doc = docs.get(title);
      let category = doc?.categories.size === 1 ? [...doc.categories][0] : null;
      category ??= doc ? 'Platform and shell' : null;
      category ??= pathRules.find(([pattern]) => pattern.test(file))?.[1] ?? null;
      category ??= titleRules.find(([pattern]) => pattern.test(title))?.[1] ?? 'Platform and shell';
      const id = createHash('sha1').update(`${file}\n${title}`).digest('hex').slice(0, 10);
      const test = { id, title, category, layer: layerOf(file), file, line: sourceLine };
      const earlier = kept.get(id);
      const steps = doc?.steps.length ? doc.steps : earlier?.steps;
      const expected = doc?.expected || earlier?.expected;
      if (steps?.length) test.steps = steps;
      if (expected) test.expected = expected;
      tests.push(test);
    }
  }
  return tests;
}

// One record per line keeps diffs and merge conflicts readable when several people add ideas.
export function serialize(data) {
  const rows = (items) => '[\n' + items.map((item) => '  ' + JSON.stringify(item)).join(',\n') + '\n]';
  return `{"tests":${rows(data.tests)},\n"ideas":${rows(data.ideas)}}`.replace(/</g, '\\u003c');
}

const dataBlock = /(<script type="application\/json" id="e2e-data">)([\s\S]*?)(<\/script>)/;

export function readPage(html = readFileSync(pagePath, 'utf8')) {
  const match = html.match(dataBlock);
  if (!match) throw new Error('docs/e2e/index.html has no e2e-data block');
  const data = JSON.parse(match[2]);
  return { html, data: { tests: data.tests ?? [], ideas: data.ideas ?? [] } };
}

function main() {
  const check = process.argv.includes('--check');
  const { html, data } = readPage();
  const next = { tests: buildTests(data.tests), ideas: data.ideas };
  const missing = next.tests.filter((test) => !test.steps?.length || !test.expected);
  if (process.argv.includes('--missing')) {
    writeFileSync(process.argv[process.argv.indexOf('--missing') + 1], JSON.stringify(missing, null, 1));
    console.log(`${missing.length} tests without steps or expected result`);
    return;
  }
  const updated = html.replace(dataBlock, (_, open, __, close) => `${open}${serialize(next)}${close}`);
  if (check) {
    if (updated !== html) {
      console.error('docs/e2e/index.html is out of date. Run: node scripts/e2e-catalog.mjs');
      process.exit(1);
    }
    console.log(`E2E catalog is current (${next.tests.length} tests, ${next.ideas.length} ideas, ${missing.length} without steps or expected result).`);
    return;
  }
  writeFileSync(pagePath, updated);
  const counts = Object.groupBy(next.tests, (test) => test.category);
  for (const name of categories) console.log(`${String(counts[name]?.length ?? 0).padStart(4)}  ${name}`);
  console.log(`${next.tests.length} tests, ${next.ideas.length} ideas written to docs/e2e/index.html`);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) main();
