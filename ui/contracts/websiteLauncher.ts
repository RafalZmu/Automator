export type WebsiteLauncherSite = { id: string; name: string; url: string };
export type WebsiteLauncherGroup = { id: string; name: string; websites: WebsiteLauncherSite[] };
export type WebsiteLauncherRow = { id: string; name: string; alias: string; groups: WebsiteLauncherGroup[] };
export type WebsiteLauncherSettings = { rows: WebsiteLauncherRow[] };
export type WebsiteLauncherQuickAction = { id: string; name: string; alias: string };

const idPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;
const aliasPattern = /^[a-z]{1,32}$/i;

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function validText(value: unknown, maximum: number): value is string {
  return typeof value === 'string' && value.trim().length > 0 && value.length <= maximum && !/[\x00-\x1f\x7f]/.test(value);
}

function validUrl(value: unknown): value is string {
  if (typeof value !== 'string' || value.length > 2048) return false;
  try {
    const url = new URL(value);
    return (url.protocol === 'http:' || url.protocol === 'https:') && Boolean(url.hostname) && !url.username && !url.password;
  } catch {
    return false;
  }
}

export function validateWebsiteLauncherSettings(value: unknown): string[] {
  if (!isRecord(value) || !Array.isArray(value.rows) || value.rows.length > 64) return ['Website shortcuts must be a list of at most 64 rows.'];
  try {
    if (new TextEncoder().encode(JSON.stringify(value)).byteLength > 64 * 1024)
      return ['Website shortcut settings exceed the 64 KiB limit.'];
  } catch {
    return ['Website shortcut settings are invalid.'];
  }
  const errors: string[] = [];
  const ids = new Set<string>();
  const aliases = new Set<string>();
  for (const [rowIndex, row] of value.rows.entries()) {
    const path = `Shortcut ${rowIndex + 1}`;
    if (!isRecord(row) || typeof row.id !== 'string' || !idPattern.test(row.id) || ids.has(row.id)) {
      errors.push(`${path} needs a unique, valid key.`);
      continue;
    }
    ids.add(row.id);
    if (!validText(row.name, 128)) errors.push(`${path} needs a name of at most 128 characters.`);
    if (typeof row.alias !== 'string' || !aliasPattern.test(row.alias)) errors.push(`${path} needs a shortcut made of 1 to 32 letters.`);
    else {
      const alias = row.alias.toLocaleLowerCase();
      if (aliases.has(alias)) errors.push(`${path} has an alias already used by another website shortcut.`);
      aliases.add(alias);
    }
    if (!Array.isArray(row.groups) || row.groups.length < 1 || row.groups.length > 16) {
      errors.push(`${path} needs 1 to 16 browser groups.`);
      continue;
    }
    const groupIds = new Set<string>();
    for (const [groupIndex, group] of row.groups.entries()) {
      if (!isRecord(group) || typeof group.id !== 'string' || !idPattern.test(group.id) || groupIds.has(group.id)) {
        errors.push(`${path}, group ${groupIndex + 1} needs a unique, valid key.`);
        continue;
      }
      groupIds.add(group.id);
      if (!validText(group.name, 64)) errors.push(`${path}, group ${groupIndex + 1} needs a name of at most 64 characters.`);
      if (!Array.isArray(group.websites) || group.websites.length < 1 || group.websites.length > 32) {
        errors.push(`${path}, group ${groupIndex + 1} needs 1 to 32 websites.`);
        continue;
      }
      const websiteIds = new Set<string>();
      for (const [websiteIndex, website] of group.websites.entries()) {
        if (!isRecord(website) || typeof website.id !== 'string' || !idPattern.test(website.id) || websiteIds.has(website.id)) {
          errors.push(`${path}, group ${groupIndex + 1}, website ${websiteIndex + 1} needs a unique, valid key.`);
          continue;
        }
        websiteIds.add(website.id);
        if (!validText(website.name, 128)) errors.push(`${path}, group ${groupIndex + 1}, website ${websiteIndex + 1} needs a name.`);
        if (!validUrl(website.url)) errors.push(`${path}, group ${groupIndex + 1}, website ${websiteIndex + 1} needs an absolute HTTP or HTTPS URL.`);
      }
    }
  }
  return errors;
}

export function parseWebsiteLauncherSettings(value: unknown): WebsiteLauncherSettings {
  const errors = validateWebsiteLauncherSettings(value);
  if (errors.length || !isRecord(value) || !Array.isArray(value.rows)) throw new Error(errors[0] ?? 'Website shortcut settings are invalid.');
  return structuredClone(value) as WebsiteLauncherSettings;
}

export function readWebsiteLauncherSettings(value: unknown): WebsiteLauncherSettings {
  try { return parseWebsiteLauncherSettings(value); }
  catch { return { rows: [] }; }
}

export function readWebsiteLauncherQuickActions(value: unknown): WebsiteLauncherQuickAction[] {
  let parsed = value;
  if (typeof parsed === 'string') {
    try { parsed = JSON.parse(parsed); }
    catch { return []; }
  }
  if (!Array.isArray(parsed)) return [];
  return parsed.slice(0, 64).flatMap((item) => isRecord(item)
    && typeof item.id === 'string' && idPattern.test(item.id)
    && validText(item.name, 128)
    && typeof item.alias === 'string' && aliasPattern.test(item.alias)
    ? [{ id: item.id, name: item.name, alias: item.alias.toLocaleLowerCase() }]
    : []);
}
