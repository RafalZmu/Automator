import { randomUUID } from 'node:crypto';
import type { AutomationResult } from '../../contracts/rpc.ts';

export type RunState = 'running' | 'success' | 'failed' | 'timedOut' | 'canceled' | 'error';
export type RunKind = 'script' | 'browser-profile' | 'browser-tests';

export type RunRecord = {
  runId: string;
  kind: RunKind;
  moduleId: string;
  requestId: string;
  targetId: string;
  state: RunState;
  startedAt: string;
  finishedAt: string | null;
  message: string;
  data: unknown;
};

export type RunSpec = {
  kind: RunKind;
  moduleId: string;
  targetId: string;
  execute: (requestId: string) => Promise<AutomationResult>;
  classify?: (result: AutomationResult) => RunState;
};

export class RunLimitError extends Error {}

type Entry = { record: RunRecord; done: Promise<void> };

const defaultClassify = (result: AutomationResult): RunState => (result.status === 'error' ? 'failed' : 'success');

function isCanceled(error: unknown): boolean {
  const candidate = error as { code?: unknown; data?: { category?: unknown } } | null;
  return candidate?.code === -32800 || candidate?.data?.category === 'canceled';
}

/** Holds MCP-started runs in memory only. The backend blocks per call, so the host keeps the call open. */
export class RunRegistry {
  private readonly entries = new Map<string, Entry>();
  private readonly cancelBackend: (moduleId: string, requestId: string) => Promise<boolean>;
  private readonly options: { maxRuns?: number; maxConcurrent?: number; now?: () => Date };

  constructor(
    cancelBackend: (moduleId: string, requestId: string) => Promise<boolean>,
    options: { maxRuns?: number; maxConcurrent?: number; now?: () => Date } = {},
  ) {
    this.cancelBackend = cancelBackend;
    this.options = options;
  }

  start(spec: RunSpec): RunRecord {
    const maxConcurrent = this.options.maxConcurrent ?? 4;
    if ([...this.entries.values()].filter((entry) => entry.record.state === 'running').length >= maxConcurrent)
      throw new RunLimitError(`At most ${maxConcurrent} runs can be active at once. Wait for one to finish or cancel it.`);
    const now = this.options.now ?? (() => new Date());
    const record: RunRecord = {
      runId: randomUUID(), kind: spec.kind, moduleId: spec.moduleId, requestId: `mcp-${randomUUID()}`,
      targetId: spec.targetId, state: 'running', startedAt: now().toISOString(), finishedAt: null, message: '', data: null,
    };
    const classify = spec.classify ?? defaultClassify;
    const done = Promise.resolve()
      .then(() => spec.execute(record.requestId))
      .then((result) => {
        record.state = classify(result);
        record.message = result.message;
        record.data = result.data;
      }, (error: unknown) => {
        record.state = isCanceled(error) ? 'canceled' : 'error';
        record.message = isCanceled(error) ? 'The run was canceled.' : (error instanceof Error ? error.message : String(error));
      })
      .then(() => { record.finishedAt = now().toISOString(); });
    this.entries.set(record.runId, { record, done });
    this.evict();
    return record;
  }

  get(runId: string): RunRecord | undefined { return this.entries.get(runId)?.record; }

  list(): RunRecord[] { return [...this.entries.values()].map((entry) => entry.record).reverse(); }

  /** Resolves when the run finishes or after waitMs, whichever comes first. */
  async wait(runId: string, waitMs: number): Promise<RunRecord | undefined> {
    const entry = this.entries.get(runId);
    if (!entry) return undefined;
    if (entry.record.state !== 'running' || waitMs <= 0) return entry.record;
    let timer: NodeJS.Timeout | undefined;
    await Promise.race([entry.done, new Promise<void>((resolve) => { timer = setTimeout(resolve, waitMs); })]);
    if (timer) clearTimeout(timer);
    return entry.record;
  }

  async cancel(runId: string): Promise<boolean> {
    const record = this.entries.get(runId)?.record;
    if (!record || record.state !== 'running') return false;
    return this.cancelBackend(record.moduleId, record.requestId);
  }

  async cancelAll(): Promise<void> {
    const running = [...this.entries.values()].filter((entry) => entry.record.state === 'running');
    await Promise.allSettled(running.map((entry) => this.cancelBackend(entry.record.moduleId, entry.record.requestId)));
    await Promise.race([Promise.allSettled(running.map((entry) => entry.done)), new Promise((resolve) => setTimeout(resolve, 3000))]);
  }

  private evict(): void {
    const maxRuns = this.options.maxRuns ?? 50;
    for (const [runId, entry] of this.entries) {
      if (this.entries.size <= maxRuns) return;
      if (entry.record.state !== 'running') this.entries.delete(runId);
    }
  }
}
