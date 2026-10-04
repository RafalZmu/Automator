import assert from 'node:assert/strict';
import { test } from 'node:test';
import { canAutoRunTabCommand, matchTabCommands, resolveTabCommand } from '../ui/commands/commandMatching.ts';

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
