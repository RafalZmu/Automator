import { GlobalVariableHints } from '../variables/GlobalVariables';
import {
  Braces, Check, Clock3, FileCode2, Loader2, Pencil, Play, Plus, Save, Square, Terminal, Trash2, X,
} from 'lucide-react';
import { useCallback, useContext, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import type { AutomationServices } from '../automationServices';
import type { AutomationModuleActionRequest, BackendUiState, ModuleSettingsUpdateRequest } from '../../contracts/rpc';
import { FILE_EXPLORER_REGISTER_SCRIPT_ACTION_ID } from '../../contracts/fileExplorer';
import type { ScriptRunnerInterpreter, ScriptTemplateDescriptor, ScriptTemplateValue } from '../../contracts/scriptRunner';
import { parseScriptTemplateCatalog, validateScriptParameterValues } from '../../contracts/scriptRunner';
import {
  clearSensitiveValues, createPowerShellProfileDraft, filterScriptTemplates, initialParameterValues, normalizeParameterPositions,
  normalizeParametersForSave, profileParameters, rememberableValues, resetTemplateValues, templateDetailParameters, updateScriptPath, type ScriptProfile,
} from './scriptTemplateViewModel';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import { InlineConfirmButton } from '../components/InlineConfirmButton';
import { PathField } from '../components/PathField';
import { ExplorerActionsPanel } from './ExplorerActionsPanel';
import { FileExplorerLaunchContext } from '../FileExplorerLaunchContext';
import { ScriptParameterBuilder } from './ScriptParameterBuilder';
import { ScriptParameterFields } from './ScriptParameterFields';
import {
  activeScriptRun, cancelScriptRun, clearFinishedScriptRuns, describeRunError, describeRunState, startScriptRun, useScriptRuns,
} from './scriptRunStore';

type ViewProps = {
  tab: BackendUiState['tabs'][number];
  moduleState: BackendUiState['moduleStates'][number] | undefined;
  services: AutomationServices;
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
  aiAccess: false,
  parameters: [],
});

const interpreterName = (interpreter: ScriptRunnerInterpreter) => interpreter === 'python' ? 'Python' : interpreter === 'bash' ? 'Bash' : 'PowerShell';

/** Module inputs are JSON. Round-tripping drops the optional fields that are left undefined. */
const moduleInput = (value: unknown) => JSON.parse(JSON.stringify(value)) as AutomationModuleActionRequest['input'];

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

const formatRunTime = (timestamp: number) => new Date(timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' });
const formatDuration = (milliseconds: number) => milliseconds < 1000 ? `${milliseconds} ms` : `${(milliseconds / 1000).toFixed(1)} s`;

function ElapsedTime({ since }: { since: number }) {
  const [now, setNow] = useState(Date.now());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);
  return <>{Math.max(0, Math.floor((now - since) / 1000))} s</>;
}

const trustNote = 'Scripts run as your Windows user with the same access as Automator. They are not sandboxed. Review and trust this code before running it.';

export function ScriptRunnerView({ tab, services }: ViewProps) {
  const fileExplorerLaunch = useContext(FileExplorerLaunchContext);
  const [profiles, setProfiles] = useState<ScriptProfile[]>([]);
  const [interpreterDefaults, setInterpreterDefaults] = useState<InterpreterDefaults>(emptyInterpreterDefaults);
  const [settingsReady, setSettingsReady] = useState(false);
  const [editing, setEditing] = useState<ScriptProfile | null>(null);
  // Remembered input values for the profile being edited, and whether its input definitions are expanded.
  const [editValues, setEditValues] = useState<Record<string, ScriptTemplateValue>>({});
  const [definitionsOpen, setDefinitionsOpen] = useState(false);
  // The saved profile whose row hosts the editor. The key field is editable, so the row is matched by the original id.
  const [editingOriginalId, setEditingOriginalId] = useState<string | null>(null);
  // Runs live in a window-level store, so they keep going and stay visible when this view is closed and reopened.
  const runs = useScriptRuns();
  const activeRun = runs.find((run) => run.finishedAt === null);
  const runningId = activeRun?.profileId ?? null;
  // null shows the newest finished run, 'none' hides the result panel, anything else is a chosen history entry.
  const [shownRunId, setShownRunId] = useState<string | null>(null);
  const [notice, setNotice] = useState('');
  const [templates, setTemplates] = useState<ScriptTemplateDescriptor[]>([]);
  const [explorerOpen, setExplorerOpen] = useState(false);
  const [libraryOpen, setLibraryOpen] = useState(false);
  const [templateQuery, setTemplateQuery] = useState('');
  const [libraryDetail, setLibraryDetail] = useState<ScriptTemplateDescriptor | null>(null);
  const [libraryValues, setLibraryValues] = useState<Record<string, ScriptTemplateValue>>({});
  // The one profile whose inputs are open in the Run form.
  const [runTarget, setRunTarget] = useState<ScriptProfile | null>(null);
  const [runValues, setRunValues] = useState<Record<string, ScriptTemplateValue>>({});
  const moduleSettingsRef = useRef<ModuleSettingsValue>({});
  const profilesRef = useRef<ScriptProfile[]>([]);
  profilesRef.current = profiles;

  const closeLibrary = useCallback(() => {
    setLibraryOpen(false);
    setLibraryDetail(null);
    setLibraryValues({});
  }, []);
  const closeRunForm = useCallback(() => {
    setRunTarget(null);
    setRunValues({});
  }, []);
  const beginEditing = useCallback((profile: ScriptProfile) => {
    setEditing(profile);
    setEditingOriginalId(profile.id);
    setEditValues(initialParameterValues(profileParameters(profile, templates), profile.values, profile.templateOrigin?.id));
    setDefinitionsOpen(!profile.templateOrigin && (profile.parameters ?? []).length === 0);
  }, [templates]);
  const openExplorer = useCallback(() => {
    setExplorerOpen(true);
    setEditing(null);
    closeLibrary();
    closeRunForm();
  }, [closeLibrary, closeRunForm]);
  // Explorer runs use the same window-level store as the Run form, so they show the running banner and history.
  const startExplorerRun = useCallback(async (profile: { id: string; name: string }, input: Record<string, unknown>) => {
    const bridge = window.automator;
    const action = tab.actions.find((candidate) => candidate.id === 'runExplorerAction');
    if (!bridge || !action) throw new Error('Running scripts is not available in this window.');
    setShownRunId(null);
    return startScriptRun({ bridge, moduleId: tab.id, contractVersion: tab.contractVersion, action }, profile, undefined, input);
  }, [tab]);

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
      const catalog = await services.modules.dispatch('listTemplates', {});
      if (catalog.status === 'error') throw new Error(catalog.message);
      const rawTemplates = catalog.data && typeof catalog.data === 'object' && 'templates' in catalog.data ? catalog.data.templates : [];
      setTemplates(parseScriptTemplateCatalog(rawTemplates));
      await loadModuleSettings(savedProfiles);
      setNotice('');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not read saved profiles.');
      setSettingsReady(true);
    }
  }, [loadModuleSettings, services]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  useEffect(() => {
    const request = fileExplorerLaunch.request;
    if (!settingsReady || editing || runningId !== null || !request || request.actionId !== FILE_EXPLORER_REGISTER_SCRIPT_ACTION_ID) return;
    try {
      beginEditing(createPowerShellProfileDraft(request.filePath, interpreterDefaults.powershell, profiles.map(profile => profile.id)));
      setShownRunId('none');
      setExplorerOpen(false);
      closeLibrary();
      closeRunForm();
      setNotice('');
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'The selected PowerShell script could not be registered.');
    } finally {
      fileExplorerLaunch.consume();
    }
  }, [fileExplorerLaunch.request, fileExplorerLaunch.consume, settingsReady, editing, runningId, interpreterDefaults.powershell, profiles, closeLibrary, closeRunForm, beginEditing]);

  const saveProfile = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!editing) return;
    const normalized: ScriptProfile = {
      ...editing,
      id: editing.id.trim().toLowerCase(),
      name: editing.name.trim(),
      arguments: editing.arguments.filter((argument) => argument.length > 0),
      timeoutSeconds: Math.max(1, Math.min(3600, Math.floor(editing.timeoutSeconds))),
      // Library profiles take their inputs from the library template, so only user scripts send declarations.
      parameters: editing.templateOrigin ? undefined : normalizeParametersForSave(editing.parameters ?? []),
    };
    // Remember what was typed into the input fields. Passwords and transient values are dropped, and a choice
    // that is no longer offered is forgotten rather than blocking the save.
    const savedParameters = profileParameters(normalized, templates);
    const savedValues = rememberableValues(savedParameters, editValues, normalized.templateOrigin?.id);
    for (const parameter of savedParameters)
      if (parameter.type === 'choice' && !parameter.options.includes(String(savedValues[parameter.key]))) delete savedValues[parameter.key];
    normalized.values = savedValues;
    try {
      const result = await services.modules.dispatch('saveProfile', moduleInput(normalized));
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
      await refresh();
      setEditing(null);
      setNotice(settingsWarning ? `${result.message}${settingsWarning}` : result.message);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not save this profile.');
    }
  };

  const openEditor = (profile: ScriptProfile) => {
    if (editingRowId === profile.id) {
      setEditing(null);
      return;
    }
    closeRunForm();
    setShownRunId('none');
    beginEditing({ ...profile, arguments: [...profile.arguments], parameters: profile.parameters ? [...profile.parameters] : profile.parameters });
  };

  const openNewEditor = () => {
    setShownRunId('none');
    beginEditing(blankProfile(interpreterDefaults));
  };

  const openRunForm = (profile: ScriptProfile) => {
    setEditing(null);
    setRunTarget(profile);
    setRunValues(initialParameterValues(profileParameters(profile, templates), profile.values, profile.templateOrigin?.id));
  };

  const cancelActiveRun = () => {
    const active = activeScriptRun();
    if (active) void cancelScriptRun(active.id);
  };

  const dispatchRun = async (profile: ScriptProfile, values: Record<string, ScriptTemplateValue> | undefined) => {
    if (activeScriptRun()) {
      cancelActiveRun();
      return;
    }
    const bridge = window.automator;
    const action = tab.actions.find((candidate) => candidate.id === 'runProfile');
    if (!bridge || !action) {
      setNotice('Running scripts is not available in this window.');
      return;
    }
    setNotice('');
    setShownRunId(null);
    try {
      const record = await startScriptRun({ bridge, moduleId: tab.id, contractVersion: tab.contractVersion, action }, profile, values);
      setNotice(record.message);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'The script could not be run.');
    } finally {
      setRunValues((current) => clearSensitiveValues(profileParameters(profile, templates), current));
    }
  };

  const runProfile = async (profile: ScriptProfile) => {
    if (profile.templateOrigin && !templates.some((item) => item.id === profile.templateOrigin?.id && item.version === profile.templateOrigin.version)) {
      setNotice('This template version is unavailable. Remove and reinstall its profile.');
      return;
    }
    if (profileParameters(profile, templates).length > 0) {
      setNotice('');
      if (runTarget?.id === profile.id && runningId === null) closeRunForm();
      else openRunForm(profile);
      return;
    }
    await dispatchRun(profile, undefined);
  };

  const submitRun = async () => {
    if (!runTarget) return;
    try {
      const values = validateScriptParameterValues(profileParameters(runTarget, templates), runValues, true);
      setNotice('');
      await dispatchRun(runTarget, values);
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'These inputs are invalid.');
    }
  };

  const rememberValues = async (profile: ScriptProfile, values: Record<string, ScriptTemplateValue>) => {
    try {
      const result = await services.modules.dispatch('saveProfile', moduleInput({
        ...profile,
        values: rememberableValues(profileParameters(profile, templates), values, profile.templateOrigin?.id),
      }));
      if (result.status === 'error') throw new Error(result.message);
      setNotice(`${profile.name}: values remembered. Passwords are never remembered.`);
      await refresh();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not remember these values.');
    }
  };

  const installTemplate = async (template: ScriptTemplateDescriptor, values?: Record<string, ScriptTemplateValue>, additionalCopy = false) => {
    try {
      const payload = {
        id: template.id,
        ...(values ? { values: rememberableValues(template.parameters, values, template.id) } : {}),
        ...(additionalCopy ? { copy: true } : {}),
      };
      const result = await services.modules.dispatch('installTemplate', moduleInput(payload));
      if (result.status === 'error') throw new Error(result.message);
      setNotice(result.message);
      setLibraryDetail(null);
      await refresh();
      // A new copy is only useful once it has its own name and values, so open its editor right away.
      const added = additionalCopy && result.data && typeof result.data === 'object' && 'profile' in result.data ? result.data.profile as ScriptProfile : null;
      if (added) {
        closeLibrary();
        openEditor(added);
      }
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not install this template.');
    }
  };

  const showTemplateDetails = (template: ScriptTemplateDescriptor) => {
    const installedProfile = profiles.find((profile) => profile.templateOrigin?.id === template.id && profile.templateOrigin?.version === template.version);
    if (installedProfile) {
      setLibraryDetail(null);
      openRunForm(installedProfile);
      return;
    }
    setLibraryDetail(template);
    setLibraryValues((current) => resetTemplateValues(template, current));
  };

  const makeEditableCopy = () => {
    if (!editing?.templateOrigin) return;
    const template = templates.find((item) => item.id === editing.templateOrigin?.id && item.version === editing.templateOrigin?.version);
    if (!template) {
      setNotice('This template version is unavailable. Remove and reinstall its profile.');
      return;
    }
    setEditing({ ...editing, templateOrigin: null, parameters: normalizeParameterPositions(template.parameters.map((parameter) => ({ ...parameter }))) });
    setDefinitionsOpen(false);
  };

  const deleteProfile = async (profile: ScriptProfile) => {
    try {
      const result = await services.modules.dispatch('deleteProfile', { id: profile.id });
      setNotice(result.message);
      if (runTarget?.id === profile.id) closeRunForm();
      await refresh();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not remove this profile.');
    }
  };

  const commands = useMemo(() => [
    { id: 'new-script-profile', label: 'Create script', keywords: ['new', 'python', 'bash', 'powershell'], disabled: !settingsReady || runningId !== null, run: () => openNewEditor() },
    ...(runningId ? [{ id: 'cancel-script-run', label: 'Cancel running script', keywords: ['stop', 'terminate'], run: () => cancelActiveRun() }] : []),
    ...profiles.flatMap((profile) => {
      const hasInputs = profileParameters(profile, templates).length > 0;
      return [
        { id: `run-script:${profile.id}`, label: `Run ${profile.name}`, keywords: [profile.id, profile.interpreter], disabled: runningId !== null,
          ...(hasInputs ? {} : { confirmationPrompt: `Run script profile “${profile.name}”?` }), run: () => runProfile(profile) },
        { id: `edit-script:${profile.id}`, label: `Edit ${profile.name}`, keywords: [profile.id, 'profile'], disabled: runningId !== null, run: () => openEditor(profile) },
        { id: `delete-script:${profile.id}`, label: `Delete ${profile.name}`, keywords: [profile.id, 'remove'], disabled: runningId !== null, confirmationPrompt: `Delete saved script profile “${profile.name}”?`, run: () => deleteProfile(profile) },
      ];
    }),
  ], [deleteProfile, interpreterDefaults, profiles, runProfile, runningId, settingsReady, templates]);
  useRegisterTabCommands(tab.id, commands);

  const browseForScript = async (): Promise<string | null> => {
    if (!editing) return null;
    try {
      const path = await services.files.pickScriptFile(editing.interpreter);
      if (path) setEditing((current) => current ? {
        ...updateScriptPath(current, path),
        workingDirectory: current.workingDirectory || path.slice(0, Math.max(path.lastIndexOf('\\'), path.lastIndexOf('/'))),
      } : current);
      return path;
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not open the script picker.');
      return null;
    }
  };

  const browseForWorkingDirectory = async (): Promise<string | null> => {
    try {
      const path = await services.files.pickWorkingDirectory();
      if (path) setEditing((current) => current ? { ...current, workingDirectory: path } : current);
      return path;
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Could not open the folder picker.');
      return null;
    }
  };

  const latestFinishedId = runs.find((run) => run.finishedAt !== null)?.id;
  const shownRun = shownRunId === 'none' ? undefined : runs.find((run) => run.id === (shownRunId ?? latestFinishedId));
  const output = shownRun?.output ?? null;
  const editingRowId = editing && profiles.some((profile) => profile.id === editingOriginalId) ? editingOriginalId : null;
  // Keep the expanded inputs in view when they open from the Library or the command palette.
  useEffect(() => {
    if (runTarget) document.querySelector('.script-run-panel')?.scrollIntoView?.({ block: 'nearest', behavior: 'smooth' });
  }, [runTarget?.id]);
  useEffect(() => {
    if (editingRowId) document.querySelector('.script-profile-item > .script-editor')?.scrollIntoView?.({ block: 'nearest', behavior: 'smooth' });
  }, [editingRowId]);
  const runParameters = runTarget ? profileParameters(runTarget, templates) : [];
  const runIsActive = runTarget !== null && runningId === runTarget.id;
  const editingTemplate = editing?.templateOrigin ? templates.find((item) => item.id === editing.templateOrigin?.id && item.version === editing.templateOrigin?.version) : undefined;
  // Passwords and the transient Firebird database are never remembered, so the edit screen does not ask for them.
  const editParameters = editing
    ? profileParameters(editing, templates).filter((parameter) => !parameter.sensitive
      && !(editing.templateOrigin && parameter.key === 'database' && editing.templateOrigin.id.startsWith('firebird-3-')))
    : [];
  const editingTemplateHasPassword = editingTemplate?.parameters.some((parameter) => parameter.sensitive) ?? false;

  const renderEditor = () => (
    <>
      {editing && editing.templateOrigin && (
        <form className="script-editor" onSubmit={(event) => void saveProfile(event)}>
          <div className="script-editor-heading"><FileCode2 size={16} /><strong>Library profile</strong>
            <button className="icon-button" type="button" aria-label="Close editor" onClick={() => setEditing(null)}><X size={15} /></button></div>
          <div className="script-fields">
            <label><span>Name</span><input required maxLength={128} value={editing.name} onChange={(event) => setEditing({ ...editing, name: event.target.value })} /></label>
            <div className="script-field-wide script-readonly">
              <span>Interpreter</span><strong>{interpreterName(editing.interpreter)}</strong>
              <span>Script</span><code>{editing.scriptPath}</code>
            </div>
            {editParameters.length > 0 && <div className="script-field-wide script-input-values">
              <h2>Inputs</h2>
              <p className="script-section-help">Values remembered for this script. The run form starts with them. Passwords are never remembered.</p>
              <div className="script-fields"><ScriptParameterFields parameters={editParameters} values={editValues} services={services}
                onChange={(key, value) => setEditValues((current) => ({ ...current, [key]: value }))} /></div>
            </div>}
            <p className="script-field-wide">To change the script or declare your own inputs, make an editable copy.</p>
            {editingTemplateHasPassword
              ? <p className="script-field-wide">This template takes a password, which is never saved, so it cannot be turned into an editable copy.</p>
              : <div className="script-editor-actions script-field-wide"><button className="secondary-button" type="button" onClick={makeEditableCopy}>Make an editable copy</button></div>}
          </div>
          <div className="script-editor-actions"><button className="secondary-button" type="button" onClick={() => setEditing(null)}>Cancel</button><button className="primary-button" type="submit"><Save size={14} /> Save name</button></div>
        </form>
      )}

      {editing && !editing.templateOrigin && (
        <form className="script-editor" onSubmit={(event) => void saveProfile(event)}>
          <div className="script-editor-heading"><FileCode2 size={16} /><strong>{profiles.some((profile) => profile.id === editing.id) ? 'Edit script' : 'New script'}</strong>
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
            <div className="script-field-wide"><PathField label="Script file" kind="file" required value={editing.scriptPath} placeholder={editing.interpreter === 'python' ? 'C:\\Scripts\\report.py' : editing.interpreter === 'bash' ? 'C:\\Scripts\\report.sh' : 'C:\\Scripts\\report.ps1'} onChange={(value) => setEditing((current) => current ? updateScriptPath(current, value) : current)} onBrowse={browseForScript} /></div>
            <div className="script-field-wide"><PathField label="Working directory" kind="directory" required value={editing.workingDirectory} placeholder="C:\\Scripts" onChange={(value) => setEditing({ ...editing, workingDirectory: value })} onBrowse={browseForWorkingDirectory} /></div>
            <label><span>Output</span><select value={editing.outputMode} onChange={(event) => setEditing({ ...editing, outputMode: event.target.value as ScriptProfile['outputMode'] })}><option value="text">Plain text</option><option value="json">JSON</option></select></label>
            <label><span>Timeout <small>seconds</small></span><input required type="number" min={1} max={3600} value={editing.timeoutSeconds} onChange={(event) => setEditing({ ...editing, timeoutSeconds: Number(event.target.value) })} /></label>
            <label className="script-field-wide"><span>AI agents</span><span><input type="checkbox" checked={editing.aiAccess === true} onChange={(event) => setEditing({ ...editing, aiAccess: event.target.checked })} /> AI can use this script <small>Lets agents connected through Agent access list and run it.</small></span></label>
          </div>
          {editParameters.length > 0 && <section className="script-parameter-section">
            <h2>Inputs</h2>
            <p className="script-section-help">Values remembered for this script. The run form starts with them, and you are asked again each time you run it.</p>
            <div className="script-fields"><ScriptParameterFields parameters={editParameters} values={editValues} services={services}
              onChange={(key, value) => setEditValues((current) => ({ ...current, [key]: value }))} /></div>
          </section>}
          <details className="script-advanced" open={definitionsOpen} onToggle={(event) => setDefinitionsOpen(event.currentTarget.open)}>
            <summary>{(editing.parameters ?? []).length === 0 ? 'Define inputs for this script' : 'Edit input definitions'}</summary>
            <p className="script-section-help">Each input is one argument, in this order. The example is the grey text shown in the empty box, and the hint appears under it.</p>
            <ScriptParameterBuilder parameters={editing.parameters ?? []} onChange={(parameters) => setEditing((current) => current ? { ...current, parameters } : current)} />
          </details>
          <details className="script-advanced">
            <summary>Fixed arguments (advanced)</summary>
            <p className="script-section-help">Passed before the inputs, one per line. Use them for flags that never change, such as <code>--daily</code>.</p>
            <label className="script-field-wide"><span>Arguments <small>one argument per line</small></span><textarea rows={3} value={editing.arguments.join('\n')} placeholder={'--daily\nvalue with spaces'} onChange={(event) => setEditing({ ...editing, arguments: event.target.value.split(/\r?\n/) })} /></label>
            <GlobalVariableHints onInsert={(reference) => setEditing({ ...editing, arguments: [...editing.arguments, reference] })} />
          </details>
          <p className="script-trust-note">{trustNote} Saving a script remembers its interpreter path as the default for new scripts; existing scripts keep their saved path.</p>
          <div className="script-editor-actions"><button className="secondary-button" type="button" onClick={() => setEditing(null)}>Cancel</button><button className="primary-button" type="submit"><Save size={14} /> Save script</button></div>
        </form>
      )}
    </>
  );

  return (
    <section className="module-view script-runner-view" aria-label="Script Runner">
      <div className="script-heading">
        <div className="script-heading-title">
          <span className="script-module-icon"><Terminal size={16} /></span>
          <div><h1>Script Runner</h1><p>Saved Python, Bash, and PowerShell scripts with typed inputs</p></div>
        </div>
        <div className="script-editor-actions">
          <button className="secondary-button" type="button" disabled={runningId !== null} onClick={() => setExplorerOpen((current) => !current)}>Explorer actions</button>
          <button className="secondary-button" type="button" onClick={() => libraryOpen ? closeLibrary() : setLibraryOpen(true)}>Library</button>
          <button className="primary-button" type="button" disabled={!settingsReady || runningId !== null} onClick={openNewEditor}>
            <Plus size={14} /> New script
          </button>
        </div>
      </div>

      {notice && <p className="script-notice" role="status">{notice}</p>}

      {activeRun && <section className="script-running" role="status" aria-label="Script running">
        <Loader2 size={15} className="script-running-spinner" />
        <div className="script-running-copy"><strong>{activeRun.profileName}</strong><span>{activeRun.state === 'canceled' ? 'Canceling…' : 'Running'} · <ElapsedTime since={activeRun.startedAt} /></span></div>
        <button className="secondary-button" type="button" disabled={activeRun.state === 'canceled'} onClick={cancelActiveRun}><Square size={13} /> Cancel run</button>
      </section>}

      <ExplorerActionsPanel services={services} profiles={profiles} templates={templates} open={explorerOpen} onOpen={openExplorer} busy={runningId !== null} onStartRun={startExplorerRun} />

      {libraryOpen && <section className="script-editor" aria-label="Script template library">
        <div className="script-editor-heading"><FileCode2 size={16} /><strong>Script Library</strong><button className="icon-button" type="button" aria-label="Close library" onClick={closeLibrary}><X size={15} /></button></div>
        <label className="script-field-wide"><span>Search templates</span><input type="search" value={templateQuery} placeholder="Name, description, or tag" onChange={(event) => setTemplateQuery(event.target.value)} /></label>
        <div className="script-profile-list">{filterScriptTemplates(templates, templateQuery).map((template) => {
          const targetDatabaseParameter = (template.id === 'firebird-3-backup-zip' || template.id === 'firebird-3-backup' || template.id === 'firebird-3-restore')
            ? template.parameters.find((parameter) => parameter.key === 'database')
            : undefined;
          const installedProfile = profiles.find((profile) => profile.templateOrigin?.id === template.id
            && profile.templateOrigin?.version === template.version);
          return <article className="script-profile-row script-template-row" key={template.id}>
            <span className="script-profile-icon"><Terminal size={16} /></span>
            <div className="script-profile-copy"><strong>{template.name}</strong><span>{template.description} · v{template.version} · {template.tags.join(', ')}</span></div>
            <button className="secondary-button" type="button" disabled={runningId !== null && !installedProfile} onClick={() => showTemplateDetails(template)}>Details</button>
            <button className={installedProfile ? 'secondary-button' : 'primary-button'} type="button" disabled={!settingsReady || Boolean(installedProfile) || runningId !== null} onClick={() => void installTemplate(template)}>
              {installedProfile ? 'Profile added' : <><Plus size={13} />Add to Script Runner</>}
            </button>
            {installedProfile && <button className="secondary-button" type="button" disabled={!settingsReady || runningId !== null} title="Add this script again as a separate entry, for example to run it with different folders"
              onClick={() => void installTemplate(template, undefined, true)}><Plus size={13} />Add another copy</button>}
            {targetDatabaseParameter?.type === 'file' && <div className="script-template-target">
              <PathField label={`${targetDatabaseParameter.label}${targetDatabaseParameter.required ? ' *' : ''}`} kind="file" required={targetDatabaseParameter.required} value={String(libraryValues[targetDatabaseParameter.key] ?? '')} onChange={(value) => setLibraryValues((current) => ({ ...current, [targetDatabaseParameter.key]: value }))} onBrowse={() => services.files.pickPath('file')} />
            </div>}
          </article>;
        })}</div>
        {libraryDetail && <div className="script-fields script-template-details">
          <h2 className="script-field-wide">{libraryDetail.name} · v{libraryDetail.version}</h2>
          <p className="script-field-wide">{libraryDetail.description}</p>
          <p className="script-trust-note script-field-wide">{trustNote} Passwords are never saved with a profile; you enter them when you run it.</p>
          <ScriptParameterFields parameters={templateDetailParameters(libraryDetail)} values={libraryValues} services={services}
            onChange={(key, value) => setLibraryValues((current) => ({ ...current, [key]: value }))} />
          <div className="script-editor-actions script-field-wide">
            <button className="secondary-button" type="button" onClick={() => setLibraryDetail(null)}>Close</button>
            <button className="primary-button" type="button" disabled={!settingsReady || runningId !== null} onClick={() => void installTemplate(libraryDetail, libraryValues)}><Plus size={13} /> Add with these values</button>
          </div>
        </div>}
      </section>}

      {editing && editingRowId === null && renderEditor()}

      <div className="script-profile-list" aria-live="polite">
        {profiles.length ? profiles.map((profile) => {
          const inputCount = profileParameters(profile, templates).length;
          return (
            <div className="script-profile-item" key={profile.id} data-expanded={runTarget?.id === profile.id || editingRowId === profile.id}>
            <article className="script-profile-row">
              <span className="script-profile-icon">{profile.interpreter === 'python' ? <Braces size={16} /> : <Terminal size={16} />}</span>
              <div className="script-profile-copy"><strong>{profile.name}</strong><span>{interpreterName(profile.interpreter)} · {profile.scriptPath || 'Script path missing'}{inputCount ? ` · ${inputCount} input${inputCount === 1 ? '' : 's'}` : ''}{profile.aiAccess ? ' · AI can use' : ''}</span></div>
              <span className="script-profile-time"><Clock3 size={12} /> {profile.timeoutSeconds}s</span>
              <button className="icon-button script-row-action" type="button" aria-label={runningId === profile.id ? `Cancel ${profile.name}` : `Run ${profile.name}`} title={runningId === profile.id ? 'Cancel run' : inputCount ? 'Enter inputs and run' : 'Run script'} disabled={runningId !== null && runningId !== profile.id} aria-expanded={inputCount ? runTarget?.id === profile.id : undefined} onClick={() => void runProfile(profile)}>
                {runningId === profile.id ? <Square size={14} /> : <Play size={15} />}
              </button>
              <button className="icon-button script-row-action" type="button" aria-label={`Edit ${profile.name}`} title="Edit" disabled={runningId !== null} onClick={() => openEditor(profile)}><Pencil size={14} /></button>
              <InlineConfirmButton className="icon-button script-row-action" label={`Remove ${profile.name}`} confirmLabel={`Yes, remove ${profile.name}`} title="Remove" disabled={runningId !== null} onConfirm={() => deleteProfile(profile)}><Trash2 size={14} /></InlineConfirmButton>
            </article>
            {editingRowId === profile.id && renderEditor()}
            {runTarget?.id === profile.id && <section className="script-run-panel" aria-label={`Run ${runTarget.name}`}>
              {runTarget.templateOrigin && <p className="script-trust-note">{trustNote}</p>}
              <div className="script-fields">
                <ScriptParameterFields parameters={runParameters} values={runValues} services={services}
                  onChange={(key, value) => setRunValues((current) => ({ ...current, [key]: value }))} />
              </div>
              <div className="script-editor-actions">
                {runIsActive
                  ? <button className="secondary-button" type="button" onClick={() => cancelActiveRun()}><Square size={13} /> Cancel run</button>
                  : <>
                    <button className="secondary-button" type="button" aria-label="Close inputs" onClick={closeRunForm}>Close</button>
                    <button className="secondary-button" type="button" disabled={runningId !== null} onClick={() => void rememberValues(runTarget, runValues)}>Remember these values</button>
                    <button className="primary-button" type="button" disabled={runningId !== null} onClick={() => void submitRun()}><Play size={14} /> Run</button>
                  </>}
              </div>
            </section>}
            </div>
          );
        }) : !editing ? <div className="script-empty"><span className="empty-icon"><Terminal size={20} /></span><strong>No scripts yet</strong><p>Add a Python, Bash, or PowerShell script and declare its inputs, or add a ready-made script from the Library.</p><button className="secondary-button" type="button" disabled={!settingsReady} onClick={openNewEditor}><Plus size={13} /> Create a script</button></div> : null}
      </div>

      {shownRun && shownRun.finishedAt !== null && <section className="script-output" aria-label="Script output">
        <header><div><Check size={14} /><strong>{shownRun.id === latestFinishedId ? 'Latest result' : 'Past result'}</strong><span>{shownRun.profileName} · {formatRunTime(shownRun.startedAt)} · {output?.durationMilliseconds ?? shownRun.finishedAt - shownRun.startedAt} ms</span></div><button className="icon-button" type="button" aria-label="Clear script output" onClick={() => setShownRunId('none')}><X size={14} /></button></header>
        {shownRun.state === 'canceled' && <p className="script-output-status">The run was canceled.</p>}
        {!output && shownRun.state !== 'canceled' && shownRun.message && <p className="script-output-status">{shownRun.message}</p>}
        {output?.timedOut && <p className="script-output-status">The script timed out.</p>}
        {output && output.exitCode !== null && output.exitCode !== 0 && <p className="script-output-status">Exit code {output.exitCode}</p>}
        {output?.parseFailure && <p className="script-output-status">The output was not valid JSON.</p>}
        {output && output.structuredOutput !== null && output.structuredOutput !== undefined && <pre>{JSON.stringify(output.structuredOutput, null, 2)}</pre>}
        {output?.stdout && <pre>{output.stdout}{output.stdoutTruncated ? '\n… output truncated' : ''}</pre>}
        {output?.stderr && <pre className="script-stderr">{output.stderr}{output.stderrTruncated ? '\n… output truncated' : ''}</pre>}
        {output && !output.stdout && !output.stderr && output.structuredOutput == null && <p className="script-no-output">The script did not produce output.</p>}
      </section>}

      {runs.length > 0 && <section className="script-history" aria-label="Run history">
        <header><strong>Run history</strong><small>Kept in this window until Automator restarts. Run activity keeps a permanent record of when each run happened, without its output.</small>
          {runs.some((run) => run.finishedAt !== null) && <button className="secondary-button" type="button" onClick={() => { clearFinishedScriptRuns(); setShownRunId(null); }}>Clear history</button>}</header>
        <ul>{runs.map((run) => <li key={run.id}>
          <button type="button" className="script-history-item" aria-pressed={shownRun?.id === run.id} disabled={run.finishedAt === null} onClick={() => setShownRunId(run.id)}>
            <span className={`script-run-state is-${run.state}`}>{describeRunState(run.state)}</span>
            <strong>{run.profileName}</strong>
            <span>{formatRunTime(run.startedAt)}{run.finishedAt !== null ? ` · ${formatDuration(run.finishedAt - run.startedAt)}` : ''}</span>
            {describeRunError(run) && <span className="script-history-error">{describeRunError(run)}</span>}
          </button>
        </li>)}</ul>
      </section>}
    </section>
  );
}
