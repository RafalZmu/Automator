const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const os = require('node:os');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const { test } = require('node:test');

const workspace = path.resolve(__dirname, '../..');

async function loadExportHelpers() {
  return import(pathToFileURL(path.join(workspace, 'electron', 'codexExport.ts')).href);
}

async function loadScopePicker() {
  return import(pathToFileURL(path.join(workspace, 'electron', 'codexScopePathPicker.ts')).href);
}

function codexState() {
  return { visible: true, mode: 'launcher', tabs: [{ slot: 9, id: 'codex', capabilities: [{ id: 'codex.task-builder', version: 1 }] }] };
}

test('Codex export requires the first-party main frame, active slot 9, and a valid draft ID', async () => {
  const { authorizeCodexExport } = await loadExportHelpers();
  const context = { role: 'launcher', selectedTab: 9 };
  assert.doesNotThrow(() => authorizeCodexExport(true, context, codexState(), 'a'.repeat(32)));
  assert.throws(() => authorizeCodexExport(false, context, codexState(), 'a'.repeat(32)), /main frame/i);
  assert.throws(() => authorizeCodexExport(true, { ...context, selectedTab: 8 }, codexState(), 'a'.repeat(32)), /active Codex tab/i);
  assert.throws(() => authorizeCodexExport(true, context, { ...codexState(), tabs: [{ slot: 9, id: 'codex', capabilities: [] }] }, 'a'.repeat(32)), /access/i);
  assert.throws(() => authorizeCodexExport(true, context, codexState(), '..\\outside'), /draft ID/i);
});

test('Codex export chooses format extensions and create-new writes never replace user files', async () => {
  const { extensionForCodexFormat, writeCodexSourceCreateNew } = await loadExportHelpers();
  assert.equal(extensionForCodexFormat('python'), 'py');
  assert.equal(extensionForCodexFormat('playwright'), 'spec.ts');
  assert.equal(extensionForCodexFormat('workflow'), 'json');
  assert.throws(() => extensionForCodexFormat('unknown'), /format/i);
  const directory = await fs.mkdtemp(path.join(os.tmpdir(), 'automator-codex-export-'));
  try {
    const destination = path.join(directory, 'task.py');
    await writeCodexSourceCreateNew(destination, 'print("first")');
    await assert.rejects(writeCodexSourceCreateNew(destination, 'print("replacement")'), { code: 'EEXIST' });
    assert.equal(await fs.readFile(destination, 'utf8'), 'print("first")');
    await assert.rejects(writeCodexSourceCreateNew(path.join(directory, 'too-large.py'), 'x'.repeat(262145)), /export limit/i);
  } finally { await fs.rm(directory, { recursive: true, force: true }); }
});

test('Codex review flow exposes first-run approval, separate effect confirmation, and success-only saving', async () => {
  const source = await fs.readFile(path.join(workspace, 'ui', 'modules', 'CodexView.tsx'), 'utf8');
  assert.match(source, /Review and run/);
  assert.match(source, /Confirm effects and run/);
  assert.match(source, /runSucceeded && !draft\.saved/);
  assert.match(source, /readonly source|Complete source/i);
  assert.match(source, /Accept each additional item/);
  assert.match(source, /existing sign-in and default model; Automator disables its known shell, browser, app, MCP, plugin, and search features/i);
  const bridge = await fs.readFile(path.join(workspace, 'ui', 'bridge.ts'), 'utf8');
  assert.match(bridge, /The browser preview has no Codex process/);
});

test('Codex scope picker selects existing files or folders only from active slot 9', async () => {
  const { getCodexScopePathPickerSpec } = await loadScopePicker();
  assert.deepEqual(getCodexScopePathPickerSpec('file', 9, true), {
    title: 'Select a file for task scope', properties: ['openFile'], filters: [{ name: 'All files', extensions: ['*'] }],
  });
  assert.deepEqual(getCodexScopePathPickerSpec('directory', 9, true), {
    title: 'Select a folder for task scope', properties: ['openDirectory'],
  });
  assert.throws(() => getCodexScopePathPickerSpec('file', 2, true), /active Codex tab/i);
  assert.throws(() => getCodexScopePathPickerSpec('directory', 9, false), /active Codex tab/i);
  assert.throws(() => getCodexScopePathPickerSpec('save', 9, true), /kind is invalid/i);

  const source = await fs.readFile(path.join(workspace, 'ui', 'modules', 'CodexView.tsx'), 'utf8');
  assert.match(source, /Browse file/);
  assert.match(source, /Browse folder/);
  assert.match(source, /pickCodexScopePath/);
});
