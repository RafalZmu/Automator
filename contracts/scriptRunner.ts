export type ScriptRunnerInterpreter = 'python' | 'bash' | 'powershell';

export type ScriptFileFilter = {
  name: string;
  extensions: string[];
};

export function isScriptRunnerInterpreter(value: unknown): value is ScriptRunnerInterpreter {
  return value === 'python' || value === 'bash' || value === 'powershell';
}

export function scriptFileFilter(interpreter: ScriptRunnerInterpreter): ScriptFileFilter {
  switch (interpreter) {
    case 'python': return { name: 'Python scripts', extensions: ['py'] };
    case 'bash': return { name: 'Bash scripts', extensions: ['sh'] };
    case 'powershell': return { name: 'PowerShell scripts', extensions: ['ps1'] };
  }
}

export type ScriptTemplateValue = string | boolean;
type ScriptTemplateParameterBase = {
  key: string;
  label: string;
  description?: string;
  required: boolean;
  argumentIndex: number;
};
export type ScriptTemplateParameter =
  | (ScriptTemplateParameterBase & { type: 'text'; sensitive?: boolean; defaultValue?: string; options?: never })
  | (ScriptTemplateParameterBase & { type: 'file' | 'directory'; sensitive?: false; defaultValue?: string; options?: never })
  | (ScriptTemplateParameterBase & { type: 'boolean'; sensitive?: false; defaultValue?: boolean; options?: never })
  | (ScriptTemplateParameterBase & { type: 'choice'; sensitive?: false; defaultValue?: string; options: string[] });
export type ScriptTemplateDescriptor = {
  id: string;
  version: number;
  name: string;
  description: string;
  tags: string[];
  interpreter: ScriptRunnerInterpreter;
  assetId: string;
  outputMode: 'text' | 'json';
  timeoutSeconds: number;
  parameters: ScriptTemplateParameter[];
};

const templateIdPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;
const parameterKeyPattern = /^[a-z][a-z0-9_]{0,63}$/;
const maxArgumentLength = 8192;

function object(value: unknown): Record<string, unknown> {
  if (value === null || typeof value !== 'object' || Array.isArray(value)) throw new Error('Expected an object.');
  return value as Record<string, unknown>;
}

function bounded(value: unknown, name: string, max: number): string {
  if (typeof value !== 'string' || !value.trim() || value.length > max) throw new Error(`${name} is invalid.`);
  return value;
}

function validateParameter(value: unknown, index: number): ScriptTemplateParameter {
  const item = object(value);
  const key = bounded(item.key, 'Parameter key', 64);
  if (!parameterKeyPattern.test(key)) throw new Error('Parameter key is invalid.');
  const label = bounded(item.label, 'Parameter label', 128);
  const description = item.description == null ? undefined : bounded(item.description, 'Parameter description', 512);
  const type = item.type;
  if (type !== 'text' && type !== 'file' && type !== 'directory' && type !== 'boolean' && type !== 'choice') throw new Error('Parameter type is unsupported.');
  if (typeof item.required !== 'boolean') throw new Error('Parameter required flag is invalid.');
  if (item.sensitive != null && typeof item.sensitive !== 'boolean') throw new Error('Parameter sensitive flag is invalid.');
  if (item.sensitive && type !== 'text') throw new Error('Only text parameters may be sensitive.');
  if (!Number.isInteger(item.argumentIndex) || item.argumentIndex !== index || index > 62) throw new Error('Parameter argument mapping is invalid.');
  let options: string[] | undefined;
  if (type === 'choice') {
    if (!Array.isArray(item.options) || item.options.length < 1 || item.options.length > 32) throw new Error('Choice options are invalid.');
    options = item.options.map(option => bounded(option, 'Choice option', 128));
    if (new Set(options).size !== options.length) throw new Error('Choice options must be unique.');
  } else if (item.options != null) throw new Error('Only choice parameters may have options.');
  let defaultValue: ScriptTemplateValue | undefined;
  if (item.defaultValue != null) {
    if (item.sensitive) throw new Error('Sensitive parameters cannot have catalog defaults.');
    if (type === 'boolean') {
      if (typeof item.defaultValue !== 'boolean') throw new Error('Boolean default is invalid.');
      defaultValue = item.defaultValue;
    } else {
      defaultValue = bounded(item.defaultValue, 'Parameter default', maxArgumentLength);
      if (type === 'choice' && !options!.includes(defaultValue)) throw new Error('Choice default is invalid.');
    }
  }
  const base = { key, label, description, required: item.required, argumentIndex: index };
  if (type === 'choice') return { ...base, type, options: options!, defaultValue: defaultValue as string | undefined };
  if (type === 'boolean') return { ...base, type, defaultValue: defaultValue as boolean | undefined };
  if (type === 'text') return { ...base, type, sensitive: item.sensitive as boolean | undefined, defaultValue: defaultValue as string | undefined };
  return { ...base, type, defaultValue: defaultValue as string | undefined };
}

export function parseScriptTemplateCatalog(value: unknown): ScriptTemplateDescriptor[] {
  if (!Array.isArray(value) || value.length > 64) throw new Error('Template catalog is invalid.');
  const ids = new Set<string>();
  return value.map(raw => {
    const item = object(raw);
    const id = bounded(item.id, 'Template ID', 64);
    if (!templateIdPattern.test(id) || ids.has(id)) throw new Error('Template ID is invalid or duplicated.');
    ids.add(id);
    if (!Number.isSafeInteger(item.version) || (item.version as number) < 1) throw new Error('Template version is invalid.');
    const name = bounded(item.name, 'Template name', 128);
    const description = bounded(item.description, 'Template description', 1024);
    if (!Array.isArray(item.tags) || item.tags.length > 16) throw new Error('Template tags are invalid.');
    const tags = item.tags.map(tag => bounded(tag, 'Template tag', 64));
    if (new Set(tags).size !== tags.length) throw new Error('Template tags must be unique.');
    if (!isScriptRunnerInterpreter(item.interpreter)) throw new Error('Template interpreter is invalid.');
    const assetId = bounded(item.assetId, 'Template asset ID', 64);
    if (!templateIdPattern.test(assetId)) throw new Error('Template asset ID is invalid.');
    if (item.outputMode !== 'text' && item.outputMode !== 'json') throw new Error('Template output mode is invalid.');
    if (!Number.isInteger(item.timeoutSeconds) || (item.timeoutSeconds as number) < 1 || (item.timeoutSeconds as number) > 3600) throw new Error('Template timeout is invalid.');
    if (!Array.isArray(item.parameters) || item.parameters.length > 63) throw new Error('Template parameters are invalid.');
    const parameters = item.parameters.map(validateParameter);
    if (new Set(parameters.map(parameter => parameter.key)).size !== parameters.length) throw new Error('Parameter keys must be unique.');
    return { id, version: item.version as number, name, description, tags, interpreter: item.interpreter, assetId,
      outputMode: item.outputMode, timeoutSeconds: item.timeoutSeconds as number, parameters };
  });
}

export function validateScriptTemplateValues(template: ScriptTemplateDescriptor, value: unknown): Record<string, ScriptTemplateValue> {
  const input = object(value);
  const keys = new Set(template.parameters.map(parameter => parameter.key));
  for (const key of Object.keys(input)) if (!keys.has(key)) throw new Error(`Unknown template input: ${key}.`);
  const result: Record<string, ScriptTemplateValue> = {};
  for (const parameter of template.parameters) {
    const raw = input[parameter.key] ?? parameter.defaultValue;
    if (raw === undefined) {
      if (parameter.required) throw new Error(`${parameter.label} is required.`);
      continue;
    }
    if (parameter.type === 'boolean') {
      if (typeof raw !== 'boolean') throw new Error(`${parameter.label} must be a boolean.`);
    } else {
      if (typeof raw !== 'string' || raw.length > maxArgumentLength || (parameter.required && !raw.trim())) throw new Error(`${parameter.label} is invalid.`);
      if (parameter.type === 'choice' && !parameter.options?.includes(raw)) throw new Error(`${parameter.label} has an invalid choice.`);
    }
    result[parameter.key] = raw;
  }
  return result;
}
