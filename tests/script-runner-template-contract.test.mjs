import assert from 'node:assert/strict';
import { test } from 'node:test';
import { parseScriptTemplateCatalog, validateScriptTemplateValues } from '../contracts/scriptRunner.ts';

const firebird = {
  id: 'firebird-3-backup-zip', version: 1, name: 'Firebird 3 backup and ZIP',
  description: 'Back up a Firebird database and compress the backup.', tags: ['database', 'backup'],
  interpreter: 'powershell', assetId: 'firebird-3-backup-zip', outputMode: 'text', timeoutSeconds: 3600,
  parameters: [
    { key: 'database', label: 'Target database', type: 'file', required: true, argumentIndex: 0 },
    { key: 'destination', label: 'ZIP destination', type: 'file', required: true, argumentIndex: 1 },
    { key: 'username', label: 'Username', type: 'text', required: true, argumentIndex: 2 },
    { key: 'password', label: 'Password', type: 'text', required: true, sensitive: true, argumentIndex: 3 },
    { key: 'overwrite', label: 'Overwrite output', type: 'boolean', required: false, defaultValue: false, argumentIndex: 4 },
    { key: 'compression', label: 'Compression', type: 'choice', required: true, options: ['fast', 'optimal'], defaultValue: 'optimal', argumentIndex: 5 },
  ],
};

test('Script template contract accepts a Firebird descriptor and validates transient values', () => {
  const [template] = parseScriptTemplateCatalog([firebird]);
  assert.equal(template.id, firebird.id);
  assert.deepEqual(validateScriptTemplateValues(template, {
    database: 'C:\\Data Files\\live.fdb', destination: 'D:\\Backups\\live.zip',
    username: 'SYSDBA', password: 'masterkey', compression: 'fast',
  }), {
    database: 'C:\\Data Files\\live.fdb', destination: 'D:\\Backups\\live.zip',
    username: 'SYSDBA', password: 'masterkey', overwrite: false, compression: 'fast',
  });
  assert.throws(() => validateScriptTemplateValues(template, { database: '', destination: 'x', username: 'u', password: 'p', compression: 'fast' }));
  assert.throws(() => validateScriptTemplateValues(template, { database: 'x', destination: 'y', username: 'u', password: 'p', compression: 'unknown' }));
  assert.throws(() => validateScriptTemplateValues(template, { database: 'x', destination: 'y', username: 'u', password: 'p', compression: 'fast', scriptPath: 'C:\\evil.ps1' }));
  assert.throws(() => validateScriptTemplateValues(template, { database: 'x'.repeat(8193), destination: 'y', username: 'u', password: 'p', compression: 'fast' }));
});

test('Script template contract rejects duplicate IDs, keys, argument mappings, and malformed inputs', () => {
  assert.throws(() => parseScriptTemplateCatalog([firebird, firebird]));
  for (const change of [
    { parameters: firebird.parameters.map((p, i) => i === 1 ? { ...p, key: firebird.parameters[0].key } : p) },
    { parameters: firebird.parameters.map((p, i) => i === 1 ? { ...p, argumentIndex: 0 } : p) },
    { parameters: firebird.parameters.map((p, i) => i === 1 ? { ...p, argumentIndex: 63 } : p) },
    { parameters: firebird.parameters.map((p, i) => i === 1 ? { ...p, type: 'executable' } : p) },
    { parameters: firebird.parameters.map((p, i) => i === 1 ? { ...p, label: 'x'.repeat(129) } : p) },
    { parameters: firebird.parameters.map((p, i) => i === 3 ? { ...p, defaultValue: 'masterkey' } : p) },
  ]) assert.throws(() => parseScriptTemplateCatalog([{ ...firebird, ...change }]));
  assert.throws(() => parseScriptTemplateCatalog([{ ...firebird, assetId: '../other.ps1' }]));
});

test('Explorer mapping contract normalizes extensions and rejects unsafe or malformed mappings', async () => {
  const { parseExplorerActions } = await import('../contracts/scriptRunner.ts');
  const action = { id: 'backup', profileId: 'saved-backup', label: 'Backup', extensions: ['.FDB', '.fdb'], fileParameterKey: 'database' };
  assert.deepEqual(parseExplorerActions([action]), [{ ...action, extensions: ['.fdb'] }]);
  for (const change of [{ id: '../evil' }, { profileId: '' }, { label: 'x\ncommand' }, { extensions: ['.*'] }, { extensions: ['fdb'] }, { fileParameterKey: '../path' }])
    assert.throws(() => parseExplorerActions([{ ...action, ...change }]));
  assert.throws(() => parseExplorerActions([{ ...action, id: 'register-powershell-script' }]));
  assert.throws(() => parseExplorerActions([action, action]));
});

test('PowerShell Explorer registration creates a unique prefilled profile draft', async () => {
  const { createPowerShellProfileDraft } = await import('../ui/modules/scriptTemplateViewModel.ts');
  const draft = createPowerShellProfileDraft('C:\\Tools\\Zażółć report.ps1', 'C:\\PowerShell\\pwsh.exe', ['script-za-report']);
  assert.deepEqual(draft, {
    id: 'script-za-report-2', name: 'Zażółć report', interpreter: 'powershell', interpreterPath: 'C:\\PowerShell\\pwsh.exe',
    scriptPath: 'C:\\Tools\\Zażółć report.ps1', arguments: [], workingDirectory: 'C:\\Tools', outputMode: 'text', timeoutSeconds: 60,
  });
  assert.throws(() => createPowerShellProfileDraft('relative.ps1', 'C:\\PowerShell\\pwsh.exe', []));
  assert.throws(() => createPowerShellProfileDraft('C:\\Tools\\not-a-script.txt', 'C:\\PowerShell\\pwsh.exe', []));
});
