import type { ScriptRunnerInterpreter, ScriptTemplateDescriptor, ScriptTemplateParameter, ScriptTemplateValue } from '../../contracts/scriptRunner';

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
  /** Typed parameters declared by a user script. Template profiles take theirs from the library. */
  parameters?: ScriptTemplateParameter[] | null;
  /** Non-sensitive values remembered for the Run form. */
  values?: Record<string, ScriptTemplateValue> | null;
};

const firebirdTemplateIds = new Set(['firebird-3-backup-zip', 'firebird-3-backup']);

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

/**
 * Prefills typed inputs: remembered values first, then declared defaults. Sensitive inputs are never remembered,
 * so they start empty except for the Firebird account defaults that the bundled templates document.
 */
export function initialParameterValues(
  parameters: ScriptTemplateParameter[],
  saved?: Record<string, ScriptTemplateValue> | null,
  templateId?: string,
): Record<string, ScriptTemplateValue> {
  const firebird = templateId !== undefined && firebirdTemplateIds.has(templateId);
  const result: Record<string, ScriptTemplateValue> = {};
  for (const parameter of parameters) {
    const remembered = parameter.sensitive ? undefined : saved?.[parameter.key];
    if (firebird && parameter.sensitive && parameter.key === 'password') result[parameter.key] = 'masterkey';
    else if (firebird && parameter.key === 'username' && remembered === undefined) result[parameter.key] = 'SYSDBA';
    else if (remembered !== undefined && valueMatchesParameter(parameter, remembered)) result[parameter.key] = remembered;
    else if (parameter.defaultValue !== undefined && !parameter.sensitive) result[parameter.key] = parameter.defaultValue;
    else if (parameter.type === 'boolean') result[parameter.key] = false;
    else result[parameter.key] = '';
  }
  return result;
}

export function initialTemplateValues(template: ScriptTemplateDescriptor): Record<string, ScriptTemplateValue> {
  return initialParameterValues(template.parameters, undefined, template.id);
}

/** The parameters a profile's Run form asks for. Template profiles use their catalog definition. */
export function profileParameters(profile: ScriptProfile, templates: ScriptTemplateDescriptor[]): ScriptTemplateParameter[] {
  if (profile.templateOrigin) {
    const template = templates.find((item) => item.id === profile.templateOrigin?.id && item.version === profile.templateOrigin.version);
    return template?.parameters ?? [];
  }
  return profile.parameters ?? [];
}

/**
 * Values that may be stored with a profile. Sensitive inputs are dropped, and the Firebird target database stays
 * transient as documented for those templates.
 */
export function rememberableValues(
  parameters: ScriptTemplateParameter[],
  values: Record<string, ScriptTemplateValue>,
  templateId?: string,
): Record<string, ScriptTemplateValue> {
  const result: Record<string, ScriptTemplateValue> = {};
  const transientKey = templateId !== undefined && firebirdTemplateIds.has(templateId) ? 'database' : undefined;
  for (const parameter of parameters) {
    const value = values[parameter.key];
    if (!parameter.sensitive && parameter.key !== transientKey && value !== undefined && valueMatchesParameter(parameter, value)) result[parameter.key] = value;
  }
  return result;
}

/** Clears sensitive inputs after a run so a password never stays in the form. */
export function clearSensitiveValues(
  parameters: ScriptTemplateParameter[],
  values: Record<string, ScriptTemplateValue>,
): Record<string, ScriptTemplateValue> {
  const result = { ...values };
  for (const parameter of parameters) if (parameter.sensitive) result[parameter.key] = '';
  return result;
}

/** Turns builder rows into the definition the backend accepts: trimmed text, options without blank lines, valid defaults. */
export function normalizeParametersForSave(parameters: ScriptTemplateParameter[]): ScriptTemplateParameter[] {
  return normalizeParameterPositions(parameters.map((parameter) => {
    const description = parameter.description?.trim() ? parameter.description.trim() : undefined;
    const example = parameter.example?.trim() ? parameter.example.trim() : undefined;
    const base = { key: parameter.key, label: parameter.label.trim(), required: parameter.required, argumentIndex: 0, description, example };
    if (parameter.type === 'choice') {
      const options = (parameter.options ?? []).map((option) => option.trim()).filter(Boolean);
      const defaultValue = typeof parameter.defaultValue === 'string' && options.includes(parameter.defaultValue) ? parameter.defaultValue : undefined;
      return { ...base, type: 'choice', options, defaultValue } as ScriptTemplateParameter;
    }
    if (parameter.type === 'boolean') return { ...base, type: 'boolean', defaultValue: parameter.defaultValue === true } as ScriptTemplateParameter;
    const defaultValue = typeof parameter.defaultValue === 'string' && parameter.defaultValue.trim() ? parameter.defaultValue : undefined;
    return { ...base, type: parameter.type, defaultValue } as ScriptTemplateParameter;
  }));
}

/** Changes a parameter's type and resets the fields that belong to the previous type. */
export function withParameterType(parameter: ScriptTemplateParameter, type: ScriptTemplateParameter['type']): ScriptTemplateParameter {
  const base = { key: parameter.key, label: parameter.label, description: parameter.description, example: parameter.example, required: parameter.required, argumentIndex: parameter.argumentIndex };
  if (type === 'choice') return { ...base, type, options: ['Option 1'] } as ScriptTemplateParameter;
  if (type === 'boolean') return { ...base, type, defaultValue: false } as ScriptTemplateParameter;
  return { ...base, type } as ScriptTemplateParameter;
}

function valueMatchesParameter(parameter: ScriptTemplateParameter, value: ScriptTemplateValue): boolean {
  return parameter.type === 'boolean' ? typeof value === 'boolean' : typeof value === 'string';
}

export function templateDetailParameters(template: ScriptTemplateDescriptor): ScriptTemplateDescriptor['parameters'] {
  return firebirdTemplateIds.has(template.id)
    ? template.parameters.filter((parameter) => parameter.key !== 'database')
    : template.parameters;
}

export function resetTemplateValues(
  template: ScriptTemplateDescriptor,
  current: Record<string, ScriptTemplateValue>,
): Record<string, ScriptTemplateValue> {
  const result = initialTemplateValues(template);
  if (firebirdTemplateIds.has(template.id) && typeof current.database === 'string') result.database = current.database;
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

/** Parameter definitions edited by the profile editor. Argument positions always follow list order. */
export function normalizeParameterPositions(parameters: ScriptTemplateParameter[]): ScriptTemplateParameter[] {
  return parameters.map((parameter, index) => ({ ...parameter, argumentIndex: index }) as ScriptTemplateParameter);
}

export function newParameterKey(existing: ScriptTemplateParameter[]): string {
  const taken = new Set(existing.map((parameter) => parameter.key));
  for (;;) {
    const key = `p_${Math.random().toString(36).slice(2, 8)}`;
    if (!taken.has(key)) return key;
  }
}

export function newParameter(existing: ScriptTemplateParameter[]): ScriptTemplateParameter {
  return { key: newParameterKey(existing), label: '', type: 'text', required: false, argumentIndex: existing.length };
}
