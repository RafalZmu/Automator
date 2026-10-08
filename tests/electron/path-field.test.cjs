const assert = require('node:assert/strict');
const { test } = require('node:test');
const { pathToFileURL } = require('node:url');
const path = require('node:path');
const fs = require('node:fs/promises');

const root = path.resolve(__dirname, '../..');
const modelUrl = pathToFileURL(path.join(root, 'ui/components/pathFieldModel.ts')).href;
const pickerUrl = pathToFileURL(path.join(root, 'electron/scriptRunnerPathPicker.ts')).href;

test('path drop accepts one File and resolves its path without reading contents', async () => {
  const { resolvePathDrop } = await import(modelUrl);
  const file = { name: 'db backup.fdb', size: 0 };
  let resolved;
  const result = await resolvePathDrop([file], async (item) => { resolved = item; return 'C:\\Data\\db backup.fdb'; });
  assert.equal(resolved, file);
  assert.deepEqual(result, { path: 'C:\\Data\\db backup.fdb', error: null });
});

test('editable path input preserves pasted text and Browse applies the selected path', async () => {
  const { applyPathInputChange, browseAndApplyPath } = await import(modelUrl);
  const changes = [];
  const pastedPath = 'C:\\Database Backups\\quarter 1.fdb';
  applyPathInputChange(pastedPath, (value) => changes.push(value));
  assert.deepEqual(changes, [pastedPath]);
  let browseCalls = 0;
  await browseAndApplyPath(async () => { browseCalls += 1; return 'C:\\Data\\picked.fdb'; }, (value) => changes.push(value));
  assert.equal(browseCalls, 1);
  assert.deepEqual(changes, [pastedPath, 'C:\\Data\\picked.fdb']);
  await browseAndApplyPath(async () => null, (value) => changes.push(value));
  assert.equal(changes.length, 2, 'canceling Browse leaves the current value unchanged');
});

test('Script Runner uses file and directory PathFields with the matching picker kind', async () => {
  const source = await fs.readFile(path.join(root, 'ui/modules/ScriptRunnerView.tsx'), 'utf8');
  assert.match(source, /<PathField[^>]*label="Script file" kind="file"/);
  assert.match(source, /<PathField[^>]*label="Working directory" kind="directory"/);
  assert.match(source, /parameter\.type === 'file' \|\| parameter\.type === 'directory'/);
  assert.match(source, /kind=\{parameter\.type\}[^\n]*onBrowse=\{\(\) => services\.files\.pickPath\(parameter\.type\)\}/);
});

test('Firebird Target database PathField is rendered on its library card before Details or installation', async () => {
  const source = await fs.readFile(path.join(root, 'ui/modules/ScriptRunnerView.tsx'), 'utf8');
  assert.match(source, /template\.id === 'firebird-3-backup-zip'[\s\S]*?<PathField[^>]*label=\{`\$\{targetDatabaseParameter\.label\}/);
  assert.match(source, /value=\{String\(templateValues\[targetDatabaseParameter\.key\] \?\? ''\)\}/);
  assert.match(source, /onChange=\{\(value\) => setTemplateValues\(\(current\) => \(\{ \.\.\.current, \[targetDatabaseParameter\.key\]: value \}\)\)\}/);
});

test('path drop rejects multiple files, unsupported payloads, and browser-preview paths accessibly', async () => {
  const { resolvePathDrop } = await import(modelUrl);
  const file = { name: 'one.fdb', size: 1 };
  assert.equal((await resolvePathDrop([file, file], async () => 'path')).error, 'Drop one item at a time.');
  assert.equal((await resolvePathDrop([{ name: 4, size: 1 }], async () => 'path')).error, 'This item cannot be used as a filesystem path.');
  assert.match((await resolvePathDrop([file], async () => '')).error, /no local filesystem path/);
  assert.match((await resolvePathDrop([file], async () => { throw new Error('preview'); })).error, /Try Browse or paste a path/);
});

test('path field picker accepts only file or directory for active Script Runner and keeps dialog options host-owned', async () => {
  const { getScriptRunnerPathPickerSpec } = await import(pickerUrl);
  assert.deepEqual(getScriptRunnerPathPickerSpec('file', 2, true), {
    title: 'Select a file', properties: ['openFile'], filters: [{ name: 'All files', extensions: ['*'] }],
  });
  assert.deepEqual(getScriptRunnerPathPickerSpec('directory', 2, true), {
    title: 'Select a directory', properties: ['openDirectory', 'createDirectory'],
  });
  assert.throws(() => getScriptRunnerPathPickerSpec('save', 2, true), /kind is invalid/);
  assert.throws(() => getScriptRunnerPathPickerSpec('file', 1, true), /active Script Runner/);
  assert.throws(() => getScriptRunnerPathPickerSpec('file', 2, false), /active Script Runner/);
});
