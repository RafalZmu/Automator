import { createContext, useContext, useEffect, useId, useState } from 'react';
import { Plus, Save, Trash2 } from 'lucide-react';
import { parseGlobalVariables, type GlobalVariableEntry, type GlobalVariableSnapshot, type VariableJsonValue } from '../contracts/variables';
import './GlobalVariables.css';

export const GlobalVariablesContext = createContext<GlobalVariableSnapshot>({ version: 1, values: {}, migrationConflicts: [] });

export function GlobalVariableHints({ syntax = 'template', onInsert }: { syntax?: 'template' | 'pointer'; onInsert?: (reference: string) => void }) {
  const { values } = useContext(GlobalVariablesContext);
  const id = useId();
  const names = Object.keys(values);
  const reference = (name: string) => syntax === 'pointer' ? `/variables/${name}` : `{{variables.${name}}}`;
  return <details className="variable-hints"><summary>Global variable references{names.length ? ` (${names.length})` : ''}</summary>
    <p>{syntax === 'pointer' ? 'Use these JSON pointers when mapping from initial input.' : 'Insert a reference in this field. String values expand as text; other values expand as JSON.'} Manage values in Options → Variables.</p>
    {names.length ? <ul id={id}>{names.map((name) => <li key={name}>{onInsert ? <button type="button" className="secondary-button" onClick={() => onInsert(reference(name))} aria-label={`Insert variable ${name}`}><code>{reference(name)}</code></button> : <code>{reference(name)}</code>}</li>)}</ul> : <p>No global variables yet.</p>}
  </details>;
}

export function GlobalVariablesEditor({ load, save, onChanged }: {
  load: () => Promise<GlobalVariableSnapshot>;
  save: (values: Record<string, VariableJsonValue>) => Promise<GlobalVariableSnapshot>;
  onChanged?: (snapshot: GlobalVariableSnapshot) => void;
}) {
  const [entries, setEntries] = useState<GlobalVariableEntry[]>([]);
  const [conflicts, setConflicts] = useState<string[]>([]);
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(true);
  const populate = (snapshot: GlobalVariableSnapshot) => {
    setEntries(Object.entries(snapshot.values).map(([name, value]) => ({ name, json: JSON.stringify(value, null, 2) })));
    setConflicts(snapshot.migrationConflicts);
    onChanged?.(snapshot);
  };
  useEffect(() => { let alive = true; void load().then((snapshot) => { if (alive) populate(snapshot); })
    .catch(() => { if (alive) setNotice('Could not load global variables.'); }).finally(() => { if (alive) setBusy(false); });
    return () => { alive = false; };
  }, [load]);
  const persist = async () => {
    try {
      const values = parseGlobalVariables(entries);
      setBusy(true);
      populate(await save(values));
      setNotice('Variables saved.');
    } catch (error) { setNotice(error instanceof Error ? error.message : 'Could not save variables.'); }
    finally { setBusy(false); }
  };
  return <section className="global-variables-editor" aria-label="Global variables">
    <h3>Variables</h3><p>Reusable JSON values for scripts, request bodies, and workflow inputs. These values are included in exports. Store credentials in the API secret manager.</p>
    {conflicts.length > 0 && <div role="alert">Legacy workflow values awaiting migration: {conflicts.join(', ')}. Existing names with different values and the 256-entry or 64 KiB limits can block migration. Rename or remove conflicting global values, or free space, then save to retry. Legacy values remain available to their workflow.</div>}
    {entries.map((entry, index) => <div key={index} className="workflow-variable-row">
      <label>Name<input aria-label={`Variable name ${index + 1}`} value={entry.name} maxLength={192} disabled={busy} onChange={(event) => setEntries((current) => current.map((item, i) => i === index ? { ...item, name: event.target.value } : item))} /></label>
      <label>JSON value<textarea aria-label={`Variable JSON ${index + 1}`} value={entry.json} rows={2} spellCheck={false} disabled={busy} onChange={(event) => setEntries((current) => current.map((item, i) => i === index ? { ...item, json: event.target.value } : item))} /></label>
      <button type="button" className="icon-button" aria-label={`Delete variable ${index + 1}`} disabled={busy} onClick={() => setEntries((current) => current.filter((_, i) => i !== index))}><Trash2 size={14} /></button>
    </div>)}
    <button type="button" className="secondary-button" disabled={busy || entries.length >= 256} onClick={() => setEntries((current) => [...current, { name: '', json: '""' }])}><Plus size={14} /> Add variable</button>
    <button type="button" className="primary-button" disabled={busy} onClick={() => void persist()}><Save size={14} /> Save variables</button>
    {notice && <p role="status">{notice}</p>}
  </section>;
}
