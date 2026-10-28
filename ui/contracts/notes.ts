export type Note = { id: string; title: string; body: string };

export const MAX_NOTES = 50;
export const MAX_NOTE_TITLE_LENGTH = 128;
export const MAX_NOTE_BODY_LENGTH = 3000;

const idPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function validTitle(value: unknown): value is string {
  return typeof value === 'string' && value.trim().length > 0 && value.length <= MAX_NOTE_TITLE_LENGTH && !/[\x00-\x1f\x7f]/.test(value);
}

/** A note body is markdown, so it may span lines and contain tabs, but no other control characters. It may be empty. */
function validBody(value: unknown): value is string {
  return typeof value === 'string' && value.length <= MAX_NOTE_BODY_LENGTH && !/[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]/.test(value);
}

/** Checks a note before it is sent to the host, which validates it again before storing it. */
export function validateNote(value: unknown): string[] {
  if (!isRecord(value) || typeof value.id !== 'string' || !idPattern.test(value.id)) return ['The note needs a valid key.'];
  const errors: string[] = [];
  if (!validTitle(value.title)) errors.push(`The note needs a title of at most ${MAX_NOTE_TITLE_LENGTH} characters.`);
  if (!validBody(value.body)) errors.push(`The note text can have at most ${MAX_NOTE_BODY_LENGTH} characters.`);
  return errors;
}

/** Reads the `listNotes` result data. Invalid or repeated entries are skipped so one bad record never hides the others. */
export function readNotes(data: unknown): Note[] {
  if (!isRecord(data) || !Array.isArray(data.notes)) return [];
  const ids = new Set<string>();
  const notes: Note[] = [];
  for (const item of data.notes) {
    if (!isRecord(item) || validateNote(item).length || ids.has(item.id as string) || notes.length >= MAX_NOTES) continue;
    ids.add(item.id as string);
    notes.push({ id: item.id as string, title: item.title as string, body: item.body as string });
  }
  return notes;
}
