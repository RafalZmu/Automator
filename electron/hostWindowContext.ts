import type { AutomationWindowContext } from './automationServiceAuthorization';

/** Renderer-provided context is discarded; the BrowserWindow owner supplies the trusted value. */
export function injectHostWindowContext<T extends Record<string, unknown>>(
  params: T,
  context: AutomationWindowContext,
): T & { hostWindowContext: AutomationWindowContext } {
  const { hostWindowContext: _untrusted, ...safeParams } = params;
  return { ...safeParams, hostWindowContext: { role: context.role, selectedTab: context.selectedTab } } as T & { hostWindowContext: AutomationWindowContext };
}
