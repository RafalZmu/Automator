import assert from 'node:assert/strict';
import { test } from 'node:test';
import { canAutoRunTabCommand, matchTabCommands, resolveTabCommand } from '../ui/commands/commandMatching.ts';
import { findShortcutCommand, globalShortcutFor, isEditableTarget, parseShortcut, shortcutAllowed, shortcutMatches } from '../ui/commands/keyboardShortcuts.ts';

const commands = [
  { id: 'new-script', label: 'New script profile', keywords: ['create', 'script'] },
  { id: 'run-build', label: 'Run build', keywords: ['execute', 'profile'] },
  { id: 'run-tests', label: 'Run Playwright tests', keywords: ['browser', 'test'] },
];

test('command search matches every query word against a command label or keywords', () => {
  assert.deepEqual(matchTabCommands(commands, 'create script').map((command) => command.id), ['new-script']);
  assert.deepEqual(matchTabCommands(commands, 'playwright browser').map((command) => command.id), ['run-tests']);
});

test('command search ignores case and diacritics and returns no results for an unknown phrase', () => {
  assert.deepEqual(matchTabCommands([{ ...commands[0], label: 'Créer un profil' }], 'CREER profil').map((command) => command.id), ['new-script']);
  assert.deepEqual(matchTabCommands(commands, 'missing action'), []);
});

test('website aliases work directly in their tab and use a trailing w to disambiguate global search', () => {
  const app = { id: 'app-docs', label: 'Documentation app', keywords: ['docs'] };
  const website = { id: 'website-docs', label: 'docs · Project sites', keywords: ['docs', 'docsw'] };
  assert.deepEqual(matchTabCommands([website], 'docs').map((command) => command.id), ['website-docs']);
  assert.deepEqual(matchTabCommands([app, website], 'docsw').map((command) => command.id), ['website-docs']);
});

test('a command executes from Enter only when the query has one unique match', () => {
  assert.deepEqual(resolveTabCommand(matchTabCommands(commands, 'run')), { kind: 'ambiguous' });
  assert.deepEqual(resolveTabCommand(matchTabCommands(commands, 'run build')), { kind: 'unique', command: commands[1] });
  assert.deepEqual(resolveTabCommand(matchTabCommands(commands, 'nothing')), { kind: 'none' });
});

test('delayed automatic execution is limited to unique enabled actions without confirmation prompts', () => {
  assert.equal(canAutoRunTabCommand(commands[0]), true);
  assert.equal(canAutoRunTabCommand({ ...commands[0], disabled: true }), false);
  assert.equal(canAutoRunTabCommand({ ...commands[0], confirmationPrompt: 'Delete this profile?' }), false);
  assert.equal(canAutoRunTabCommand(undefined), false);
});

const plain = (key, extra = {}) => ({ key, ctrlKey: false, shiftKey: false, altKey: false, metaKey: false, target: { tagName: 'DIV', isContentEditable: false }, ...extra });
const ctrl = (key, extra = {}) => plain(key, { ctrlKey: true, ...extra });

test('shortcut chords parse into a lower-case key and exact modifiers', () => {
  assert.deepEqual(parseShortcut('Ctrl+Shift+Tab'), { key: 'tab', ctrl: true, shift: true, alt: false });
  assert.deepEqual(parseShortcut('Ctrl+/'), { key: '/', ctrl: true, shift: false, alt: false });
  assert.deepEqual(parseShortcut('F1'), { key: 'f1', ctrl: false, shift: false, alt: false });
});

test('shortcut matching requires the exact modifier set and ignores IME composition', () => {
  assert.equal(shortcutMatches('Ctrl+N', ctrl('n')), true);
  assert.equal(shortcutMatches('Ctrl+N', ctrl('N', { shiftKey: true })), false, 'Ctrl+Shift+N must not run Ctrl+N');
  assert.equal(shortcutMatches('Ctrl+N', ctrl('n', { altKey: true })), false, 'AltGr-style chords must not run Ctrl+N');
  assert.equal(shortcutMatches('Ctrl+N', ctrl('n', { metaKey: true })), false);
  assert.equal(shortcutMatches('Ctrl+N', ctrl('n', { isComposing: true })), false);
  assert.equal(shortcutMatches('Ctrl+Shift+Tab', plain('Tab', { ctrlKey: true, shiftKey: true })), true);
  assert.equal(shortcutMatches('Ctrl+Tab', plain('Tab', { ctrlKey: true, shiftKey: true })), false);
  assert.equal(shortcutMatches('Ctrl+/', ctrl('/', { shiftKey: true })), true, 'Polish layouts type / with Shift');
  assert.equal(shortcutMatches('Ctrl+,', ctrl(',')), true);
});

test('unmodified printable shortcuts never fire inside text controls', () => {
  const inInput = { tagName: 'INPUT', isContentEditable: false };
  const inEditor = { tagName: 'DIV', isContentEditable: true };
  assert.equal(shortcutAllowed('S', plain('s')), true);
  assert.equal(shortcutAllowed('S', plain('S', { shiftKey: true })), false, 'Shift+S is not the S shortcut');
  assert.equal(shortcutAllowed('S', plain('s', { target: inInput })), false);
  assert.equal(shortcutAllowed('S', plain('s', { target: { tagName: 'TEXTAREA' } })), false);
  assert.equal(shortcutAllowed('S', plain('s', { target: inEditor })), false);
  assert.equal(shortcutAllowed('S', plain('s', { target: { tagName: 'BUTTON' } })), true);
  assert.equal(isEditableTarget(inInput), true);
  assert.equal(isEditableTarget(null), false);
});

test('modifier and function-key chords work from text controls', () => {
  const inInput = { tagName: 'INPUT' };
  assert.equal(shortcutAllowed('Ctrl+S', ctrl('s', { target: inInput })), true);
  assert.equal(shortcutAllowed('Ctrl+/', ctrl('/', { target: { tagName: 'TEXTAREA' } })), true);
  assert.equal(shortcutAllowed('F1', plain('F1', { target: inInput })), true);
});

test('global shortcuts route per surface and never steal the chord from other surfaces', () => {
  assert.equal(globalShortcutFor(ctrl('k'), 'launcher'), 'find');
  assert.equal(globalShortcutFor(ctrl('k'), 'workspace'), 'find');
  assert.equal(globalShortcutFor(ctrl(','), 'launcher'), 'settings');
  assert.equal(globalShortcutFor(ctrl(','), 'workspace'), undefined, 'Settings is launcher-only');
  assert.equal(globalShortcutFor(plain('F1'), 'workspace'), 'shortcuts');
  assert.equal(globalShortcutFor(ctrl('/'), 'launcher'), 'shortcuts');
  assert.equal(globalShortcutFor(plain('Tab', { ctrlKey: true }), 'workspace'), 'next-tab');
  assert.equal(globalShortcutFor(plain('Tab', { ctrlKey: true, shiftKey: true }), 'workspace'), 'previous-tab');
  assert.equal(globalShortcutFor(plain('Tab'), 'workspace'), undefined, 'plain Tab keeps normal focus movement');
  assert.equal(globalShortcutFor(plain('k'), 'launcher'), undefined, 'plain K types into the search');
});

test('tab shortcuts run only enabled commands without confirmation', () => {
  const tabCommands = [
    { id: 'delete', label: 'Delete', shortcut: 'Ctrl+D', confirmationPrompt: 'Delete?', run: () => undefined },
    { id: 'disabled-new', label: 'New', shortcut: 'Ctrl+N', disabled: true, run: () => undefined },
    { id: 'new', label: 'New item', shortcut: 'Ctrl+N', run: () => undefined },
    { id: 'save', label: 'Save', shortcut: 'Ctrl+S', run: () => undefined },
  ];
  assert.equal(findShortcutCommand(tabCommands, ctrl('n'))?.id, 'new');
  assert.equal(findShortcutCommand(tabCommands, ctrl('d')), undefined, 'confirmation-gated actions stay in the action search');
  assert.equal(findShortcutCommand(tabCommands, ctrl('s', { target: { tagName: 'INPUT' } }))?.id, 'save');
  assert.equal(findShortcutCommand(tabCommands, ctrl('x')), undefined);
});
