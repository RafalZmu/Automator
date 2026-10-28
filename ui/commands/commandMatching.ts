export type TabCommand = {
  id: string;
  label: string;
  keywords?: readonly string[];
  disabled?: boolean;
  confirmationPrompt?: string;
  run: () => void | Promise<void>;
};

export type TabCommandResolution<T extends TabCommand = TabCommand> =
  | { kind: 'none' }
  | { kind: 'ambiguous' }
  | { kind: 'unique'; command: T };

function normalize(value: string): string {
  return value.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLocaleLowerCase().trim();
}

/** Match all query words against the explicit label/keyword index for each tab command. */
export function matchTabCommands<T extends TabCommand>(commands: readonly T[], query: string): T[] {
  const words = normalize(query).split(/\s+/u).filter(Boolean);
  if (words.length === 0) return commands.filter((command) => !command.disabled);

  return commands.filter((command) => {
    if (command.disabled) return false;
    const searchable = normalize([command.label, ...(command.keywords ?? [])].join(' '));
    return words.every((word) => searchable.includes(word));
  });
}

/**
 * A query that equals a command shortcut (an alias or a tab number) selects only the commands with that shortcut,
 * so partial text matches elsewhere, such as a timer name or a digit in a description, never compete with it.
 * Other typed queries list the matches that have a shortcut before those without one.
 */
export function matchQuickActions<T extends TabCommand & { shortcut?: string }>(commands: readonly T[], query: string): T[] {
  const typed = normalize(query);
  if (typed) {
    const exact = commands.filter((command) => !command.disabled && command.shortcut && normalize(command.shortcut) === typed);
    if (exact.length > 0) return exact;
  }
  const matches = matchTabCommands(commands, query);
  if (!typed) return matches;
  // Typing lists actions that have a shortcut first; each group keeps its relevance order.
  return [...matches.filter((command) => command.shortcut), ...matches.filter((command) => !command.shortcut)];
}

/** Only a single match can be launched by Enter; ambiguous and empty queries need more input. */
export function resolveTabCommand<T extends TabCommand>(matches: readonly T[]): TabCommandResolution<T> {
  if (matches.length === 0) return { kind: 'none' };
  if (matches.length > 1) return { kind: 'ambiguous' };
  return { kind: 'unique', command: matches[0] };
}

/** Confirmation-gated actions can never be started by delayed unique-match execution. */
export function canAutoRunTabCommand(command: TabCommand | undefined): command is TabCommand {
  return Boolean(command && !command.disabled && !command.confirmationPrompt);
}
