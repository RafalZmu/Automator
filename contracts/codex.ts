import { z } from 'zod';

export const codexTaskFormatSchema = z.enum(['python', 'playwright', 'workflow']);
export type CodexTaskFormat = z.infer<typeof codexTaskFormatSchema>;
export const codexTaskStatusSchema = z.strictObject({
  available: z.boolean(),
  state: z.enum(['ready', 'unavailable', 'signedOut', 'configurationError']),
  message: z.string().min(1).max(2048),
  version: z.string().max(128).nullable().optional(),
});
export const codexTaskInputSchema = z.strictObject({
  name: z.string().regex(/^[A-Za-z][A-Za-z0-9_]{0,63}$/),
  description: z.string().max(512),
  type: z.enum(['string', 'number', 'boolean', 'path', 'json']),
  required: z.boolean(),
});
export const codexTaskEffectSchema = z.strictObject({
  kind: z.enum(['read', 'visit', 'create', 'modify', 'overwrite', 'delete', 'submit', 'send', 'download']),
  description: z.string().max(512),
  target: z.string().max(512),
});
export const codexTaskDraftSchema = z.strictObject({
  id: z.string().regex(/^[0-9a-f]{32}$/i),
  plan: z.string().min(1).max(12000),
  format: codexTaskFormatSchema,
  source: z.string().min(1).max(65536),
  scope: z.array(z.string().min(1).max(256)).max(32),
  proposedScope: z.array(z.string().min(1).max(256)).max(32),
  inputs: z.array(codexTaskInputSchema).max(32),
  effects: z.array(codexTaskEffectSchema).max(32),
  sourceHash: z.string().regex(/^[0-9a-f]{64}$/i),
  createdAt: z.string().datetime({ offset: true }),
  runSucceeded: z.boolean(),
  saved: z.boolean(),
  managedPath: z.string().max(4096).nullable().optional(),
  approvedRevision: z.string().max(128).nullable().optional(),
  successfulRunRevision: z.string().max(128).nullable().optional(),
  reviewSourceHash: z.string().max(128).nullable().optional(),
  planIsHistorical: z.boolean(),
});
export const codexTaskDraftSummarySchema = z.strictObject({
  id: z.string().regex(/^[0-9a-f]{32}$/i),
  plan: z.string().min(1).max(12000),
  format: codexTaskFormatSchema,
  createdAt: z.string().datetime({ offset: true }),
  runSucceeded: z.boolean(),
  saved: z.boolean(),
});
export const codexTaskRunSchema = z.strictObject({
  succeeded: z.boolean(),
  message: z.string().max(4096),
  output: z.json().nullable().optional(),
  status: z.enum(['success', 'information', 'warning', 'error']),
});
export const codexTaskSaveSchema = z.strictObject({ saved: z.boolean(), message: z.string().max(4096) });
export const codexTaskGenerateInputSchema = z.strictObject({
  prompt: z.string().trim().min(1).max(12000),
  scope: z.array(z.string().trim().min(1).max(256)).min(1).max(32),
  preferredFormat: codexTaskFormatSchema.optional(),
});
export const codexTaskIdInputSchema = z.strictObject({ id: z.string().regex(/^[0-9a-f]{32}$/i) });
export const codexTaskRunInputSchema = codexTaskIdInputSchema.extend({
  effectConfirmed: z.boolean().optional(),
  taskInput: z.record(z.string(), z.json()).optional(),
});
export const codexTaskApproveInputSchema = codexTaskIdInputSchema.extend({ expectedReviewSourceHash: z.string().regex(/^[0-9a-f]{64}$/i).nullable().optional() });
