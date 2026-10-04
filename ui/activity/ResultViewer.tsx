import { useState } from 'react';
import { Braces, FileText, Play } from 'lucide-react';
import type { AutomationResult } from '../../contracts/rpc';
import { supportedFollowUps } from './transientResults';
import './activity.css';

type ResultAction = AutomationResult['actions'][number];
export type ResultViewerProps = {
  result: AutomationResult;
  registeredActions?: readonly { id: string; version: number; command?: string | null }[];
  onAction?: (action: ResultAction) => void | Promise<void>;
};

function readable(value: unknown): string { return typeof value === 'string' ? value : JSON.stringify(value, null, 2) ?? 'null'; }
function object(value: unknown): Record<string, unknown> { return value !== null && typeof value === 'object' && !Array.isArray(value) ? value as Record<string, unknown> : {}; }

export function ResultViewer({ result, registeredActions = [], onAction }: ResultViewerProps) {
  const [format, setFormat] = useState<'structured' | 'json'>('structured');
  const [pending, setPending] = useState<string | null>(null);
  const [error, setError] = useState('');
  const data = object(result.data);
  const steps = Array.isArray(data.steps) ? data.steps : [];
  const actions = supportedFollowUps(result, registeredActions);
  const runFollowUp = async (action: ResultAction) => {
    if (!onAction || pending) return;
    setPending(action.id); setError('');
    try { await onAction(action); }
    catch (failure) { setError(failure instanceof Error ? failure.message : 'The follow-up action failed.'); }
    finally { setPending(null); }
  };
  return <section className="result-viewer" aria-label="Current result">
    <header><strong>Current result</strong><span className={`activity-status is-${result.status}`}>{result.status}</span>
      <div className="result-format" aria-label="Result format"><button type="button" className="secondary-button" aria-pressed={format === 'structured'} onClick={() => setFormat('structured')}><FileText size={12} /> Structured</button><button type="button" className="secondary-button" aria-pressed={format === 'json'} onClick={() => setFormat('json')}><Braces size={12} /> JSON</button></div></header>
    <p className="result-message">{result.message}</p>
    {format === 'json' ? <pre>{readable(result.data)}</pre> : steps.length ? <ol className="result-steps">{steps.map((item, index) => {
      const step = object(item); const summary = object(step.summary);
      return <li key={String(step.stepId ?? index)}><strong>{String(step.stepId ?? `Step ${index + 1}`)}</strong><span>{String(step.status ?? summary.status ?? '')} · {String(summary.durationMilliseconds ?? 0)} ms</span>
        {!step.outputOmitted && 'output' in step && <details><summary>Output</summary><pre>{readable(step.output)}</pre></details>}</li>;
    })}</ol> : <>
      {typeof data.stdout === 'string' && data.stdout && <details open><summary>Standard output{data.stdoutTruncated ? ' (truncated)' : ''}</summary><pre>{data.stdout}</pre></details>}
      {typeof data.stderr === 'string' && data.stderr && <details open><summary>Standard error{data.stderrTruncated ? ' (truncated)' : ''}</summary><pre>{data.stderr}</pre></details>}
      {data.structuredOutput !== undefined && data.structuredOutput !== null ? <details open><summary>Structured output</summary><pre>{readable(data.structuredOutput)}</pre></details>
        : !('stdout' in data) && <pre>{readable(result.data)}</pre>}
      {data.parseFailure === true && <p role="status">The output was not valid JSON.</p>}
      {data.timedOut === true && <p role="status">The run timed out.</p>}
    </>}
    {actions.length > 0 && onAction && <div className="result-follow-ups" aria-label="Follow-up actions">{actions.map((action) => <button className="secondary-button" type="button" key={action.id} disabled={pending !== null} onClick={() => void runFollowUp(action)}><Play size={12} /> {pending === action.id ? 'Running…' : action.label}</button>)}</div>}
    {error && <p role="alert">{error}</p>}
    <p className="result-memory-note">Output is kept in this window only and is cleared when Automator restarts.</p>
  </section>;
}
