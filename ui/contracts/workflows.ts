export const WORKFLOW_PROFILE_MODULES = [
  { id: 'script-runner', label: 'Script Runner' },
  { id: 'api', label: 'API' },
  { id: 'browser-automation', label: 'Browser Automation' },
] as const;

export type WorkflowProfileModuleId = typeof WORKFLOW_PROFILE_MODULES[number]['id'];
export type WorkflowStatus = 'success' | 'information' | 'warning' | 'error';
export type WorkflowJsonValue = null | boolean | number | string | WorkflowJsonValue[] | { [key: string]: WorkflowJsonValue };

export type WorkflowInputBinding = {
  targetJsonPointer: string;
  literalPresent?: boolean;
  literal?: WorkflowJsonValue;
  sourceStepId?: string;
  sourceJsonPointer?: string;
};

export type WorkflowStep = {
  id: string;
  moduleId: WorkflowProfileModuleId;
  profileId: string;
  inputs: WorkflowInputBinding[];
};

export type WorkflowProfile = {
  id: string;
  name: string;
  variables: Record<string, WorkflowJsonValue>;
  variableReferences?: Record<string, string>;
  steps: WorkflowStep[];
};

export type WorkflowVariableEntry = { key: string; valueText: string };

export type WorkflowSummary = { id: string; name: string; stepCount: number };

export type WorkflowExecutionSummary = {
  status: WorkflowStatus;
  category: string;
  durationMilliseconds: number;
};

export type WorkflowStepResult = {
  stepId: string;
  moduleId: WorkflowProfileModuleId;
  profileId: string;
  status: WorkflowStatus;
  output: WorkflowJsonValue;
  summary: WorkflowExecutionSummary;
};

export type WorkflowRunResult = {
  output: WorkflowJsonValue;
  steps: WorkflowStepResult[];
  summary: WorkflowExecutionSummary;
};

const workflowIdPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;
const stepIdPattern = /^[a-z0-9][a-z0-9._-]{0,63}$/;
const profileIdPattern = /^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$/;
const workflowVariableKeyPattern = /^[A-Za-z_][A-Za-z0-9_.-]{0,63}$/;
const jsonPointerEscapePattern = /~(?:[^01]|$)/;
const maximumWorkflowSteps = 32;
const maximumBindingsPerStep = 64;
const maximumStructuredDataBytes = 512 * 1024;
const maximumWorkflowInputBytes = 48 * 1024;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isJsonValue(value: unknown): value is WorkflowJsonValue {
  if (value === null || typeof value === 'string' || typeof value === 'boolean') return true;
  if (typeof value === 'number') return Number.isFinite(value);
  if (Array.isArray(value)) return value.every(isJsonValue);
  return isRecord(value) && Object.values(value).every(isJsonValue);
}

function parseJsonPointer(pointer: string, allowRoot: boolean): string[] | null {
  if (pointer === '') return allowRoot ? [] : null;
  if (!pointer.startsWith('/') || pointer.length > 4096 || jsonPointerEscapePattern.test(pointer)) return null;
  const segments = pointer.slice(1).split('/');
  if (segments.some((segment) => segment.length === 0)) return null;
  return segments.map((segment) => segment.replace(/~1/g, '/').replace(/~0/g, '~'));
}

function jsonByteLength(value: unknown): number | null {
  try {
    const serialized = JSON.stringify(value);
    return serialized === undefined ? null : new TextEncoder().encode(serialized).byteLength;
  } catch {
    return null;
  }
}

function validateSummary(value: unknown): value is WorkflowExecutionSummary {
  return isRecord(value)
    && ['success', 'information', 'warning', 'error'].includes(String(value.status))
    && typeof value.category === 'string'
    && /^[a-z][a-z0-9._-]{0,63}$/.test(value.category)
    && typeof value.durationMilliseconds === 'number'
    && Number.isFinite(value.durationMilliseconds)
    && value.durationMilliseconds >= 0;
}

export function validateWorkflowProfile(value: unknown): string | null {
  if (!isRecord(value) || typeof value.id !== 'string' || !workflowIdPattern.test(value.id)) {
    return 'Workflow ID must be a lowercase key with letters, numbers, dots, dashes, or underscores.';
  }
  if (typeof value.name !== 'string' || value.name.trim().length === 0 || value.name.length > 128) {
    return 'Workflow name is required and must be at most 128 characters.';
  }
  if (value.variables !== undefined && !isRecord(value.variables)) {
    return 'Workflow variables must be a JSON object.';
  }
  const variables = isRecord(value.variables) ? value.variables : {};
  if (value.variableReferences !== undefined) {
    if (!isRecord(value.variableReferences) || Object.keys(value.variableReferences).length > 64) return 'Workflow variable references are invalid.';
    for (const [alias, name] of Object.entries(value.variableReferences)) {
      if (!workflowVariableKeyPattern.test(alias) || typeof name !== 'string' || !/^[A-Za-z_][A-Za-z0-9_.-]{0,191}$/.test(name)
        || ['input', 'variables', '__proto__', 'constructor', 'prototype'].includes(name) || /^(secret|system)\./i.test(name)) return 'Workflow variable references are invalid.';
    }
  }
  if (Object.keys(variables).length > 64) return 'A workflow can contain at most 64 variables.';
  for (const [key, variableValue] of Object.entries(variables)) {
    if (!workflowVariableKeyPattern.test(key)) return `Workflow variable key '${key}' must start with a letter or underscore and contain only letters, numbers, dots, dashes, or underscores.`;
    if (!isJsonValue(variableValue)) return `Workflow variable '${key}' must contain a JSON value.`;
  }
  if (!Array.isArray(value.steps) || value.steps.length === 0 || value.steps.length > maximumWorkflowSteps) {
    return `Add between 1 and ${maximumWorkflowSteps} workflow steps.`;
  }

  const stepIds = new Set<string>();
  const previousStepIds = new Set<string>();
  for (const rawStep of value.steps) {
    if (!isRecord(rawStep) || typeof rawStep.id !== 'string' || !stepIdPattern.test(rawStep.id)) {
      return 'Every step needs a valid lowercase step ID.';
    }
    if (stepIds.has(rawStep.id)) return `Duplicate step ID '${rawStep.id}'.`;
    if (!WORKFLOW_PROFILE_MODULES.some((module) => module.id === rawStep.moduleId)) {
      return `Step '${rawStep.id}' uses an unsupported profile module.`;
    }
    if (typeof rawStep.profileId !== 'string' || !profileIdPattern.test(rawStep.profileId)) {
      return `Step '${rawStep.id}' needs a valid saved profile ID.`;
    }
    if (!Array.isArray(rawStep.inputs) || rawStep.inputs.length > maximumBindingsPerStep) {
      return `Step '${rawStep.id}' has too many input mappings.`;
    }

    const targetPaths: string[][] = [];
    for (const rawBinding of rawStep.inputs) {
      if (!isRecord(rawBinding) || typeof rawBinding.targetJsonPointer !== 'string') {
        return `Step '${rawStep.id}' has an invalid input mapping.`;
      }
      const targetPath = parseJsonPointer(rawBinding.targetJsonPointer, false);
      if (!targetPath) return `Step '${rawStep.id}' has an invalid destination JSON pointer.`;

      const hasLiteral = typeof rawBinding.literalPresent === 'boolean'
        ? rawBinding.literalPresent
        : Object.hasOwn(rawBinding, 'literal');
      const hasStepReference = Object.hasOwn(rawBinding, 'sourceStepId') || Object.hasOwn(rawBinding, 'sourceJsonPointer');
      if (hasLiteral === hasStepReference) return 'Each input mapping needs exactly one literal or earlier-step source.';
      if (hasLiteral && jsonByteLength(rawBinding.literal) === null) return 'An input mapping literal must be valid JSON.';
      if (hasLiteral && jsonByteLength(rawBinding.literal)! > maximumStructuredDataBytes) return 'An input mapping literal is too large.';
      if (hasStepReference) {
        if (typeof rawBinding.sourceStepId !== 'string'
          || (rawBinding.sourceStepId !== '$input' && !previousStepIds.has(rawBinding.sourceStepId))) {
          return 'Input mappings can only read initial input or output from an earlier step.';
        }
        if (typeof rawBinding.sourceJsonPointer !== 'string' || !parseJsonPointer(rawBinding.sourceJsonPointer, true)) {
          return `Step '${rawStep.id}' has an invalid source JSON pointer.`;
        }
      }

      for (const priorPath of targetPaths) {
        const commonLength = Math.min(priorPath.length, targetPath.length);
        if (priorPath.slice(0, commonLength).every((segment, index) => segment === targetPath[index])) {
          return `Step '${rawStep.id}' has conflicting destination JSON pointers.`;
        }
      }
      targetPaths.push(targetPath);
    }

    stepIds.add(rawStep.id);
    previousStepIds.add(rawStep.id);
  }

  if (jsonByteLength(value) === null || jsonByteLength(value)! > maximumWorkflowInputBytes) {
    return 'A saved workflow exceeds the 48 KiB limit.';
  }
  return null;
}

export function parseWorkflowVariableEntries(entries: readonly WorkflowVariableEntry[]): {
  variables?: Record<string, WorkflowJsonValue>;
  error?: string;
} {
  if (entries.length > 64) return { error: 'A workflow can contain at most 64 variables.' };
  const variables = Object.create(null) as Record<string, WorkflowJsonValue>;
  for (const entry of entries) {
    const key = entry.key.trim();
    if (!workflowVariableKeyPattern.test(key)) {
      return { error: 'Variable keys must start with a letter or underscore and contain only letters, numbers, dots, dashes, or underscores.' };
    }
    if (Object.hasOwn(variables, key)) return { error: `Duplicate workflow variable key '${key}'.` };
    let value: unknown;
    try { value = JSON.parse(entry.valueText); }
    catch { return { error: `Workflow variable '${key}' must contain valid JSON.` }; }
    if (!isJsonValue(value)) return { error: `Workflow variable '${key}' must contain a JSON value.` };
    variables[key] = value;
  }
  if (jsonByteLength(variables) === null || jsonByteLength(variables)! > maximumWorkflowInputBytes) {
    return { error: 'Workflow variables exceed the 48 KiB limit.' };
  }
  return { variables };
}

export function readWorkflowProfile(value: unknown): WorkflowProfile | null {
  if (validateWorkflowProfile(value) !== null || !isRecord(value)) return null;
  return { ...value, variables: isRecord(value.variables) ? value.variables as Record<string, WorkflowJsonValue> : {} } as WorkflowProfile;
}

export function readWorkflowRunResult(value: unknown): WorkflowRunResult | null {
  if (!isRecord(value) || !Array.isArray(value.steps) || !validateSummary(value.summary)) return null;
  if (jsonByteLength(value) === null || jsonByteLength(value)! > maximumStructuredDataBytes) return null;
  const steps: WorkflowStepResult[] = [];
  for (const step of value.steps) {
    if (!isRecord(step) || typeof step.stepId !== 'string' || typeof step.moduleId !== 'string'
        || !WORKFLOW_PROFILE_MODULES.some((module) => module.id === step.moduleId)
        || typeof step.profileId !== 'string' || !['success', 'information', 'warning', 'error'].includes(String(step.status))
        || !Object.hasOwn(step, 'output') || !validateSummary(step.summary)) return null;
    steps.push(step as unknown as WorkflowStepResult);
  }
  if (!Object.hasOwn(value, 'output')) return null;
  return value as unknown as WorkflowRunResult;
}
