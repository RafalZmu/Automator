import { useSyncExternalStore } from 'react';
import { getLatestTransientResult, subscribeTransientResults } from './transientResults';
import { ResultViewer } from './ResultViewer';

/** A current-window result surface, deliberately separate from historical run selection. */
export function LatestModuleResult({ moduleId, moduleTitle }: { moduleId: string; moduleTitle: string }) {
  const latest = useSyncExternalStore(subscribeTransientResults, () => getLatestTransientResult(moduleId));
  if (!latest) return null;
  return <section className="latest-module-result" aria-label={`Latest ${moduleTitle} result in this window`}>
    <h2>Latest {moduleTitle} result in this window</h2>
    <p>This is the most recent result from this window, shown independently of the selected history entry.</p>
    <ResultViewer result={latest.result} />
  </section>;
}
