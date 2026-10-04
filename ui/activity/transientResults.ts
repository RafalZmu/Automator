import type { AutomationResult } from '../../contracts/rpc';

export type TransientRunResult = { id: string; moduleId: string; actionId: string; result: AutomationResult };
const results = new Map<string, TransientRunResult>();
const listeners = new Set<() => void>();

export function isRunAction(moduleId: string, actionId: string): boolean {
  return (moduleId === 'script-runner' && ['runProfile', 'runAgain'].includes(actionId))
    || (moduleId === 'api' && actionId === 'runProfile')
    || (moduleId === 'browser-automation' && ['runTests', 'runAction', 'repeatAction'].includes(actionId))
    || (moduleId === 'workflows' && actionId === 'runWorkflow');
}

/** Deliberately memory-only; no browser storage, export, or logging touches result data. */
export function rememberTransientResult(id: string, moduleId: string, actionId: string, result: AutomationResult): void {
  if (!isRunAction(moduleId, actionId)) return;
  results.delete(id);
  results.set(id, { id, moduleId, actionId, result });
  while (results.size > 20) results.delete(results.keys().next().value!);
  for (const listener of listeners) { try { listener(); } catch { /* A view cannot prevent another view receiving a result. */ } }
}
export function getTransientResult(id: string): TransientRunResult | undefined { return results.get(id); }
export function getLatestTransientResult(moduleId: string): TransientRunResult | undefined {
  return [...results.values()].reverse().find((value) => value.moduleId === moduleId);
}
export function subscribeTransientResults(listener: () => void): () => void {
  listeners.add(listener);
  return () => { listeners.delete(listener); };
}
export function clearTransientResults(): void {
  results.clear();
  for (const listener of listeners) { try { listener(); } catch { /* Clearing one view must not block others. */ } }
}

/** Reject returned actions not registered on the originating module and version. */
export function supportedFollowUps(result: AutomationResult, actions: readonly { id: string; version: number; command?: string | null }[]) {
  return result.actions.filter((action) => actions.some((registered) => registered.id === action.id
    && registered.version === action.version && !registered.command));
}
