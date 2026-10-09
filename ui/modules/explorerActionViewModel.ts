import type { FileExplorerLaunchRequest } from '../../contracts/rpc';
import type { ExplorerActionDefinition, ScriptTemplateDescriptor, ScriptTemplateValue } from '../../contracts/scriptRunner';
import { initialTemplateValues } from './scriptTemplateViewModel.ts';
export type ExplorerProfile = { id: string; name: string; arguments: string[]; templateOrigin?: { id: string; version: number } | null };
export type ExplorerRunForm = { request: FileExplorerLaunchRequest; action: ExplorerActionDefinition; profile: ExplorerProfile; arguments: string[]; template?: ScriptTemplateDescriptor; values: Record<string, ScriptTemplateValue> };
export function normalizeExplorerExtensions(value: string): string[] {
  const extensions = value.trim().split(/[\s,;]+/).filter(Boolean).map(item => `.${item.replace(/^\./, '').toLowerCase()}`);
  if (!extensions.length || extensions.length > 32 || extensions.some(item => !/^\.[a-z0-9][a-z0-9_-]{0,31}$/.test(item))) throw new Error('Enter file extensions such as .fdb, .txt.');
  return [...new Set(extensions)];
}
export function registrationNotice(value: unknown): string {
  if (!value || typeof value !== 'object' || !('state' in value) || value.state === 'updated') return '';
  return 'message' in value && typeof value.message === 'string' ? value.message : value.state === 'error' ? 'The Explorer menu could not be updated. Reopen this section to retry.' : 'Explorer menu registration is available in the installed Windows build.';
}
export function prepareExplorerForm(request: FileExplorerLaunchRequest, actions: ExplorerActionDefinition[], profiles: ExplorerProfile[], templates: ScriptTemplateDescriptor[]): ExplorerRunForm {
  const action = actions.find(item => item.id === request.actionId);
  if (!action) throw new Error('The selected Explorer action no longer exists.');
  const profile = profiles.find(item => item.id === action.profileId);
  if (!profile) throw new Error('The selected script profile no longer exists.');
  if (!/^(?:[a-z]:[\\/]|\\\\[^\\/]+[\\/][^\\/]+[\\/])/i.test(request.filePath) || /[\x00-\x1f]/.test(request.filePath)) throw new Error('The selected file must have an absolute Windows path.');
  const extension = /\.[^\\/.]+$/.exec(request.filePath)?.[0].toLowerCase();
  if (!extension || !action.extensions.includes(extension)) throw new Error('The selected file extension does not match this Explorer action.');
  let template: ScriptTemplateDescriptor | undefined;
  let values: Record<string, ScriptTemplateValue> = {};
  if (profile.templateOrigin) {
    template = templates.find(item => item.id === profile.templateOrigin?.id && item.version === profile.templateOrigin.version);
    if (!template) throw new Error('This template version is unavailable. Remove and reinstall its profile.');
    if (!template.parameters.some(item => item.key === action.fileParameterKey && (item.type === 'file' || item.type === 'directory'))) throw new Error('The Explorer action needs a declared file or directory template parameter.');
    values = { ...initialTemplateValues(template), [action.fileParameterKey!]: request.filePath };
  } else if (!profile.arguments.some(item => item.includes('{{file.path}}'))) throw new Error('This profile needs {{file.path}} in its saved arguments.');
  return { request, action, profile, arguments: profile.arguments.map(item => item.replaceAll('{{file.path}}', request.filePath)), template, values };
}
