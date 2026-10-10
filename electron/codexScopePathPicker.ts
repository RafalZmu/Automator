import type { OpenDialogOptions } from 'electron';

export type CodexScopePathKind = 'file' | 'directory';

/** Returns host-owned native dialog options for paths added to a Codex task's declared scope. */
export function getCodexScopePathPickerSpec(kind: unknown, selectedTab: number, activeLauncher: boolean): OpenDialogOptions {
  if (selectedTab !== 9 || !activeLauncher)
    throw new Error('Path selection is only available in the active Codex tab.');
  if (kind === 'file') return {
    title: 'Select a file for task scope',
    properties: ['openFile'],
    filters: [{ name: 'All files', extensions: ['*'] }],
  };
  if (kind === 'directory') return {
    title: 'Select a folder for task scope',
    properties: ['openDirectory'],
  };
  throw new Error('The path picker kind is invalid.');
}
