import { useSyncExternalStore } from 'react';
import type { AutomationModuleActionRequest, AutomationResult } from '../../contracts/rpc';
import { rememberTransientResult } from '../activity/transientResults.ts';
import type { PlaywrightRunKind } from '../contracts/browser.ts';

type RunBridge = {
  dispatchModuleAction(request: AutomationModuleActionRequest): Promise<AutomationResult>;
  cancelAutomationModuleAction(moduleId: string, requestId: string): Promise<unknown>;
};

export type BrowserRunTarget = {
  bridge: RunBridge;
  moduleId: string;
  contractVersion: number;
  action: { id: string; version: number };
};

export type ActiveBrowserRun = { id: string; kind: PlaywrightRunKind; target: string; startedAt: number; canceling: boolean };
export type FinishedBrowserRun = { id: string; state: 'finished' | 'canceled' | 'error'; message: string; finishedAt: number };
type Snapshot = { active: ActiveBrowserRun | null; last: FinishedBrowserRun | null };

const listeners = new Set<() => void>();
let snapshot: Snapshot = { active: null, last: null };
let activeBridge: { bridge: RunBridge; moduleId: string } | null = null;

const publish = (next: Snapshot) => {
  snapshot = next;
  for (const listener of listeners) { try { listener(); } catch { /* One view cannot block another. */ } }
};

/**
 * Starts a Playwright run that belongs to this window rather than to the Browser Automation view, so closing the
 * compact window, reopening it on its home surface, or switching tabs neither hides nor aborts it. Results are
 * saved by the backend; this store only tracks what is running now. At most one run is active at a time.
 */
export async function startBrowserRun(
  target: BrowserRunTarget, kind: PlaywrightRunKind, label: string, input: Record<string, string>,
): Promise<FinishedBrowserRun> {
  if (snapshot.active) throw new Error('A Playwright run is already in progress. Wait for it or cancel it first.');
  const id = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  activeBridge = { bridge: target.bridge, moduleId: target.moduleId };
  publish({ ...snapshot, active: { id, kind, target: label, startedAt: Date.now(), canceling: false } });
  const finish = (state: FinishedBrowserRun['state'], message: string) => {
    const finished: FinishedBrowserRun = { id, state, message, finishedAt: Date.now() };
    activeBridge = null;
    publish({ active: null, last: finished });
    return finished;
  };
  try {
    const result = await target.bridge.dispatchModuleAction({
      requestId: id,
      contractVersion: target.contractVersion,
      moduleId: target.moduleId,
      actionId: target.action.id,
      actionVersion: target.action.version,
      input: { kind, ...input } as AutomationModuleActionRequest['input'],
    });
    rememberTransientResult(id, target.moduleId, target.action.id, result);
    return finish(result.status === 'error' ? 'error' : 'finished', result.message);
  } catch (error) {
    if (getBrowserRunSnapshot().active?.canceling) return finish('canceled', 'The Playwright run was canceled.');
    return finish('error', error instanceof Error ? error.message : 'The Playwright run failed.');
  }
}

export async function cancelBrowserRun(): Promise<void> {
  const active = snapshot.active;
  const bridge = activeBridge;
  if (!active || !bridge || active.canceling) return;
  publish({ ...snapshot, active: { ...active, canceling: true } });
  await bridge.bridge.cancelAutomationModuleAction(bridge.moduleId, active.id).catch(() => undefined);
}

export const getBrowserRunSnapshot = (): Snapshot => snapshot;
export function subscribeBrowserRun(listener: () => void): () => void {
  listeners.add(listener);
  return () => { listeners.delete(listener); };
}
export function useBrowserRun(): Snapshot { return useSyncExternalStore(subscribeBrowserRun, getBrowserRunSnapshot); }
