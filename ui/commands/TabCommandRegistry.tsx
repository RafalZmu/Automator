import { createContext, useContext, useEffect, useMemo, useRef, useSyncExternalStore, type ReactNode } from 'react';
import type { TabCommand } from './commandMatching';

type CommandProvider = () => readonly TabCommand[];

class Registry {
  private readonly providers = new Map<string, CommandProvider>();
  private readonly snapshots = new Map<string, readonly TabCommand[]>();
  private readonly listeners = new Set<() => void>();
  private catalogSnapshot: readonly { scope: string; command: TabCommand }[] = emptyCatalog;

  subscribe = (listener: () => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  getCommands = (scope: string): readonly TabCommand[] => this.providers.get(scope)?.() ?? emptyCommands;

  getCatalog = (): readonly { scope: string; command: TabCommand }[] => this.catalogSnapshot;

  getLiveCommand = (scope: string, commandId: string): TabCommand | undefined =>
    this.providers.get(scope)?.().find((command) => command.id === commandId);

  register(scope: string, provider: CommandProvider): () => void {
    this.providers.set(scope, provider);
    this.snapshots.set(scope, this.snapshot(provider()));
    this.publish();
    return () => {
      if (this.providers.get(scope) !== provider) return;
      this.providers.delete(scope);
      this.publish();
    };
  }

  private snapshot(commands: readonly TabCommand[]): readonly TabCommand[] {
    return Object.freeze(commands.map(({ id, label, keywords, disabled, confirmationPrompt }) => Object.freeze({
      id,
      label,
      keywords: keywords ? Object.freeze([...keywords]) : undefined,
      disabled,
      confirmationPrompt,
      run: () => undefined,
    })));
  }

  publish(): void {
    this.catalogSnapshot = Object.freeze([...this.snapshots.entries()]
      .flatMap(([scope, commands]) => commands.map((command) => Object.freeze({ scope, command }))));
    for (const listener of this.listeners) listener();
  }
}

const emptyCommands: readonly TabCommand[] = Object.freeze([]);
const RegistryContext = createContext<Registry | null>(null);

export function TabCommandRegistryProvider({ children }: { children: ReactNode }) {
  const registry = useMemo(() => new Registry(), []);
  return <RegistryContext.Provider value={registry}>{children}</RegistryContext.Provider>;
}

function useRegistry(): Registry {
  const registry = useContext(RegistryContext);
  if (!registry) throw new Error('Tab command hooks require TabCommandRegistryProvider.');
  return registry;
}

/** Register commands for a view. Callbacks always resolve from the most recent render. */
export function useRegisterTabCommands(scope: string, commands: readonly TabCommand[]): void {
  const registry = useRegistry();
  const current = useRef(commands);
  current.current = commands;
  const descriptor = JSON.stringify(commands.map(({ id, label, keywords, disabled, confirmationPrompt }) =>
    [id, label, keywords ?? [], disabled ?? false, confirmationPrompt ?? '']));

  useEffect(() => registry.register(scope, () => current.current), [registry, scope, descriptor]);
}

export function useTabCommands(scope: string): readonly TabCommand[] {
  const registry = useRegistry();
  return useSyncExternalStore(registry.subscribe, () => registry.getCommands(scope), () => emptyCommands);
}

/** Catalog metadata survives view unmounts; callbacks are always resolved from the live view. */
export function useAllTabCommandCatalog(): readonly { scope: string; command: TabCommand }[] {
  const registry = useRegistry();
  return useSyncExternalStore(registry.subscribe, registry.getCatalog, () => emptyCatalog);
}

export function useTabCommandResolver(): (scope: string, commandId: string) => TabCommand | undefined {
  const registry = useRegistry();
  return (scope, commandId) => registry.getLiveCommand(scope, commandId);
}

const emptyCatalog: readonly { scope: string; command: TabCommand }[] = Object.freeze([]);
