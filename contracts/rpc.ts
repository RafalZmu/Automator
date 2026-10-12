import { z } from 'zod';
import { FILE_EXPLORER_REGISTER_SCRIPT_ACTION_ID } from './fileExplorer.ts';

export const PROTOCOL_VERSION = 3 as const;
export const MAX_RPC_REQUEST_LINE_BYTES = 7 * 1024 * 1024;
export const MAX_RPC_RESPONSE_LINE_BYTES = 28 * 1024 * 1024;
export const MAX_HTTP_REQUEST_BODY_BYTES = 1 * 1024 * 1024;
export const MAX_HTTP_RESPONSE_BODY_BYTES = 4 * 1024 * 1024;
export const MAX_MODULE_ACTION_INPUT_BYTES = 64 * 1024;
export const MAX_MODULE_SETTINGS_BYTES = 64 * 1024;
export const MAX_MODULE_RESULT_DATA_BYTES = 512 * 1024;

const utf8ByteLength = (value: string) => new TextEncoder().encode(value).byteLength;

const absoluteWindowsPath = z.string().min(1).max(4096).regex(/^(?:[A-Za-z]:[\\/]|\\\\)/);
const hwnd = z.string().min(3).max(18).regex(/^0x[\da-f]{1,16}$/i).refine((value) => BigInt(value) !== 0n);
const mode = z.enum(['launcher', 'catalog', 'settings', 'recordingHotkey', 'aliasEditing']);
const hotkey = z.string().min(1).max(32).refine((value) => {
  const named = new Set([
    'rightcontrol', 'rctrl', 'leftcontrol', 'lctrl', 'rightalt', 'ralt', 'leftalt', 'lalt',
    'rightshift', 'rshift', 'leftshift', 'lshift', 'rightwindows', 'rwin', 'leftwindows', 'lwin',
    'space', 'capslock', 'escape', 'slash', '/',
  ]);
  if (named.has(value.toLowerCase())) return true;
  if (/^[A-Za-z0-9]$/.test(value)) return true;
  if (/^F(?:[1-9]|1[0-2])$/i.test(value)) return true;
  if (/^Key0*(?:[1-9]|[1-9]\d|1\d\d|2[0-4]\d|25[0-5])$/i.test(value)) return true;
  return false;
});
const alias = z.string().min(1).max(32).regex(/^[A-Za-z]+$/);

export const bindingParamsSchema = z.strictObject({
  id: z.string().min(1).max(128),
  name: z.string().min(1).max(256),
  targetPath: absoluteWindowsPath,
  alias: z.string().max(32),
  arguments: z.string().max(2048),
});

const initializeParams = z.strictObject({
  protocolVersion: z.literal(PROTOCOL_VERSION),
  buildId: z.string().min(1).max(128),
  hostProcessId: z.number().int().positive(),
  hostExecutablePath: absoluteWindowsPath,
  portableExecutablePath: absoluteWindowsPath.nullable(),
  testMode: z.boolean(),
  dataDirectory: absoluteWindowsPath.nullable(),
}).superRefine((value, context) => {
  if (value.testMode && !value.dataDirectory) {
    context.addIssue({ code: 'custom', path: ['dataDirectory'], message: 'Test mode requires an isolated data directory.' });
  }
  if (!value.testMode && value.dataDirectory) {
    context.addIssue({ code: 'custom', path: ['dataDirectory'], message: 'Production cannot override the data directory.' });
  }
});

const emptyParams = z.strictObject({});
export const hostWindowContextSchema = z.strictObject({
  role: z.enum(['launcher', 'workspace']),
  selectedTab: z.number().int().min(1).max(9),
});
export type HostWindowContext = z.infer<typeof hostWindowContextSchema>;
const automationModuleParams = z.strictObject({
  moduleId: z.string().min(1).max(64),
  hostWindowContext: hostWindowContextSchema.optional(),
});
const boundedJson = (maximumBytes: number) => z.json().refine((value) => {
  const encoded = JSON.stringify(value);
  return encoded !== undefined && utf8ByteLength(encoded) <= maximumBytes;
}, `JSON payload exceeds the ${maximumBytes}-byte limit.`);
export const automationModuleActionRequestSchema = z.strictObject({
  requestId: z.string().min(1).max(128),
  contractVersion: z.number().int().positive().max(16),
  moduleId: z.string().min(1).max(64).regex(/^[a-z][a-z0-9.-]*$/),
  actionId: z.string().min(1).max(64).regex(/^[a-z][A-Za-z0-9._-]*$/),
  actionVersion: z.number().int().positive().max(1024),
  input: boundedJson(MAX_MODULE_ACTION_INPUT_BYTES),
  hostWindowContext: hostWindowContextSchema.optional(),
});
export type AutomationModuleActionRequest = z.infer<typeof automationModuleActionRequestSchema>;
export const automationModuleActionCancelRequestSchema = z.strictObject({
  moduleId: z.string().min(1).max(64).regex(/^[a-z][a-z0-9.-]*$/),
  requestId: z.string().min(1).max(128),
  hostWindowContext: hostWindowContextSchema.optional(),
});
export type AutomationModuleActionCancelRequest = z.infer<typeof automationModuleActionCancelRequestSchema>;
export const moduleSettingsUpdateRequestSchema = z.strictObject({
  contractVersion: z.number().int().positive().max(16),
  moduleId: z.string().min(1).max(64).regex(/^[a-z][a-z0-9.-]*$/),
  settingsVersion: z.number().int().positive().max(1024),
  value: boundedJson(MAX_MODULE_SETTINGS_BYTES),
  hostWindowContext: hostWindowContextSchema.optional(),
});
export type ModuleSettingsUpdateRequest = z.infer<typeof moduleSettingsUpdateRequestSchema>;
export const moduleSettingsUpdateResultSchema = z.strictObject({
  saved: z.literal(true),
  moduleId: z.string().min(1).max(64).regex(/^[a-z][a-z0-9.-]*$/),
  settingsVersion: z.number().int().positive().max(1024),
  hostWindowContext: hostWindowContextSchema.optional(),
});
export type ModuleSettingsUpdateResult = z.infer<typeof moduleSettingsUpdateResultSchema>;
export const moduleSettingsGetRequestSchema = z.strictObject({
  contractVersion: z.number().int().positive().max(16),
  moduleId: z.string().min(1).max(64).regex(/^[a-z][a-z0-9.-]*$/),
  settingsVersion: z.number().int().positive().max(1024),
  hostWindowContext: hostWindowContextSchema.optional(),
});
export type ModuleSettingsGetRequest = z.infer<typeof moduleSettingsGetRequestSchema>;
export const moduleSettingsGetResultSchema = moduleSettingsGetRequestSchema.extend({
  value: boundedJson(MAX_MODULE_SETTINGS_BYTES),
});
export type ModuleSettingsGetResult = z.infer<typeof moduleSettingsGetResultSchema>;

const automationActionSchema = z.strictObject({
  id: z.string().min(1).max(64).regex(/^[a-z][A-Za-z0-9._-]*$/),
  label: z.string().min(1).max(128),
  version: z.number().int().positive(),
  payload: boundedJson(MAX_MODULE_ACTION_INPUT_BYTES),
});
export const automationResultSchema = z.strictObject({
  contractVersion: z.literal(1),
  status: z.enum(['success', 'information', 'warning', 'error']),
  message: z.string().max(4096),
  data: boundedJson(MAX_MODULE_RESULT_DATA_BYTES),
  actions: z.array(automationActionSchema).max(32).superRefine((actions, context) => {
    const ids = new Set<string>();
    actions.forEach((action, index) => {
      if (ids.has(action.id)) context.addIssue({ code: 'custom', path: [index, 'id'], message: 'Follow-up action ids must be unique.' });
      ids.add(action.id);
    });
  }),
});
export type AutomationResult = z.infer<typeof automationResultSchema>;

const globalVariableNameSchema = z.string().min(1).max(192).regex(/^[A-Za-z_][A-Za-z0-9_.-]*$/)
  .refine((name) => !['input', 'variables', '__proto__', 'constructor', 'prototype'].includes(name)
    && !/^(secret|system)\./i.test(name));
const globalVariableValuesSchema = z.record(globalVariableNameSchema, z.json()).superRefine((values, context) => {
  if (Object.keys(values).length > 256) context.addIssue({ code: 'custom', message: 'At most 256 global variables are allowed.' });
  if (utf8ByteLength(JSON.stringify(values)) > 64 * 1024) context.addIssue({ code: 'custom', message: 'Global variables exceed 64 KiB.' });
});
export const globalVariableSnapshotSchema = z.strictObject({
  version: z.literal(1),
  values: globalVariableValuesSchema,
  migrationConflicts: z.array(globalVariableNameSchema).max(256),
});
export type GlobalVariableSnapshot = z.infer<typeof globalVariableSnapshotSchema>;
export const globalVariablesGetParamsSchema = z.strictObject({ version: z.literal(1) });
export const globalVariablesSetParamsSchema = z.strictObject({ version: z.literal(1), values: globalVariableValuesSchema });
export const automationHttpRequestSchema = z.strictObject({
  uri: z.string().min(1).max(8192),
  method: z.string().min(1).max(32),
  headers: z.record(z.string().min(1).max(128), z.string().max(8192)).superRefine((headers, context) => {
    if (Object.keys(headers).length > 64) context.addIssue({ code: 'custom', message: 'At most 64 HTTP headers are allowed.' });
    const totalBytes = Object.entries(headers).reduce((sum, [name, value]) => sum + utf8ByteLength(name) + utf8ByteLength(value), 0);
    if (totalBytes > 256 * 1024) context.addIssue({ code: 'custom', message: 'HTTP headers exceed the maximum size.' });
  }),
  body: z.string().nullable().superRefine((body, context) => {
    if (body !== null && utf8ByteLength(body) > MAX_HTTP_REQUEST_BODY_BYTES)
      context.addIssue({ code: 'custom', message: 'HTTP request body exceeds the 1 MiB limit.' });
  }),
});
export type AutomationHttpRequest = z.infer<typeof automationHttpRequestSchema>;

export const automationHttpResultSchema = z.strictObject({
  statusCode: z.number().int().min(100).max(599),
  headers: z.record(z.string().max(128), z.string().max(8192)),
  contentType: z.string().max(1024),
  bodyText: z.string().superRefine((body, context) => {
    if (utf8ByteLength(body) > MAX_HTTP_RESPONSE_BODY_BYTES)
      context.addIssue({ code: 'custom', message: 'HTTP response body exceeds the 4 MiB limit.' });
  }),
});
export type AutomationHttpResult = z.infer<typeof automationHttpResultSchema>;

export const automationKeyboardEligibilityResultSchema = z.strictObject({
  eligible: z.boolean(),
  revision: z.number().int().nonnegative(),
});
export type AutomationKeyboardEligibilityResult = z.infer<typeof automationKeyboardEligibilityResultSchema>;

export const automationKeyboardInputNotificationSchema = z.strictObject({
  jsonrpc: z.literal('2.0'),
  method: z.literal('automation/keyboardInput'),
  params: z.strictObject({
    moduleId: z.string().min(1).max(64),
    event: z.strictObject({
      sequence: z.number().int().nonnegative(),
      code: z.string().min(1).max(64),
      virtualKey: z.number().int().min(0).max(0xFFFF),
      isDown: z.boolean(),
      isRepeat: z.boolean(),
      modifiers: z.number().int().min(0).max(15),
    }),
  }),
});
export type AutomationKeyboardInputNotification = z.infer<typeof automationKeyboardInputNotificationSchema>['params'];

export const automationKeyboardAvailabilityNotificationSchema = z.strictObject({
  jsonrpc: z.literal('2.0'),
  method: z.literal('automation/keyboardAvailability'),
  params: z.strictObject({
    moduleId: z.string().min(1).max(64),
    eligible: z.boolean(),
    revision: z.number().int().nonnegative(),
  }),
});
export type AutomationKeyboardAvailabilityNotification = z.infer<typeof automationKeyboardAvailabilityNotificationSchema>['params'];

export const automationNotificationSchema = z.strictObject({
  jsonrpc: z.literal('2.0'),
  method: z.literal('automation/notification'),
  params: z.strictObject({
    title: z.string().trim().min(1).max(128).refine((value) => !/[\x00-\x1f\x7f]/.test(value)),
    body: z.string().trim().min(1).max(256).refine((value) => !/[\x00-\x1f\x7f]/.test(value)),
  }),
});
export type AutomationNotification = z.infer<typeof automationNotificationSchema>['params'];

const methodParams = {
  initialize: initializeParams,
  getState: emptyParams,
  'launcher/open': emptyParams,
  'launcher/close': emptyParams,
  'launcher/setWindowContext': z.strictObject({
    windowHandleHex: hwnd,
    visible: z.boolean(),
    rendererFocused: z.boolean(),
    nativeDialogActive: z.boolean(),
    mode,
  }),
  'launcher/verifyForeground': z.strictObject({ windowHandleHex: hwnd }),
  'launcher/acquireForeground': emptyParams,
  'launcher/restoreForeground': z.strictObject({ windowHandleHex: hwnd }),
  'launcher/setMode': z.strictObject({ mode }),
  'launcher/selectTab': z.strictObject({ tab: z.number().int().min(1).max(9) }),
  'launcher/setQuery': z.strictObject({ query: z.string().max(512) }),
  'launcher/openCatalog': emptyParams,
  'catalog/select': z.strictObject({ bindingId: z.string().min(1).max(128) }),
  'catalog/addCustom': z.strictObject({ path: absoluteWindowsPath.regex(/\.(?:exe|lnk)$/i) }),
  'binding/saveAlias': z.strictObject({ bindingId: z.string().min(1).max(128), alias }),
  'binding/select': z.strictObject({ bindingId: z.string().min(1).max(128) }),
  'binding/remove': z.strictObject({ bindingId: z.string().min(1).max(128) }),
  'settings/update': z.strictObject({
    hotkey,
    theme: z.enum(['Light', 'Dark']),
    startWithWindows: z.boolean(),
    bindings: z.array(bindingParamsSchema).max(256),
  }).superRefine((value, context) => {
    const ids = new Set<string>();
    const aliases = new Set<string>();
    value.bindings.forEach((binding, index) => {
      const id = binding.id.toLocaleLowerCase('en-US');
      if (ids.has(id)) context.addIssue({ code: 'custom', path: ['bindings', index, 'id'], message: 'Binding ids must be unique.' });
      ids.add(id);
      if (binding.alias) {
        const currentAlias = binding.alias.toLocaleLowerCase('en-US');
        if (!/^[A-Za-z]+$/.test(binding.alias)) context.addIssue({ code: 'custom', path: ['bindings', index, 'alias'], message: 'Alias must contain letters only.' });
        if (aliases.has(currentAlias)) context.addIssue({ code: 'custom', path: ['bindings', index, 'alias'], message: 'Aliases must be unique.' });
        aliases.add(currentAlias);
      }
    });
  }),
  'settings/import': z.strictObject({ path: absoluteWindowsPath }),
  'settings/export': z.strictObject({ path: absoluteWindowsPath }),
  'settings/openLogFolder': emptyParams,
  'settings/relink': z.strictObject({ bindingId: z.string().min(1).max(128), path: absoluteWindowsPath }),
  'hotkey/startRecording': emptyParams,
  'hotkey/cancelRecording': emptyParams,
  'launcher/activateBinding': z.strictObject({ bindingId: z.string().min(1).max(128) }),
  'automation/keyboardEligibility': automationModuleParams,
  'automation/keyboardSubscribe': automationModuleParams,
  'automation/keyboardUnsubscribe': automationModuleParams,
  'automation/httpRequest': automationModuleParams.extend({
    requestId: z.string().min(1).max(128),
    request: automationHttpRequestSchema,
  }),
  'automation/httpCancel': automationModuleParams.extend({ requestId: z.string().min(1).max(128) }),
  'automation/moduleAction': automationModuleActionRequestSchema,
  'automation/moduleActionCancel': automationModuleActionCancelRequestSchema,
  'module/settingsUpdate': moduleSettingsUpdateRequestSchema,
  'module/settingsGet': moduleSettingsGetRequestSchema,
  'variables/get': globalVariablesGetParamsSchema,
  'variables/set': globalVariablesSetParamsSchema,
  'activity/list': emptyParams,
} as const;

export type RpcMethod = keyof typeof methodParams;
export type RpcRequest = { jsonrpc: '2.0'; id: string | number; method: RpcMethod; params: Record<string, unknown> };

export class RpcValidationError extends Error {
  readonly code: number;

  constructor(message: string, code: number) {
    super(message);
    this.name = 'RpcValidationError';
    this.code = code;
  }
}

export function validateRpcRequest(value: unknown): RpcRequest {
  const envelope = z.object({
    jsonrpc: z.literal('2.0'),
    id: z.union([z.string(), z.number().finite()]),
    method: z.string().min(1).max(120),
    params: z.unknown().optional(),
  }).safeParse(value);
  if (!envelope.success) throw new RpcValidationError('Request envelope is invalid.', -32600);

  const request = envelope.data;
  if (!Object.hasOwn(methodParams, request.method)) throw new RpcValidationError('Unknown or unsupported method.', -32601);
  const params = request.params === undefined ? {} : request.params;
  if (params === null || typeof params !== 'object' || Array.isArray(params)) {
    throw new RpcValidationError('Method parameters must be an object.', -32602);
  }

  const parsed = methodParams[request.method as RpcMethod].safeParse(params);
  if (!parsed.success) throw new RpcValidationError('Method parameters are invalid.', -32602);
  return { jsonrpc: request.jsonrpc, id: request.id, method: request.method as RpcMethod, params: parsed.data as Record<string, unknown> };
}

export const bindingStateSchema = bindingParamsSchema.extend({ iconDataUrl: z.string().max(12_000).nullable() });
export const tabMetadataSchema = z.strictObject({
  slot: z.number().int().min(1).max(9),
  id: z.string().min(1).max(64),
  title: z.string().min(1).max(128),
  iconKey: z.string().min(1).max(64),
  kind: z.string().min(1).max(64),
  searchEnabled: z.boolean(),
  contractVersion: z.number().int().positive().max(16),
  settingsVersion: z.number().int().positive().max(1024),
  actions: z.array(z.strictObject({
    id: z.string().min(1).max(64),
    version: z.number().int().positive(),
    command: z.string().min(1).max(128).nullable().optional(),
    requiredCapabilities: z.array(z.strictObject({ id: z.string().min(1).max(64), version: z.number().int().positive() })).max(16),
  })).max(32),
  capabilities: z.array(z.strictObject({ id: z.string().min(1).max(64), version: z.number().int().positive() })).max(16),
});

export const backendUiStateSchema = z.strictObject({
  protocolVersion: z.literal(PROTOCOL_VERSION),
  buildId: z.string().min(1).max(128),
  revision: z.number().int().nonnegative(),
  tabRegistryVersion: z.number().int().positive(),
  moduleStates: z.array(z.strictObject({
    moduleId: z.string().min(1).max(64),
    version: z.number().int().positive(),
    values: z.record(z.string(), z.string().max(2048)),
  })).length(9),
  visible: z.boolean(),
  selectedTab: z.number().int().min(1).max(9),
  query: z.string().max(512),
  previousForegroundHwnd: hwnd.nullable(),
  mode,
  tabs: z.array(tabMetadataSchema).length(9),
  bindings: z.array(bindingStateSchema).max(256),
  catalogApps: z.array(bindingStateSchema).max(256),
  catalogTotalCount: z.number().int().nonnegative().max(100_000),
  aliasEditCandidate: bindingStateSchema.nullable(),
  hotkey,
  theme: z.enum(['Light', 'Dark']),
  startWithWindows: z.boolean(),
  settingsValid: z.boolean(),
  settingsError: z.string().max(2048).nullable(),
  missingBindings: z.array(z.strictObject({ id: z.string(), name: z.string() })).max(256),
  catalogLoading: z.boolean(),
  hookInstalled: z.boolean(),
  busy: z.boolean(),
  error: z.string().max(2048).nullable(),
  matchKind: z.enum(['None', 'Exact', 'Partial', 'AmbiguousExact']),
  matchedBindingId: z.string().nullable(),
});

export type BackendUiState = z.infer<typeof backendUiStateSchema>;
export const settingsImportResultSchema = z.strictObject({
  state: backendUiStateSchema,
  includesAutomationLibrary: z.boolean(),
  importedLibraryRecords: z.number().int().min(0).max(2_000),
  warningCount: z.number().int().min(0).max(200_000),
  warnings: z.array(z.strictObject({
    code: z.string().min(1).max(64),
    moduleId: z.string().min(1).max(64),
    recordId: z.string().min(1).max(64),
    field: z.string().min(1).max(128),
    message: z.string().min(1).max(256),
  })).max(250),
  warningsTruncated: z.boolean(),
});
export type SettingsImportResult = z.infer<typeof settingsImportResultSchema>;
export const backendInitializeResultSchema = z.strictObject({
  state: backendUiStateSchema,
  hookInstalled: z.boolean(),
  hookInstallErrorCode: z.number().int(),
  sessionId: z.string().min(1),
});

export const backendNotificationSchema = z.discriminatedUnion('method', [
  z.strictObject({ jsonrpc: z.literal('2.0'), method: z.literal('stateChanged'), params: backendUiStateSchema }),
  z.strictObject({ jsonrpc: z.literal('2.0'), method: z.literal('hotkey/recorded'), params: z.strictObject({ key: hotkey }) }),
  automationKeyboardInputNotificationSchema,
  automationKeyboardAvailabilityNotificationSchema,
  automationNotificationSchema,
  z.strictObject({ jsonrpc: z.literal('2.0'), method: z.literal('activity/changed'), params: emptyParams }),
  z.strictObject({ jsonrpc: z.literal('2.0'), method: z.literal('variables/changed'), params: emptyParams }),
]);

export const fileExplorerLaunchRequestSchema = z.object({
  actionId: z.string().min(1).max(64).regex(/^[a-z0-9][a-z0-9._-]{0,63}$/),
  filePath: absoluteWindowsPath.refine((value) => !/[\x00-\x1f]/.test(value)),
}).strict().superRefine((request, context) => {
  if (request.actionId === FILE_EXPLORER_REGISTER_SCRIPT_ACTION_ID && !/\.ps1$/i.test(request.filePath)) {
    context.addIssue({ code: 'custom', path: ['filePath'], message: 'PowerShell registration requires a .ps1 file.' });
  }
});
export type FileExplorerLaunchRequest = z.infer<typeof fileExplorerLaunchRequestSchema>;
