import type { AutomationWindowRole, AutomationWindowContext } from './automationServiceAuthorization';
import type { BackendUiState } from '../contracts/rpc';

/** Associates renderer identities with a host-created role and a local active tab. */
export class WindowContextRegistry<TRenderer extends object = object> {
  private readonly contexts = new Map<TRenderer, AutomationWindowContext>();

  register(renderer: TRenderer, role: AutomationWindowRole): () => void {
    const context: AutomationWindowContext = { role, selectedTab: 1 };
    this.contexts.set(renderer, context);
    return () => {
      if (this.contexts.get(renderer) === context) this.contexts.delete(renderer);
    };
  }

  get(renderer: TRenderer): AutomationWindowContext | null {
    return this.contexts.get(renderer) ?? null;
  }

  setSelectedTab(renderer: TRenderer, slot: number): void {
    const current = this.contexts.get(renderer);
    if (!current) throw new Error('The renderer does not belong to an Automator window.');
    if (!Number.isSafeInteger(slot) || slot < 1 || slot > 9) throw new Error('The tab slot must be an integer from 1 to 9.');
    current.selectedTab = slot;
  }
}

/** Keeps launcher visibility and tab selection private to the quick launcher surface. */
export function projectStateForWindow(state: BackendUiState, context: AutomationWindowContext): BackendUiState {
  if (context.role === 'launcher') return state;
  return {
    ...state,
    visible: true,
    selectedTab: context.selectedTab,
    query: '',
    mode: 'launcher',
    aliasEditCandidate: null,
    error: null,
    busy: false,
    matchKind: 'None',
    matchedBindingId: null,
  };
}
