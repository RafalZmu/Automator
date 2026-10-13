import type { ScriptTemplateParameter, ScriptTemplateValue } from '../../contracts/scriptRunner';
import type { AutomationServices } from '../automationServices';
import { PathField } from '../components/PathField';

type Props = {
  parameters: ScriptTemplateParameter[];
  values: Record<string, ScriptTemplateValue>;
  onChange: (key: string, value: ScriptTemplateValue) => void;
  services: AutomationServices;
};

/** Typed inputs shared by bundled template forms and the Run form of user script profiles. */
export function ScriptParameterFields({ parameters, values, onChange, services }: Props) {
  return <>{parameters.map((parameter) => {
    if (parameter.type === 'file' || parameter.type === 'directory') {
      return <PathField key={parameter.key}
        label={`${parameter.label}${parameter.required ? ' *' : ''}`} hint={parameter.description}
        kind={parameter.type} placeholder={parameter.example} required={parameter.required} value={String(values[parameter.key] ?? '')}
        onChange={(value) => onChange(parameter.key, value)} onBrowse={() => services.files.pickPath(parameter.type)} />;
    }
    return <label key={parameter.key} className={parameter.type === 'boolean' ? 'script-check-field' : undefined}>
      <span>{parameter.label}{parameter.required ? ' *' : ''}</span>
      {parameter.type === 'boolean'
        ? <input type="checkbox" checked={Boolean(values[parameter.key])} onChange={(event) => onChange(parameter.key, event.target.checked)} />
        : parameter.type === 'choice'
          ? <select required={parameter.required} value={String(values[parameter.key] ?? '')} onChange={(event) => onChange(parameter.key, event.target.value)}>
            <option value="">Choose…</option>
            {parameter.options.map((option) => <option key={option}>{option}</option>)}
          </select>
          : <input required={parameter.required} placeholder={parameter.example} type={parameter.sensitive ? 'password' : 'text'} value={String(values[parameter.key] ?? '')} onChange={(event) => onChange(parameter.key, event.target.value)} />}
      {parameter.description && <small className="script-field-hint">{parameter.description}</small>}
    </label>;
  })}</>;
}
