import { ArrowDown, ArrowUp, Plus, Trash2 } from 'lucide-react';
import type { ScriptTemplateParameter } from '../../contracts/scriptRunner';
import { newParameter, normalizeParameterPositions, withParameterType } from './scriptTemplateViewModel';

type Props = {
  parameters: ScriptTemplateParameter[];
  onChange: (parameters: ScriptTemplateParameter[]) => void;
};

const typeLabels: Record<ScriptTemplateParameter['type'], string> = {
  text: 'Text',
  file: 'File',
  directory: 'Folder',
  boolean: 'Yes or no',
  choice: 'Choice',
};

/** Declares a script's typed inputs. Each row becomes one argument, in list order. */
export function ScriptParameterBuilder({ parameters, onChange }: Props) {
  const replace = (next: ScriptTemplateParameter[]) => onChange(normalizeParameterPositions(next));
  const update = (index: number, change: (parameter: ScriptTemplateParameter) => ScriptTemplateParameter) =>
    replace(parameters.map((parameter, position) => position === index ? change(parameter) : parameter));
  const move = (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= parameters.length) return;
    const next = [...parameters];
    [next[index], next[target]] = [next[target], next[index]];
    replace(next);
  };

  return <div className="script-parameter-builder">
    {parameters.length === 0 && <p className="script-parameter-empty">No inputs yet. The script runs with its fixed arguments only.</p>}
    {parameters.map((parameter, index) => <div className="script-parameter-row" key={parameter.key}>
      <label><span>Label</span><input required maxLength={128} value={parameter.label} placeholder="Source folder" onChange={(event) => update(index, (current) => ({ ...current, label: event.target.value }))} /></label>
      <label><span>Type</span><select value={parameter.type} onChange={(event) => update(index, (current) => withParameterType(current, event.target.value as ScriptTemplateParameter['type']))}>
        {Object.entries(typeLabels).map(([type, label]) => <option key={type} value={type}>{label}</option>)}
      </select></label>
      {parameter.type === 'boolean' && <label className="script-check-field"><input type="checkbox" checked={parameter.defaultValue === true} onChange={(event) => update(index, (current) => ({ ...current, defaultValue: event.target.checked }) as ScriptTemplateParameter)} /><span>Checked by default</span></label>}
      {(parameter.type === 'text' || parameter.type === 'file' || parameter.type === 'directory') && <label><span>Default value <small>optional</small></span><input value={typeof parameter.defaultValue === 'string' ? parameter.defaultValue : ''} onChange={(event) => update(index, (current) => ({ ...current, defaultValue: event.target.value }) as ScriptTemplateParameter)} /></label>}
      {parameter.type === 'choice' && <>
        <label className="script-parameter-wide"><span>Choices <small>one per line</small></span><textarea rows={3} value={(parameter.options ?? []).join('\n')} onChange={(event) => update(index, (current) => ({ ...current, options: event.target.value.split(/\r?\n/) }) as ScriptTemplateParameter)} /></label>
        <label><span>Default choice <small>optional</small></span><select value={typeof parameter.defaultValue === 'string' ? parameter.defaultValue : ''} onChange={(event) => update(index, (current) => ({ ...current, defaultValue: event.target.value || undefined }) as ScriptTemplateParameter)}>
          <option value="">None</option>
          {(parameter.options ?? []).filter(Boolean).map((option) => <option key={option} value={option}>{option}</option>)}
        </select></label>
      </>}
      {parameter.type !== 'boolean' && parameter.type !== 'choice' && <label><span>Example <small>faint text in the empty box</small></span><input maxLength={256} value={parameter.example ?? ''} placeholder={parameter.type === 'file' ? 'C:\Data\report.csv' : parameter.type === 'directory' ? 'C:\Data' : '2024-01-31'} onChange={(event) => update(index, (current) => ({ ...current, example: event.target.value }))} /></label>}
      <label className="script-parameter-wide"><span>Hint <small>small text under the box</small></span><input maxLength={512} value={parameter.description ?? ''} onChange={(event) => update(index, (current) => ({ ...current, description: event.target.value }))} /></label>
      <div className="script-parameter-actions">
        <label className="script-check-field script-parameter-required"><input type="checkbox" checked={parameter.required} onChange={(event) => update(index, (current) => ({ ...current, required: event.target.checked }))} /><span>Required</span></label>
        <button className="icon-button" type="button" aria-label={`Move ${parameter.label || 'input'} up`} title="Move up" disabled={index === 0} onClick={() => move(index, -1)}><ArrowUp size={14} /></button>
        <button className="icon-button" type="button" aria-label={`Move ${parameter.label || 'input'} down`} title="Move down" disabled={index === parameters.length - 1} onClick={() => move(index, 1)}><ArrowDown size={14} /></button>
        <button className="icon-button" type="button" aria-label={`Remove ${parameter.label || 'input'}`} title="Remove input" onClick={() => replace(parameters.filter((_, position) => position !== index))}><Trash2 size={14} /></button>
      </div>
    </div>)}
    <button className="secondary-button" type="button" disabled={parameters.length >= 32} onClick={() => replace([...parameters, newParameter(parameters)])}><Plus size={13} /> Add input</button>
  </div>;
}
