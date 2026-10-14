import { useSyncExternalStore } from 'react';
import type { AutomationModuleActionRequest, AutomationResult } from '../../contracts/rpc';
import type { ScriptTemplateValue } from '../../contracts/scriptRunner';
import { rememberTransientResult } from '../activity/transientResults.ts';

/** Output of one script run. It stays in this window's memory only and is never persisted or logged. */
export type ScriptRunOutput = {
  profileId: string;
  exitCode: number | null;
  timedOut: boolean;
  stdout: string;
  stderr: string;
  stdoutTruncated: boolean;
  stderrTruncated: boolean;
  durationMilliseconds: number;
  structuredOutput: unknown;
  parseFailure: boolean;
};

export type ScriptRunState = 'running' | 'success' | 'failed' | 'timedOut' | 'canceled' | 'error';

export type ScriptRunRecord = {
  id: string;
  profileId: string;
  profileName: string;
  startedAt: number;
  finishedAt: number | null;
  state: ScriptRunState;
  message: string;
  output: ScriptRunOutput | null;
};

type RunBridge = {
  dispatchModuleAction(request: AutomationModuleActionRequest): Promise<AutomationResult>;
  cancelAutomationModuleAction(moduleId: string, requestId: string): Promise<unknown>;
};

export type ScriptRunTarget = {
  bridge: RunBridge;
  moduleId: string;
  contractVersion: number;
  action: { id: string; version: number };
};

const MAXIMUM_RECORDS = 30;
const listeners = new Set<() => void>();
let records: readonly ScriptRunRecord[] = [];
const targets = new Map<string, { bridge: RunBridge; moduleId: string }>();

const emit = () => { for (const listener of listeners) { try { listener(); } catch { /* One view cannot block another. */ } } };
const replace = (id: string, change: (record: ScriptRunRecord) => ScriptRunRecord) => {
  records = records.map((record) => record.id === id ? change(record) : record);
  emit();
};

function readOutput(data: unknown): ScriptRunOutput | null {
  return data && typeof data === 'object' && 'profileId' in data ? data as ScriptRunOutput : null;
}

function finishedState(output: ScriptRunOutput | null, status: AutomationResult['status']): ScriptRunState {
  if (output?.timedOut) return 'timedOut';
  if (output && output.exitCode !== null && output.exitCode !== 0) return 'failed';
  return status === 'error' ? 'error' : 'success';
}

function remember(record: ScriptRunRecord) {
  records = [record, ...records.filter((existing) => existing.id !== record.id)].slice(0, MAXIMUM_RECORDS);
  emit();
}

/**
 * Starts a run that is owned by this window rather than by the Script Runner view, so closing the compact window
 * or switching tabs does not hide or abort it. At most one run is active at a time.
 */
export async function startScriptRun(
  target: ScriptRunTarget, profile: { id: string; name: string }, values: Record<string, ScriptTemplateValue> | undefined,
): Promise<ScriptRunRecord> {
  if (activeScriptRun()) throw new Error('A script is already running. Wait for it or cancel it first.');
  const id = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  const started: ScriptRunRecord = { id, profileId: profile.id, profileName: profile.name, startedAt: Date.now(), finishedAt: null, state: 'running', message: '', output: null };
  targets.set(id, { bridge: target.bridge, moduleId: target.moduleId });
  remember(started);
  try {
    const result = await target.bridge.dispatchModuleAction({
      requestId: id,
      contractVersion: target.contractVersion,
      moduleId: target.moduleId,
      actionId: target.action.id,
      actionVersion: target.action.version,
      input: { id: profile.id, ...(values ? { values } : {}) } as AutomationModuleActionRequest['input'],
    });
    rememberTransientResult(id, target.moduleId, target.action.id, result);
    const output = readOutput(result.data);
    let finished: ScriptRunRecord = started;
    replace(id, (record) => finished = { ...record, finishedAt: Date.now(), state: record.state === 'canceled' ? 'canceled' : finishedState(output, result.status), message: result.message, output });
    return finished;
  } catch (error) {
    let finished: ScriptRunRecord = started;
    replace(id, (record) => finished = {
      ...record, finishedAt: Date.now(), state: record.state === 'canceled' ? 'canceled' : 'error',
      message: record.state === 'canceled' ? 'The run was canceled.' : error instanceof Error ? error.message : 'The script could not be run.',
    });
    return finished;
  } finally {
    targets.delete(id);
  }
}

/** Adds a run started elsewhere (File Explorer actions) so it appears in the same history. */
export function recordExternalScriptRun(profile: { id: string; name: string }, result: { status: AutomationResult['status']; message: string; data: unknown }) {
  const output = readOutput(result.data);
  const now = Date.now();
  remember({
    id: globalThis.crypto?.randomUUID?.() ?? `${now}-${Math.random().toString(16).slice(2)}`,
    profileId: profile.id, profileName: profile.name,
    startedAt: now - (output?.durationMilliseconds ?? 0), finishedAt: now,
    state: finishedState(output, result.status), message: result.message, output,
  });
}

export async function cancelScriptRun(id: string): Promise<void> {
  const target = targets.get(id);
  if (!target) return;
  replace(id, (record) => record.state === 'running' ? { ...record, state: 'canceled' } : record);
  await target.bridge.cancelAutomationModuleAction(target.moduleId, id).catch(() => undefined);
}

export function activeScriptRun(): ScriptRunRecord | undefined { return records.find((record) => record.finishedAt === null); }
export function getScriptRuns(): readonly ScriptRunRecord[] { return records; }
export function clearFinishedScriptRuns() {
  records = records.filter((record) => record.finishedAt === null);
  emit();
}
export function subscribeScriptRuns(listener: () => void): () => void {
  listeners.add(listener);
  return () => { listeners.delete(listener); };
}
export function useScriptRuns(): readonly ScriptRunRecord[] { return useSyncExternalStore(subscribeScriptRuns, getScriptRuns); }

export function describeRunState(state: ScriptRunState): string {
  switch (state) {
    case 'running': return 'Running';
    case 'success': return 'Succeeded';
    case 'failed': return 'Failed';
    case 'timedOut': return 'Timed out';
    case 'canceled': return 'Canceled';
    case 'error': return 'Error';
  }
}
