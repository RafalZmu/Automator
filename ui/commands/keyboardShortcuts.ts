import type { TabCommand } from './commandMatching';

export type ShortcutSurface = 'launcher' | 'workspace';
export type GlobalShortcutId = 'find' | 'shortcuts' | 'settings' | 'next-tab' | 'previous-tab';

export type ShortcutEvent = {
  key: string;
  ctrlKey: boolean;
  shiftKey: boolean;
  altKey: boolean;
  metaKey: boolean;
  isComposing?: boolean;
  target?: EventTarget | null;
};

export type ShortcutReferenceRow = { keys: readonly string[]; label: string };

/** Chords available on every Automator surface. Each one needs Ctrl, Alt or a function key, so typing is never intercepted. */
export const globalShortcuts: readonly ({ id: GlobalShortcutId; surfaces: readonly ShortcutSurface[] } & ShortcutReferenceRow)[] = [
  { id: 'find', keys: ['Ctrl+K'], label: 'Find an app, tab or action', surfaces: ['launcher', 'workspace'] },
  { id: 'shortcuts', keys: ['Ctrl+/', 'F1'], label: 'Show this list', surfaces: ['launcher', 'workspace'] },
  { id: 'settings', keys: ['Ctrl+,'], label: 'Open Settings', surfaces: ['launcher'] },
  { id: 'next-tab', keys: ['Ctrl+Tab'], label: 'Next tab', surfaces: ['launcher', 'workspace'] },
  { id: 'previous-tab', keys: ['Ctrl+Shift+Tab'], label: 'Previous tab', surfaces: ['launcher', 'workspace'] },
];

/** Digits and Escape are handled by the same keyboard layer on both surfaces; they are listed here for the reference panel. */
export function anywhereShortcuts(surface: ShortcutSurface): readonly ShortcutReferenceRow[] {
  return [
    ...globalShortcuts.filter((shortcut) => shortcut.surfaces.includes(surface)).map(({ keys, label }) => ({ keys, label })),
    { keys: ['1–9'], label: 'Switch to a tab (not while typing)' },
    { keys: ['Esc'], label: surface === 'launcher' ? 'Close the launcher' : 'Close the panel, leave a text field, then hide the workspace' },
  ];
}

/** Launcher panel keys that are not commands: they act on the application list and the add-app flow. */
export const launcherShortcuts: readonly ShortcutReferenceRow[] = [
  { keys: ['/'], label: 'Add an application' },
  { keys: ['↑', '↓'], label: 'Move through the application list' },
  { keys: ['Enter'], label: 'Open the first match in the app search' },
];

/** Splits "Ctrl+Shift+N" into its key and modifiers. The key is always lower case so it compares with event.key. */
export function parseShortcut(keys: string): { key: string; ctrl: boolean; shift: boolean; alt: boolean } {
  const parts = keys.split('+').map((part) => part.trim().toLocaleLowerCase()).filter(Boolean);
  const key = parts.pop() ?? '';
  return { key, ctrl: parts.includes('ctrl'), shift: parts.includes('shift'), alt: parts.includes('alt') };
}

/**
 * Exact modifier match: Ctrl+N does not fire for Ctrl+Shift+N, and IME composition never triggers a chord.
 * Symbols ignore Shift because some layouts need it to type them (Ctrl+/ is Ctrl+Shift+7 on a Polish keyboard).
 */
export function shortcutMatches(keys: string, event: ShortcutEvent): boolean {
  if (event.isComposing || event.metaKey) return false;
  const chord = parseShortcut(keys);
  const symbol = chord.key.length === 1 && !/[a-z0-9]/.test(chord.key);
  return event.key.toLocaleLowerCase() === chord.key
    && event.ctrlKey === chord.ctrl
    && event.altKey === chord.alt
    && (symbol || event.shiftKey === chord.shift);
}

/** Text controls and contenteditable regions own typed characters. */
export function isEditableTarget(target: EventTarget | null | undefined): boolean {
  const element = target as { tagName?: unknown; isContentEditable?: unknown } | null | undefined;
  if (!element || typeof element.tagName !== 'string') return false;
  return element.isContentEditable === true || ['INPUT', 'TEXTAREA', 'SELECT'].includes(element.tagName.toLocaleUpperCase());
}

/** A printable key without Ctrl or Alt would type into a text control, so it only acts outside editable targets. */
export function shortcutAllowed(keys: string, event: ShortcutEvent): boolean {
  if (!shortcutMatches(keys, event)) return false;
  const chord = parseShortcut(keys);
  const printable = chord.key.length === 1 && !chord.ctrl && !chord.alt;
  return !printable || !isEditableTarget(event.target);
}

export function globalShortcutFor(event: ShortcutEvent, surface: ShortcutSurface): GlobalShortcutId | undefined {
  return globalShortcuts.find((shortcut) => shortcut.surfaces.includes(surface)
    && shortcut.keys.some((keys) => shortcutAllowed(keys, event)))?.id;
}

/**
 * Tab command shortcuts run only enabled commands that need no confirmation. Destructive or externally visible
 * actions stay in the tab action search, where the confirmation can be shown and answered.
 */
export function findShortcutCommand<T extends TabCommand>(commands: readonly T[], event: ShortcutEvent): T | undefined {
  return commands.find((command) => command.shortcut !== undefined && !command.disabled && !command.confirmationPrompt
    && shortcutAllowed(command.shortcut, event));
}
