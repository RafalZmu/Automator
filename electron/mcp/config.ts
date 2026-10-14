import fs from 'node:fs';
import path from 'node:path';
import { randomBytes } from 'node:crypto';
import { MCP_DEFAULT_PORT, mcpPortSchema } from '../../contracts/mcp.ts';

export type McpConfig = { enabled: boolean; port: number; token: string };

/** Encrypts the bearer token at rest. The host supplies Electron safeStorage; tests supply a fake. */
export type TokenProtector = {
  encrypt(value: string): string;
  decrypt(value: string): string;
};

const CONFIG_VERSION = 1;

export function generateMcpToken(): string {
  return randomBytes(32).toString('base64url');
}

export function loadMcpConfig(
  filePath: string,
  protector: TokenProtector,
  defaults: { port: number } = { port: MCP_DEFAULT_PORT },
  createToken: () => string = generateMcpToken,
): McpConfig {
  try {
    const raw = JSON.parse(fs.readFileSync(filePath, 'utf8')) as Record<string, unknown>;
    if (raw && raw.version === CONFIG_VERSION && typeof raw.enabled === 'boolean'
      && (raw.port === 0 || mcpPortSchema.safeParse(raw.port).success) && typeof raw.tokenCipher === 'string') {
      const token = protector.decrypt(raw.tokenCipher);
      if (token.length >= 32) return { enabled: raw.enabled, port: raw.port as number, token };
    }
  } catch { /* A missing, corrupt or undecryptable file is replaced with fresh defaults below. */ }
  const config: McpConfig = { enabled: true, port: defaults.port, token: createToken() };
  saveMcpConfig(filePath, config, protector);
  return config;
}

export function saveMcpConfig(filePath: string, config: McpConfig, protector: TokenProtector): void {
  fs.mkdirSync(path.dirname(filePath), { recursive: true });
  const temporary = `${filePath}.${process.pid}.tmp`;
  fs.writeFileSync(temporary, JSON.stringify({
    version: CONFIG_VERSION,
    enabled: config.enabled,
    port: config.port,
    tokenCipher: protector.encrypt(config.token),
  }), 'utf8');
  fs.renameSync(temporary, filePath);
}

export function claudeAddCommand(port: number, token: string): string {
  return `claude mcp add --transport http automator http://127.0.0.1:${port}/mcp --header "Authorization: Bearer ${token}"`;
}
