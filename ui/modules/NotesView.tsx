import { ChevronDown, ChevronRight, NotebookPen, Pencil, Plus, Save, Trash2, X } from 'lucide-react';
import { Fragment, useEffect, useRef, useState, type ReactNode } from 'react';
import type { AutomationServices } from '../automationServices';
import type { BackendUiState, ModuleSettingsUpdateRequest } from '../../contracts/rpc';
import { MAX_NOTE_BODY_LENGTH, MAX_NOTES, readNotesSettings, validateNotesSettings, type Note, type NotesSettings } from '../contracts/notes';
import { countCheckboxes, parseMarkdown, toggleCheckbox, type Block, type Inline } from './notesMarkdown';
import './NotesView.css';

type ViewProps = { tab: BackendUiState['tabs'][number]; moduleState: BackendUiState['moduleStates'][number] | undefined; services: AutomationServices };
const makeId = () => `note-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`;
type Draft = { id: string; title: string; body: string; isNew: boolean };

function renderInlines(inlines: readonly Inline[]): ReactNode {
  return inlines.map((inline, index) => {
    switch (inline.kind) {
      case 'text': return <Fragment key={index}>{inline.text}</Fragment>;
      case 'code': return <code key={index}>{inline.text}</code>;
      case 'strong': return <strong key={index}>{renderInlines(inline.children)}</strong>;
      case 'em': return <em key={index}>{renderInlines(inline.children)}</em>;
      case 'del': return <del key={index}>{renderInlines(inline.children)}</del>;
      case 'link': return <a key={index} href={inline.href} target="_blank" rel="noopener noreferrer">{renderInlines(inline.children)}</a>;
    }
  });
}

function renderBlock(block: Block, index: number, onToggle: (line: number) => void, disabled: boolean): ReactNode {
  switch (block.kind) {
    case 'heading': {
      const Heading = `h${Math.min(6, block.level + 2)}` as 'h3';
      return <Heading key={index}>{renderInlines(block.inlines)}</Heading>;
    }
    case 'paragraph': return <p key={index}>{renderInlines(block.inlines)}</p>;
    case 'quote': return <blockquote key={index}>{renderInlines(block.inlines)}</blockquote>;
    case 'rule': return <hr key={index} />;
    case 'code': return <pre key={index}><code>{block.text}</code></pre>;
    case 'list': {
      const List = block.ordered ? 'ol' : 'ul';
      return <List key={index}>{block.items.map((item, itemIndex) => item.task
        ? <li key={itemIndex} className="note-task" data-checked={item.task.checked}>
          <label><input type="checkbox" checked={item.task.checked} disabled={disabled} onChange={() => onToggle(item.task!.line)} /><span>{renderInlines(item.inlines)}</span></label>
        </li>
        : <li key={itemIndex}>{renderInlines(item.inlines)}</li>)}</List>;
    }
  }
}

/** A red dot means the note still has an unticked checkbox, green means every checkbox is ticked, and no dot means it has none. */
function NoteDot({ body }: { body: string }) {
  const { total, unticked } = countCheckboxes(body);
  if (total === 0) return <span className="note-dot-slot" aria-hidden="true" />;
  const open = unticked > 0;
  return <span className="note-dot" data-state={open ? 'open' : 'done'} role="img"
    aria-label={open ? `${unticked} unticked checkbox${unticked === 1 ? '' : 'es'}` : 'All checkboxes ticked'} title={open ? `${unticked} unticked` : 'All ticked'} />;
}

export function NotesView({ tab }: ViewProps) {
  const [settings, setSettings] = useState<NotesSettings>({ notes: [] });
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState('');
  const [expanded, setExpanded] = useState<ReadonlySet<string>>(new Set());
  const [draft, setDraft] = useState<Draft | null>(null);
  const current = useRef(settings);
  current.current = settings;

  useEffect(() => {
    let alive = true;
    void (async () => {
      try {
        const bridge = window.automator;
        if (!bridge) throw new Error('The Automator settings bridge is unavailable.');
        const value = await bridge.getModuleSettings({ contractVersion: tab.contractVersion, moduleId: tab.id, settingsVersion: tab.settingsVersion });
        if (!alive) return;
        setSettings(readNotesSettings(value.value));
        if (validateNotesSettings(value.value).length) setNotice('Some saved notes were invalid and could not be loaded.');
      } catch (error) {
        if (alive) setNotice(error instanceof Error ? error.message : 'Could not load notes.');
      } finally {
        if (alive) setReady(true);
      }
    })();
    return () => { alive = false; };
  }, [tab.contractVersion, tab.id, tab.settingsVersion]);

  /** Every change is written straight to the module preferences. The list on screen only changes once the write succeeded. */
  const persist = async (next: NotesSettings, success = ''): Promise<boolean> => {
    const errors = validateNotesSettings(next);
    if (errors.length) { setNotice(errors[0]); return false; }
    const bridge = window.automator;
    if (!bridge) { setNotice('The Automator settings bridge is unavailable.'); return false; }
    setBusy(true);
    try {
      await bridge.updateModuleSettings({ contractVersion: tab.contractVersion, moduleId: tab.id, settingsVersion: tab.settingsVersion,
        value: next as ModuleSettingsUpdateRequest['value'] });
      current.current = next;
      setSettings(next);
      setNotice(success);
      return true;
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save notes.');
      return false;
    } finally { setBusy(false); }
  };

  const toggleExpanded = (id: string) => setExpanded((open) => {
    const next = new Set(open);
    if (!next.delete(id)) next.add(id);
    return next;
  });

  const startNew = () => setDraft({ id: makeId(), title: '', body: '', isNew: true });
  const startEdit = (note: Note) => setDraft({ ...note, isNew: false });

  const saveDraft = async () => {
    if (!draft) return;
    const note: Note = { id: draft.id, title: draft.title.trim(), body: draft.body };
    const notes = draft.isNew ? [...current.current.notes, note] : current.current.notes.map((item) => item.id === note.id ? note : item);
    if (await persist({ notes }, 'Note saved.')) {
      setDraft(null);
      setExpanded((open) => new Set(open).add(note.id));
    }
  };

  const remove = async (note: Note) => {
    if (await persist({ notes: current.current.notes.filter((item) => item.id !== note.id) }, 'Note deleted.')) {
      setExpanded((open) => { const next = new Set(open); next.delete(note.id); return next; });
      setDraft((editing) => editing?.id === note.id ? null : editing);
    }
  };

  const tick = (note: Note, line: number) => {
    const body = toggleCheckbox(note.body, line);
    if (body === note.body) return;
    void persist({ notes: current.current.notes.map((item) => item.id === note.id ? { ...item, body } : item) });
  };

  const editor = (note: Draft) => <div className="note-editor">
    <label>Title<input value={note.title} maxLength={128} placeholder="Shopping list" autoFocus
      onChange={(event) => setDraft({ ...note, title: event.target.value })} /></label>
    <label>Note (markdown)<textarea value={note.body} maxLength={MAX_NOTE_BODY_LENGTH} rows={Math.min(12, Math.max(4, note.body.split('\n').length + 1))} spellCheck={false}
      placeholder={'Write text, a list, or a checklist:\n- [ ] first task\n- [x] done task'} onChange={(event) => setDraft({ ...note, body: event.target.value })} /></label>
    <div className="note-editor-actions">
      <button type="button" className="secondary-button" onClick={() => setDraft(null)} disabled={busy}><X size={14} /> Cancel</button>
      <button type="button" className="primary-button" onClick={() => void saveDraft()} disabled={busy || note.title.trim() === ''}><Save size={14} /> Save note</button>
    </div>
  </div>;

  return <section className="notes-view" aria-label="Quick notes">
    <header className="notes-heading">
      <div><h1>Quick notes</h1><p>Short markdown notes. A red dot marks a note with an unticked checkbox.</p></div>
      <button type="button" className="primary-button" onClick={startNew} disabled={!ready || busy || draft?.isNew === true || settings.notes.length >= MAX_NOTES}><Plus size={14} /> Add note</button>
    </header>
    {!ready && <p className="notes-status" role="status">Loading saved notes…</p>}
    {draft?.isNew && <article className="note-card">{editor(draft)}</article>}
    {ready && settings.notes.length === 0 && !draft && <div className="notes-empty"><NotebookPen size={20} /><strong>No notes yet</strong><span>Add a note and use <code>- [ ]</code> for checkboxes.</span></div>}
    <div className="notes-list">
      {settings.notes.map((note) => {
        const open = expanded.has(note.id);
        const editing = draft?.id === note.id && !draft.isNew;
        const panel = `note-panel-${note.id}`;
        return <article className="note-card" key={note.id} data-open={open}>
          <button type="button" className="note-row" aria-expanded={open} aria-controls={panel} onClick={() => toggleExpanded(note.id)}>
            <span className="note-title">{note.title}</span>
            <NoteDot body={note.body} />
            {open ? <ChevronDown size={15} aria-hidden="true" /> : <ChevronRight size={15} aria-hidden="true" />}
          </button>
          {open && <div className="note-panel" id={panel}>
            {editing ? editor(draft) : <>
              <div className="note-body">{note.body.trim() === '' ? <p className="note-empty">This note is empty.</p>
                : parseMarkdown(note.body).map((block, index) => renderBlock(block, index, (line) => tick(note, line), busy))}</div>
              <div className="note-actions">
                <button type="button" className="secondary-button" aria-label={`Edit ${note.title}`} onClick={() => startEdit(note)} disabled={busy}><Pencil size={13} /> Edit</button>
                <button type="button" className="secondary-button note-delete" aria-label={`Delete ${note.title}`} onClick={() => void remove(note)} disabled={busy}><Trash2 size={13} /> Delete</button>
              </div>
            </>}
          </div>}
        </article>;
      })}
    </div>
    {notice && <p className="notes-status" role="status">{notice}</p>}
  </section>;
}
