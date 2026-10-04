export type VariableJsonValue = null | boolean | number | string | VariableJsonValue[] | { [key: string]: VariableJsonValue };
export type GlobalVariableSnapshot = { version: 1; values: Record<string, VariableJsonValue>; migrationConflicts: string[] };
export type GlobalVariableEntry = { name: string; json: string };
const reserved = new Set(['input', 'variables', '__proto__', 'constructor', 'prototype']);
function validateValue(value: unknown, depth = 0): void {
  if (depth > 63) throw new Error('Variable JSON nesting exceeds the 64-level limit.');
  if (typeof value === 'number' && !Number.isFinite(value)) throw new Error('Variable JSON numbers must be finite.');
  if (value && typeof value === 'object') for (const child of Object.values(value)) validateValue(child, depth + 1);
}
export function parseGlobalVariables(entries: readonly GlobalVariableEntry[]): Record<string, VariableJsonValue> {
  if (entries.length > 256) throw new Error('You can define at most 256 variables.');
  const values = Object.create(null) as Record<string, VariableJsonValue>;
  for (const entry of entries) {
    const name = entry.name.trim();
    if (!/^[A-Za-z_][A-Za-z0-9_.-]{0,191}$/.test(name) || reserved.has(name) || /^(secret|system)\./i.test(name))
      throw new Error('Names must start with a letter or underscore; use letters, numbers, dots, dashes or underscores. System and secret names are reserved.');
    if (Object.hasOwn(values, name)) throw new Error(`Duplicate variable: ${name}.`);
    try { values[name] = JSON.parse(entry.json) as VariableJsonValue; }
    catch { throw new Error(`Enter valid JSON for ${name}. Strings need quotes; lists use square brackets.`); }
    validateValue(values[name]);
  }
  if (new TextEncoder().encode(JSON.stringify(values)).length > 64 * 1024) throw new Error('Variables exceed the 64 KiB limit.');
  return values;
}
