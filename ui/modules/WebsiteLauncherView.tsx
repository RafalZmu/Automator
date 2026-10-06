import { ChevronDown, ChevronUp, ExternalLink, FolderPlus, Globe2, Plus, Save, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { AutomationServices } from '../automationServices';
import type { BackendUiState, ModuleSettingsUpdateRequest } from '../../contracts/rpc';
import { readWebsiteLauncherSettings, validateWebsiteLauncherSettings, type WebsiteLauncherGroup, type WebsiteLauncherRow, type WebsiteLauncherSettings, type WebsiteLauncherSite } from '../contracts/websiteLauncher';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import './WebsiteLauncherView.css';

type ViewProps = { tab: BackendUiState['tabs'][number]; moduleState: BackendUiState['moduleStates'][number] | undefined; services: AutomationServices };
const emptySettings = (): WebsiteLauncherSettings => ({ rows: [] });
const makeId = (prefix: string) => `${prefix}-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 7)}`;
const blankSite = (): WebsiteLauncherSite => ({ id: makeId('site'), name: 'Website', url: '' });
const blankGroup = (): WebsiteLauncherGroup => ({ id: makeId('group'), name: 'Browser window', websites: [blankSite()] });
const blankRow = (): WebsiteLauncherRow => ({ id: makeId('web'), name: '', alias: '', groups: [blankGroup()] });
function moveItem<T>(items: readonly T[], index: number, delta: number): T[] {
  const target = index + delta;
  if (target < 0 || target >= items.length) return [...items];
  const next = [...items];
  [next[index], next[target]] = [next[target], next[index]];
  return next;
}

export function WebsiteLauncherView({ tab, services }: ViewProps) {
  const [settings, setSettings] = useState<WebsiteLauncherSettings>(emptySettings);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const persistedSettings = useRef(JSON.stringify(emptySettings()));
  const dirty = JSON.stringify(settings) !== persistedSettings.current;

  const save = useCallback(async (next: WebsiteLauncherSettings = settings) => {
    const validation = validateWebsiteLauncherSettings(next);
    if (validation.length) {
      setErrors(validation);
      throw new Error(validation[0]);
    }
    const bridge = window.automator;
    if (!bridge) throw new Error('The Automator settings bridge is unavailable.');
    await bridge.updateModuleSettings({ contractVersion: tab.contractVersion, moduleId: tab.id, settingsVersion: tab.settingsVersion,
      value: next as ModuleSettingsUpdateRequest['value'] });
    persistedSettings.current = JSON.stringify(next);
    setSettings(next);
    setErrors([]);
    window.dispatchEvent(new CustomEvent('automator:website-launcher-settings-changed', {
      detail: next.rows.map(({ id, name, alias }) => ({ id, name, alias })),
    }));
    return next;
  }, [settings, tab.contractVersion, tab.id, tab.settingsVersion]);

  useEffect(() => {
    let alive = true;
    void (async () => {
      try {
        const bridge = window.automator;
        if (!bridge) throw new Error('The Automator settings bridge is unavailable.');
        const value = await bridge.getModuleSettings({ contractVersion: tab.contractVersion, moduleId: tab.id, settingsVersion: tab.settingsVersion });
        const parsed = readWebsiteLauncherSettings(value.value);
        if (alive) {
          setSettings(parsed);
          persistedSettings.current = JSON.stringify(parsed);
          if (validateWebsiteLauncherSettings(value.value).length) setNotice('Some saved shortcuts were invalid and could not be loaded.');
        }
      } catch (error) {
        if (alive) setNotice(error instanceof Error ? error.message : 'Could not load website shortcuts.');
      } finally {
        if (alive) setReady(true);
      }
    })();
    return () => { alive = false; };
  }, [tab.contractVersion, tab.id, tab.settingsVersion]);

  const launch = useCallback(async (row: WebsiteLauncherRow) => {
    setBusy(row.id);
    setNotice('');
    try {
      if (dirty) await save(settings);
      const result = await services.modules.dispatch('launchRow', { rowId: row.id });
      if (result.status === 'error') throw new Error(result.message);
      setNotice(result.message || `${row.name} opened.`);
    } catch (error) {
      const message = error instanceof Error ? error.message : 'The website shortcut could not be launched.';
      setNotice(message);
      throw error;
    } finally {
      setBusy(null);
    }
  }, [dirty, save, services, settings]);

  const commands = useMemo(() => settings.rows.filter((row) => /^[a-z]{1,32}$/i.test(row.alias)).map((row) => ({
    id: `launch-${row.id}`,
    label: `${row.alias.toLocaleLowerCase()} · ${row.name || 'Website shortcut'}`,
    keywords: [row.alias.toLocaleLowerCase(), `${row.alias.toLocaleLowerCase()}w`, row.name, 'launch websites'],
    disabled: !ready || busy !== null,
    run: () => launch(row),
  })), [settings.rows, ready, busy, launch]);
  useRegisterTabCommands('website-launcher', commands);

  const updateRow = (rowId: string, update: (row: WebsiteLauncherRow) => WebsiteLauncherRow) => {
    setSettings((current) => ({ ...current, rows: current.rows.map((row) => row.id === rowId ? update(row) : row) }));
    setNotice('Unsaved changes');
  };
  const replaceGroup = (row: WebsiteLauncherRow, groupId: string, update: (group: WebsiteLauncherGroup) => WebsiteLauncherGroup): WebsiteLauncherRow => ({
    ...row, groups: row.groups.map((group) => group.id === groupId ? update(group) : group),
  });
  const saveAndReport = async () => {
    setBusy('save');
    setNotice('');
    try { await save(); setNotice('Website shortcuts saved.'); }
    catch (error) { setNotice(error instanceof Error ? error.message : 'Could not save website shortcuts.'); }
    finally { setBusy(null); }
  };

  return <section className="website-launcher-view" aria-label="Website Launcher">
    <header className="website-launcher-heading">
      <div><h1>Website Launcher</h1><p>Group sites into browser windows and set their tab order. Window handling depends on your default browser.</p></div>
      <div className="website-launcher-actions">
        <button type="button" className="secondary-button" onClick={() => { setSettings((current) => ({ ...current, rows: [...current.rows, blankRow()] })); setNotice('Unsaved changes'); }} disabled={!ready || settings.rows.length >= 64}><Plus size={14} /> Add shortcut</button>
        <button type="button" className="primary-button" onClick={() => void saveAndReport()} disabled={!ready || busy !== null}><Save size={14} /> Save</button>
      </div>
    </header>
    {!ready && <p className="website-launcher-status" role="status">Loading saved shortcuts…</p>}
    {ready && settings.rows.length === 0 && <div className="website-launcher-empty"><Globe2 size={20} /><strong>No website shortcuts yet</strong><span>Add a shortcut, give it an alias, and place sites into browser groups.</span></div>}
    <div className="website-launcher-list">
      {settings.rows.map((row, rowIndex) => <article className="website-shortcut-card" key={row.id}>
        <div className="website-shortcut-top">
          <span className="website-shortcut-number">{rowIndex + 1}</span>
          <div className="website-shortcut-fields">
            <label>Name<input value={row.name} maxLength={128} placeholder="Project websites" onChange={(event) => updateRow(row.id, (item) => ({ ...item, name: event.target.value }))} /></label>
            <label>Tab shortcut<input value={row.alias} maxLength={32} placeholder="docs" autoComplete="off" spellCheck={false} onChange={(event) => updateRow(row.id, (item) => ({ ...item, alias: event.target.value.replace(/[^a-z]/gi, '') }))} /></label>
          </div>
          <button type="button" className="website-icon-button" aria-label={`Move ${row.name || 'shortcut'} up`} title="Move up" disabled={busy !== null || rowIndex === 0} onClick={() => { setSettings((current) => ({ ...current, rows: moveItem(current.rows, rowIndex, -1) })); setNotice('Unsaved changes'); }}><ChevronUp size={13} /></button>
          <button type="button" className="website-icon-button" aria-label={`Move ${row.name || 'shortcut'} down`} title="Move down" disabled={busy !== null || rowIndex === settings.rows.length - 1} onClick={() => { setSettings((current) => ({ ...current, rows: moveItem(current.rows, rowIndex, 1) })); setNotice('Unsaved changes'); }}><ChevronDown size={13} /></button>
          <button type="button" className="website-icon-button" aria-label={`Launch ${row.name || 'website shortcut'}`} title="Launch this website set" disabled={!ready || busy !== null || validateWebsiteLauncherSettings({ rows: [row] }).length > 0} onClick={() => void launch(row).catch(() => undefined)}><ExternalLink size={15} /></button>
          <button type="button" className="website-icon-button website-delete" aria-label={`Delete ${row.name || 'website shortcut'}`} title="Delete shortcut" disabled={busy !== null} onClick={() => { setSettings((current) => ({ ...current, rows: current.rows.filter((item) => item.id !== row.id) })); setNotice('Unsaved changes'); }}><Trash2 size={15} /></button>
        </div>
        <div className="website-groups">
          {row.groups.map((group, groupIndex) => <section className="website-group" key={group.id} aria-label={`Browser group ${groupIndex + 1}`}>
            <div className="website-group-heading"><span className="website-group-badge">Window {groupIndex + 1}</span>
              <input aria-label={`Window ${groupIndex + 1} name`} value={group.name} maxLength={64} placeholder="Group name" onChange={(event) => updateRow(row.id, (item) => replaceGroup(item, group.id, (current) => ({ ...current, name: event.target.value })))} />
              <button type="button" className="website-icon-button" aria-label={`Move window ${groupIndex + 1} up`} title="Move window up" disabled={busy !== null || groupIndex === 0} onClick={() => updateRow(row.id, (item) => ({ ...item, groups: moveItem(item.groups, groupIndex, -1) }))}><ChevronUp size={12} /></button>
              <button type="button" className="website-icon-button" aria-label={`Move window ${groupIndex + 1} down`} title="Move window down" disabled={busy !== null || groupIndex === row.groups.length - 1} onClick={() => updateRow(row.id, (item) => ({ ...item, groups: moveItem(item.groups, groupIndex, 1) }))}><ChevronDown size={12} /></button>
              <button type="button" className="website-icon-button" aria-label={`Remove window ${groupIndex + 1}`} title="Remove browser window" disabled={row.groups.length <= 1} onClick={() => updateRow(row.id, (item) => ({ ...item, groups: item.groups.filter((candidate) => candidate.id !== group.id) }))}><Trash2 size={13} /></button>
            </div>
            <div className="website-sites">
              {group.websites.map((site, siteIndex) => <div className="website-site-row" key={site.id}>
                <label>Tab {siteIndex + 1}<input aria-label={`Window ${groupIndex + 1} tab ${siteIndex + 1} name`} value={site.name} maxLength={128} placeholder="Site name" onChange={(event) => updateRow(row.id, (item) => replaceGroup(item, group.id, (current) => ({ ...current, websites: current.websites.map((candidate) => candidate.id === site.id ? { ...candidate, name: event.target.value } : candidate) })))} /></label>
                <label>URL<input aria-label={`Window ${groupIndex + 1} tab ${siteIndex + 1} URL`} value={site.url} maxLength={2048} placeholder="https://example.com" onChange={(event) => updateRow(row.id, (item) => replaceGroup(item, group.id, (current) => ({ ...current, websites: current.websites.map((candidate) => candidate.id === site.id ? { ...candidate, url: event.target.value } : candidate) })))} /></label>
                <span className="website-site-order"><button type="button" className="website-icon-button" aria-label={`Move tab ${siteIndex + 1} up`} title="Move tab up" disabled={busy !== null || siteIndex === 0} onClick={() => updateRow(row.id, (item) => replaceGroup(item, group.id, (current) => ({ ...current, websites: moveItem(current.websites, siteIndex, -1) })))}><ChevronUp size={11} /></button><button type="button" className="website-icon-button" aria-label={`Move tab ${siteIndex + 1} down`} title="Move tab down" disabled={busy !== null || siteIndex === group.websites.length - 1} onClick={() => updateRow(row.id, (item) => replaceGroup(item, group.id, (current) => ({ ...current, websites: moveItem(current.websites, siteIndex, 1) })))}><ChevronDown size={11} /></button>
                  <button type="button" className="website-icon-button" aria-label={`Remove ${site.name || `tab ${siteIndex + 1}`}`} title="Remove website" disabled={group.websites.length <= 1} onClick={() => updateRow(row.id, (item) => replaceGroup(item, group.id, (current) => ({ ...current, websites: current.websites.filter((candidate) => candidate.id !== site.id) })))}><Trash2 size={12} /></button>
                </span>
              </div>)}
            </div>
            <button type="button" className="website-add-link" onClick={() => updateRow(row.id, (item) => replaceGroup(item, group.id, (current) => ({ ...current, websites: [...current.websites, blankSite()] })))} disabled={group.websites.length >= 32}><Plus size={12} /> Add website to this window</button>
          </section>)}
          <button type="button" className="website-add-group" onClick={() => updateRow(row.id, (item) => ({ ...item, groups: [...item.groups, blankGroup()] }))} disabled={row.groups.length >= 16}><FolderPlus size={14} /> Add browser window</button>
        </div>
        <p className="website-shortcut-hint">In this tab, type <kbd>{row.alias || 'alias'}</kbd>. In Global Action Search, type <kbd>{row.alias || 'alias'}w</kbd>.</p>
      </article>)}
    </div>
    {errors.length > 0 && <div className="website-launcher-validation" role="alert"><strong>Fix these items before saving:</strong><ul>{errors.slice(0, 8).map((error, index) => <li key={`${index}-${error}`}>{error}</li>)}</ul></div>}
    {notice && <p className="website-launcher-status" role="status">{notice}</p>}
  </section>;
}
