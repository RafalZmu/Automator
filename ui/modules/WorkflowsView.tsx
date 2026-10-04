import {
  ArrowDown, ArrowUp, Braces, CircleAlert, Clock3, ListPlus, Play, Plus, Save, Square, Trash2,
} from 'lucide-react';
import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import type { AutomationServices } from '../automationServices';
import type { BackendUiState } from '../../contracts/rpc';
import { useRegisterTabCommands } from '../commands/TabCommandRegistry';
import './WorkflowsView.css';
import { GlobalVariableHints } from '../variables/GlobalVariables';
import {
  readWorkflowRunResult,
  readWorkflowProfile,
  parseWorkflowVariableEntries,
  validateWorkflowProfile,
  WORKFLOW_PROFILE_MODULES,
  type WorkflowInputBinding,
  type WorkflowProfile,
  type WorkflowProfileModuleId,
  type WorkflowRunResult,
  type WorkflowStep,
  type WorkflowSummary,
} from '../contracts/workflows';

type ViewProps = {
  tab: BackendUiState['tabs'][number];
  moduleState: BackendUiState['moduleStates'][number] | undefined;
  services: AutomationServices;
};

type BindingDraft = WorkflowInputBinding & { literalText?: string };
type WorkflowVariableDraft = { key: string; valueText: string };
type WorkflowDraft = Omit<WorkflowProfile, 'steps' | 'variables'> & {
  variables: WorkflowVariableDraft[];
  steps: Array<Omit<WorkflowStep, 'inputs'> & { inputs: BindingDraft[] }>;
};
type SavedProfileSummary = { profileId: string; name: string };
type WorkflowRunPayload = WorkflowRunResult & { workflowId: string; outputOmitted?: boolean };

const emptyProfiles = (): Record<WorkflowProfileModuleId, SavedProfileSummary[]> => ({
  'script-runner': [], api: [], 'browser-automation': [],
});

function createStepId(steps: WorkflowStep[]): string {
  let index = steps.length + 1;
  while (steps.some((step) => step.id === `step-${index}`)) index++;
  return `step-${index}`;
}

function blankWorkflow(): WorkflowDraft {
  return { id: `workflow-${Date.now().toString(36)}`, name: '', variables: [], steps: [] };
}

function toDraft(profile: WorkflowProfile): WorkflowDraft {
  return {
    ...profile,
    variables: Object.entries(profile.variables ?? {}).map(([key, value]) => ({ key, valueText: JSON.stringify(value, null, 2) ?? 'null' })),
    steps: profile.steps.map((step) => ({
      ...step,
      inputs: step.inputs.map((binding) => (binding.literalPresent ?? (Object.hasOwn(binding, 'literal') && !binding.sourceStepId))
        ? { ...binding, literalText: JSON.stringify(binding.literal, null, 2) ?? 'null' }
        : { ...binding }),
    })),
  };
}

function toProfile(draft: WorkflowDraft): { profile?: WorkflowProfile; error?: string } {
  try {
    const parsedVariables = parseWorkflowVariableEntries(draft.variables);
    if (!parsedVariables.variables) return { error: parsedVariables.error ?? 'Workflow variables are invalid.' };
    const steps = draft.steps.map(({ inputs, ...step }) => ({
      ...step,
      inputs: inputs.map(({ literalText, ...binding }) => {
        if (binding.sourceStepId || binding.sourceJsonPointer !== undefined) return { ...binding, literalPresent: false };
        return {
          targetJsonPointer: binding.targetJsonPointer,
          literal: JSON.parse(literalText ?? 'null') as WorkflowInputBinding['literal'],
          literalPresent: true,
        };
      }),
    }));
    const profile: WorkflowProfile = { id: draft.id.trim().toLowerCase(), name: draft.name.trim(), variables: parsedVariables.variables, variableReferences: draft.variableReferences, steps };
    const validation = validateWorkflowProfile(profile);
    return validation ? { error: validation } : { profile };
  } catch {
    return { error: 'Each literal input must contain valid JSON.' };
  }
}

function readWorkflows(data: unknown): WorkflowSummary[] {
  if (!data || typeof data !== 'object' || !('workflows' in data) || !Array.isArray(data.workflows)) return [];
  return data.workflows.filter((item): item is WorkflowSummary => Boolean(item && typeof item === 'object'
    && 'id' in item && typeof item.id === 'string'
    && 'name' in item && typeof item.name === 'string'
    && 'stepCount' in item && typeof item.stepCount === 'number'));
}

function readSavedProfiles(data: unknown): SavedProfileSummary[] {
  if (!data || typeof data !== 'object' || !('profiles' in data) || !Array.isArray(data.profiles)) return [];
  return data.profiles.filter((item): item is SavedProfileSummary => Boolean(item && typeof item === 'object'
    && 'profileId' in item && typeof item.profileId === 'string'
    && 'name' in item && typeof item.name === 'string'));
}

function getErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}

function safePretty(value: unknown): string {
  try { return JSON.stringify(value, null, 2) ?? 'null'; }
  catch { return 'null'; }
}

export function WorkflowsView({ services, tab }: ViewProps) {
  const [workflows, setWorkflows] = useState<WorkflowSummary[]>([]);
  const [profilesByModule, setProfilesByModule] = useState(emptyProfiles);
  const [draft, setDraft] = useState<WorkflowDraft | null>(null);
  const [runResult, setRunResult] = useState<WorkflowRunPayload | null>(null);
  const [running, setRunning] = useState(false);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState('');
  const runController = useRef<AbortController | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    try {
      const [workflowResult, ...catalogResults] = await Promise.all([
        services.modules.dispatch('listWorkflows', {}),
        ...WORKFLOW_PROFILE_MODULES.map((module) => services.modules.dispatch('listSavedProfiles', { moduleId: module.id })),
      ]);
      if (workflowResult.status === 'error') throw new Error(workflowResult.message);
      setWorkflows(readWorkflows(workflowResult.data));
      const catalogs = emptyProfiles();
      WORKFLOW_PROFILE_MODULES.forEach((module, index) => {
        const result = catalogResults[index];
        if (result.status !== 'error') catalogs[module.id] = readSavedProfiles(result.data);
      });
      setProfilesByModule(catalogs);
      setNotice('');
    } catch (error) {
      setNotice(getErrorMessage(error, 'Could not load workflows and saved profiles.'));
    } finally {
      setLoading(false);
    }
  }, [services]);

  useEffect(() => {
    void refresh();
    return () => runController.current?.abort();
  }, [refresh]);

  const validationError = useMemo(() => draft ? toProfile(draft).error ?? '' : '', [draft]);

  const saveWorkflow = async () => {
    if (!draft) return;
    const normalized = toProfile(draft);
    if (!normalized.profile) {
      setNotice(normalized.error ?? 'The workflow is invalid.');
      return;
    }
    try {
      const result = await services.modules.dispatch('saveWorkflow', normalized.profile);
      if (result.status === 'error') throw new Error(result.message);
      setDraft(null);
      setRunResult(null);
      await refresh();
      setNotice(result.message);
    } catch (error) {
      setNotice(getErrorMessage(error, 'Could not save this workflow.'));
    }
  };

  const runWorkflow = async (workflowId: string) => {
    if (runController.current) {
      runController.current.abort();
      return;
    }
    const controller = new AbortController();
    runController.current = controller;
    setRunning(true);
    setRunResult(null);
    setNotice('Workflow is running…');
    try {
      const result = await services.modules.dispatch('runWorkflow', { workflowId }, { signal: controller.signal });
      if (result.data && typeof result.data === 'object' && 'steps' in result.data) {
        const parsed = readWorkflowRunResult(result.data);
        if (parsed) setRunResult({ ...parsed, workflowId, outputOmitted: 'outputOmitted' in result.data ? Boolean(result.data.outputOmitted) : false });
      }
      setNotice(result.message);
    } catch (error) {
      setNotice(controller.signal.aborted ? 'Workflow canceled.' : getErrorMessage(error, 'The workflow could not be run.'));
    } finally {
      if (runController.current === controller) runController.current = null;
      setRunning(false);
    }
  };

  const deleteWorkflow = async (workflow: WorkflowSummary) => {
    try {
      const result = await services.modules.dispatch('deleteWorkflow', { workflowId: workflow.id });
      if (result.status === 'error') throw new Error(result.message);
      if (runResult?.workflowId === workflow.id) setRunResult(null);
      await refresh();
      setNotice(result.message);
    } catch (error) {
      setNotice(getErrorMessage(error, 'Could not remove this workflow.'));
    }
  };

  const editWorkflow = async (workflow: WorkflowSummary) => {
    try {
      const result = await services.modules.dispatch('getWorkflow', { workflowId: workflow.id });
      if (result.status === 'error') throw new Error(result.message);
      const profile = result.data && typeof result.data === 'object' && 'workflow' in result.data
        ? readWorkflowProfile(result.data.workflow) : null;
      if (!profile) throw new Error('The saved workflow is invalid.');
      createOrEdit(profile);
    } catch (error) {
      setNotice(getErrorMessage(error, 'Could not open this workflow.'));
    }
  };

  const addStep = () => {
    setDraft((current) => {
      if (!current) return current;
      const id = createStepId(current.steps);
      const step: WorkflowDraft['steps'][number] = {
        id,
        moduleId: 'script-runner',
        profileId: '',
        inputs: [],
      };
      return { ...current, steps: [...current.steps, step] };
    });
  };

  const updateStep = (stepIndex: number, update: (step: WorkflowDraft['steps'][number]) => WorkflowDraft['steps'][number]) => {
    setDraft((current) => current ? {
      ...current,
      steps: current.steps.map((step, index) => index === stepIndex ? update(step) : step),
    } : current);
  };

  const moveStep = (stepIndex: number, offset: -1 | 1) => {
    setDraft((current) => {
      if (!current) return current;
      const targetIndex = stepIndex + offset;
      if (targetIndex < 0 || targetIndex >= current.steps.length) return current;
      const steps = [...current.steps];
      [steps[stepIndex], steps[targetIndex]] = [steps[targetIndex], steps[stepIndex]];
      return { ...current, steps };
    });
  };

  const addBinding = (stepIndex: number) => updateStep(stepIndex, (step) => ({
    ...step,
    inputs: [...step.inputs, { targetJsonPointer: '/value', literal: '', literalPresent: true, literalText: '""' }],
  }));

  const createOrEdit = (workflow?: WorkflowProfile) => {
    setRunResult(null);
    setDraft(workflow ? toDraft(workflow) : blankWorkflow());
  };

  const commandCatalog = useMemo(() => {
    const commands = [
      { id: 'workflow:new', label: 'New workflow', keywords: ['create workflow'], disabled: running || Boolean(draft), run: () => createOrEdit() },
      { id: 'workflow:add-step', label: 'Add workflow step', keywords: ['new step'], disabled: running || !draft || draft.steps.length >= 32, run: addStep },
      { id: 'workflow:save', label: 'Save workflow', keywords: ['save'], disabled: running || !draft || Boolean(validationError), confirmationPrompt: 'Save the current workflow?', run: () => saveWorkflow() },
      { id: 'workflow:cancel', label: 'Cancel workflow run', keywords: ['stop'], disabled: !running, run: () => runController.current?.abort() },
      ...workflows.flatMap((workflow) => [
        { id: `workflow:run:${workflow.id}`, label: `Run ${workflow.name}`, keywords: ['execute', workflow.id], disabled: running || Boolean(draft), confirmationPrompt: `Run workflow “${workflow.name}”?`, run: () => runWorkflow(workflow.id) },
        { id: `workflow:edit:${workflow.id}`, label: `Edit ${workflow.name}`, keywords: ['open', workflow.id], disabled: running || Boolean(draft), run: () => editWorkflow(workflow) },
        { id: `workflow:delete:${workflow.id}`, label: `Delete ${workflow.name}`, keywords: ['remove', workflow.id], disabled: running || Boolean(draft), confirmationPrompt: `Delete workflow “${workflow.name}”?`, run: () => deleteWorkflow(workflow) },
      ]),
    ];
    return commands;
  }, [addStep, createOrEdit, deleteWorkflow, draft, editWorkflow, runWorkflow, running, saveWorkflow, validationError, workflows]);
  useRegisterTabCommands(tab.id, commandCatalog);

  return (
    <section className="module-view workflows-view" aria-label="Workflows">
      <header className="workflow-heading">
        <div className="workflow-heading-title">
          <span className="workflow-module-icon"><Braces size={16} /></span>
          <div><h1>Workflows</h1><p>Chain saved profiles and pass structured JSON between steps.</p></div>
        </div>
        <button className="primary-button" type="button" disabled={running} onClick={() => createOrEdit()}><Plus size={13} /> New workflow</button>
      </header>

      {notice && <p className="workflow-notice" role="status">{notice}</p>}

      {draft ? (
        <section className="workflow-editor" aria-label="Workflow editor">
          <div className="workflow-editor-heading"><strong>{workflows.some((workflow) => workflow.id === draft.id) ? 'Edit workflow' : 'New workflow'}</strong>
            <button className="secondary-button" type="button" onClick={() => setDraft(null)}>Close</button></div>
          <div className="workflow-fields">
            <label><span>Name</span><input required maxLength={128} value={draft.name} placeholder="Daily report" onChange={(event) => setDraft({ ...draft, name: event.target.value })} /></label>
            <label><span>Workflow key</span><input required pattern="[a-z0-9][a-z0-9._-]{0,63}" value={draft.id} onChange={(event) => setDraft({ ...draft, id: event.target.value.trim().toLowerCase() })} /></label>
          </div>

          <GlobalVariableHints syntax="pointer" />
          {draft.variables.length > 0 && <p role="status">This workflow has legacy values awaiting migration. Resolve the reported conflicts in Options → Variables. Existing values are preserved while you edit steps.</p>}

          <div className="workflow-step-list">
            {!draft.steps.length && <p className="workflow-empty-steps">Add saved profiles in the order they should run.</p>}
            {draft.steps.map((step, stepIndex) => {
              const moduleLabel = WORKFLOW_PROFILE_MODULES.find((item) => item.id === step.moduleId)?.label ?? step.moduleId;
              return (
                <article className="workflow-step" key={step.id}>
                  <header className="workflow-step-heading">
                    <span className="workflow-step-index">{stepIndex + 1}</span><strong>Step {stepIndex + 1}</strong><span className="workflow-step-id">{step.id}</span>
                    <button className="icon-button" type="button" aria-label={`Move step ${stepIndex + 1} up`} disabled={stepIndex === 0} onClick={() => moveStep(stepIndex, -1)}><ArrowUp size={13} /></button>
                    <button className="icon-button" type="button" aria-label={`Move step ${stepIndex + 1} down`} disabled={stepIndex === draft.steps.length - 1} onClick={() => moveStep(stepIndex, 1)}><ArrowDown size={13} /></button>
                    <button className="icon-button" type="button" aria-label={`Remove step ${stepIndex + 1}`} onClick={() => setDraft((current) => current ? { ...current, steps: current.steps.filter((_, index) => index !== stepIndex) } : current)}><Trash2 size={13} /></button>
                  </header>
                  <div className="workflow-step-fields">
                    <label><span>Profile type</span><select value={step.moduleId} onChange={(event) => updateStep(stepIndex, (current) => ({
                      ...current,
                      moduleId: event.target.value as WorkflowProfileModuleId,
                      profileId: '',
                    }))}>{WORKFLOW_PROFILE_MODULES.map((item) => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
                    <label><span>Saved profile</span><select required value={step.profileId} onChange={(event) => updateStep(stepIndex, (current) => ({ ...current, profileId: event.target.value }))}>
                      <option value="">Choose a {moduleLabel} profile…</option>
                      {profilesByModule[step.moduleId].map((profile) => <option key={profile.profileId} value={profile.profileId}>{profile.name} · {profile.profileId}</option>)}
                    </select></label>
                  </div>
                  <div className="workflow-mappings-heading"><strong>Input mappings</strong><button className="secondary-button" type="button" onClick={() => addBinding(stepIndex)}><ListPlus size={12} /> Add mapping</button></div>
                  {!step.inputs.length ? <p className="workflow-mapping-empty">{stepIndex === 0 ? 'No mapped values. The first profile receives the workflow variables.' : 'No mapped values. The profile receives the previous step output.'}</p> : step.inputs.map((binding, bindingIndex) => (
                    <div className="workflow-mapping" key={`${step.id}-${bindingIndex}`}>
                      <label><span>Destination pointer</span><input value={binding.targetJsonPointer} placeholder="/request/query" onChange={(event) => updateStep(stepIndex, (current) => ({
                        ...current,
                        inputs: current.inputs.map((item, index) => index === bindingIndex ? { ...item, targetJsonPointer: event.target.value } : item),
                      }))} /></label>
                      <label><span>Value source</span><select value={!binding.sourceStepId ? 'literal' : binding.sourceStepId === '$input' ? 'input' : 'step'} onChange={(event) => updateStep(stepIndex, (current) => ({
                        ...current,
                        inputs: current.inputs.map((item, index) => index !== bindingIndex ? item : event.target.value === 'step'
                          ? { targetJsonPointer: item.targetJsonPointer, sourceStepId: draft.steps[Math.max(0, stepIndex - 1)]?.id ?? '', sourceJsonPointer: '', literalPresent: false }
                          : event.target.value === 'input'
                            ? { targetJsonPointer: item.targetJsonPointer, sourceStepId: '$input', sourceJsonPointer: '', literalPresent: false }
                          : { targetJsonPointer: item.targetJsonPointer, literal: '', literalPresent: true, literalText: '""' }),
                      }))}><option value="literal">JSON value</option><option value="input">Workflow variables</option><option value="step" disabled={stepIndex === 0}>Earlier step output</option></select></label>
                      {binding.sourceStepId ? <>
                        <label><span>{binding.sourceStepId === '$input' ? 'From input' : 'From step'}</span><select value={binding.sourceStepId} onChange={(event) => updateStep(stepIndex, (current) => ({
                          ...current,
                          inputs: current.inputs.map((item, index) => index === bindingIndex ? { ...item, sourceStepId: event.target.value } : item),
                        }))}><option value="$input">Workflow variables</option>{draft.steps.slice(0, stepIndex).map((prior) => <option key={prior.id} value={prior.id}>{prior.id}</option>)}</select></label>
                        <label><span>Source pointer <small>blank selects the whole output</small></span><input value={binding.sourceJsonPointer ?? ''} placeholder="/items/0" onChange={(event) => updateStep(stepIndex, (current) => ({
                          ...current,
                          inputs: current.inputs.map((item, index) => index === bindingIndex ? { ...item, sourceJsonPointer: event.target.value } : item),
                        }))} /></label>
                      </> : <label className="workflow-mapping-literal"><span>JSON value</span><textarea rows={2} spellCheck={false} value={binding.literalText ?? 'null'} onChange={(event) => updateStep(stepIndex, (current) => ({
                        ...current,
                        inputs: current.inputs.map((item, index) => index === bindingIndex ? { ...item, literalText: event.target.value } : item),
                      }))} /></label>}
                      <button className="icon-button workflow-remove-mapping" type="button" aria-label={`Remove mapping ${bindingIndex + 1} from step ${stepIndex + 1}`} onClick={() => updateStep(stepIndex, (current) => ({ ...current, inputs: current.inputs.filter((_, index) => index !== bindingIndex) }))}><Trash2 size={13} /></button>
                    </div>
                  ))}
                </article>
              );
            })}
          </div>

          <div className="workflow-editor-footer">
            <button className="secondary-button" type="button" onClick={addStep} disabled={draft.steps.length >= 32}><Plus size={13} /> Add step</button>
            <span className="workflow-step-hint">Runs in order and stops at the first failed step.</span>
          </div>
          {validationError && <p className="workflow-validation"><CircleAlert size={13} /> {validationError}</p>}
          <div className="workflow-save-actions"><button className="secondary-button" type="button" onClick={() => setDraft(null)}>Cancel</button><button className="primary-button" type="button" disabled={Boolean(validationError)} onClick={() => void saveWorkflow()}><Save size={13} /> Save workflow</button></div>
        </section>
      ) : (
        <div className="workflow-list" aria-live="polite">
          {workflows.map((workflow) => (
            <article className="workflow-row" key={workflow.id}>
              <span className="workflow-row-icon"><Braces size={15} /></span>
              <div className="workflow-row-copy"><strong>{workflow.name}</strong><span>{workflow.stepCount} step{workflow.stepCount === 1 ? '' : 's'} · {workflow.id}</span></div>
              <button className="icon-button" type="button" aria-label={`Run ${workflow.name}`} disabled={running} title="Run workflow" onClick={() => void runWorkflow(workflow.id)}>{running ? <Square size={14} /> : <Play size={14} />}</button>
              <button className="icon-button" type="button" aria-label={`Edit ${workflow.name}`} disabled={running} title="Edit workflow" onClick={() => void editWorkflow(workflow)}><Save size={13} /></button>
              <button className="icon-button" type="button" aria-label={`Remove ${workflow.name}`} disabled={running} title="Remove workflow" onClick={() => void deleteWorkflow(workflow)}><Trash2 size={13} /></button>
            </article>
          ))}
          {!loading && workflows.length === 0 && !draft && <div className="workflow-empty">
            <span className="empty-icon"><Braces size={20} /></span><strong>No workflows yet</strong>
            <p>Build a sequence from saved Script Runner, API, and Browser Automation profiles.</p>
            <button className="secondary-button" type="button" onClick={() => createOrEdit()}><Plus size={13} /> Create a workflow</button>
          </div>}
          {loading && <p className="workflow-loading">Loading workflows…</p>}
        </div>
      )}

      {!draft && (running || runResult) && <section className="workflow-run-panel" aria-label="Workflow run">
        {running && <button className="secondary-button workflow-cancel" type="button" onClick={() => runController.current?.abort()}><Square size={12} /> Cancel run</button>}
        {runResult && <div className="workflow-run-result" aria-label="Latest workflow result">
          <header><strong>Latest run</strong><span><Clock3 size={12} /> {runResult.summary.durationMilliseconds} ms · {runResult.summary.status}</span></header>
          {runResult.outputOmitted && <p className="workflow-result-note">Large structured output was omitted from this view. Safe run summary was retained.</p>}
          <ol>{runResult.steps.map((step, index) => <li key={`${step.stepId}-${index}`} className={`workflow-result-step is-${step.status}`}>
            <div><strong>{step.stepId}</strong><span>{step.moduleId} · {step.profileId}</span><em>{step.status} · {step.summary.durationMilliseconds} ms</em></div>
            {!('outputOmitted' in step && step.outputOmitted) && <details><summary>View structured output</summary><pre>{safePretty(step.output)}</pre></details>}
          </li>)}</ol>
        </div>}
      </section>}
    </section>
  );
}
