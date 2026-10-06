export type PathDropResult = { path: string | null; error: string | null };

export function applyPathInputChange(value: string, onChange: (value: string) => void): void {
  onChange(value);
}

export async function browseAndApplyPath(
  browse: () => Promise<string | null>,
  onChange: (value: string) => void,
): Promise<string | null> {
  const path = await browse();
  if (path) onChange(path);
  return path;
}

/** Applies the one-item-only policy and resolves an Electron File without reading its contents. */
export async function resolvePathDrop(
  items: ArrayLike<File>,
  resolveFile: (file: File) => Promise<string>,
): Promise<PathDropResult> {
  if (!items || items.length === 0) return { path: null, error: 'Drop one file or folder here.' };
  if (items.length !== 1) return { path: null, error: 'Drop one item at a time.' };
  const file = items[0];
  if (!file || typeof file.name !== 'string' || typeof file.size !== 'number')
    return { path: null, error: 'This item cannot be used as a filesystem path.' };
  try {
    const path = await resolveFile(file);
    return path ? { path, error: null } : { path: null, error: 'This item has no local filesystem path. Try Browse or paste a path.' };
  } catch {
    return { path: null, error: 'This item has no local filesystem path. Try Browse or paste a path.' };
  }
}
