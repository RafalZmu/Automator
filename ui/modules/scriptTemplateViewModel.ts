import type { ScriptRunnerInterpreter, ScriptTemplateDescriptor, ScriptTemplateValue } from '../../contracts/scriptRunner';

export type ScriptProfile = {
  id: string;
  name: string;
  interpreter: ScriptRunnerInterpreter;
  interpreterPath: string;
  scriptPath: string;
  arguments: string[];
  workingDirectory: string;
  outputMode: 'text' | 'json';
  timeoutSeconds: number;
  templateOrigin?: { id: string; version: number } | null;
};

export function createPowerShellProfileDraft(filePath: string, interpreterPath: string, existingProfileIds: Iterable<string>): ScriptProfile {
  if (filePath.length > 4096 || /[\x00-\x1f]/.test(filePath)
      || !/^(?:[A-Za-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+(?:[\\/]|$))/.test(filePath)
      || !/\.ps1$/i.test(filePath)) throw new Error('Register in Automator requires an absolute .ps1 file path.');
  const separator = Math.max(filePath.lastIndexOf('\\'), filePath.lastIndexOf('/'));
  const fileName = filePath.slice(separator + 1);
  const name = fileName.replace(/\.ps1$/i, '').slice(0, 128) || 'PowerShell script';
  const slug = name.toLowerCase().replace(/[^a-z0-9._-]+/g, '-').replace(/-+/g, '-').replace(/^[^a-z0-9]+/, '').slice(0, 56) || 'powershell';
  const baseId = `script-${slug}`;
  const usedIds = new Set(existingProfileIds);
  let id = baseId;
  for (let suffix = 2; usedIds.has(id); suffix++) id = `${baseId.slice(0, 63 - String(suffix).length)}-${suffix}`;
  const workingDirectory = /^[A-Za-z]:[\\/]/.test(filePath) && separator === 2
    ? filePath.slice(0, 3)
    : filePath.slice(0, separator);
  return {
    id,
    name,
    interpreter: 'powershell',
    interpreterPath,
    scriptPath: filePath,
    arguments: [],
    workingDirectory,
    outputMode: 'text',
    timeoutSeconds: 60,
  };
}

export function filterScriptTemplates<T extends Pick<ScriptTemplateDescriptor, 'name' | 'description' | 'tags'>>(
  templates: T[], query: string,
): T[] {
  const tokens = query.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);
  if (!tokens.length) return templates;
  return templates.filter((template) => {
    const searchable = `${template.name} ${template.description} ${template.tags.join(' ')}`.toLocaleLowerCase();
    return tokens.every((token) => searchable.includes(token));
  });
}

export function initialTemplateValues(template: ScriptTemplateDescriptor): Record<string, ScriptTemplateValue> {
  const result: Record<string, ScriptTemplateValue> = {};
  for (const parameter of template.parameters) {
    if (parameter.defaultValue !== undefined && !parameter.sensitive) result[parameter.key] = parameter.defaultValue;
    else if (parameter.sensitive && parameter.key === 'password' && (template.id === 'firebird-3-backup-zip' || template.id === 'firebird-3-backup')) result[parameter.key] = 'masterkey';
    else if (parameter.key === 'username' && (template.id === 'firebird-3-backup-zip' || template.id === 'firebird-3-backup')) result[parameter.key] = 'SYSDBA';
    else if (parameter.type === 'boolean') result[parameter.key] = false;
    else result[parameter.key] = '';
  }
  return result;
}

export function templateDetailParameters(template: ScriptTemplateDescriptor): ScriptTemplateDescriptor['parameters'] {
  return (template.id === 'firebird-3-backup-zip' || template.id === 'firebird-3-backup')
    ? template.parameters.filter((parameter) => parameter.key !== 'database')
    : template.parameters;
}

export function resetTemplateValues(
  template: ScriptTemplateDescriptor,
  current: Record<string, ScriptTemplateValue>,
): Record<string, ScriptTemplateValue> {
  const result = initialTemplateValues(template);
  if ((template.id === 'firebird-3-backup-zip' || template.id === 'firebird-3-backup') && typeof current.database === 'string') result.database = current.database;
  return result;
}

export function clearSensitiveTemplateValues(
  template: ScriptTemplateDescriptor,
  values: Record<string, ScriptTemplateValue>,
): Record<string, ScriptTemplateValue> {
  const result = { ...values };
  for (const parameter of template.parameters) if (parameter.sensitive) result[parameter.key] = '';
  return result;
}

export function updateScriptPath<T extends { scriptPath: string; templateOrigin?: { id: string; version: number } | null }>(
  profile: T,
  scriptPath: string,
): T {
  return profile.scriptPath !== scriptPath && profile.templateOrigin
    ? { ...profile, scriptPath, templateOrigin: null }
    : { ...profile, scriptPath };
}
