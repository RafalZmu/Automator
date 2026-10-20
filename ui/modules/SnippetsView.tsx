import { ChevronDown, ChevronUp, Check, ClipboardCopy, Plus, Save, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { AutomationServices } from '../automationServices';
import type { BackendUiState, ModuleSettingsUpdateRequest } from '../../contracts/rpc';
import { MAX_SNIPPET_VALUE_LENGTH, MAX_SNIPPETS, isValidSnippetAlias, readSnippetsSettings, validateSnippetsSettings, type Snippet, type SnippetsSettings } from '../contracts/snippets';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import './SnippetsView.css';

type ViewProps = { tab: BackendUiState['tabs'][number]; moduleState: BackendUiState['moduleStates'][number] | undefined; services: AutomationServices };
const emptySettings = (): SnippetsSettings => ({ snippets: [] });
const makeId = (prefix: string) => `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`;
const blankSnippet = (): Snippet => ({ id: makeId('snippet'), name: '', alias: '', value: '' });
const COPIED_FEEDBACK_MS = 1800;
function moveItem<T>(items: readonly T[], index: number, delta: number): T[] {
  const target = index + delta;
  if (target < 0 || target >= items.length) return [...items];
  const next = [...items];
  [next[index], next[target]] = [next[target], next[index]];
  return next;
}

export function SnippetsView({ tab }: ViewProps) {
  const [settings, setSettings] = useState<SnippetsSettings>(emptySettings);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const persistedSettings = useRef(JSON.stringify(emptySettings()));
  const copiedTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => () => clearTimeout(copiedTimer.current), []);

  useEffect(() => {
    let alive = true;
    void (async () => {
      try {
        const bridge = window.automator;
        if (!bridge) throw new Error('The Automator settings bridge is unavailable.');
        const value = await bridge.getModuleSettings({ contractVersion: tab.contractVersion, moduleId: tab.id, settingsVersion: tab.settingsVersion });
        const parsed = readSnippetsSettings(value.value);
        if (alive) {
          setSettings(parsed);
          persistedSettings.current = JSON.stringify(parsed);
          if (validateSnippetsSettings(value.value).length) setNotice('Some saved snippets were invalid and could not be loaded.');
        }
      } catch (error) {
        if (alive) setNotice(error instanceof Error ? error.message : 'Could not load snippets.');
      } finally {
        if (alive) setReady(true);
      }
    })();
    return () => { alive = false; };
  }, [tab.contractVersion, tab.id, tab.settingsVersion]);

  const save = useCallback(async () => {
    const validation = validateSnippetsSettings(settings);
    if (validation.length) {
      setErrors(validation);
      throw new Error(validation[0]);
    }
    const bridge = window.automator;
    if (!bridge) throw new Error('The Automator settings bridge is unavailable.');
    await bridge.updateModuleSettings({ contractVersion: tab.contractVersion, moduleId: tab.id, settingsVersion: tab.settingsVersion,
      value: settings as ModuleSettingsUpdateRequest['value'] });
    persistedSettings.current = JSON.stringify(settings);
    setErrors([]);
  }, [settings, tab.contractVersion, tab.id, tab.settingsVersion]);

  const saveAndReport = async () => {
    setBusy(true);
    setNotice('');
    try { await save(); setNotice('Snippets saved.'); }
    catch (error) { setNotice(error instanceof Error ? error.message : 'Could not save snippets.'); }
    finally { setBusy(false); }
  };

  /** Copies the snippet as it is on screen, saved or not. The text never leaves this function except for the clipboard. */
  const copy = useCallback(async (snippet: Snippet) => {
    try {
      if (!navigator.clipboard?.writeText) throw new Error('The clipboard is unavailable.');
      await navigator.clipboard.writeText(snippet.value);
    } catch (error) {
      const message = error instanceof Error && error.message ? error.message : 'The snippet could not be copied.';
      setNotice(`Could not copy ${snippet.name || 'the snippet'}: ${message}`);
      // The notice already shows the failure. The rethrow lets the tab search report it; the Copy button discards it.
      throw error;
    }
    setNotice(`Copied ${snippet.name || 'snippet'} to the clipboard.`);
    setCopiedId(snippet.id);
    clearTimeout(copiedTimer.current);
    copiedTimer.current = setTimeout(() => setCopiedId(null), COPIED_FEEDBACK_MS);
  }, []);

  // Only the name and shortcut are registered: the tab search and Global Action Search keep this list in memory and show it.
  const commands = useMemo(() => settings.snippets
    .filter((snippet) => snippet.alias !== '' && isValidSnippetAlias(snippet.alias) && snippet.value.trim() !== '')
    .map((snippet) => ({
      id: `copy-${snippet.id}`,
      label: `${snippet.alias.toLocaleLowerCase()} · ${snippet.name || 'Snippet'}`,
      keywords: [snippet.alias.toLocaleLowerCase(), snippet.name, 'copy snippet clipboard'],
      disabled: !ready,
      run: () => copy(snippet),
    })), [settings.snippets, ready, copy]);
  useRegisterTabCommands('snippets', commands);

  const updateSnippet = (snippetId: string, update: (snippet: Snippet) => Snippet) => {
    setSettings((current) => ({ snippets: current.snippets.map((snippet) => snippet.id === snippetId ? update(snippet) : snippet) }));
    setNotice('Unsaved changes');
  };
  const changeList = (update: (snippets: Snippet[]) => Snippet[]) => {
    setSettings((current) => ({ snippets: update(current.snippets) }));
    setNotice('Unsaved changes');
  };

  return <section className="snippets-view" aria-label="Snippets">
    <header className="snippets-heading">
      <div><h1>Snippets</h1><p>Save text you paste often. Type a snippet's shortcut in the search above and press Enter, or press its Copy button, to put it on the clipboard.</p></div>
      <div className="snippets-actions">
        <button type="button" className="secondary-button" onClick={() => changeList((snippets) => [...snippets, blankSnippet()])} disabled={!ready || settings.snippets.length >= MAX_SNIPPETS}><Plus size={14} /> Add snippet</button>
        <button type="button" className="primary-button" onClick={() => void saveAndReport()} disabled={!ready || busy}><Save size={14} /> Save</button>
      </div>
    </header>
    {!ready && <p className="snippets-status" role="status">Loading saved snippets…</p>}
    {ready && settings.snippets.length === 0 && <div className="snippets-empty"><ClipboardCopy size={20} /><strong>No snippets yet</strong><span>Add a snippet, give it a shortcut, and paste its text anywhere after copying it.</span></div>}
    <div className="snippets-list">
      {settings.snippets.map((snippet, index) => {
        const label = snippet.name || 'snippet';
        const copied = copiedId === snippet.id;
        return <article className="snippet-card" key={snippet.id}>
          <div className="snippet-top">
            <span className="snippet-number">{index + 1}</span>
            <div className="snippet-fields">
              <label>Name<input value={snippet.name} maxLength={128} placeholder="Company NIP" onChange={(event) => updateSnippet(snippet.id, (item) => ({ ...item, name: event.target.value }))} /></label>
              <label>Shortcut<input value={snippet.alias} maxLength={32} placeholder="nip" autoComplete="off" spellCheck={false} onChange={(event) => updateSnippet(snippet.id, (item) => ({ ...item, alias: event.target.value.replace(/[^a-z0-9]/gi, '') }))} /></label>
            </div>
            <button type="button" className="snippet-icon-button snippet-copy" data-copied={copied} aria-label={`Copy ${label}`} title="Copy to clipboard" disabled={!ready || snippet.value.trim() === ''} onClick={() => void copy(snippet).catch(() => undefined)}>{copied ? <Check size={15} /> : <ClipboardCopy size={15} />}</button>
            <button type="button" className="snippet-icon-button" aria-label={`Move ${label} up`} title="Move up" disabled={busy || index === 0} onClick={() => changeList((snippets) => moveItem(snippets, index, -1))}><ChevronUp size={13} /></button>
            <button type="button" className="snippet-icon-button" aria-label={`Move ${label} down`} title="Move down" disabled={busy || index === settings.snippets.length - 1} onClick={() => changeList((snippets) => moveItem(snippets, index, 1))}><ChevronDown size={13} /></button>
            <button type="button" className="snippet-icon-button snippet-delete" aria-label={`Delete ${label}`} title="Delete snippet" disabled={busy} onClick={() => changeList((snippets) => snippets.filter((item) => item.id !== snippet.id))}><Trash2 size={15} /></button>
          </div>
          <label className="snippet-value">Text to copy<textarea value={snippet.value} maxLength={MAX_SNIPPET_VALUE_LENGTH} rows={Math.min(6, Math.max(2, snippet.value.split('\n').length))} spellCheck={false} placeholder="Text that is copied exactly as written" onChange={(event) => updateSnippet(snippet.id, (item) => ({ ...item, value: event.target.value }))} /></label>
          <p className="snippet-hint">{snippet.alias ? <>In this tab's search, type <kbd>{snippet.alias}</kbd> and press Enter.</> : 'Add a shortcut to copy this by typing it, or use the Copy button.'}</p>
        </article>;
      })}
    </div>
    {errors.length > 0 && <div className="snippets-validation" role="alert"><strong>Fix these items before saving:</strong><ul>{errors.slice(0, 8).map((error, index) => <li key={`${index}-${error}`}>{error}</li>)}</ul></div>}
    {notice && <p className="snippets-status" role="status">{notice}</p>}
  </section>;
}
