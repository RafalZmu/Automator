import { useEffect, useMemo, useRef, useState, type KeyboardEvent as ReactKeyboardEvent } from 'react';
import { ArrowDown, ArrowUp, CornerDownLeft, Search } from 'lucide-react';
import { matchTabCommands, resolveTabCommand } from './commandMatching';
import { useTabCommands } from './TabCommandRegistry';

export function TabCommandBar({ scope }: { scope: string }) {
  const commands = useTabCommands(scope);
  const [query, setQuery] = useState('');
  const [selected, setSelected] = useState(-1);
  const [armedCommand, setArmedCommand] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);
  const matches = useMemo(() => matchTabCommands(commands, query), [commands, query]);

  useEffect(() => {
    setQuery('');
    setSelected(-1);
    setArmedCommand(null);
    setError('');
  }, [scope]);

  useEffect(() => {
    const onFocusRequest = (event: Event) => {
      const detail = (event as CustomEvent<{ scope?: string }>).detail;
      if (detail?.scope !== scope) return;
      inputRef.current?.focus({ preventScroll: true });
      inputRef.current?.select();
    };
    window.addEventListener('automator:focus-tab-search', onFocusRequest);
    return () => window.removeEventListener('automator:focus-tab-search', onFocusRequest);
  }, [scope]);

  const execute = async (commandId?: string) => {
    const resolution = resolveTabCommand(matches);
    const command = commandId ? matches.find((item) => item.id === commandId) : resolution.kind === 'unique' ? resolution.command : null;
    if (!command || busy) return;
    if (command.confirmationPrompt && armedCommand !== command.id) {
      setArmedCommand(command.id);
      return;
    }
    setBusy(true);
    setError('');
    try {
      await command.run();
      setQuery('');
      setSelected(-1);
      setArmedCommand(null);
    } catch (failure) {
      setError(failure instanceof Error ? failure.message : 'The action could not be completed.');
    } finally {
      setBusy(false);
    }
  };

  const onKeyDown = (event: ReactKeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'ArrowDown' && matches.length > 0) {
      event.preventDefault();
      setSelected((value) => (value + 1) % matches.length);
      setArmedCommand(null);
    } else if (event.key === 'ArrowUp' && matches.length > 0) {
      event.preventDefault();
      setSelected((value) => value <= 0 ? matches.length - 1 : value - 1);
      setArmedCommand(null);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      if (selected >= 0) void execute(matches[selected]?.id);
      else void execute();
    } else if (event.key === 'Escape') {
      setQuery('');
      setSelected(-1);
      setArmedCommand(null);
      setError('');
      inputRef.current?.blur();
    }
  };

  const resolution = resolveTabCommand(matches);
  const confirm = armedCommand ? matches.find((item) => item.id === armedCommand) : null;
  return <section className="tab-command" aria-label="Tab action search" data-tab-command-scope={scope}>
    <label className="tab-command-input" htmlFor="tab-command-input">
      <Search size={15} aria-hidden="true" />
      <input ref={inputRef} id="tab-command-input" value={query} onChange={(event) => { setQuery(event.target.value); setSelected(-1); setArmedCommand(null); }} onKeyDown={onKeyDown}
        placeholder="Search actions in this tab…" autoComplete="off" spellCheck={false} aria-autocomplete="list" aria-controls="tab-command-results" />
    </label>
    {query.trim() && <div id="tab-command-results" className="tab-command-results" role="listbox" aria-label="Matching tab actions">
      {matches.length === 0 ? <p className="tab-command-empty">No matching action</p> : matches.map((command, index) => {
        const isSelected = selected === index || (selected < 0 && resolution.kind === 'unique');
        return <button key={command.id} type="button" role="option" aria-selected={isSelected} className="tab-command-option" data-selected={isSelected}
          onMouseEnter={() => { setSelected(index); setArmedCommand(null); }} onClick={() => void execute(command.id)} disabled={busy}>
          <span>{command.label}</span><span className="tab-command-enter"><CornerDownLeft size={12} /> {command.confirmationPrompt ? 'Enter twice' : 'Enter'}</span>
        </button>;
      })}
      {confirm && <p className="tab-command-confirm" role="status">{confirm.confirmationPrompt} Press Enter again to continue.</p>}
      {error && <p className="tab-command-error" role="alert">{error}</p>}
      {!confirm && matches.length > 1 && <p className="tab-command-empty"><ArrowUp size={11} /><ArrowDown size={11} /> Choose an action, then press Enter.</p>}
    </div>}
  </section>;
}
