import { Check, CircleAlert, FilePlus2, FolderOpen, LoaderCircle, Play, Plus, RefreshCw, Square, Tag, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import type { AutomationModuleActionRequest, BackendUiState } from '../../contracts/rpc';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import type { AutomationServices } from '../automationServices';
import {
  readPlaywrightExplorerState, validatePlaywrightSectionPath,
  type PlaywrightExplorerState, type PlaywrightTestIdentity,
} from '../contracts/browser';

type ViewProps = {
  tab: BackendUiState['tabs'][number];
  moduleState: BackendUiState['moduleStates'][number] | undefined;
  services: AutomationServices;
};

type RunOutput = {
  message: string;
  timedOut: boolean;
  durationMilliseconds: number;
  summary: { passed: number; failed: number; skipped: number; interrupted: number } | null;
  tests: Array<{ title: string; file: string; line: number; project: string | null; status: string }>;
  outputTruncated: boolean;
};

const styles = `
.playwright-view{gap:8px}.playwright-heading{display:flex;align-items:center;gap:9px;flex:none}.playwright-heading-copy{flex:1;min-width:0}.playwright-heading h1{margin:0;color:var(--ink);font-size:12px;font-weight:680}.playwright-heading p{margin:3px 0 0;color:var(--muted);font-size:9px}.playwright-project{flex:none;display:flex;align-items:center;gap:6px;padding:7px;border:1px solid var(--line);border-radius:9px;background:rgba(255,255,255,.28)}.window-shell[data-theme="dark"] .playwright-project{background:rgba(16,25,29,.28)}.playwright-project input{flex:1;min-width:0;height:28px;padding:5px 7px;border:1px solid var(--line);border-radius:7px;outline:0;color:var(--ink);background:var(--control);font:9px inherit}.playwright-project input:focus,.playwright-inline-editor input:focus{border-color:var(--accent);box-shadow:0 0 0 2px rgba(25,139,162,.1)}.playwright-project .secondary-button{flex:none}.playwright-info{display:flex;align-items:center;gap:6px;margin:0;padding:7px 9px;border:1px solid var(--line);border-radius:8px;color:var(--muted-strong);background:rgba(255,255,255,.27);font-size:9px;line-height:1.4}.playwright-info[data-error="true"]{color:var(--danger);background:rgba(255,240,238,.48)}.playwright-toolbar{display:flex;align-items:center;gap:6px;flex:none}.playwright-toolbar .spacer{flex:1}.playwright-toolbar select{height:29px;min-width:105px;padding:4px 7px;border:1px solid var(--line);border-radius:7px;color:var(--ink);background:var(--control);font:9px inherit}.playwright-runner{font-size:8px;color:var(--muted);white-space:nowrap}.playwright-list{flex:1;min-height:0;display:flex;flex-direction:column;gap:7px;overflow:auto;padding:1px 2px 3px 0;scrollbar-width:thin}.playwright-file{flex:none;border:1px solid var(--line);border-radius:10px;overflow:hidden;background:rgba(255,255,255,.26)}.window-shell[data-theme="dark"] .playwright-file{background:rgba(16,25,29,.22)}.playwright-file summary{display:flex;align-items:center;gap:6px;min-height:38px;padding:6px 8px;cursor:pointer;list-style:none}.playwright-file summary::-webkit-details-marker{display:none}.playwright-file-name{flex:1;min-width:0;overflow:hidden;color:var(--ink);font:9px "Cascadia Code",Consolas,monospace;text-overflow:ellipsis;white-space:nowrap}.playwright-file-count{color:var(--muted);font-size:8px;white-space:nowrap}.playwright-file-tools{display:flex;gap:3px}.playwright-test{display:flex;flex-direction:column;gap:4px;padding:7px 8px;border-top:1px solid var(--line)}.playwright-test-main{display:flex;align-items:center;gap:6px;min-width:0}.playwright-test-copy{flex:1;min-width:0}.playwright-test-title{overflow:hidden;color:var(--ink);font-size:9px;text-overflow:ellipsis;white-space:nowrap}.playwright-test-meta{margin-top:2px;color:var(--muted);font-size:7px}.playwright-test-actions{display:flex;align-items:center;gap:2px}.playwright-test-actions .icon-button{width:26px;height:26px}.playwright-tag-list{display:flex;flex-wrap:wrap;gap:4px}.playwright-tag{padding:2px 6px;border:1px solid rgba(26,131,151,.18);border-radius:99px;color:var(--accent-strong);background:var(--accent-soft);font-size:7px}.playwright-inline-editor{display:flex;align-items:center;gap:5px}.playwright-inline-editor input{flex:1;min-width:80px;height:25px;padding:4px 6px;border:1px solid var(--line);border-radius:6px;color:var(--ink);background:var(--control);font:8px inherit}.playwright-inline-editor small{color:var(--muted);font-size:7px}.playwright-form{flex:none;display:flex;flex-direction:column;gap:7px;padding:8px;border:1px solid var(--line);border-radius:9px;background:rgba(255,255,255,.32)}.playwright-form header{display:flex;align-items:center;gap:6px;color:var(--ink);font-size:9px}.playwright-form header strong{flex:1}.playwright-form-fields{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr);gap:6px}.playwright-form label{display:flex;flex-direction:column;gap:3px;color:var(--muted);font-size:8px}.playwright-form label:first-child{grid-column:1/-1}.playwright-form input{min-width:0;height:28px;padding:5px 7px;border:1px solid var(--line);border-radius:7px;color:var(--ink);background:var(--control);font:9px inherit}.playwright-form-actions{display:flex;justify-content:flex-end;gap:5px}.playwright-empty{flex:1;min-height:100px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:6px;color:var(--muted);text-align:center}.playwright-empty strong{color:var(--ink);font-size:10px}.playwright-empty p{max-width:320px;margin:0;font-size:8px;line-height:1.5}.playwright-stale{flex:none;display:flex;align-items:center;gap:7px;padding:7px;border:1px solid var(--line);border-radius:8px;color:var(--muted-strong);font-size:8px}.playwright-stale span{flex:1}.playwright-run-result{flex:none;max-height:27%;display:flex;flex-direction:column;gap:5px;padding:7px;border:1px solid var(--line);border-radius:9px;background:rgba(255,255,255,.28);overflow:auto}.playwright-run-result header{display:flex;align-items:center;gap:6px;color:var(--ink);font-size:9px}.playwright-run-result header strong{flex:1}.playwright-outcome{display:flex;gap:7px;align-items:center;color:var(--muted-strong);font-size:8px}.playwright-outcome[data-status="passed"]{color:var(--success)}.playwright-outcome[data-status="failed"],.playwright-outcome[data-status="timedOut"]{color:var(--danger)}.playwright-outcome span{flex:1;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.playwright-view .icon-button{flex:none}.playwright-view .icon-button:disabled{opacity:.45}
@media(max-width:430px){.playwright-project{flex-wrap:wrap}.playwright-project input{flex-basis:100%}.playwright-toolbar{flex-wrap:wrap}.playwright-form-fields{grid-template-columns:1fr}.playwright-form label:first-child{grid-column:auto}}
`;

function readRunOutput(message: string, value: unknown): RunOutput {
  const record = value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : {};
  const summaryValue = record.summary && typeof record.summary === 'object' && !Array.isArray(record.summary)
    ? record.summary as Record<string, unknown> : null;
  const tests = Array.isArray(record.tests) ? record.tests.flatMap((test) => {
    if (!test || typeof test !== 'object' || Array.isArray(test)) return [];
    const item = test as Record<string, unknown>;
    if (typeof item.title !== 'string' || typeof item.file !== 'string' || typeof item.status !== 'string') return [];
    return [{ title: item.title, file: item.file, line: typeof item.line === 'number' ? item.line : 0,
      project: typeof item.project === 'string' ? item.project : null, status: item.status }];
  }) : [];
  const number = (value: unknown) => typeof value === 'number' && Number.isFinite(value) ? Math.max(0, value) : 0;
  return {
    message,
    timedOut: record.timedOut === true,
    durationMilliseconds: number(record.durationMilliseconds),
    summary: summaryValue ? { passed: number(summaryValue.passed), failed: number(summaryValue.failed),
      skipped: number(summaryValue.skipped), interrupted: number(summaryValue.interrupted) } : null,
    tests: tests.slice(0, 256),
    outputTruncated: record.outputTruncated === true,
  };
}

function displayTestTitle(test: PlaywrightTestIdentity): string {
  return test.titlePath.join(' › ');
}

function safeTagName(value: string): string {
  return value.trim().replace(/[^A-Za-z0-9 ._-]+/g, '').slice(0, 48);
}

export function BrowserAutomationView({ tab, services }: ViewProps) {
  const [explorer, setExplorer] = useState<PlaywrightExplorerState | null>(null);
  const [projectDraft, setProjectDraft] = useState('');
  const [notice, setNotice] = useState('');
  const [noticeIsError, setNoticeIsError] = useState(false);
  const [busy, setBusy] = useState<string | null>(null);
  const [form, setForm] = useState<{ file: string; testName: string } | null>(null);
  const [formKind, setFormKind] = useState<'section' | 'test'>('section');
  const [tagEditorId, setTagEditorId] = useState<string | null>(null);
  const [tagDraft, setTagDraft] = useState('');
  const [selectedTag, setSelectedTag] = useState('');
  const [runOutput, setRunOutput] = useState<RunOutput | null>(null);
  const runController = useRef<AbortController | null>(null);

  const showResult = useCallback((result: { status: string; message: string; data: unknown }) => {
    if (result.status === 'error') throw new Error(result.message);
    setNotice(result.message);
    setNoticeIsError(result.status === 'warning' && /could not|failed|unavailable|not found|exceeded/i.test(result.message));
    return result.data;
  }, []);

  const loadState = useCallback(async () => {
    let discovering = false;
    try {
      const result = await services.modules.dispatch('getExplorerState', {});
      const data = result.data && typeof result.data === 'object' ? result.data as Record<string, unknown> : {};
      const parsed = readPlaywrightExplorerState(data.state);
      if (!parsed) throw new Error('Playwright Explorer returned invalid saved state.');
      setExplorer(parsed);
      setProjectDraft(parsed.projectRoot ?? '');
      setSelectedTag((current) => current && parsed.tags.includes(current) ? current : parsed.tags[0] ?? '');
      if (result.status !== 'error') {
        setNotice(parsed.isManagedProject && parsed.runner.available ? 'Loading the example test…' : parsed.runner.message);
        setNoticeIsError(false);
      }
      if (parsed.isManagedProject && parsed.runner.available) {
        discovering = true;
        setBusy('discover');
        const discoveryResult = await services.modules.dispatch('discoverTests', {});
        const discoveryData = showResult(discoveryResult);
        const payload = discoveryData && typeof discoveryData === 'object' ? discoveryData as Record<string, unknown> : {};
        const discovered = readPlaywrightExplorerState(payload.state);
        if (!discovered) throw new Error('Playwright test discovery returned invalid data.');
        setExplorer(discovered);
        setProjectDraft(discovered.projectRoot ?? '');
        setSelectedTag((current) => current && discovered.tags.includes(current) ? current : discovered.tags[0] ?? '');
      }
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not load the Playwright Test Explorer.');
      setNoticeIsError(true);
    } finally {
      if (discovering) setBusy((current) => current === 'discover' ? null : current);
    }
  }, [services, showResult]);

  const refreshTests = useCallback(async () => {
    if (busy) return;
    setBusy('discover');
    try {
      const result = await services.modules.dispatch('discoverTests', {});
      const data = showResult(result);
      const payload = data && typeof data === 'object' ? data as Record<string, unknown> : {};
      const parsed = readPlaywrightExplorerState(payload.state);
      if (!parsed) throw new Error('Playwright test discovery returned invalid data.');
      setExplorer(parsed);
      setProjectDraft(parsed.projectRoot ?? '');
      setSelectedTag((current) => current && parsed.tags.includes(current) ? current : parsed.tags[0] ?? '');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not discover Playwright tests.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  }, [busy, services, showResult]);

  useEffect(() => { void loadState(); }, [loadState]);

  const chooseProject = async () => {
    if (busy) return;
    try {
      const path = await services.files.pickBrowserProjectDirectory();
      if (path) setProjectDraft(path);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not open the project folder picker.');
      setNoticeIsError(true);
    }
  };

  const saveProject = async (event?: FormEvent) => {
    event?.preventDefault();
    if (!projectDraft.trim() || busy) return;
    setBusy('project');
    try {
      const result = await services.modules.dispatch('setProject', { projectRoot: projectDraft.trim() });
      const data = showResult(result) as Record<string, unknown>;
      const parsed = readPlaywrightExplorerState(data.state);
      if (!parsed) throw new Error('The saved project state was invalid.');
      setExplorer(parsed);
      setProjectDraft(parsed.projectRoot ?? '');
      setRunOutput(null);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save the selected project.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const openCreateSection = () => {
    setFormKind('section');
    setForm({ file: 'tests/new.spec.ts', testName: 'new test' });
  };

  const openAppendTest = (file: string) => {
    setFormKind('test');
    setForm({ file, testName: 'new test' });
  };

  const saveTestFile = async (event: FormEvent) => {
    event.preventDefault();
    if (!form || busy) return;
    const normalized = validatePlaywrightSectionPath(form.file);
    if (!normalized) {
      setNotice('Use a project-relative *.spec.ts or *.test.ts file path without `..` segments.');
      setNoticeIsError(true);
      return;
    }
    setBusy('file');
    try {
      const result = await services.modules.dispatch(formKind === 'section' ? 'createSection' : 'appendTest', {
        file: normalized, testName: form.testName.trim(),
      });
      showResult(result);
      setForm(null);
      await refreshTests();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not update the Playwright test file.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const editTags = (test: PlaywrightTestIdentity) => {
    setTagEditorId(test.id);
    setTagDraft(test.tags.join(', '));
  };

  const saveTags = async (test: PlaywrightTestIdentity) => {
    if (busy) return;
    const tags = [...new Set(tagDraft.split(',').map(safeTagName).filter(Boolean))].slice(0, 16);
    setBusy(`tags:${test.id}`);
    try {
      const result = await services.modules.dispatch('saveTestTags', { testId: test.id, tags });
      showResult(result);
      setExplorer((current) => {
        if (!current) return current;
        const files = current.files.map((file) => ({ ...file, tests: file.tests.map((candidate) => candidate.id === test.id ? { ...candidate, tags } : candidate) }));
        const knownTags = [...new Set(files.flatMap((file) => file.tests.flatMap((candidate) => candidate.tags)))].sort((a, b) => a.localeCompare(b));
        return { ...current, files, tags: knownTags };
      });
      setTagEditorId(null);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save test tags.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const run = async (kind: 'file' | 'test' | 'tag', target: string) => {
    if (busy) return;
    const controller = new AbortController();
    runController.current = controller;
    setBusy('run');
    setRunOutput(null);
    try {
      const input: Record<string, string> = { kind };
      if (kind === 'file') input.file = target;
      else if (kind === 'test') input.testId = target;
      else input.tag = target;
      const result = await services.modules.dispatch('runTests', input as AutomationModuleActionRequest['input'], { signal: controller.signal });
      const data = showResult(result);
      const output = readRunOutput(result.message, data);
      setRunOutput(output);
    } catch (error) {
      if (error instanceof Error && error.name === 'AbortError') {
        setNotice('Playwright run cancelled.');
        setNoticeIsError(false);
      } else {
        setNotice(error instanceof Error ? error.message : 'Playwright run failed.');
        setNoticeIsError(true);
      }
    } finally {
      if (runController.current === controller) runController.current = null;
      setBusy(null);
    }
  };

  const removeStaleTags = async () => {
    if (busy || !explorer?.staleTags.length) return;
    setBusy('stale');
    try {
      const result = await services.modules.dispatch('removeStaleTags', {});
      showResult(result);
      await refreshTests();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not remove stale test tags.');
      setNoticeIsError(true);
    } finally { setBusy(null); }
  };

  const commands = [
    { id: 'refresh-tests', label: 'Refresh Playwright tests', keywords: ['discover', 'reload'], disabled: !explorer?.projectRoot || !!busy, run: refreshTests },
    { id: 'choose-project', label: 'Choose Playwright project folder', keywords: ['folder', 'directory'], disabled: !!busy, run: chooseProject },
    { id: 'create-section', label: 'Create Playwright test section', keywords: ['new file', 'spec'], disabled: !explorer?.projectRoot || !!busy, run: openCreateSection },
    ...((explorer?.files ?? []).flatMap((file) => [
      { id: `run-file:${file.path}`, label: `Run section ${file.path}`, keywords: ['playwright test file'], disabled: !!busy, confirmationPrompt: `Run all Playwright tests in ${file.path}?`, run: () => run('file', file.path) },
      { id: `append:${file.path}`, label: `Add test to ${file.path}`, keywords: ['new test'], disabled: !!busy, run: () => openAppendTest(file.path) },
      ...file.tests.map((test) => ({ id: `run-test:${test.id}`, label: `Run ${displayTestTitle(test)}`, keywords: ['playwright test', file.path], disabled: !!busy, confirmationPrompt: `Run “${displayTestTitle(test)}”?`, run: () => run('test', test.id) })),
    ])),
    ...(explorer?.tags ?? []).map((tag) => ({ id: `run-tag:${tag}`, label: `Run tag group ${tag}`, keywords: ['playwright tests'], disabled: !!busy, confirmationPrompt: `Run Playwright tests tagged “${tag}”?`, run: () => run('tag', tag) })),
    { id: 'remove-stale-tags', label: 'Remove stale Playwright test tags', keywords: ['cleanup'], disabled: !explorer?.staleTags.length || !!busy, confirmationPrompt: 'Remove saved tags for tests that are no longer discovered?', run: removeStaleTags },
  ];
  useRegisterTabCommands(tab.id, commands);

  const outcomesByLocation = new Map((runOutput?.tests ?? []).map((outcome) => [`${outcome.file.toLowerCase()}:${outcome.line}:${outcome.title.toLowerCase()}`, outcome.status]));
  const staleTagCount = explorer?.staleTags.length ?? 0;

  return <section className="module-view playwright-view" aria-label="Playwright Test Explorer">
    <style>{styles}</style>
    <header className="playwright-heading">
      <span className="script-module-icon"><FilePlus2 size={17} /></span>
      <div className="playwright-heading-copy"><h1>Playwright Test Explorer</h1><p>Agent-authored test files, run and grouped with Automator tags</p></div>
      {explorer?.runner.version && <span className="playwright-runner">Playwright {explorer.runner.version}</span>}
    </header>

    <form className="playwright-project" onSubmit={(event) => void saveProject(event)}>
      <FolderOpen size={14} />
      <input aria-label="Playwright project folder" value={projectDraft} onChange={(event) => setProjectDraft(event.target.value)} placeholder="Select a project folder containing node_modules/@playwright/test" />
      <button className="secondary-button" type="button" disabled={!!busy} onClick={() => void chooseProject()}>Browse</button>
      <button className="primary-button" disabled={!!busy || !projectDraft.trim()}>{busy === 'project' ? <LoaderCircle size={12} /> : <Check size={12} />} Use folder</button>
    </form>

    {notice && <p className="playwright-info" data-error={noticeIsError} role={noticeIsError ? 'alert' : 'status'}>{noticeIsError ? <CircleAlert size={12} /> : <Check size={12} />}{notice}</p>}
    {explorer?.projectRoot && !explorer.runner.available && <p className="playwright-info" data-error="true" role="alert">{explorer.runner.message}</p>}

    {form && <form className="playwright-form" onSubmit={(event) => void saveTestFile(event)}>
      <header><FilePlus2 size={13} /><strong>{formKind === 'section' ? 'Create a test section' : 'Add a test to this section'}</strong></header>
      <div className="playwright-form-fields">
        <label><span>Test file in the project</span><input required maxLength={512} value={form.file} disabled={formKind === 'test'} onChange={(event) => setForm({ ...form, file: event.target.value })} placeholder="tests/auth/login.spec.ts" /></label>
        <label><span>Test name</span><input required maxLength={256} value={form.testName} onChange={(event) => setForm({ ...form, testName: event.target.value })} placeholder="user can sign in" /></label>
      </div>
      <div className="playwright-form-actions"><button className="secondary-button" type="button" disabled={!!busy} onClick={() => setForm(null)}>Cancel</button><button className="primary-button" disabled={!!busy}>{busy === 'file' ? <LoaderCircle size={12} /> : <Plus size={12} />} {formKind === 'section' ? 'Create file' : 'Add test'}</button></div>
    </form>}

    {explorer && staleTagCount > 0 && <div className="playwright-stale"><Tag size={12} /><span>{staleTagCount} tag record{staleTagCount === 1 ? '' : 's'} refer to tests no longer found after refresh.</span><button className="secondary-button" type="button" disabled={!!busy} onClick={() => void removeStaleTags()}><Trash2 size={11} /> Clean up</button></div>}

    <div className="playwright-toolbar">
      <button className="secondary-button" type="button" disabled={!!busy || !explorer?.projectRoot} onClick={() => void refreshTests()}>{busy === 'discover' ? <LoaderCircle size={12} /> : <RefreshCw size={12} />} Refresh tests</button>
      <button className="secondary-button" type="button" disabled={!!busy || !explorer?.projectRoot} onClick={openCreateSection}><Plus size={12} /> New section</button>
      <span className="spacer" />
      <select aria-label="Tag group" value={selectedTag} onChange={(event) => setSelectedTag(event.target.value)} disabled={!explorer?.tags.length}>
        {explorer?.tags.length ? explorer.tags.map((tag) => <option key={tag} value={tag}>{tag}</option>) : <option value="">No tag groups</option>}
      </select>
      <button className="secondary-button" type="button" disabled={!!busy || !selectedTag} onClick={() => void run('tag', selectedTag)}><Play size={11} /> Run tag</button>
      {busy === 'run' && <button className="secondary-button" type="button" onClick={() => runController.current?.abort()}><Square size={10} /> Cancel</button>}
    </div>

    <div className="playwright-list" aria-live="polite">
      {!explorer?.projectRoot && <div className="playwright-empty"><FolderOpen size={24} /><strong>Select a Playwright project</strong><p>Choose a project folder containing its own Playwright Test runner. Automator will not install or modify dependencies.</p></div>}
      {explorer?.projectRoot && !explorer.runner.available && <div className="playwright-empty"><CircleAlert size={24} /><strong>Playwright Test is not set up here</strong><p>Install the runner in this project yourself, then refresh. Existing project tests remain untouched.</p></div>}
      {explorer?.projectRoot && explorer.runner.available && explorer.files.length === 0 && <div className="playwright-empty"><FilePlus2 size={24} /><strong>No tests discovered</strong><p>Create a test section here or add test files with your agent, then refresh.</p><button className="secondary-button" type="button" disabled={!!busy} onClick={openCreateSection}><Plus size={11} /> Create section</button></div>}
      {explorer?.files.map((file) => <details className="playwright-file" key={file.path} open>
        <summary><FilePlus2 size={13} /><span className="playwright-file-name" title={file.path}>{file.path}</span><span className="playwright-file-count">{file.tests.length} test{file.tests.length === 1 ? '' : 's'}</span>
          <span className="playwright-file-tools"><button className="icon-button" type="button" aria-label={`Run section ${file.path}`} title="Run section" disabled={!!busy} onClick={(event) => { event.preventDefault(); event.stopPropagation(); void run('file', file.path); }}><Play size={12} /></button><button className="icon-button" type="button" aria-label={`Add test to ${file.path}`} title="Add test" disabled={!!busy} onClick={(event) => { event.preventDefault(); event.stopPropagation(); openAppendTest(file.path); }}><Plus size={12} /></button></span>
        </summary>
        {file.tests.map((test) => {
          const outcomeKey = `${test.file.toLowerCase()}:${test.line}:${test.titlePath.at(-1)?.toLowerCase() ?? ''}`;
          const outcome = outcomesByLocation.get(outcomeKey);
          return <article className="playwright-test" key={test.id}>
            <div className="playwright-test-main"><div className="playwright-test-copy"><div className="playwright-test-title" title={displayTestTitle(test)}>{displayTestTitle(test)}</div><div className="playwright-test-meta">{test.project ? `[${test.project}] · ` : ''}line {test.line}{outcome ? ` · ${outcome}` : ''}</div></div>
              <span className="playwright-test-actions"><button className="icon-button" type="button" title="Edit tags" aria-label={`Edit tags for ${displayTestTitle(test)}`} disabled={!!busy} onClick={() => editTags(test)}><Tag size={12} /></button><button className="icon-button" type="button" title="Run test" aria-label={`Run ${displayTestTitle(test)}`} disabled={!!busy} onClick={() => void run('test', test.id)}><Play size={12} /></button></span>
            </div>
            {test.tags.length > 0 && <div className="playwright-tag-list">{test.tags.map((tag) => <span className="playwright-tag" key={tag}>{tag}</span>)}</div>}
            {tagEditorId === test.id && <div className="playwright-inline-editor"><Tag size={11} /><input aria-label={`Tags for ${displayTestTitle(test)}`} value={tagDraft} onChange={(event) => setTagDraft(event.target.value)} placeholder="smoke, login" /><small>comma separated</small><button className="icon-button" type="button" aria-label="Save tags" disabled={!!busy} onClick={() => void saveTags(test)}><Check size={12} /></button><button className="icon-button" type="button" aria-label="Cancel tag editing" disabled={!!busy} onClick={() => setTagEditorId(null)}>×</button></div>}
          </article>;
        })}
      </details>)}
    </div>

    {runOutput && <section className="playwright-run-result" aria-label="Playwright run results">
      <header><Check size={12} /><strong>Run results</strong><span>{(runOutput.durationMilliseconds / 1000).toFixed(1)} s</span><button className="icon-button" aria-label="Clear run results" onClick={() => setRunOutput(null)}>×</button></header>
      <p className="playwright-info">{runOutput.message}{runOutput.outputTruncated ? ' Test runner output was truncated; the run itself continued.' : ''}</p>
      {runOutput.tests.map((test, index) => <div className="playwright-outcome" data-status={test.status} key={`${test.file}:${test.line}:${test.project}:${index}`}><span>{test.project ? `[${test.project}] ` : ''}{test.file}:{test.line} · {test.title}</span><strong>{test.status}</strong></div>)}
    </section>}
  </section>;
}
