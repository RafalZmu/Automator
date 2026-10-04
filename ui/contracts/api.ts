export type ApiResponseMode = 'text' | 'json';

/** Persisted profile shape. Secret headers contain opaque references, never secret values. */
export type ApiProfile = {
  id: string;
  name: string;
  method: string;
  url: string;
  allowLocalNetwork: boolean;
  headers: Record<string, string>;
  secretHeaders: Record<string, string>;
  bodyTemplate: string | null;
  defaultInput?: unknown;
  responseMode: ApiResponseMode;
  timeoutSeconds: number;
};

export type ApiCurlImportDraft = {
  method: string;
  url: string;
  headers: Record<string, string>;
  secretHeaders: string[];
  /** Ephemeral values for the explicit vault-save step. Never assign these to profile headers. */
  secretValues: Record<string, string>;
  bodyTemplate: string | null;
  defaultInput?: unknown;
};

export type DecodedApiResponse = {
  bodyText: string;
  structuredOutput: unknown | null;
  hasStructuredOutput: boolean;
  parseFailure: boolean;
};

const idPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;
const secretIdPattern = /^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$/;
const headerNamePattern = /^[!#$%&'*+.^_`|~0-9A-Za-z-]+$/;
const secretOnlyHeaders = new Set([
  'authorization', 'proxy-authorization', 'cookie', 'set-cookie',
]);
const credentialLikeBodyKey = /(?:password|passwd|token|secret|credential|api[-_]?key|access[-_]?key|private[-_]?key)/i;
const maximumJsonInputBytes = 512 * 1024;

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function stringMap(value: unknown): Record<string, string> | null {
  if (!isRecord(value)) return null;
  const entries = Object.entries(value);
  if (entries.length > 64 || entries.some(([key, item]) => !key || typeof item !== 'string')) return null;
  return Object.fromEntries(entries) as Record<string, string>;
}

function validJsonInput(value: unknown): boolean {
  try {
    const json = JSON.stringify(value);
    return json !== undefined && new TextEncoder().encode(json).length <= maximumJsonInputBytes;
  } catch {
    return false;
  }
}

export function validateApiProfile(profile: ApiProfile): string[] {
  const issues: string[] = [];
  if (!profile || typeof profile !== 'object') return ['Profile data is invalid.'];
  if (!idPattern.test(profile.id)) issues.push('Profile key must be a short lowercase identifier.');
  if (!profile.name.trim() || profile.name.length > 128) issues.push('Name is required and must be at most 128 characters.');
  const method = profile.method.trim().toUpperCase();
  if (!['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'].includes(method)) issues.push('Choose a supported HTTP method.');

  try {
    const parsed = new URL(profile.url);
    if (!['http:', 'https:'].includes(parsed.protocol) || parsed.username || parsed.password || !parsed.hostname) {
      issues.push('URL must be an absolute HTTP or HTTPS address without credentials.');
    }
  } catch {
    issues.push('Enter a valid absolute HTTP or HTTPS URL.');
  }

  if (typeof profile.allowLocalNetwork !== 'boolean') issues.push('Local network policy must be enabled or disabled.');
  if (!profile.headers || typeof profile.headers !== 'object' || Array.isArray(profile.headers)) {
    issues.push('Ordinary headers are invalid.');
  } else {
    for (const [name, value] of Object.entries(profile.headers)) {
      if (!headerNamePattern.test(name) || name.length > 128) issues.push(`Header name “${name}” is invalid.`);
      if (typeof value !== 'string' || value.length > 8192) issues.push(`Header “${name}” has an invalid value.`);
      if (secretOnlyHeaders.has(name.toLowerCase())) issues.push(`Header “${name}” must be stored as a secret header.`);
    }
  }

  if (!profile.secretHeaders || typeof profile.secretHeaders !== 'object' || Array.isArray(profile.secretHeaders)) {
    issues.push('Secret headers are invalid.');
  } else {
    const ordinaryNames = new Set(Object.keys(profile.headers ?? {}).map((name) => name.toLowerCase()));
    for (const [name, secretId] of Object.entries(profile.secretHeaders)) {
      if (!headerNamePattern.test(name) || name.length > 128) issues.push(`Secret header name “${name}” is invalid.`);
      if (!secretIdPattern.test(secretId)) issues.push(`Secret reference for “${name}” is invalid.`);
      if (ordinaryNames.has(name.toLowerCase())) issues.push(`Header “${name}” cannot be both ordinary and secret.`);
    }
  }

  if (profile.bodyTemplate !== null && (typeof profile.bodyTemplate !== 'string' || new TextEncoder().encode(profile.bodyTemplate).length > 1024 * 1024)) {
    issues.push('Body template must be at most 1 MiB.');
  }
  if (profile.defaultInput !== undefined && !validJsonInput(profile.defaultInput)) {
    issues.push('Default JSON input must be valid JSON data and at most 512 KiB.');
  }
  if (profile.responseMode !== 'text' && profile.responseMode !== 'json') issues.push('Choose text or JSON response mode.');
  if (!Number.isInteger(profile.timeoutSeconds) || profile.timeoutSeconds < 1 || profile.timeoutSeconds > 3600) {
    issues.push('Timeout must be between 1 and 3600 seconds.');
  }
  return [...new Set(issues)];
}

/** Reads known profile fields only; legacy renderer-provided allowedHosts are deliberately ignored. */
export function readApiProfiles(value: unknown): ApiProfile[] {
  if (!isRecord(value) || !Array.isArray(value.profiles)) return [];
  return value.profiles.flatMap((candidate) => {
    if (!isRecord(candidate)) return [];
    const headers = stringMap(candidate.headers);
    const secretHeaders = stringMap(candidate.secretHeaders);
    if (!headers || !secretHeaders
        || typeof candidate.id !== 'string' || typeof candidate.name !== 'string'
        || typeof candidate.method !== 'string' || typeof candidate.url !== 'string'
        || typeof candidate.allowLocalNetwork !== 'boolean'
        || (candidate.bodyTemplate !== null && typeof candidate.bodyTemplate !== 'string')
        || (candidate.defaultInput !== undefined && !validJsonInput(candidate.defaultInput))
        || (candidate.responseMode !== 'text' && candidate.responseMode !== 'json')
        || typeof candidate.timeoutSeconds !== 'number') return [];
    const profile: ApiProfile = {
      id: candidate.id,
      name: candidate.name,
      method: candidate.method,
      url: candidate.url,
      allowLocalNetwork: candidate.allowLocalNetwork,
      headers,
      secretHeaders,
      bodyTemplate: candidate.bodyTemplate,
      ...(candidate.defaultInput === undefined ? {} : { defaultInput: candidate.defaultInput }),
      responseMode: candidate.responseMode,
      timeoutSeconds: candidate.timeoutSeconds,
    };
    return validateApiProfile(profile).length === 0 ? [profile] : [];
  });
}

/** Parses a copied curl command as inert text. No shell is invoked and the request is never sent here. */
export function parseApiCurlImport(command: string): ApiCurlImportDraft {
  if (typeof command !== 'string' || command.trim().length === 0 || command.length > 1024 * 1024) {
    throw new Error('Paste a cURL command up to 1 MiB.');
  }
  const tokens = command.includes('^"') || /\^\r?\n/.test(command)
    ? tokenizeWindowsCurl(command)
    : tokenizeBashCurl(command);
  if (tokens.length < 2 || !/^curl(?:\.exe)?$/i.test(tokens[0])) throw new Error('Paste a supported Chrome Copy as cURL command.');

  let url = '';
  let method = '';
  let body: string | null = null;
  let hasUrlEncodedBody = false;
  const headers: Record<string, string> = {};
  const secretValues: Record<string, string> = {};
  const seenHeaders = new Set<string>();

  const valueFor = (index: number, option: string, inline?: string): [string, number] => {
    if (inline !== undefined) return [inline, index];
    if (index + 1 >= tokens.length) {
      throw new Error(`The cURL ${option} option needs a value.`);
    }
    return [tokens[index + 1], index + 1];
  };

  for (let index = 1; index < tokens.length; index += 1) {
    const token = tokens[index];
    if (token === '--') {
      if (index + 1 < tokens.length && !url) url = tokens[index + 1];
      if (index + 2 < tokens.length) throw new Error('The cURL command has extra positional arguments.');
      break;
    }
    if (token === '-X' || token === '--request' || token.startsWith('--request=')) {
      const [value, next] = valueFor(index, token, token.startsWith('--request=') ? token.slice(10) : undefined);
      method = value.toUpperCase();
      index = next;
      continue;
    }
    if (token === '-H' || token === '--header' || token.startsWith('--header=')) {
      const [value, next] = valueFor(index, token, token.startsWith('--header=') ? token.slice(9) : undefined);
      const separator = value.indexOf(':');
      if (separator <= 0) throw new Error('A cURL header must use the Name: value format.');
      const name = value.slice(0, separator).trim();
      const headerValue = value.slice(separator + 1).trim();
      if (!headerNamePattern.test(name) || name.length > 128 || headerValue.length > 8192) throw new Error('The cURL command contains an invalid header.');
      const key = name.toLowerCase();
      if (seenHeaders.has(key)) throw new Error('The cURL command has duplicate headers; combine them before importing.');
      seenHeaders.add(key);
      if (secretOnlyHeaders.has(key)) {
        if (!headerValue) throw new Error(`The cURL credential header ${name} is empty.`);
        secretValues[name] = headerValue;
      }
      else headers[name] = headerValue;
      index = next;
      continue;
    }
    if (token === '--url' || token.startsWith('--url=')) {
      const [value, next] = valueFor(index, token, token.startsWith('--url=') ? token.slice(6) : undefined);
      if (url) throw new Error('The cURL command contains more than one URL.');
      url = value;
      index = next;
      continue;
    }
    if (token === '-d' || token === '--data' || token === '--data-raw' || token === '--data-binary'
        || token === '--data-ascii' || token === '--data-urlencode'
        || token.startsWith('--data=') || token.startsWith('--data-raw=') || token.startsWith('--data-binary=')
        || token.startsWith('--data-ascii=') || token.startsWith('--data-urlencode=')) {
      const dataOption = token.split('=', 1)[0];
      const inline = token.includes('=') ? token.slice(token.indexOf('=') + 1) : undefined;
      const [value, next] = valueFor(index, dataOption, inline);
      if (value.startsWith('@')) throw new Error('cURL file uploads cannot be imported.');
      const normalized = dataOption === '--data-urlencode' ? encodeCurlFormValue(value) : value;
      body = body === null ? normalized : `${body}&${normalized}`;
      hasUrlEncodedBody ||= dataOption === '--data-urlencode';
      index = next;
      continue;
    }
    if (['--compressed', '--location', '-L', '--silent', '-s', '--show-error', '-S', '--globoff', '-g', '--http1.1', '--http2'].includes(token)) continue;
    if (token.startsWith('-')) throw new Error('The cURL command uses an unsupported option.');
    if (url) throw new Error('The cURL command contains more than one URL.');
    url = token;
  }

  if (!url) throw new Error('The cURL command is missing its URL.');
  let parsedUrl: URL;
  try {
    parsedUrl = new URL(url);
  } catch {
    throw new Error('The cURL URL must be an absolute HTTP or HTTPS address.');
  }
  if (!['http:', 'https:'].includes(parsedUrl.protocol) || parsedUrl.username || parsedUrl.password) {
    throw new Error('The cURL URL must be an absolute HTTP or HTTPS address without credentials.');
  }
  if (body !== null && !method) method = 'POST';
  if (!method) method = 'GET';
  if (!['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'].includes(method)) {
    throw new Error('The cURL request method is not supported.');
  }

  let bodyTemplate = body;
  let defaultInput: unknown;
  if (body !== null && !hasUrlEncodedBody) {
    try {
      defaultInput = JSON.parse(body) as unknown;
      if (containsCredentialLikeKey(defaultInput)) {
        throw new Error('The request body appears to contain credentials; remove them before importing.');
      }
      bodyTemplate = null;
    } catch (error) {
      if (error instanceof Error && error.message.includes('appears to contain credentials')) throw error;
      if (/(?:password|passwd|token|secret|credential|api[-_]?key|access[-_]?key)\s*[:=]/i.test(body)) {
        throw new Error('The request body appears to contain credentials; remove them before importing.');
      }
      bodyTemplate = body;
    }
  }
  if (hasUrlEncodedBody && !Object.keys(headers).some((name) => name.toLowerCase() === 'content-type')) {
    headers['Content-Type'] = 'application/x-www-form-urlencoded';
  }

  return {
    method,
    url: parsedUrl.href,
    headers,
    secretHeaders: Object.keys(secretValues),
    secretValues,
    bodyTemplate,
    ...(defaultInput === undefined ? {} : { defaultInput }),
  };
}

function containsCredentialLikeKey(value: unknown): boolean {
  if (Array.isArray(value)) return value.some(containsCredentialLikeKey);
  if (!isRecord(value)) return false;
  return Object.entries(value).some(([key, item]) => credentialLikeBodyKey.test(key) || containsCredentialLikeKey(item));
}

function encodeCurlFormValue(value: string): string {
  const separator = value.indexOf('=');
  if (separator < 0) return encodeURIComponent(value);
  return `${encodeURIComponent(value.slice(0, separator))}=${encodeURIComponent(value.slice(separator + 1))}`;
}

function tokenizeBashCurl(command: string): string[] {
  const tokens: string[] = [];
  let value = '';
  let quote: "'" | '"' | null = null;
  let started = false;
  const push = () => { if (started) tokens.push(value); value = ''; started = false; };

  for (let index = 0; index < command.length; index += 1) {
    const character = command[index];
    const next = command[index + 1];
    if (quote === "'") {
      if (character === "'") quote = null;
      else value += character;
      continue;
    }
    if (quote === '"') {
      if (character === '"') quote = null;
      else if (character === '\\') {
        if (next === undefined) throw new Error('The cURL quoting is incomplete.');
        if (next === '\n') { index += 1; continue; }
        if (next === '\r' && command[index + 2] === '\n') { index += 2; continue; }
        if ('"\\$`'.includes(next)) {
          if (next === '$' || next === '`') throw new Error('Unsupported shell syntax in the cURL command.');
          value += next;
        } else value += `\\${next}`;
        index += 1;
      } else if (character === '$' || character === '`') throw new Error('Unsupported shell syntax in the cURL command.');
      else value += character;
      continue;
    }
    if (/\s/.test(character)) { push(); continue; }
    if (character === "'") { quote = "'"; started = true; continue; }
    if (character === '"') { quote = '"'; started = true; continue; }
    if (character === '\\') {
      if (next === undefined) throw new Error('The cURL quoting is incomplete.');
      if (next === '\n') { index += 1; continue; }
      if (next === '\r' && command[index + 2] === '\n') { index += 2; continue; }
      value += next;
      started = true;
      index += 1;
      continue;
    }
    if ('$`;&|<>()'.includes(character)) throw new Error('Unsupported shell syntax in the cURL command.');
    value += character;
    started = true;
  }
  if (quote !== null) throw new Error('The cURL quoting is incomplete.');
  push();
  return tokens;
}

function tokenizeWindowsCurl(command: string): string[] {
  if (/%[A-Za-z_][A-Za-z0-9_]*%|![A-Za-z_][A-Za-z0-9_]*!|\$\(|\$\{/u.test(command)) {
    throw new Error('Unsupported shell syntax in the cURL command.');
  }
  const tokens: string[] = [];
  let value = '';
  let quoted = false;
  let started = false;
  const push = () => { if (started) tokens.push(value); value = ''; started = false; };

  for (let index = 0; index < command.length; index += 1) {
    const character = command[index];
    const next = command[index + 1];
    if (character === '^' && (next === '\n' || (next === '\r' && command[index + 2] === '\n'))) {
      index += next === '\r' ? 2 : 1;
      continue;
    }
    if (character === '^' && next === '\\' && command[index + 2] === '^' && command[index + 3] === '"' && quoted) {
      value += '"';
      started = true;
      index += 3;
      continue;
    }
    if (character === '^' && next === '^') { value += '^'; started = true; index += 1; continue; }
    if (character === '^' && next && '&|<>'.includes(next)) { value += next; started = true; index += 1; continue; }
    if (character === '^' && next === '"') {
      const following = command[index + 2];
      if (!quoted) quoted = true;
      else if (following === undefined || /\s/.test(following)) quoted = false;
      else value += '"';
      started = true;
      index += 1;
      continue;
    }
    if (character === '"') { quoted = !quoted; started = true; continue; }
    if (!quoted && /\s/.test(character)) { push(); continue; }
    if (!quoted && '&|<>;()$`'.includes(character)) throw new Error('Unsupported shell syntax in the cURL command.');
    value += character;
    started = true;
  }
  if (quoted) throw new Error('The cURL quoting is incomplete.');
  push();
  return tokens;
}

/** `{{input}}` is inserted as raw JSON, allowing an object to occupy a JSON value position. */
export function buildApiRequestBody(bodyTemplate: string | null, input: unknown): string | null {
  if (bodyTemplate === null) {
    if (input === undefined) return null;
    const json = JSON.stringify(input);
    if (json === undefined) throw new Error('API input must be valid JSON data.');
    return json;
  }
  if (!bodyTemplate.includes('{{input}}')) return bodyTemplate;
  const json = JSON.stringify(input === undefined ? null : input);
  if (json === undefined) throw new Error('API input must be valid JSON data.');
  return bodyTemplate.replaceAll('{{input}}', json);
}

export function decodeApiResponse(responseMode: ApiResponseMode, bodyText: string): DecodedApiResponse {
  if (responseMode === 'text') return { bodyText, structuredOutput: null, hasStructuredOutput: false, parseFailure: false };
  try {
    return { bodyText: '', structuredOutput: JSON.parse(bodyText) as unknown, hasStructuredOutput: true, parseFailure: false };
  } catch {
    return { bodyText, structuredOutput: null, hasStructuredOutput: false, parseFailure: true };
  }
}
