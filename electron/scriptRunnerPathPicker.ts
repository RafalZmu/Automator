import type { OpenDialogOptions } from 'electron';

export type ScriptRunnerPathKind = 'file' | 'directory';

/** Validates renderer input and returns host-owned native dialog options. */
export function getScriptRunnerPathPickerSpec(kind: unknown, selectedTab: number, activeLauncher: boolean): OpenDialogOptions {
  if (selectedTab !== 2 || !activeLauncher)
    throw new Error('Path selection is only available in the active Script Runner tab.');
  if (kind === 'file') return {
    title: 'Select a file', properties: ['openFile'],
    filters: [{ name: 'All files', extensions: ['*'] }],
  };
  if (kind === 'directory') return {
    title: 'Select a directory', properties: ['openDirectory', 'createDirectory'],
  };
  throw new Error('The path picker kind is invalid.');
}
