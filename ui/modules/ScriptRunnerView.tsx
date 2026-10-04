import { GlobalVariableHints } from '../variables/GlobalVariables';
import {
  Braces, Check, Clock3, FileCode2, FolderOpen, Pencil, Play, Plus, Save, Square, Terminal, Trash2, X,
} from 'lucide-react';
import { useCallback, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import type { AutomationServices } from '../automationServices';
import type { BackendUiState, ModuleSettingsUpdateRequest } from '../../contracts/rpc';
import type { ScriptRunnerInterpreter } from '../../contracts/scriptRunner';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';

type ScriptProfile = {
  id: string;
  name: string;
  interpreter: ScriptRunnerInterpreter;
  interpreterPath: string;
  scriptPath: string;
  arguments: string[];
  workingDirectory: string;
  outputMode: 'text' | 'json';
  timeoutSeconds: number;
};

type ViewProps = {
  tab: BackendUiState['tabs'][number];
  moduleState: BackendUiState['moduleStates'][number] | undefined;
  services: AutomationServices;
};

type RunOutput = {
  profileId: string;
  exitCode: number | null;
  timedOut: boolean;
  stdout: string;
  stderr: string;
  stdoutTruncated: boolean;
  stderrTruncated: boolean;
  durationMilliseconds: number;
  structuredOutput: unknown;
  parseFailure: boolean;
};

type InterpreterDefaults = Record<ScriptRunnerInterpreter, string>;

type ModuleSettingsValue = Record<string, unknown> & { interpreterDefaults?: Record<string, unknown> };

const emptyInterpreterDefaults = (): InterpreterDefaults => ({ python: '', bash: '', powershell: '' });

const blankProfile = (interpreterDefaults: InterpreterDefaults): ScriptProfile => ({
  id: `script-${Date.now().toString(36)}`,
  name: '',
  interpreter: 'python',
  interpreterPath: interpreterDefaults.python,
  scriptPath: '',
  arguments: [],
  workingDirectory: '',
  outputMode: 'text',
  timeoutSeconds: 60,
});

function readProfiles(data: unknown): ScriptProfile[] {
  if (!data || typeof data !== 'object' || !('profiles' in data) || !Array.isArray(data.profiles)) return [];
  return data.profiles.filter((profile): profile is ScriptProfile => Boolean(profile && typeof profile === 'object'
    && 'id' in profile && typeof profile.id === 'string'
    && 'name' in profile && typeof profile.name === 'string'
    && 'interpreter' in profile && (profile.interpreter === 'python' || profile.interpreter === 'bash' || profile.interpreter === 'powershell')
    && 'interpreterPath' in profile && typeof profile.interpreterPath === 'string'
    && 'scriptPath' in profile && typeof profile.scriptPath === 'string'
    && 'workingDirectory' in profile && typeof profile.workingDirectory === 'string'
    && 'arguments' in profile && Array.isArray(profile.arguments)
    && 'outputMode' in profile && (profile.outputMode === 'text' || profile.outputMode === 'json')
    && 'timeoutSeconds' in profile && typeof profile.timeoutSeconds === 'number'));
}

function readSettingsValue(value: unknown): ModuleSettingsValue {
  if (!value || typeof value !== 'object' || Array.isArray(value)) return {};
  return value as ModuleSettingsValue;
}

function readInterpreterDefaults(value: unknown): InterpreterDefaults {
  const defaults = value && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : {};
  return {
    python: typeof defaults.python === 'string' ? defaults.python : '',
    bash: typeof defaults.bash === 'string' ? defaults.bash : '',
    powershell: typeof defaults.powershell === 'string' ? defaults.powershell : '',
  };
}

export function ScriptRunnerView({ tab, services }: ViewProps) {
  const [profiles, setProfiles] = useState<ScriptProfile[]>([]);
  const [interpreterDefaults, setInterpreterDefaults] = useState<InterpreterDefaults>(emptyInterpreterDefaults);
  const [settingsReady, setSettingsReady] = useState(false);
  const [editing, setEditing] = useState<ScriptProfile | null>(null);
  const [output, setOutput] = useState<RunOutput | null>(null);
  const [runningId, setRunningId] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const runController = useRef<AbortController | null>(null);
  const moduleSettingsRef = useRef<ModuleSettingsValue>({});

  const loadModuleSettings = useCallback(async (savedProfiles: ScriptProfile[]) => {
    const bridge = window.automator;
    if (!bridge) {
      setSettingsReady(true);
      return;
    }
    const request = { contractVersion: tab.contractVersion, moduleId: tab.id, settingsVersion: tab.settingsVersion };
    const response = await bridge.getModuleSettings(request);
    const savedSettings = readSettingsValue(response.value);
    const rawDefaults = readSettingsValue(savedSettings.interpreterDefaults);
    const nextDefaults = readInterpreterDefaults(rawDefaults);
    let backfilled = false;
    // The library orders records by most recently saved. Adopt the newest legacy profile path once,
    // so profiles saved before interpreter defaults were added remain useful as defaults.
    for (const profile of savedProfiles) {
      if (!nextDefaults[profile.interpreter] && profile.interpreterPath.trim()) {
        nextDefaults[profile.interpreter] = profile.interpreterPath;
        backfilled = true;
      }
    }
    const nextSettings: ModuleSettingsValue = {
      ...savedSettings,
      interpreterDefaults: { ...rawDefaults, ...nextDefaults },
    };
    moduleSettingsRef.current = nextSettings;
    setInterpreterDefaults(nextDefaults);
    if (backfilled) await bridge.updateModuleSettings({ ...request, value: nextSettings as ModuleSettingsUpdateRequest['value'] });
    setSettingsReady(true);
  }, [tab.contractVersion, tab.id, tab.settingsVersion]);

  const refresh = useCallback(async () => {
    try {
      const result = await services.modules.dispatch('listProfiles', {});
      if (result.status === 'error') throw new Error(result.message);
      const savedProfiles = readProfiles(result.data);
      setProfiles(savedProfiles);
      await loadModuleSettings(savedProfiles);
      setNotice('');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not read saved profiles.');
      setSettingsReady(true);
    }
  }, [loadModuleSettings, services]);

  useEffect(() => {
    void refresh();
    return () => runController.current?.abort();
  }, [refresh]);

  const saveProfile = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!editing) return;
    const normalized: ScriptProfile = {
      ...editing,
      id: editing.id.trim().toLowerCase(),
      name: editing.name.trim(),
      arguments: editing.arguments.filter((argument) => argument.length > 0),
      timeoutSeconds: Math.max(1, Math.min(3600, Math.floor(editing.timeoutSeconds))),
    };
    try {
      const result = await services.modules.dispatch('saveProfile', normalized);
      if (result.status === 'error') throw new Error(result.message);
      let settingsWarning = '';
      try {
        const bridge = window.automator;
        if (!bridge) throw new Error('The settings bridge is unavailable.');
        const request = { contractVersion: tab.contractVersion, moduleId: tab.id, settingsVersion: tab.settingsVersion };
        const current = moduleSettingsRef.current;
        const rawDefaults = readSettingsValue(current.interpreterDefaults);
        const nextDefaults = { ...rawDefaults, [normalized.interpreter]: normalized.interpreterPath };
        const nextSettings: ModuleSettingsValue = { ...current, interpreterDefaults: nextDefaults };
        await bridge.updateModuleSettings({ ...request, value: nextSettings as ModuleSettingsUpdateRequest['value'] });
        moduleSettingsRef.current = nextSettings;
        setInterpreterDefaults(readInterpreterDefaults(nextDefaults));
      } catch (error) {
        const detail = error instanceof Error ? error.message : 'Unknown settings error.';
        settingsWarning = ` The profile was saved, but its interpreter default could not be remembered: ${detail}`;
      }
      setEditing(null);
      await refresh();
      setNotice(settingsWarning ? `${result.message}${settingsWarning}` : result.message);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save this profile.');
    }
  };

  const runProfile = async (profile: ScriptProfile) => {
    if (runController.current) {
      runController.current.abort();
      return;
    }
    const controller = new AbortController();
    runController.current = controller;
    setRunningId(profile.id);
    setNotice('');
    try {
      const result = await services.modules.dispatch('runProfile', { id: profile.id }, { signal: controller.signal });
      if (result.data && typeof result.data === 'object' && 'profileId' in result.data) setOutput(result.data as RunOutput);
      setNotice(result.message);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'The script could not be run.');
    } finally {
      if (runController.current === controller) runController.current = null;
      setRunningId(null);
    }
  };

  const deleteProfile = async (profile: ScriptProfile) => {
    try {
      const result = await services.modules.dispatch('deleteProfile', { id: profile.id });
      setNotice(result.message);
      if (output?.profileId === profile.id) setOutput(null);
      await refresh();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not remove this profile.');
    }
  };

  const commands = useMemo(() => [
    { id: 'new-script-profile', label: 'Create script profile', keywords: ['new', 'python', 'bash', 'powershell'], disabled: !settingsReady || runningId !== null, run: () => { setOutput(null); setEditing(blankProfile(interpreterDefaults)); } },
    ...(runningId ? [{ id: 'cancel-script-run', label: 'Cancel running script', keywords: ['stop', 'terminate'], run: () => runController.current?.abort() }] : []),
    ...profiles.flatMap((profile) => [
      { id: `run-script:${profile.id}`, label: `Run ${profile.name}`, keywords: [profile.id, profile.interpreter], disabled: runningId !== null, confirmationPrompt: `Run script profile “${profile.name}”?`, run: () => runProfile(profile) },
      { id: `edit-script:${profile.id}`, label: `Edit ${profile.name}`, keywords: [profile.id, 'profile'], disabled: runningId !== null, run: () => { setOutput(null); setEditing({ ...profile, arguments: [...profile.arguments] }); } },
      { id: `delete-script:${profile.id}`, label: `Delete ${profile.name}`, keywords: [profile.id, 'remove'], disabled: runningId !== null, confirmationPrompt: `Delete saved script profile “${profile.name}”?`, run: () => deleteProfile(profile) },
    ]),
  ], [deleteProfile, interpreterDefaults, profiles, runProfile, runningId, settingsReady]);
  useRegisterTabCommands(tab.id, commands);

  const browseForScript = async () => {
    if (!editing) return;
    try {
      const path = await services.files.pickScriptFile(editing.interpreter);
      if (path) setEditing((current) => current ? {
        ...current,
        scriptPath: path,
        workingDirectory: current.workingDirectory || path.slice(0, Math.max(path.lastIndexOf('\\'), path.lastIndexOf('/'))),
      } : current);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not open the script picker.');
    }
  };

  const browseForWorkingDirectory = async () => {
    try {
      const path = await services.files.pickWorkingDirectory();
      if (path) setEditing((current) => current ? { ...current, workingDirectory: path } : current);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not open the folder picker.');
    }
  };

  return (
    <section className="module-view script-runner-view" aria-label="Script Runner">
      <div className="script-heading">
        <div className="script-heading-title">
          <span className="script-module-icon"><Terminal size={16} /></span>
          <div><h1>Script Runner</h1><p>Saved Python, Bash, and PowerShell tasks</p></div>
        </div>
        <button className="primary-button" type="button" disabled={!settingsReady} onClick={() => { setOutput(null); setEditing(blankProfile(interpreterDefaults)); }}>
          <Plus size={14} /> New profile
        </button>
      </div>

      {notice && <p className="script-notice" role="status">{notice}</p>}

      {editing && (
        <form className="script-editor" onSubmit={(event) => void saveProfile(event)}>
          <div className="script-editor-heading"><FileCode2 size={16} /><strong>{profiles.some((profile) => profile.id === editing.id) ? 'Edit profile' : 'New profile'}</strong>
            <button className="icon-button" type="button" aria-label="Close editor" onClick={() => setEditing(null)}><X size={15} /></button></div>
          <div className="script-fields">
            <label><span>Name</span><input required maxLength={128} value={editing.name} placeholder="Daily report" onChange={(event) => setEditing({ ...editing, name: event.target.value })} /></label>
            <label><span>Profile key</span><input required pattern="[a-z0-9][a-z0-9._-]{0,63}" value={editing.id} onChange={(event) => setEditing({ ...editing, id: event.target.value.toLowerCase() })} /></label>
            <label><span>Interpreter</span><select value={editing.interpreter} onChange={(event) => {
              const interpreter = event.target.value as ScriptRunnerInterpreter;
              setEditing((current) => current ? {
                ...current,
                interpreter,
                interpreterPath: interpreterDefaults[interpreter],
              } : current);
            }}><option value="python">Python</option><option value="bash">Bash</option><option value="powershell">PowerShell</option></select></label>
            <label className="script-field-wide"><span>Interpreter executable</span><input required value={editing.interpreterPath} placeholder={editing.interpreter === 'python' ? 'C:\\Python\\python.exe' : editing.interpreter === 'bash' ? 'C:\\Program Files\\Git\\bin\\bash.exe' : 'C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe'} onChange={(event) => setEditing({ ...editing, interpreterPath: event.target.value })} /></label>
            <label className="script-field-wide"><span>Script file</span><span className="script-path-row"><input required value={editing.scriptPath} placeholder={editing.interpreter === 'python' ? 'C:\\Scripts\\report.py' : editing.interpreter === 'bash' ? 'C:\\Scripts\\report.sh' : 'C:\\Scripts\\report.ps1'} onChange={(event) => setEditing({ ...editing, scriptPath: event.target.value })} /><button className="secondary-button" type="button" onClick={() => void browseForScript()}><FolderOpen size={13} /> Browse</button></span></label>
            <label className="script-field-wide"><span>Arguments <small>one argument per line</small></span><textarea rows={3} value={editing.arguments.join('\n')} placeholder={'--daily\nvalue with spaces'} onChange={(event) => setEditing({ ...editing, arguments: event.target.value.split(/\r?\n/) })} /></label>
            <div className="script-field-wide"><GlobalVariableHints onInsert={(reference) => setEditing({ ...editing, arguments: [...editing.arguments, reference] })} /></div>
            <label className="script-field-wide"><span>Working directory</span><span className="script-path-row"><input required value={editing.workingDirectory} placeholder="C:\\Scripts" onChange={(event) => setEditing({ ...editing, workingDirectory: event.target.value })} /><button className="secondary-button" type="button" onClick={() => void browseForWorkingDirectory()}><FolderOpen size={13} /> Browse</button></span></label>
            <label><span>Output</span><select value={editing.outputMode} onChange={(event) => setEditing({ ...editing, outputMode: event.target.value as ScriptProfile['outputMode'] })}><option value="text">Plain text</option><option value="json">JSON</option></select></label>
            <label><span>Timeout <small>seconds</small></span><input required type="number" min={1} max={3600} value={editing.timeoutSeconds} onChange={(event) => setEditing({ ...editing, timeoutSeconds: Number(event.target.value) })} /></label>
          </div>
          <p className="script-trust-note">Scripts run as your Windows user with the same access as Automator. They are not sandboxed. Saving a profile remembers its interpreter path as the default for new profiles; existing profiles keep their saved path.</p>
          <div className="script-editor-actions"><button className="secondary-button" type="button" onClick={() => setEditing(null)}>Cancel</button><button className="primary-button" type="submit"><Save size={14} /> Save profile</button></div>
        </form>
      )}

      <div className="script-profile-list" aria-live="polite">
        {profiles.length ? profiles.map((profile) => (
          <article className="script-profile-row" key={profile.id}>
            <span className="script-profile-icon">{profile.interpreter === 'python' ? <Braces size={16} /> : <Terminal size={16} />}</span>
            <div className="script-profile-copy"><strong>{profile.name}</strong><span>{profile.interpreter === 'python' ? 'Python' : profile.interpreter === 'bash' ? 'Bash' : 'PowerShell'} · {profile.scriptPath || 'Script path missing'}</span></div>
            <span className="script-profile-time"><Clock3 size={12} /> {profile.timeoutSeconds}s</span>
            <button className="icon-button script-row-action" type="button" aria-label={runningId === profile.id ? `Cancel ${profile.name}` : `Run ${profile.name}`} title={runningId === profile.id ? 'Cancel run' : 'Run profile'} disabled={runningId !== null && runningId !== profile.id} onClick={() => void runProfile(profile)}>
              {runningId === profile.id ? <Square size={14} /> : <Play size={15} />}
            </button>
            <button className="icon-button script-row-action" type="button" aria-label={`Edit ${profile.name}`} title="Edit" disabled={runningId !== null} onClick={() => { setOutput(null); setEditing({ ...profile, arguments: [...profile.arguments] }); }}><Pencil size={14} /></button>
            <button className="icon-button script-row-action" type="button" aria-label={`Remove ${profile.name}`} title="Remove" disabled={runningId !== null} onClick={() => void deleteProfile(profile)}><Trash2 size={14} /></button>
          </article>
        )) : !editing ? <div className="script-empty"><span className="empty-icon"><Terminal size={20} /></span><strong>No script profiles yet</strong><p>Add a saved Python, Bash, or PowerShell command. Each argument is passed as its own value.</p><button className="secondary-button" type="button" disabled={!settingsReady} onClick={() => setEditing(blankProfile(interpreterDefaults))}><Plus size={13} /> Create a profile</button></div> : null}
      </div>

      {output && <section className="script-output" aria-label="Script output">
        <header><div><Check size={14} /><strong>Latest result</strong><span>{output.durationMilliseconds} ms</span></div><button className="icon-button" type="button" aria-label="Clear script output" onClick={() => setOutput(null)}><X size={14} /></button></header>
        {output.timedOut && <p className="script-output-status">The profile timed out.</p>}
        {output.exitCode !== null && output.exitCode !== 0 && <p className="script-output-status">Exit code {output.exitCode}</p>}
        {output.parseFailure && <p className="script-output-status">The output was not valid JSON.</p>}
        {output.structuredOutput !== null && output.structuredOutput !== undefined && <pre>{JSON.stringify(output.structuredOutput, null, 2)}</pre>}
        {output.stdout && <pre>{output.stdout}{output.stdoutTruncated ? '\n… output truncated' : ''}</pre>}
        {output.stderr && <pre className="script-stderr">{output.stderr}{output.stderrTruncated ? '\n… output truncated' : ''}</pre>}
        {!output.stdout && !output.stderr && output.structuredOutput == null && <p className="script-no-output">The script did not produce output.</p>}
      </section>}
    </section>
  );
}
