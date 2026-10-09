import type { ScriptTemplateDescriptor, ScriptTemplateValue } from '../../contracts/scriptRunner';

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
