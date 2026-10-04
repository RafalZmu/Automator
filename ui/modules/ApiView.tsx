import { GlobalVariableHints } from '../variables/GlobalVariables';
import {
  Braces, Check, Clock3, Globe2, KeyRound, Pencil, Play, Plus, Save, ShieldCheck, Square, Trash2, X,
} from 'lucide-react';
import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import type { AutomationServices } from '../automationServices';
import type { AutomationModuleActionRequest, BackendUiState } from '../../contracts/rpc';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import {
  decodeApiResponse, parseApiCurlImport, readApiProfiles, validateApiProfile, type ApiProfile, type ApiResponseMode,
} from '../contracts/api';

type ViewProps = {
  tab: BackendUiState['tabs'][number];
  moduleState: BackendUiState['moduleStates'][number] | undefined;
  services: AutomationServices;
};

type ApiProfileEntry = ApiProfile & { secretStatus: Record<string, boolean> };
type ApiRunOutput = {
  profileId: string;
  statusCode: number;
  contentType: string;
  headers: Record<string, string>;
  durationMilliseconds: number;
  responseMode: ApiResponseMode;
  bodyText: string;
  structuredOutput: unknown | null;
  structuredOutputPresent: boolean;
  parseFailure: boolean;
  bodyTruncated: boolean;
};

type ApiProfileDraft = ApiProfile & { headerText: string; defaultInputText: string };

function formatJsonInput(value: unknown): string {
  return value === undefined ? '' : JSON.stringify(value, null, 2);
}

function blankProfile(): ApiProfileDraft {
  return {
    id: `api-${Date.now().toString(36)}`,
    name: '',
    method: 'GET',
    url: '',
    allowLocalNetwork: false,
    headers: { Accept: 'application/json' },
    headerText: '{\n  "Accept": "application/json"\n}',
    secretHeaders: {},
    bodyTemplate: null,
    defaultInputText: '',
    responseMode: 'json',
    timeoutSeconds: 30,
  };
}

function profileDraft(profile: ApiProfile): ApiProfileDraft {
  return {
    ...profile,
    headers: { ...profile.headers },
    secretHeaders: { ...profile.secretHeaders },
    headerText: JSON.stringify(profile.headers, null, 2),
    defaultInputText: formatJsonInput(profile.defaultInput),
  };
}

function readSecretStatus(value: unknown, profiles: ApiProfile[]): Record<string, Record<string, boolean>> {
  if (!value || typeof value !== 'object' || !('secretStatus' in value)
      || !value.secretStatus || typeof value.secretStatus !== 'object' || Array.isArray(value.secretStatus)) return {};
  const source = value.secretStatus as Record<string, unknown>;
  const result: Record<string, Record<string, boolean>> = {};
  for (const profile of profiles) {
    const profileState = source[profile.id];
    if (!profileState || typeof profileState !== 'object' || Array.isArray(profileState)) continue;
    const knownHeaders = new Set(Object.keys(profile.secretHeaders).map((name) => name.toLowerCase()));
    const status: Record<string, boolean> = {};
    for (const [name, configured] of Object.entries(profileState)) {
      if (knownHeaders.has(name.toLowerCase()) && typeof configured === 'boolean') status[name] = configured;
    }
    result[profile.id] = status;
  }
  return result;
}

function readRunOutput(data: unknown): ApiRunOutput | null {
  if (!data || typeof data !== 'object') return null;
  const item = data as Partial<ApiRunOutput>;
  if (typeof item.profileId !== 'string' || typeof item.statusCode !== 'number'
      || typeof item.durationMilliseconds !== 'number'
      || (item.responseMode !== 'text' && item.responseMode !== 'json')
      || typeof item.bodyText !== 'string') return null;
  return {
    profileId: item.profileId,
    statusCode: item.statusCode,
    contentType: typeof item.contentType === 'string' ? item.contentType : '',
    headers: item.headers && typeof item.headers === 'object' ? item.headers : {},
    durationMilliseconds: item.durationMilliseconds,
    responseMode: item.responseMode,
    bodyText: item.bodyText,
    structuredOutput: item.structuredOutput ?? null,
    structuredOutputPresent: item.structuredOutputPresent === true,
    parseFailure: item.parseFailure === true,
    bodyTruncated: item.bodyTruncated === true,
  };
}

function responsePresentation(output: ApiRunOutput) {
  if (output.structuredOutputPresent) {
    return { bodyText: '', structuredOutput: output.structuredOutput, hasStructuredOutput: true, parseFailure: output.parseFailure };
  }
  return decodeApiResponse(output.responseMode, output.bodyText);
}

function parseHeaders(text: string): Record<string, string> | null {
  try {
    const value: unknown = JSON.parse(text);
    if (!value || typeof value !== 'object' || Array.isArray(value)) return null;
    if (Object.entries(value).some(([key, item]) => typeof item !== 'string' || !key)) return null;
    return value as Record<string, string>;
  } catch {
    return null;
  }
}

const API_VIEW_STYLES = `
.api-view{gap:8px}.api-heading{flex:none;justify-content:space-between;gap:10px;min-height:38px}
.api-editor{max-height:75%}.api-fields label.api-local-network{display:flex;flex-direction:row;align-items:center;gap:7px;padding:4px 2px;color:var(--muted-strong);font-size:9px}
.api-local-network input{width:14px;min-height:14px;accent-color:var(--accent)}.api-section-title{display:flex;align-items:center;justify-content:space-between;gap:8px;color:var(--muted-strong);font-size:9px;font-weight:650}
.api-section-title .secondary-button{min-height:26px;padding:0 8px;font-size:8px}.api-help{margin:3px 0 0;color:var(--muted);font-size:8px;line-height:1.4}
.api-add-secret{display:flex;gap:6px}.api-add-secret input{min-width:0;flex:1;min-height:28px;padding:5px 7px;border:1px solid var(--line);border-radius:7px;outline:0;color:var(--ink);background:var(--control);font-size:9px}.api-add-secret .secondary-button{flex:none;min-height:28px}
.api-secret-draft-row{display:flex;align-items:center;gap:7px;min-height:30px;padding:4px 7px;border:1px solid var(--line);border-radius:8px;color:var(--muted-strong);background:rgba(255,255,255,.28)}
.api-secret-draft-row strong{color:var(--ink);font-size:9px}.api-secret-draft-row span{flex:1;color:var(--muted);font-size:8px}
.api-validation{margin:0;padding:7px 9px 7px 24px;border:1px solid rgba(158,73,73,.2);border-radius:8px;color:var(--danger);background:rgba(255,240,238,.72);font-size:9px;line-height:1.5}
.window-shell[data-theme="dark"] .api-validation{background:rgba(78,34,33,.4)}.api-editor-actions{display:flex;justify-content:flex-end;gap:6px}
.api-run-input{flex:none;display:grid;grid-template-columns:minmax(110px,.7fr) minmax(0,2fr);align-items:center;gap:8px;padding:7px 9px;border:1px solid var(--line);border-radius:10px;background:rgba(255,255,255,.3)}
.api-run-input label{display:flex;flex-direction:column;gap:3px;color:var(--muted-strong);font-size:9px;font-weight:650}.api-run-input label span{display:flex;align-items:center;gap:5px}.api-run-input label small{color:var(--muted);font-size:8px;font-weight:450}
.api-run-input textarea{width:100%;min-width:0;resize:vertical;padding:6px 8px;border:1px solid var(--line);border-radius:7px;outline:0;color:var(--ink);background:var(--control);font:9px/1.4 "Cascadia Code",Consolas,monospace}
.api-run-input textarea:focus{border-color:rgba(24,130,152,.48);box-shadow:0 0 0 2px rgba(25,139,162,.09)}.window-shell[data-theme="dark"] .api-run-input{background:rgba(16,25,29,.35)}
.api-curl-import{flex:none;border:1px solid var(--line);border-radius:9px;background:rgba(255,255,255,.24)}.api-curl-import summary{padding:6px 8px;color:var(--muted-strong);cursor:pointer;font-size:9px;font-weight:650}.api-curl-import-body{display:grid;gap:6px;padding:0 8px 8px}.api-curl-import textarea{width:100%;resize:vertical;padding:6px 8px;border:1px solid var(--line);border-radius:7px;color:var(--ink);background:var(--control);font:9px/1.4 "Cascadia Code",Consolas,monospace}.api-curl-import-actions{display:flex;align-items:center;justify-content:space-between;gap:7px}.api-curl-import-actions small{color:var(--muted);font-size:8px}.api-profile-input{grid-template-columns:minmax(100px,.55fr) minmax(0,2fr);margin:0 8px 8px}.api-profile-input textarea{min-height:40px}.api-default-input{min-height:52px}
.api-profile-card{flex:none;overflow:hidden;border:1px solid var(--line);border-radius:11px;background:rgba(255,255,255,.32)}.window-shell[data-theme="dark"] .api-profile-card{background:rgba(20,31,35,.32)}
.api-profile-row{border:0;border-radius:0;background:transparent}.api-profile-row:hover{transform:none}.api-profile-copy{gap:3px}.api-profile-copy small{overflow:hidden;color:var(--muted);font-size:8px;text-overflow:ellipsis;white-space:nowrap}
.api-profile-icon{flex:none;width:31px;height:31px;display:grid;place-items:center;border:1px solid rgba(26,131,151,.15);border-radius:9px;color:var(--accent-strong);background:var(--accent-soft)}
.api-profile-time{display:inline-flex;align-items:center;gap:3px;color:var(--muted);font-size:8px}.api-secret-list{display:flex;flex-direction:column;gap:5px;padding:0 8px 8px}
.api-secret-row{display:flex;align-items:center;gap:7px;min-height:32px;padding:4px 6px;border:1px solid var(--line);border-radius:8px;background:rgba(255,255,255,.26)}
.window-shell[data-theme="dark"] .api-secret-row{background:rgba(0,0,0,.12)}.api-secret-row strong{min-width:75px;color:var(--ink);font-size:8px}.api-secret-state{display:grid;place-items:center;color:var(--accent)}
.api-secret-configured,.api-secret-missing{color:var(--success);font-size:8px}.api-secret-missing{color:var(--danger)}.api-secret-row input{min-width:0;flex:1;height:26px;padding:4px 7px;border:1px solid var(--line);border-radius:6px;outline:0;color:var(--ink);background:var(--control);font-size:8px}
.api-secret-row .secondary-button{min-height:25px;padding:0 7px;font-size:8px}.api-output{max-height:43%}.api-output header > div small{color:var(--muted);font-size:8px}.api-output-status{margin:0;color:var(--muted);font-size:9px}
.api-response-headers{flex:none;color:var(--muted-strong);font-size:8px}.api-response-headers summary{cursor:pointer}.api-response-headers dl{max-height:70px;margin:4px 0 0;overflow:auto}.api-response-headers dl div{display:grid;grid-template-columns:minmax(60px,.6fr) minmax(0,1.4fr);gap:7px;padding:2px 0}.api-response-headers dt{font-weight:650}.api-response-headers dd{margin:0;overflow-wrap:anywhere}
@media(max-width:430px){.api-profile-time{display:none}.api-secret-row{flex-wrap:wrap}.api-secret-row input{flex-basis:100%}.api-run-input{grid-template-columns:1fr}.api-run-input label{flex-direction:row;justify-content:space-between}}
`;

function newSecretId(): string {
  return globalThis.crypto?.randomUUID?.() ?? `secret-${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

export function ApiView({ services }: ViewProps) {
  const [profiles, setProfiles] = useState<ApiProfileEntry[]>([]);
  const [editing, setEditing] = useState<ApiProfileDraft | null>(null);
  const [output, setOutput] = useState<ApiRunOutput | null>(null);
  const [requestInputs, setRequestInputs] = useState<Record<string, string>>({});
  const [secretDrafts, setSecretDrafts] = useState<Record<string, string>>({});
  const [pendingImportedSecrets, setPendingImportedSecrets] = useState<Record<string, string>>({});
  const [curlText, setCurlText] = useState('');
  const [newSecretHeader, setNewSecretHeader] = useState('');
  const [runningId, setRunningId] = useState<string | null>(null);
  const [busySecret, setBusySecret] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const [validationIssues, setValidationIssues] = useState<string[]>([]);
  const runController = useRef<AbortController | null>(null);

  const refresh = useCallback(async () => {
    try {
      const result = await services.modules.dispatch('listProfiles', {});
      if (result.status === 'error') throw new Error(result.message);
      const safeProfiles = readApiProfiles(result.data);
      const status = readSecretStatus(result.data, safeProfiles);
      setProfiles(safeProfiles.map((profile) => ({ ...profile, secretStatus: status[profile.id] ?? {} })));
      setRequestInputs((current) => Object.fromEntries(safeProfiles.map((profile) => [
        profile.id,
        current[profile.id] ?? formatJsonInput(profile.defaultInput),
      ])));
      setNotice('');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not load API profiles.');
    }
  }, [services]);

  useEffect(() => {
    void refresh();
    return () => runController.current?.abort();
  }, [refresh]);

  const saveProfile = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!editing) return;
    const headers = parseHeaders(editing.headerText);
    if (!headers) {
      setValidationIssues(['Headers must be a JSON object whose values are strings.']);
      return;
    }
    let defaultInput: unknown;
    try {
      defaultInput = editing.defaultInputText.trim() ? JSON.parse(editing.defaultInputText) as unknown : undefined;
    } catch {
      setValidationIssues(['Default JSON input must contain valid JSON.']);
      return;
    }
    const normalized: ApiProfile = {
      id: editing.id.trim().toLowerCase(),
      name: editing.name.trim(),
      method: editing.method.trim().toUpperCase(),
      url: editing.url.trim(),
      allowLocalNetwork: editing.allowLocalNetwork,
      headers,
      secretHeaders: { ...editing.secretHeaders },
      bodyTemplate: editing.bodyTemplate,
      ...(defaultInput === undefined ? {} : { defaultInput }),
      responseMode: editing.responseMode,
      timeoutSeconds: Math.floor(editing.timeoutSeconds),
    };
    const issues = validateApiProfile(normalized);
    if (issues.length) {
      setValidationIssues(issues);
      return;
    }
    setValidationIssues([]);
    try {
      const result = await services.modules.dispatch('saveProfile', normalized as AutomationModuleActionRequest['input']);
      if (result.status === 'error') throw new Error(result.message);
      let importedSecretFailure = false;
      for (const [headerName, value] of Object.entries(pendingImportedSecrets)) {
        const savedHeader = Object.keys(normalized.secretHeaders).find((name) => name.toLowerCase() === headerName.toLowerCase());
        if (!savedHeader) continue;
        try {
          const secretResult = await services.modules.dispatch('setSecret', { id: normalized.id, headerName: savedHeader, value });
          if (secretResult.status === 'error') importedSecretFailure = true;
        } catch {
          importedSecretFailure = true;
        }
      }
      setRequestInputs((current) => ({ ...current, [normalized.id]: formatJsonInput(normalized.defaultInput) }));
      setPendingImportedSecrets({});
      setEditing(null);
      setNotice(importedSecretFailure
        ? 'Profile saved, but an imported credential could not be stored. Enter it in the profile credential fields.'
        : Object.keys(pendingImportedSecrets).length > 0
          ? `${result.message} Imported credentials were saved securely; no request was sent.`
          : result.message);
      await refresh();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save this API profile.');
    }
  };

  const runProfile = async (profile: ApiProfileEntry) => {
    if (runController.current) {
      runController.current.abort();
      return;
    }
    let parsedInput: unknown;
    try {
      const requestText = requestInputs[profile.id] ?? formatJsonInput(profile.defaultInput);
      parsedInput = requestText.trim() ? JSON.parse(requestText) as unknown : null;
    } catch {
      setNotice('Request input must be valid JSON.');
      return;
    }
    const controller = new AbortController();
    runController.current = controller;
    setRunningId(profile.id);
    setNotice('');
    try {
      const result = await services.modules.dispatch('runProfile', {
        id: profile.id,
        input: parsedInput as AutomationModuleActionRequest['input'],
        useDefaultInput: !(requestInputs[profile.id] ?? formatJsonInput(profile.defaultInput)).trim(),
      }, { signal: controller.signal });
      const nextOutput = readRunOutput(result.data);
      setOutput(nextOutput);
      setNotice(result.message);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'The API profile could not be run.');
    } finally {
      if (runController.current === controller) runController.current = null;
      setRunningId(null);
    }
  };

  const deleteProfile = async (profile: ApiProfileEntry) => {
    try {
      const result = await services.modules.dispatch('deleteProfile', { id: profile.id });
      if (result.status === 'error') throw new Error(result.message);
      setNotice(result.message);
      if (output?.profileId === profile.id) setOutput(null);
      await refresh();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not remove this API profile.');
    }
  };

  const saveSecret = async (profile: ApiProfileEntry, headerName: string) => {
    const key = `${profile.id}:${headerName}`;
    const value = secretDrafts[key] ?? '';
    if (!value) {
      setNotice(`Enter a value for ${headerName} first.`);
      return;
    }
    setBusySecret(key);
    try {
      const result = await services.modules.dispatch('setSecret', { id: profile.id, headerName, value });
      if (result.status === 'error') throw new Error(result.message);
      setSecretDrafts((current) => ({ ...current, [key]: '' }));
      setNotice(result.message);
      await refresh();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not store this API secret.');
    } finally {
      setBusySecret(null);
    }
  };

  const clearSecret = async (profile: ApiProfileEntry, headerName: string) => {
    const key = `${profile.id}:${headerName}`;
    setBusySecret(key);
    try {
      const result = await services.modules.dispatch('clearSecret', { id: profile.id, headerName });
      if (result.status === 'error') throw new Error(result.message);
      setNotice(result.message);
      setSecretDrafts((current) => ({ ...current, [key]: '' }));
      await refresh();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not clear this API secret.');
    } finally {
      setBusySecret(null);
    }
  };

  const addSecretHeader = () => {
    if (!editing) return;
    const headerName = newSecretHeader.trim();
    if (!headerName) return;
    const exists = Object.keys(editing.secretHeaders).some((name) => name.toLowerCase() === headerName.toLowerCase())
      || Object.keys(editing.headers).some((name) => name.toLowerCase() === headerName.toLowerCase());
    if (exists) {
      setValidationIssues([`Header “${headerName}” already exists.`]);
      return;
    }
    setEditing({ ...editing, secretHeaders: { ...editing.secretHeaders, [headerName]: newSecretId() } });
    setNewSecretHeader('');
  };

  const importCurlDraft = () => {
    try {
      const imported = parseApiCurlImport(curlText);
      const secretHeaders = Object.fromEntries(imported.secretHeaders.map((name) => [name, newSecretId()]));
      const draft = blankProfile();
      setEditing({
        ...draft,
        name: new URL(imported.url).pathname.split('/').filter(Boolean).at(-1) || 'Imported API request',
        method: imported.method,
        url: imported.url,
        headers: imported.headers,
        headerText: JSON.stringify(imported.headers, null, 2),
        secretHeaders,
        bodyTemplate: imported.bodyTemplate,
        defaultInputText: formatJsonInput(imported.defaultInput),
      });
      setPendingImportedSecrets(imported.secretValues);
      setCurlText('');
      setOutput(null);
      setValidationIssues([]);
      setNotice('cURL parsed into an editable draft. Saving stores credential headers securely; the request has not been sent.');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'The cURL command could not be imported.');
    }
  };

  const startNewProfile = () => {
    setOutput(null);
    setValidationIssues([]);
    setPendingImportedSecrets({});
    setEditing(blankProfile());
  };

  const commandActions = [
    { id: 'api:new-profile', label: 'New API profile', keywords: ['create request'], run: startNewProfile },
    { id: 'api:paste-curl', label: 'Paste cURL request', keywords: ['chrome import copy as curl'], run: () => {
      const section = document.querySelector<HTMLDetailsElement>('.api-curl-import');
      if (section) section.open = true;
      document.getElementById('api-curl-text')?.focus();
    } },
    ...profiles.flatMap((profile) => [
      { id: `api:run:${profile.id}`, label: `Run ${profile.name}`, keywords: ['send request', profile.method], confirmationPrompt: 'This sends an HTTP request using the saved profile.', run: () => runProfile(profile) },
      { id: `api:input:${profile.id}`, label: `Edit input for ${profile.name}`, keywords: ['json body variables'], run: () => document.getElementById(`api-run-input-${profile.id}`)?.focus() },
      { id: `api:edit:${profile.id}`, label: `Edit ${profile.name}`, keywords: ['change request'], run: () => { setOutput(null); setEditing(profileDraft(profile)); } },
      { id: `api:delete:${profile.id}`, label: `Delete ${profile.name}`, keywords: ['remove profile'], confirmationPrompt: 'This permanently deletes the saved request and its stored credentials.', run: () => deleteProfile(profile) },
    ]),
  ];
  useRegisterTabCommands('api', commandActions);

  const presentation = output ? responsePresentation(output) : null;

  return (
    <>
      <style>{API_VIEW_STYLES}</style>
      <section className="module-view api-view" aria-label="API profiles">
      <header className="api-heading script-heading">
        <div className="script-heading-title">
          <span className="script-module-icon"><Globe2 size={16} /></span>
          <div><h1>API</h1><p>Saved requests; each request is limited to its URL host</p></div>
        </div>
        <button className="primary-button" type="button" onClick={startNewProfile}><Plus size={14} /> New profile</button>
      </header>

      <details className="api-curl-import">
        <summary>Import Chrome “Copy as cURL”</summary>
        <div className="api-curl-import-body">
          <textarea id="api-curl-text" rows={4} spellCheck={false} value={curlText} onChange={(event) => setCurlText(event.target.value)} placeholder={'Paste curl \'https://…\' -H \'accept: …\' --data-raw …'} aria-label="cURL command to import" />
          <div className="api-curl-import-actions"><small>Parsed as an unsent draft. Credential headers go to Windows Credential Manager when saved.</small><button className="secondary-button" type="button" disabled={!curlText.trim()} onClick={importCurlDraft}>Create draft</button></div>
        </div>
      </details>

      {notice && <p className="api-notice script-notice" role="status">{notice}</p>}

      {editing && (
        <form className="api-editor script-editor" onSubmit={(event) => void saveProfile(event)}>
          <div className="script-editor-heading"><Globe2 size={15} /><strong>{profiles.some((profile) => profile.id === editing.id) ? 'Edit API profile' : 'New API profile'}</strong>
            <button className="icon-button" type="button" aria-label="Close editor" onClick={() => { setEditing(null); setPendingImportedSecrets({}); }}><X size={14} /></button></div>
          <div className="api-fields script-fields">
            <label><span>Name</span><input required maxLength={128} value={editing.name} placeholder="Get project status" onChange={(event) => setEditing({ ...editing, name: event.target.value })} /></label>
            <label><span>Profile key</span><input required pattern="[a-z0-9][a-z0-9._-]{0,63}" value={editing.id} onChange={(event) => setEditing({ ...editing, id: event.target.value.toLowerCase() })} /></label>
            <label><span>Method</span><select value={editing.method} onChange={(event) => setEditing({ ...editing, method: event.target.value })}>{['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'].map((method) => <option key={method}>{method}</option>)}</select></label>
            <label className="api-field-wide script-field-wide"><span>URL <small>This URL grants access to its host; redirects to other hosts are blocked</small></span><input required type="url" value={editing.url} placeholder="https://api.example.com/v1/status" onChange={(event) => setEditing({ ...editing, url: event.target.value })} /></label>
            <label><span>Response</span><select value={editing.responseMode} onChange={(event) => setEditing({ ...editing, responseMode: event.target.value as ApiResponseMode })}><option value="json">JSON</option><option value="text">Text</option></select></label>
            <label><span>Timeout <small>seconds</small></span><input type="number" min={1} max={3600} value={editing.timeoutSeconds} onChange={(event) => setEditing({ ...editing, timeoutSeconds: Number(event.target.value) })} /></label>
            <label className="api-field-wide script-field-wide"><span>Ordinary headers <small>JSON object; keep credentials in Secret headers</small></span><textarea rows={3} spellCheck={false} value={editing.headerText} onChange={(event) => setEditing({ ...editing, headerText: event.target.value })} /></label>
            <label className="api-field-wide script-field-wide"><span>Body template <small>Use {'{{input}}'} for raw JSON input</small></span><textarea rows={3} spellCheck={false} value={editing.bodyTemplate ?? ''} placeholder={'{"query": {{input}}}'} onChange={(event) => setEditing({ ...editing, bodyTemplate: event.target.value || null })} /></label>
            <div className="api-field-wide script-field-wide"><GlobalVariableHints onInsert={(reference) => setEditing({ ...editing, bodyTemplate: (editing.bodyTemplate ?? '') + reference })} /></div>
            <label className="api-field-wide script-field-wide"><span>Default JSON input <small>Used when this profile runs without a one-off value</small></span><textarea className="api-default-input" rows={3} spellCheck={false} value={editing.defaultInputText} placeholder={'{"query":"status"}'} onChange={(event) => setEditing({ ...editing, defaultInputText: event.target.value })} /></label>
            <label className="api-local-network"><input type="checkbox" checked={editing.allowLocalNetwork} onChange={(event) => setEditing({ ...editing, allowLocalNetwork: event.target.checked })} /><span>Allow private/local network addresses</span></label>
            <div className="api-field-wide script-field-wide api-secret-editor">
              <div className="api-section-title"><span>Secret headers</span></div>
              <div className="api-add-secret"><input aria-label="Secret header name" value={newSecretHeader} placeholder="X-Api-Key" onChange={(event) => setNewSecretHeader(event.target.value)} /><button className="secondary-button" type="button" disabled={!newSecretHeader.trim()} onClick={addSecretHeader}><Plus size={12} /> Add secret header</button></div>
              {Object.keys(editing.secretHeaders).length ? Object.keys(editing.secretHeaders).map((headerName) => (
                  <div className="api-secret-draft-row" key={headerName}><KeyRound size={13} /><strong>{headerName}</strong><span>{Object.keys(pendingImportedSecrets).some((name) => name.toLowerCase() === headerName.toLowerCase()) ? 'Imported credential; saved on submit' : 'Saved profile binding'}</span>
                  <button className="icon-button" type="button" aria-label={`Remove ${headerName}`} onClick={() => {
                    const secretHeaders = { ...editing.secretHeaders };
                    delete secretHeaders[headerName];
                    setEditing({ ...editing, secretHeaders });
                  }}><Trash2 size={13} /></button></div>
              )) : <p className="api-help">No secret headers configured. Secret values are stored in Windows Credential Manager.</p>}
              <p className="api-help">Imported values are saved only when you submit this profile. Other secret values are set from the profile row. Values are write-only.</p>
            </div>
          </div>
          {validationIssues.length > 0 && <ul className="api-validation" role="alert">{validationIssues.map((issue) => <li key={issue}>{issue}</li>)}</ul>}
          <div className="api-editor-actions"><button className="secondary-button" type="button" onClick={() => { setEditing(null); setPendingImportedSecrets({}); }}>Cancel</button><button className="primary-button" type="submit"><Save size={14} /> Save profile</button></div>
        </form>
      )}

      <div className="api-profile-list script-profile-list" aria-live="polite">
        {profiles.length ? profiles.map((profile) => (
          <article className="api-profile-card" key={profile.id}>
            <div className="api-profile-row script-profile-row">
              <span className="api-profile-icon script-profile-icon"><Globe2 size={16} /></span>
              <div className="api-profile-copy script-profile-copy"><strong>{profile.name}</strong><span><b>{profile.method}</b> · {profile.url}</span><small>Host scope is derived from this URL</small></div>
              <span className="api-profile-time"><Clock3 size={12} /> {profile.timeoutSeconds}s</span>
              <button className="icon-button script-row-action" type="button" aria-label={runningId === profile.id ? `Cancel ${profile.name}` : `Run ${profile.name}`} title={runningId === profile.id ? 'Cancel request' : 'Run profile'} disabled={runningId !== null && runningId !== profile.id} onClick={() => void runProfile(profile)}>{runningId === profile.id ? <Square size={14} /> : <Play size={15} />}</button>
              <button className="icon-button script-row-action" type="button" aria-label={`Edit ${profile.name}`} title="Edit" disabled={runningId !== null} onClick={() => { setOutput(null); setPendingImportedSecrets({}); setEditing(profileDraft(profile)); }}><Pencil size={14} /></button>
              <button className="icon-button script-row-action" type="button" aria-label={`Remove ${profile.name}`} title="Remove" disabled={runningId !== null} onClick={() => void deleteProfile(profile)}><Trash2 size={14} /></button>
            </div>
            <div className="api-run-input api-profile-input">
              <label htmlFor={`api-run-input-${profile.id}`}><span><Braces size={13} /> Request JSON input</span><small>Defaults to this profile’s saved input</small></label>
              <textarea id={`api-run-input-${profile.id}`} rows={2} spellCheck={false} value={requestInputs[profile.id] ?? formatJsonInput(profile.defaultInput)} onChange={(event) => setRequestInputs((current) => ({ ...current, [profile.id]: event.target.value }))} />
            </div>
            {Object.keys(profile.secretHeaders).length > 0 && <div className="api-secret-list">
              {Object.keys(profile.secretHeaders).map((headerName) => {
                const key = `${profile.id}:${headerName}`;
                const configured = profile.secretStatus[headerName] === true;
                return <div className="api-secret-row" key={headerName}>
                  <span className="api-secret-state">{configured ? <ShieldCheck size={13} /> : <KeyRound size={13} />}</span>
                  <strong>{headerName}</strong>
                  <span className={configured ? 'api-secret-configured' : 'api-secret-missing'}>{configured ? 'Configured' : 'Needs a value'}</span>
                  <input type="password" autoComplete="new-password" aria-label={`Secret value for ${headerName}`} placeholder={configured ? 'Replace stored value' : 'Enter secret value'} value={secretDrafts[key] ?? ''} onChange={(event) => setSecretDrafts((current) => ({ ...current, [key]: event.target.value }))} />
                  <button className="secondary-button" type="button" disabled={busySecret === key || !secretDrafts[key]} onClick={() => void saveSecret(profile, headerName)}>{busySecret === key ? 'Saving…' : configured ? 'Replace' : 'Save secret'}</button>
                  {configured && <button className="icon-button" type="button" aria-label={`Clear ${headerName}`} title="Clear secret" disabled={busySecret === key} onClick={() => void clearSecret(profile, headerName)}><X size={13} /></button>}
                </div>;
              })}
            </div>}
          </article>
        )) : !editing ? <div className="script-empty"><span className="empty-icon"><Globe2 size={20} /></span><strong>No API profiles yet</strong><p>Save a request URL. Automator scopes it to that URL’s host; credential values are stored separately in Windows Credential Manager.</p><button className="secondary-button" type="button" onClick={startNewProfile}><Plus size={13} /> Create API profile</button></div> : null}
      </div>

      {output && <section className="api-output script-output" aria-label="API response">
        <header><div><Check size={14} /><strong>HTTP {output.statusCode}</strong><span>{output.durationMilliseconds} ms</span><small>{output.contentType}</small></div><button className="icon-button" type="button" aria-label="Clear API response" onClick={() => setOutput(null)}><X size={14} /></button></header>
        {output.bodyTruncated && <p className="api-output-status">Response was shortened to fit the display limit.</p>}
        {output.parseFailure && <p className="api-output-status">Response was not valid JSON; showing it as text.</p>}
        {Object.keys(output.headers).length > 0 && <details className="api-response-headers"><summary>Response headers</summary><dl>{Object.entries(output.headers).map(([name, value]) => <div key={name}><dt>{name}</dt><dd>{value}</dd></div>)}</dl></details>}
        {presentation?.hasStructuredOutput
          ? <pre>{JSON.stringify(presentation.structuredOutput, null, 2)}</pre>
          : output.bodyText ? <pre>{output.bodyText}</pre> : <p className="script-no-output">The response body was empty.</p>}
      </section>}
      </section>
    </>
  );
}
