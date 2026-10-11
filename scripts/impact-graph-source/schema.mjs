import { createHash } from 'node:crypto';
import path from 'node:path';

export const sourceIndexShape = Object.freeze({
  schemaVersion: 1,
  files: ['id', 'path', 'language', 'areaIds', 'isTest'],
  functions: ['id', 'fileId', 'path', 'symbolKey', 'qualifiedName', 'signature', 'kind', 'startLine', 'endLine'],
  edges: ['from', 'to', 'kind', 'relation', 'evidence', 'confidence'],
  diagnostics: ['path', 'line', 'code', 'message'],
});

export function normalizeSourcePath(relativePath) {
  return String(relativePath).replaceAll('\\', '/').replace(/^\.\//, '').replace(/\/+/g, '/');
}

function slug(value) {
  const withoutExtension = value.replace(/([a-z0-9])([A-Z])/g, '$1-$2').replace(/\.[^.\/]+$/, '');
  const result = withoutExtension.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
  return result || 'source';
}

function stableId(prefix, input, label) {
  const normalized = input.normalize('NFC');
  const hash = createHash('sha256').update(normalized, 'utf8').digest('hex').slice(0, 8);
  return `${prefix}-${slug(label)}-${hash}`;
}

export function stableFileId(relativePath) {
  const normalized = normalizeSourcePath(relativePath);
  return stableId('file', normalized, path.posix.basename(normalized));
}

export function stableFunctionId(relativePath, symbolKey) {
  const normalized = normalizeSourcePath(relativePath);
  const identity = `${normalized}\0${symbolKey}`;
  const readable = String(symbolKey).split(/[.:(/]/).filter(Boolean).at(-1) || path.posix.basename(normalized);
  return stableId('function', identity, readable);
}

export function resolveAreas(relativePath, rules) {
  const normalized = normalizeSourcePath(relativePath);
  const candidates = [
    ...(rules.overrides ?? []).map(rule => ({ ...rule, tier: 3 })),
    ...(rules.shared ?? []).map(rule => ({ ...rule, tier: 2 })),
    ...(rules.defaults ?? []).map(rule => ({ ...rule, tier: 1 })),
  ].filter(rule => matches(normalized, rule));
  if (!candidates.length) return [];
  const highestTier = Math.max(...candidates.map(rule => rule.tier));
  const tierRules = candidates.filter(rule => rule.tier === highestTier);
  const specificity = Math.max(...tierRules.map(rule => (rule.path ?? rule.pattern).length + (rule.path ? 10000 : 0)));
  return [...new Set(tierRules.filter(rule => (rule.path ?? rule.pattern).length + (rule.path ? 10000 : 0) === specificity).flatMap(rule => rule.areaIds))].sort();
}

function matches(filePath, rule) {
  if (rule.path) return filePath === normalizeSourcePath(rule.path);
  const prefix = normalizeSourcePath(rule.pattern ?? '');
  if (prefix.endsWith('/')) return filePath.startsWith(prefix);
  return filePath === prefix || filePath.startsWith(`${prefix}/`);
}

export function unmappedDiagnostic(relativePath) {
  const normalized = normalizeSourcePath(relativePath);
  return { path: normalized, line: 1, code: 'unmapped-area', message: `No ownership rule matched ${normalized}.` };
}

export function classifyFile(relativePath, rules) {
  const normalized = normalizeSourcePath(relativePath);
  return {
    areaIds: resolveAreas(normalized, rules),
    isTest: rules.testPatterns.some(pattern => matches(normalized, { pattern })),
  };
}
