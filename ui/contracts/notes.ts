export type Note = { id: string; title: string; body: string };
export type NotesSettings = { notes: Note[] };

export const MAX_NOTES = 100;
export const MAX_NOTE_BODY_LENGTH = 8000;
const MAX_SETTINGS_BYTES = 64 * 1024;

const idPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function validTitle(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0 && value.length <= 128 && !/[\x00-\x1f\x7f]/.test(value);
}

/** A note body is markdown, so it may span lines and contain tabs, but no other control characters. It may be empty. */
function validBody(value: unknown): value is string {
  return typeof value === 'string' && value.length <= MAX_NOTE_BODY_LENGTH && !/[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]/.test(value);
}

export function validateNotesSettings(value: unknown): string[] {
  if (!isRecord(value) || !Array.isArray(value.notes) || value.notes.length > MAX_NOTES)
    return [`Notes must be a list of at most ${MAX_NOTES} entries.`];
  try {
    if (new TextEncoder().encode(JSON.stringify(value)).byteLength > MAX_SETTINGS_BYTES)
      return ['Notes exceed the 64 KiB limit.'];
  } catch {
    return ['Notes are invalid.'];
  }
  const errors: string[] = [];
  const ids = new Set<string>();
  for (const [index, note] of value.notes.entries()) {
    const path = `Note ${index + 1}`;
    if (!isRecord(note) || typeof note.id !== 'string' || !idPattern.test(note.id) || ids.has(note.id)) {
      errors.push(`${path} needs a unique, valid key.`);
      continue;
    }
    ids.add(note.id);
    if (!validTitle(note.title)) errors.push(`${path} needs a title of at most 128 characters.`);
    if (!validBody(note.body)) errors.push(`${path} needs text of at most ${MAX_NOTE_BODY_LENGTH} characters.`);
  }
  return errors;
}

export function parseNotesSettings(value: unknown): NotesSettings {
  const errors = validateNotesSettings(value);
  if (errors.length || !isRecord(value) || !Array.isArray(value.notes)) throw new Error(errors[0] ?? 'Notes are invalid.');
  return structuredClone(value) as NotesSettings;
}

export function readNotesSettings(value: unknown): NotesSettings {
  try { return parseNotesSettings(value); }
  catch { return { notes: [] }; }
}
