import assert from 'node:assert/strict';
import { test } from 'node:test';
import { filterScriptTemplates, initialTemplateValues, clearSensitiveTemplateValues, updateScriptPath } from '../ui/modules/scriptTemplateViewModel.ts';
import { parseScriptTemplateCatalog } from '../contracts/scriptRunner.ts';

const template = { id: 'firebird-3-backup-zip', name: 'Firebird 3 database backup and ZIP', description: 'Create an archive', tags: ['database'], parameters: [
  { key: 'database', type: 'file', required: true }, { key: 'username', type: 'text', required: true }, { key: 'password', type: 'text', sensitive: true, required: true },
  { key: 'verbose', type: 'boolean', defaultValue: true },
] };
test('library search matches name description and tags without selecting or executing', () => {
  for (const query of ['FIREBIRD', 'archive', 'database', 'firebird database']) assert.deepEqual(filterScriptTemplates([template], query), [template]);
  assert.deepEqual(filterScriptTemplates([template], 'unknown'), []);
});
test('Firebird defaults live in new forms only and secrets clear without mutating submitted values', () => {
  const values = initialTemplateValues(template);
  assert.deepEqual(values, { database: '', username: 'SYSDBA', password: 'masterkey', verbose: true });
  assert.equal(template.parameters[2].defaultValue, undefined);
  const cleared = clearSensitiveTemplateValues(template, values);
  assert.equal(cleared.password, '');
  assert.equal(values.password, 'masterkey');
  assert.equal(initialTemplateValues({ ...template, id: 'other' }).password, '');
});
test('catalog parser accepts the host JSON nulls used for optional parameter fields', () => {
  const parsed = parseScriptTemplateCatalog([{ id: 'example', version: 1, name: 'Example', description: 'A test', tags: [], interpreter: 'powershell', assetId: 'example', outputMode: 'text', timeoutSeconds: 10,
    parameters: [{ key: 'input', label: 'Input', type: 'file', required: true, argumentIndex: 0, description: null, sensitive: false, defaultValue: null, options: null }] }]);
  assert.equal(parsed[0].parameters[0].type, 'file');
});
test('editing the installed script path clears its template origin', () => {
  const profile = { scriptPath: 'C:\\Automator\\template.ps1', templateOrigin: { id: 'firebird-3-backup-zip', version: 1 } };
  assert.equal(updateScriptPath(profile, profile.scriptPath).templateOrigin?.id, 'firebird-3-backup-zip');
  assert.equal(updateScriptPath(profile, 'C:\\Scripts\\edited.ps1').templateOrigin, null);
});
