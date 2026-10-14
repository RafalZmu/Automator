import { z } from 'zod';

export const MCP_DEFAULT_PORT = 47821;
export const MCP_ENDPOINT_PATH = '/mcp';

export const mcpPortSchema = z.number().int().min(1024).max(65535);

/** Renderer-facing status. The bearer token is never part of it; the host copies the command itself. */
export const mcpStatusSchema = z.strictObject({
  enabled: z.boolean(),
  state: z.enum(['off', 'listening', 'portInUse', 'error']),
  port: z.number().int().min(0).max(65535),
  url: z.string().max(256).nullable(),
  error: z.string().max(512).nullable(),
});
export type McpStatus = z.infer<typeof mcpStatusSchema>;

export const mcpSetEnabledSchema = z.boolean();
export const mcpSetPortSchema = mcpPortSchema;
