export type Snippet = { id: string; name: string; alias: string; value: string };
export type SnippetsSettings = { snippets: Snippet[] };

export const MAX_SNIPPETS = 100;
export const MAX_SNIPPET_VALUE_LENGTH = 4000;
const MAX_SETTINGS_BYTES = 64 * 1024;

const idPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;
const aliasPattern = /^[a-z0-9]{1,32}$/i;

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function validName(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0 && value.length <= 128 && !/[\x00-\x1f\x7f]/.test(value);
}

/** A snippet is copied verbatim, so it may span lines and contain tabs, but no other control characters. */
function validValue(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0 && value.length <= MAX_SNIPPET_VALUE_LENGTH
    && !/[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]/.test(value);
}

/** An empty alias means the snippet is only copied with its button. */
export function isValidSnippetAlias(alias: string): boolean {
  return alias === '' || aliasPattern.test(alias);
}

export function validateSnippetsSettings(value: unknown): string[] {
  if (!isRecord(value) || !Array.isArray(value.snippets) || value.snippets.length > MAX_SNIPPETS)
    return [`Snippets must be a list of at most ${MAX_SNIPPETS} entries.`];
  try {
    if (new TextEncoder().encode(JSON.stringify(value)).byteLength > MAX_SETTINGS_BYTES)
      return ['Snippet settings exceed the 64 KiB limit.'];
  } catch {
    return ['Snippet settings are invalid.'];
  }
  const errors: string[] = [];
  const ids = new Set<string>();
  const aliases = new Set<string>();
  for (const [index, snippet] of value.snippets.entries()) {
    const path = `Snippet ${index + 1}`;
    if (!isRecord(snippet) || typeof snippet.id !== 'string' || !idPattern.test(snippet.id) || ids.has(snippet.id)) {
      errors.push(`${path} needs a unique, valid key.`);
      continue;
    }
    ids.add(snippet.id);
    if (!validName(snippet.name)) errors.push(`${path} needs a name of at most 128 characters.`);
    if (typeof snippet.alias !== 'string' || !isValidSnippetAlias(snippet.alias))
      errors.push(`${path} needs a shortcut of 1 to 32 letters or digits, or none.`);
    else if (snippet.alias) {
      const alias = snippet.alias.toLocaleLowerCase();
      if (aliases.has(alias)) errors.push(`${path} has a shortcut already used by another snippet.`);
      aliases.add(alias);
    }
    if (!validValue(snippet.value)) errors.push(`${path} needs text to copy, at most ${MAX_SNIPPET_VALUE_LENGTH} characters.`);
  }
  return errors;
}

export function parseSnippetsSettings(value: unknown): SnippetsSettings {
  const errors = validateSnippetsSettings(value);
  if (errors.length || !isRecord(value) || !Array.isArray(value.snippets)) throw new Error(errors[0] ?? 'Snippet settings are invalid.');
  return structuredClone(value) as SnippetsSettings;
}

export function readSnippetsSettings(value: unknown): SnippetsSettings {
  try { return parseSnippetsSettings(value); }
  catch { return { snippets: [] }; }
}
