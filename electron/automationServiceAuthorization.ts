type AutomationServiceState = {
  selectedTab: number;
  tabs: ReadonlyArray<{
    slot: number;
    id: string;
    capabilities: ReadonlyArray<{ id: string; version: number }>;
  }>;
};

export type AutomationWindowRole = 'launcher' | 'workspace';
export type AutomationWindowContext = {
  role: AutomationWindowRole;
  selectedTab: number;
};

/** Applies the renderer-side IPC boundary before forwarding a typed service call to the backend. */
export function authorizeAutomationServiceCall(
  isMainFrame: boolean,
  state: AutomationServiceState | undefined,
  moduleId: string,
  capabilityId: string,
  allowInactiveCleanup: boolean,
  windowContext?: AutomationWindowContext,
): 'forward' | 'noop' {
  if (!isMainFrame) throw new Error('Automation services are available only to the renderer main frame.');

  const role = windowContext?.role ?? 'launcher';
  if (role === 'workspace' && capabilityId === 'keyboard.input')
    throw new Error('The workspace window cannot subscribe to global keyboard input.');
  const selectedSlot = windowContext?.selectedTab ?? state?.selectedTab;
  const selectedTab = state?.tabs.find((tab) => tab.slot === selectedSlot);
  if (!selectedTab || selectedTab.id !== moduleId) {
    if (allowInactiveCleanup) return 'noop';
    throw new Error('The requested automation module is not the active module.');
  }

  if (!selectedTab.capabilities.some((capability) => capability.id === capabilityId && capability.version === 1))
    throw new Error('The active module has not declared this automation capability.');

  return 'forward';
}
