import { useCallback, useContext, useEffect, useRef, useState, type FormEvent } from 'react';
import type { AutomationServices } from '../automationServices';
import { FileExplorerLaunchContext } from '../FileExplorerLaunchContext';
import { parseExplorerActions, parseScriptTemplateCatalog, validateScriptTemplateValues, type ExplorerActionDefinition, type ScriptTemplateDescriptor } from '../../contracts/scriptRunner';
import { PathField } from '../components/PathField';
import { clearSensitiveTemplateValues } from './scriptTemplateViewModel';
import { normalizeExplorerExtensions, prepareExplorerForm, registrationNotice, type ExplorerProfile, type ExplorerRunForm } from './explorerActionViewModel';

type Props = { services: AutomationServices; profiles: ExplorerProfile[]; templates: ScriptTemplateDescriptor[]; open: boolean; onOpen: () => void; busy: boolean; onRunning: (id: string | null) => void; onResult: (data: unknown) => void };
type MappingDraft = { id: string; profileId: string; label: string; extensions: string; fileParameterKey: string };
function responseData(value: unknown): Record<string, unknown> { return value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : {}; }
function message(error: unknown): string { return error instanceof Error ? error.message : 'The Explorer action could not be updated.'; }

export function ExplorerActionsPanel({ services, profiles, templates, open, onOpen, busy, onRunning, onResult }: Props) {
  const launch = useContext(FileExplorerLaunchContext);
  const [actions, setActions] = useState<ExplorerActionDefinition[]>([]);
  const [registration, setRegistration] = useState('');
  const [notice, setNotice] = useState('');
  const [draft, setDraft] = useState<MappingDraft | null>(null);
  const [form, setForm] = useState<ExplorerRunForm | null>(null);
  const [mutating, setMutating] = useState(false);
  const [opening, setOpening] = useState(false);
  const [running, setRunning] = useState(false);
  const controller = useRef<AbortController | null>(null);
  useEffect(() => () => controller.current?.abort(), []);

  const refresh = useCallback(async () => {
    const result = await services.modules.dispatch('listExplorerActions', {});
    if (result.status === 'error') throw new Error(result.message);
    const data = responseData(result.data);
    const saved = parseExplorerActions(data.actions);
    setActions(saved);
    setRegistration(registrationNotice(data.registration));
    return saved;
  }, [services]);
  useEffect(() => {
    if (open) void refresh().catch(error => setNotice(message(error)));
  }, [open, profiles, refresh]);

  useEffect(() => {
    if (!launch.request || form || busy) return;
    let disposed = false;
    const request = launch.request;
    setOpening(true);
    onOpen();
    void (async () => {
      try {
        const saved = await refresh();
        const listed = await services.modules.dispatch('listProfiles', {});
        if (listed.status === 'error') throw new Error(listed.message);
        const catalog = await services.modules.dispatch('listTemplates', {});
        if (catalog.status === 'error') throw new Error(catalog.message);
        const currentProfiles = responseData(listed.data).profiles;
        if (!Array.isArray(currentProfiles)) throw new Error('The saved script profiles could not be read.');
        const next = prepareExplorerForm(request, saved, currentProfiles as ExplorerProfile[], parseScriptTemplateCatalog(responseData(catalog.data).templates));
        if (!disposed) { setForm(next); setDraft(null); setNotice(''); }
      } catch (error) { if (!disposed) setNotice(message(error)); }
      finally { if (!disposed) { setOpening(false); launch.consume(); } }
    })();
    return () => { disposed = true; };
  }, [launch.request, launch.consume, services, refresh, form, busy, onOpen]);

  const save = async (event: FormEvent) => {
    event.preventDefault();
    if (!draft) return;
    setMutating(true);
    try {
      const profile = profiles.find(item => item.id === draft.profileId);
      if (!profile) throw new Error('Choose a saved profile.');
      const action: ExplorerActionDefinition = { id: draft.id, profileId: draft.profileId, label: draft.label.trim(), extensions: normalizeExplorerExtensions(draft.extensions), ...(profile.templateOrigin ? { fileParameterKey: draft.fileParameterKey } : {}) };
      const result = await services.modules.dispatch('saveExplorerAction', action);
      if (result.status === 'error') throw new Error(result.message);
      setDraft(null);
      await refresh();
      setNotice(result.message);
    } catch (error) { setNotice(message(error)); }
    finally { setMutating(false); }
  };
  const remove = async (items: ExplorerActionDefinition[]) => {
    setMutating(true);
    try {
      for (const item of items) {
        const result = await services.modules.dispatch('deleteExplorerAction', { id: item.id });
        if (result.status === 'error') throw new Error(result.message);
      }
      await refresh();
      setDraft(null);
      setNotice(items.length === 1 ? 'Explorer action removed.' : 'All Explorer actions removed.');
    } catch (error) { setNotice(message(error)); await refresh().catch(() => undefined); }
    finally { setMutating(false); }
  };
  const run = async () => {
    if (!form || busy || controller.current) return;
    const abort = new AbortController();
    try {
      const templateValues = form.template ? validateScriptTemplateValues(form.template, form.values) : undefined;
      controller.current = abort;
      setRunning(true);
      onRunning(`explorer:${form.profile.id}`);
      setNotice('');
      const result = await services.modules.dispatch('runExplorerAction', { id: form.action.id, filePath: form.request.filePath, ...(templateValues ? { templateValues } : { arguments: form.arguments }) }, { signal: abort.signal });
      onResult(result.data);
      setNotice(result.message);
      if (result.status === 'success') setForm(null);
      else setForm(current => current?.template ? { ...current, values: clearSensitiveTemplateValues(current.template, current.values) } : current);
    } catch (error) { setNotice(message(error)); }
    finally { controller.current = null; setRunning(false); onRunning(null); }
  };
  const profile = profiles.find(item => item.id === draft?.profileId);
  const mappingTemplate = templates.find(item => item.id === profile?.templateOrigin?.id && item.version === profile.templateOrigin.version);
  const mappingParameters = mappingTemplate?.parameters.filter(item => item.type === 'file' || item.type === 'directory') ?? [];
  const locked = busy || mutating || opening;
  if (!open) return null;
  return <section className="script-editor explorer-actions-panel" aria-label="File Explorer actions">
    <div className="script-editor-heading"><strong>File Explorer actions</strong><button className="secondary-button" type="button" disabled={locked || !profiles.length} onClick={() => { setDraft({ id: `explorer-${crypto.randomUUID().replaceAll('-', '')}`, profileId: '', label: '', extensions: '', fileParameterKey: '' }); setNotice(''); }}>Add action</button></div>
    <p>Map saved profiles to file types in the Automator submenu. On Windows 11, use Show more options. Selecting an action opens a form; it runs after you press Run.</p>
    {registration && <p className="script-notice" role="status">{registration}</p>}
    {notice && <p className="script-notice" role="status">{notice}</p>}
    {opening && <p role="status">Opening selected file…</p>}
    {draft && <form aria-label="Explorer action mapping" onSubmit={event => void save(event)}>
      <div className="script-fields">
        <label><span>Saved profile</span><select aria-label="Saved profile" required value={draft.profileId} disabled={locked} onChange={event => setDraft({ ...draft, profileId: event.target.value, fileParameterKey: '' })}><option value="">Choose a profile…</option>{profiles.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
        <label><span>Menu label</span><input required maxLength={128} value={draft.label} disabled={locked} onChange={event => setDraft({ ...draft, label: event.target.value })} /></label>
        <label className="script-field-wide"><span>File extensions</span><input required placeholder=".fdb, .txt" value={draft.extensions} disabled={locked} onChange={event => setDraft({ ...draft, extensions: event.target.value })} /></label>
        {profile?.templateOrigin ? <label className="script-field-wide"><span>Selected file parameter</span><select aria-label="Selected file parameter" required value={draft.fileParameterKey} disabled={locked} onChange={event => setDraft({ ...draft, fileParameterKey: event.target.value })}><option value="">Choose a file or directory input…</option>{mappingParameters.map(item => <option key={item.key} value={item.key}>{item.label}</option>)}</select></label> : <p className="script-field-wide">The saved profile must include {'{{file.path}}'} in its arguments. Each line stays one process argument.</p>}
      </div>
      <div className="script-editor-actions"><button className="secondary-button" type="button" disabled={locked} onClick={() => setDraft(null)}>Cancel mapping</button><button className="primary-button" type="submit" disabled={locked}>Save action</button></div>
    </form>}
    <div className="script-profile-list">{actions.map(item => <article className="script-profile-row" key={item.id}>
      <div className="script-profile-copy"><strong>{item.label}</strong><span>{item.extensions.join(', ')} · {profiles.find(profile => profile.id === item.profileId)?.name ?? 'Profile no longer exists'}</span></div>
      <button className="secondary-button" type="button" aria-label={`Edit ${item.label}`} disabled={locked} onClick={() => setDraft({ id: item.id, profileId: item.profileId, label: item.label, extensions: item.extensions.join(', '), fileParameterKey: item.fileParameterKey ?? '' })}>Edit</button>
      <button className="secondary-button" type="button" aria-label={`Remove ${item.label}`} disabled={locked} onClick={() => void remove([item])}>Remove</button>
    </article>)}</div>
    {!actions.length && <p>No Explorer actions configured. Install a Library template or save a profile, then add an action.</p>}
    <div className="script-editor-actions"><button className="secondary-button" type="button" disabled={locked || !actions.length} onClick={() => void remove(actions)}>Remove all Explorer actions</button></div>
    {form && <section className="script-editor" aria-label="Run Explorer action">
      <div className="script-editor-heading"><strong>{form.action.label} · {form.profile.name}</strong></div>
      <label className="script-field-wide"><span>Selected file</span><input readOnly value={form.request.filePath} /></label>
      <p>Review the inputs before running. These values are used for this run and are never saved to the profile.</p>
      {form.template ? <div className="script-fields">{form.template.parameters.filter(parameter => parameter.key !== form.action.fileParameterKey).map(parameter => parameter.type === 'file' || parameter.type === 'directory'
        ? <PathField key={parameter.key} label={`${parameter.label}${parameter.required ? ' *' : ''}`} kind={parameter.type} required={parameter.required} value={String(form.values[parameter.key] ?? '')} onChange={value => setForm(current => current ? { ...current, values: { ...current.values, [parameter.key]: value } } : current)} onBrowse={() => services.files.pickPath(parameter.type)} />
        : <label key={parameter.key}><span>{parameter.label}{parameter.required ? ' *' : ''}</span>{parameter.type === 'boolean'
          ? <input type="checkbox" disabled={running} checked={Boolean(form.values[parameter.key])} onChange={event => setForm({ ...form, values: { ...form.values, [parameter.key]: event.target.checked } })} />
          : parameter.type === 'choice' ? <select required={parameter.required} disabled={running} value={String(form.values[parameter.key] ?? '')} onChange={event => setForm({ ...form, values: { ...form.values, [parameter.key]: event.target.value } })}><option value="">Choose…</option>{parameter.options.map(option => <option key={option}>{option}</option>)}</select>
          : <input required={parameter.required} disabled={running} type={parameter.sensitive ? 'password' : 'text'} value={String(form.values[parameter.key] ?? '')} onChange={event => setForm({ ...form, values: { ...form.values, [parameter.key]: event.target.value } })} />}</label>)}</div>
        : <label className="script-field-wide"><span>Run arguments <small>one argument per line</small></span><textarea aria-label="Run arguments" rows={4} disabled={running} value={form.arguments.join('\n')} onChange={event => setForm({ ...form, arguments: event.target.value.split(/\r?\n/) })} /></label>}
      <div className="script-editor-actions"><button className="secondary-button" type="button" onClick={() => { if (running) controller.current?.abort(); else { setForm(null); setNotice(''); } }}>{running ? 'Stop run' : 'Cancel'}</button><button className="primary-button" type="button" disabled={busy || opening} onClick={() => void run()}>{running ? 'Running…' : 'Run'}</button></div>
    </section>}
  </section>;
}
