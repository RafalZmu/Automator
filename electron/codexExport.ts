import fs from 'node:fs';
import type { BackendUiState } from '../contracts/rpc';
import type { AutomationWindowContext } from './automationServiceAuthorization';

const draftIdPattern = /^[0-9a-f]{32}$/i;

export function authorizeCodexExport(
  isMainFrame: boolean,
  context: AutomationWindowContext | null,
  state: BackendUiState | undefined,
  draftId: unknown,
): asserts draftId is string {
  if (!isMainFrame) throw new Error('Codex export is available only to the renderer main frame.');
  if (!context || context.selectedTab !== 9 || (context.role === 'launcher' && (!state?.visible || state.mode !== 'launcher')))
    throw new Error('Codex export is only available in the active Codex tab.');
  const module = state?.tabs.find((tab) => tab.slot === 9);
  if (module?.id !== 'codex' || !module.capabilities.some((capability) => capability.id === 'codex.task-builder' && capability.version === 1))
    throw new Error('The active tab has not declared Codex task-builder access.');
  if (typeof draftId !== 'string' || !draftIdPattern.test(draftId)) throw new Error('Codex draft ID is invalid.');
}

export function extensionForCodexFormat(format: unknown): string {
  if (format === 'python') return 'py';
  if (format === 'playwright') return 'spec.ts';
  if (format === 'workflow') return 'json';
  throw new Error('Codex draft format is invalid.');
}

export async function writeCodexSourceCreateNew(destination: string, source: string): Promise<void> {
  if (!destination || typeof source !== 'string' || Buffer.byteLength(source, 'utf8') > 256 * 1024)
    throw new Error('Codex source is invalid or exceeds the export limit.');
  const handle = await fs.promises.open(destination, 'wx');
  try { await handle.writeFile(source, { encoding: 'utf8' }); }
  finally { await handle.close(); }
}
