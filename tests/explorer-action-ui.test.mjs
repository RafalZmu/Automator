import assert from 'node:assert/strict';
import { test } from 'node:test';
import { parseExplorerActions } from '../contracts/scriptRunner.ts';
import { normalizeExplorerExtensions, prepareExplorerForm, registrationNotice } from '../ui/modules/explorerActionViewModel.ts';
const profile = { id: 'profile', name: 'Backup', arguments: ['--source', '{{file.path}}', '--output={{file.path}}.zip'] };
const action = { id: 'backup', profileId: 'profile', label: 'Backup', extensions: ['.fdb'] };
const request = { actionId: 'backup', filePath: 'C:\\Bazy danych\\żółć.fdb' };
test('Explorer mapping extensions normalize without accepting malformed file patterns', () => {
  assert.deepEqual(normalizeExplorerExtensions('FDB, .TXT; fdb'), ['.fdb', '.txt']);
  for (const value of ['', '*', 'foo.bar', '*.fdb', 'file/name']) assert.throws(() => normalizeExplorerExtensions(value));
});
test('Explorer launch prepares transient arguments and never mutates the saved profile', () => {
  const form = prepareExplorerForm(request, [action], [profile], []);
  assert.deepEqual(form.arguments, ['--source', request.filePath, `--output=${request.filePath}.zip`]);
  form.arguments[0] = 'edited';
  assert.equal(profile.arguments[0], '--source');
  assert.equal(profile.arguments[1], '{{file.path}}');
});
test('Explorer launch reports stale mappings profiles extensions and invalid absolute paths', () => {
  assert.throws(() => prepareExplorerForm(request, [], [profile], []), /action no longer exists/);
  assert.throws(() => prepareExplorerForm(request, [action], [], []), /profile no longer exists/);
  assert.throws(() => prepareExplorerForm({ ...request, filePath: 'relative.fdb' }, [action], [profile], []), /absolute/);
  assert.throws(() => prepareExplorerForm({ ...request, filePath: 'C:\\a.txt' }, [action], [profile], []), /extension/);
  assert.throws(() => prepareExplorerForm(request, [action], [{ ...profile, arguments: ['missing token'] }], []), /file.path/);
});
test('Explorer templates prefill only the mapped input and create fresh typed values', () => {
  const template = { id: 'test', version: 1, parameters: [{ key: 'input', type: 'file', required: true }, { key: 'password', type: 'text', sensitive: true, required: true }, { key: 'verbose', type: 'boolean', defaultValue: true }] };
  const mapped = { ...action, fileParameterKey: 'input' };
  const saved = { ...profile, templateOrigin: { id: 'test', version: 1 } };
  const form = prepareExplorerForm(request, [mapped], [saved], [template]);
  assert.deepEqual(form.values, { input: request.filePath, password: '', verbose: true });
  assert.equal(form.template, template);
  assert.throws(() => prepareExplorerForm(request, [{ ...mapped, fileParameterKey: 'password' }], [saved], [template]), /file or directory/);
});
test('Explorer registration warnings retain saved mappings and describe disabled hosts', () => {
  assert.match(registrationNotice({ state: 'disabled', message: 'Installed build only.' }), /Installed build only/);
  assert.match(registrationNotice({ state: 'error', message: 'Try again.' }), /Try again/);
  assert.equal(registrationNotice({ state: 'updated' }), '');
});

test('Explorer folder actions allow an empty extension list and skip the extension match', () => {
  assert.deepEqual(normalizeExplorerExtensions('', true), []);
  assert.throws(() => normalizeExplorerExtensions('', false));
  const folderAction = { id: 'folder', profileId: 'profile', label: 'Folder', extensions: [], folders: true };
  const form = prepareExplorerForm({ actionId: 'folder', filePath: 'C:\\Dane\\Projekt' }, [folderAction], [profile], []);
  assert.deepEqual(form.arguments, ['--source', 'C:\\Dane\\Projekt', '--output=C:\\Dane\\Projekt.zip']);
  assert.throws(() => prepareExplorerForm({ actionId: 'backup', filePath: 'C:\\Dane\\Projekt' }, [action], [profile], []));
  assert.deepEqual(parseExplorerActions([folderAction])[0].folders, true);
  assert.throws(() => parseExplorerActions([{ ...folderAction, folders: undefined }]));
});
