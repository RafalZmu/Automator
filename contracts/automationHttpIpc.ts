import { z } from 'zod';
import { automationHttpResultSchema, type AutomationHttpResult } from './rpc.ts';

export const automationServiceErrorCategorySchema = z.enum([
  'unsupportedScheme',
  'hostNotAllowed',
  'networkNotAllowed',
  'requestTooLarge',
  'responseTooLarge',
  'timedOut',
  'canceled',
  'transportFailure',
  'invalidResponse',
]);

export type AutomationServiceErrorCategory = z.infer<typeof automationServiceErrorCategorySchema>;

export class AutomationServiceError extends Error {
  readonly category: AutomationServiceErrorCategory;

  constructor(category: AutomationServiceErrorCategory, message: string) {
    super(message);
    this.name = 'AutomationServiceError';
    this.category = category;
  }
}

const automationHttpIpcFailureSchema = z.strictObject({
  ok: z.literal(false),
  error: z.strictObject({
    category: automationServiceErrorCategorySchema,
    message: z.string().min(1).max(2048),
  }),
});

export const automationHttpIpcResponseSchema = z.discriminatedUnion('ok', [
  z.strictObject({ ok: z.literal(true), result: automationHttpResultSchema }),
  automationHttpIpcFailureSchema,
]);

export type AutomationHttpIpcResponse = z.infer<typeof automationHttpIpcResponseSchema>;

const fallbackFailureMessage = 'The HTTP service could not complete the request.';

/** Turns the backend's JSON-RPC error into a structured-clone-safe IPC value. */
export function createAutomationHttpIpcFailure(error: unknown): Extract<AutomationHttpIpcResponse, { ok: false }> {
  const value = error !== null && typeof error === 'object'
    ? error as { category?: unknown; data?: { category?: unknown }; message?: unknown }
    : undefined;
  const rawCategory = value?.data?.category ?? value?.category;
  const category = automationServiceErrorCategorySchema.safeParse(rawCategory);
  const message = category.success && typeof value?.message === 'string'
    ? value.message.trim().slice(0, 2048)
    : fallbackFailureMessage;

  return {
    ok: false,
    error: {
      category: category.success ? category.data : 'transportFailure',
      message: message || fallbackFailureMessage,
    },
  };
}

/** Validates the IPC envelope and restores a typed error in the renderer. */
export function readAutomationHttpIpcResponse(value: unknown): AutomationHttpResult {
  const parsed = automationHttpIpcResponseSchema.safeParse(value);
  if (!parsed.success) {
    throw new AutomationServiceError('invalidResponse', 'Automator returned an invalid HTTP service response.');
  }
  if (!parsed.data.ok) {
    throw new AutomationServiceError(parsed.data.error.category, parsed.data.error.message);
  }
  return parsed.data.result;
}
