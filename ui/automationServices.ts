import type {
  AutomationHttpRequest,
  AutomationHttpResult,
  AutomationKeyboardInputNotification,
  AutomationModuleActionRequest,
  AutomationResult,
} from '../contracts/rpc';
import type { AutomatorBridge } from './types';
import type { ScriptRunnerInterpreter } from '../contracts/scriptRunner';
import { readAutomationHttpIpcResponse } from '../contracts/automationHttpIpc.ts';
import { rememberTransientResult } from './activity/transientResults.ts';

export { AutomationServiceError, type AutomationServiceErrorCategory } from '../contracts/automationHttpIpc.ts';

export type AutomationServices = {
  keyboard: {
    subscribe(handler: (event: AutomationKeyboardInputNotification['event']) => void): () => void;
  };
  http: {
    request(request: AutomationHttpRequest, options?: { signal?: AbortSignal }): Promise<AutomationHttpResult>;
  };
  modules: {
    dispatch(actionId: string, input: AutomationModuleActionRequest['input'], options?: { signal?: AbortSignal }): Promise<AutomationResult>;
  };
  files: {
    pickScriptFile(interpreter: ScriptRunnerInterpreter): Promise<string | null>;
    pickWorkingDirectory(): Promise<string | null>;
    pickBrowserProjectDirectory(): Promise<string | null>;
    saveWorkTimeCsv(csvText: string): Promise<boolean>;
  };
  dispose(): Promise<void>;
};

export type ModuleActionContext = {
  contractVersion: number;
  actions: readonly { id: string; version: number }[];
};

function abortError(): Error {
  return Object.assign(new Error('The HTTP request was aborted.'), { name: 'AbortError' });
}

/** Creates a module-bound facade and reconciles keyboard availability across focus revoke/regain. */
export function createAutomationServices(
  moduleId: string,
  bridge: AutomatorBridge,
  capabilities: readonly string[] = [],
  moduleContext?: ModuleActionContext,
): AutomationServices {
  const hasKeyboard = capabilities.includes('keyboard.input');
  const hasHttp = capabilities.includes('http.request');
  const keyboardHandlers = new Set<(event: AutomationKeyboardInputNotification['event']) => void>();
  const activeRequestIds = new Set<string>();
  const activeModuleActionIds = new Set<string>();
  let availabilityRevision = -1;
  let keyboardEligible = false;
  let backendSubscribed = false;
  let disposed = false;
  let disposal: Promise<void> | undefined;
  let requestSequence = 0;
  let transitions = Promise.resolve();

  const queueKeyboardReconcile = () => {
    transitions = transitions.then(async () => {
      if (disposed) return;
      if (!keyboardEligible || keyboardHandlers.size === 0) {
        if (backendSubscribed) {
          backendSubscribed = false;
          if (keyboardEligible) await bridge.unsubscribeAutomationKeyboard(moduleId).catch(() => undefined);
        }
        return;
      }
      if (backendSubscribed) return;

      const revisionAtStart = availabilityRevision;
      try {
        await bridge.subscribeAutomationKeyboard(moduleId);
        if (disposed) {
          backendSubscribed = true;
          return;
        }
        if (!keyboardEligible) {
          // A newer availability transition means the acknowledgement may describe a revoked stream.
          backendSubscribed = false;
          return;
        }
        if (keyboardHandlers.size === 0) {
          backendSubscribed = true;
          queueKeyboardReconcile();
          return;
        }
        if (availabilityRevision !== revisionAtStart) {
          // A revoke/regain crossing the pending acknowledgement requires a fresh subscription.
          backendSubscribed = false;
          queueKeyboardReconcile();
          return;
        }
        backendSubscribed = true;
      } catch {
        backendSubscribed = false;
      }
    }).catch(() => undefined);
  };

  const applyAvailability = (notification: { moduleId: string; eligible: boolean; revision: number }) => {
    if (disposed || notification.moduleId !== moduleId
        || typeof notification.eligible !== 'boolean'
        || !Number.isSafeInteger(notification.revision) || notification.revision < 0
        || notification.revision <= availabilityRevision) return;
    availabilityRevision = notification.revision;
    keyboardEligible = notification.eligible;
    // A false notification follows host revocation, which has already detached this subscription.
    if (!keyboardEligible) backendSubscribed = false;
    queueKeyboardReconcile();
  };

  const stopAvailability = hasKeyboard
    ? bridge.onAutomationKeyboardAvailability(applyAvailability)
    : () => {};
  const stopInput = hasKeyboard
    ? bridge.onAutomationKeyboardInput((notification) => {
      if (disposed || !keyboardEligible || notification.moduleId !== moduleId) return;
      for (const handler of [...keyboardHandlers]) {
        try { handler(notification.event); } catch { /* One tab listener cannot block other subscribers. */ }
      }
    })
    : () => {};

  if (hasKeyboard) {
    // Register both event listeners before taking the snapshot so focus edges cannot be missed.
    void bridge.getAutomationKeyboardEligibility(moduleId).then((snapshot) => {
      if (disposed || !snapshot || typeof snapshot.eligible !== 'boolean'
          || !Number.isSafeInteger(snapshot.revision) || snapshot.revision < 0
          || (availabilityRevision >= 0 && snapshot.revision <= availabilityRevision)) return;
      availabilityRevision = snapshot.revision;
      keyboardEligible = snapshot.eligible;
      if (!keyboardEligible) backendSubscribed = false;
      queueKeyboardReconcile();
    }).catch(() => undefined);
  }

  return {
    keyboard: {
      subscribe(handler) {
        if (!hasKeyboard) throw new Error(`Module '${moduleId}' has not declared keyboard.input.`);
        if (disposed) throw new Error('Automation service context has been disposed.');
        keyboardHandlers.add(handler);
        queueKeyboardReconcile();
        let subscribed = true;
        return () => {
          if (!subscribed) return;
          subscribed = false;
          keyboardHandlers.delete(handler);
          queueKeyboardReconcile();
        };
      },
    },
    http: {
      async request(request, options = {}) {
        if (!hasHttp) throw new Error(`Module '${moduleId}' has not declared http.request.`);
        if (disposed) throw new Error('Automation service context has been disposed.');
        const signal = options.signal;
        if (signal?.aborted) throw abortError();
        const requestId = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${++requestSequence}`;
        const pending = bridge.automationHttpRequest(moduleId, requestId, request).then(readAutomationHttpIpcResponse);
        activeRequestIds.add(requestId);
        const cancel = () => { void bridge.cancelAutomationHttpRequest(moduleId, requestId).catch(() => undefined); };
        signal?.addEventListener('abort', cancel, { once: true });
        if (signal?.aborted) cancel();
        try {
          return await pending;
        } finally {
          signal?.removeEventListener('abort', cancel);
          activeRequestIds.delete(requestId);
        }
      },
    },
    modules: {
      async dispatch(actionId, input, options = {}) {
        if (!moduleContext) throw new Error(`Module '${moduleId}' has no registered actions.`);
        if (disposed) throw new Error('Automation service context has been disposed.');
        const action = moduleContext.actions.find((candidate) => candidate.id === actionId);
        if (!action) throw new Error(`Action '${actionId}' is not declared by module '${moduleId}'.`);
        const signal = options.signal;
        if (signal?.aborted) throw abortError();
        const requestId = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${++requestSequence}`;
        const request = bridge.dispatchModuleAction({
          requestId,
          contractVersion: moduleContext.contractVersion,
          moduleId,
          actionId: action.id,
          actionVersion: action.version,
          input,
        });
        activeModuleActionIds.add(requestId);
        const cancel = () => { void bridge.cancelAutomationModuleAction(moduleId, requestId).catch(() => undefined); };
        signal?.addEventListener('abort', cancel, { once: true });
        if (signal?.aborted) cancel();
        try {
          const result = await request;
          rememberTransientResult(requestId, moduleId, action.id, result);
          return result;
        } finally {
          signal?.removeEventListener('abort', cancel);
          activeModuleActionIds.delete(requestId);
        }
      },
    },
    files: {
      async pickScriptFile(interpreter) {
        if (moduleId !== 'script-runner') throw new Error(`Module '${moduleId}' cannot open the script picker.`);
        if (disposed) throw new Error('Automation service context has been disposed.');
        return bridge.pickScriptFile(interpreter);
      },
      async pickWorkingDirectory() {
        if (moduleId !== 'script-runner') throw new Error(`Module '${moduleId}' cannot open the folder picker.`);
        if (disposed) throw new Error('Automation service context has been disposed.');
        return bridge.pickWorkingDirectory();
      },
      async pickBrowserProjectDirectory() {
        if (moduleId !== 'browser-automation') throw new Error(`Module '${moduleId}' cannot open the Playwright project picker.`);
        if (disposed) throw new Error('Automation service context has been disposed.');
        return bridge.pickBrowserProjectDirectory();
      },
      async saveWorkTimeCsv(csvText) {
        if (moduleId !== 'focus-sessions') throw new Error('CSV export is only available to Work Time.');
        if (disposed) throw new Error('Automation service context has been disposed.');
        return bridge.saveWorkTimeCsv(csvText);
      },
    },
    dispose() {
      if (disposal) return disposal;
      disposed = true;
      keyboardEligible = false;
      keyboardHandlers.clear();
      stopAvailability();
      stopInput();
      disposal = (async () => {
        await Promise.all([...activeRequestIds].map((requestId) =>
          bridge.cancelAutomationHttpRequest(moduleId, requestId).catch(() => ({ canceled: false }))));
        await Promise.all([...activeModuleActionIds].map((requestId) =>
          bridge.cancelAutomationModuleAction(moduleId, requestId).catch(() => ({ canceled: false }))));
        await transitions.catch(() => undefined);
        if (backendSubscribed) {
          backendSubscribed = false;
          await bridge.unsubscribeAutomationKeyboard(moduleId).catch(() => undefined);
        }
      })();
      return disposal;
    },
  };
}
