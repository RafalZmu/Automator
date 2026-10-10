import type { ComponentType, ReactNode } from 'react';
import { motion, useReducedMotion } from 'motion/react';
import { Grid2X2 } from 'lucide-react';
import type { BackendUiState } from '../contracts/rpc';
import type { AutomationServices } from './automationServices';
import type { BundledTabViewKind } from './viewKinds';
import { ScriptRunnerView } from './modules/ScriptRunnerView';
import { ApiView } from './modules/ApiView';
import { BrowserAutomationView } from './modules/BrowserAutomationView';
import { WorkflowsView } from './modules/WorkflowsView';
import { SchedulerView } from './modules/SchedulerView';
import { FocusSessionsView } from './modules/FocusSessionsView';
import { WebsiteLauncherView } from './modules/WebsiteLauncherView';
import { CodexView } from './modules/CodexView';

type ViewProps = {
  tab: BackendUiState['tabs'][number];
  moduleState: BackendUiState['moduleStates'][number] | undefined;
  services: AutomationServices;
  surface: 'launcher' | 'workspace';
  children?: ReactNode;
};

function LauncherTabView({ children }: ViewProps) {
  const reduced = useReducedMotion();
  return (
    <motion.section
      className="module-view"
      aria-label="Launcher applications"
      initial={reduced ? false : { opacity: 0, y: 5 }}
      animate={{ opacity: 1, y: 0 }}
      exit={reduced ? undefined : { opacity: 0, y: -3 }}
      transition={{ duration: reduced ? 0 : 0.14, ease: 'easeOut' }}
    >
      {children}
    </motion.section>
  );
}

function ReservedTabView({ tab, moduleState }: ViewProps) {
  const reduced = useReducedMotion();
  return (
    <motion.section
      className="reserved-view"
      aria-label={tab.title}
      initial={reduced ? false : { opacity: 0, y: 5 }}
      animate={{ opacity: 1, y: 0 }}
      exit={reduced ? undefined : { opacity: 0, y: -3 }}
      transition={{ duration: reduced ? 0 : 0.14, ease: 'easeOut' }}
    >
      <span className="reserved-icon"><Grid2X2 size={22} strokeWidth={1.7} /></span>
      <h1>{tab.title}</h1>
      <p>{moduleState?.values.status === 'reserved' ? 'This workspace is ready for a future module.' : 'This module is not available yet.'}</p>
    </motion.section>
  );
}

export const launcherViewRegistry = Object.freeze({
  launcher: LauncherTabView,
  'script-runner': ScriptRunnerView,
  api: ApiView,
  'browser-automation': BrowserAutomationView,
  workflows: WorkflowsView,
  scheduler: SchedulerView,
  'focus-sessions': FocusSessionsView,
  'website-launcher': WebsiteLauncherView,
  codex: CodexView,
  reserved: ReservedTabView,
} satisfies Record<BundledTabViewKind, ComponentType<ViewProps>>);

export type LauncherViewKind = keyof typeof launcherViewRegistry;
