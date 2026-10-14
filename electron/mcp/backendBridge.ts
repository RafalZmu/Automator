import {
  automationResultSchema,
  validateRpcRequest,
  type AutomationResult,
  type BackendUiState,
  type RpcMethod,
} from '../../contracts/rpc.ts';

/** What the MCP tools need from Automator: typed module actions and run cancellation. */
export type McpBackend = {
  call(moduleId: string, actionId: string, input: unknown, requestId?: string): Promise<AutomationResult>;
  cancel(moduleId: string, requestId: string): Promise<boolean>;
};

/** A failure whose message is safe to show to the agent. */
export class McpToolError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'McpToolError';
  }
}

export type BackendBridgeDependencies = {
  getBackend(): { request(method: RpcMethod, params: Record<string, unknown>): Promise<unknown> } | undefined;
  getState(): BackendUiState | undefined;
  /** Returns null when the backend can take calls, otherwise a short reason. */
  unavailableReason(): string | null;
  newRequestId(): string;
};

/**
 * Sends module actions with a host-built workspace window context. The MCP client never supplies the context,
 * and the slot comes from the live tab registry so a renumbered tab keeps working.
 */
export function createMcpBackend(deps: BackendBridgeDependencies): McpBackend {
  const requireBackend = () => {
    const reason = deps.unavailableReason();
    const backend = deps.getBackend();
    if (reason || !backend) throw new McpToolError(reason ?? 'The Automator backend is unavailable.');
    return backend;
  };
  const requireTab = (moduleId: string) => {
    const tab = deps.getState()?.tabs.find((candidate) => candidate.id === moduleId);
    if (!tab) throw new McpToolError(`The Automator module '${moduleId}' is unavailable.`);
    return tab;
  };

  return {
    async call(moduleId, actionId, input, requestId = deps.newRequestId()) {
      const backend = requireBackend();
      const tab = requireTab(moduleId);
      const action = tab.actions.find((candidate) => candidate.id === actionId);
      if (!action) throw new McpToolError(`The action '${actionId}' is unavailable in module '${moduleId}'.`);
      const request = validateRpcRequest({
        jsonrpc: '2.0', id: 1, method: 'automation/moduleAction',
        params: {
          requestId, contractVersion: tab.contractVersion, moduleId, actionId, actionVersion: action.version,
          input, hostWindowContext: { role: 'workspace', selectedTab: tab.slot },
        },
      });
      const parsed = automationResultSchema.safeParse(await backend.request(request.method, request.params));
      if (!parsed.success) throw new McpToolError('Automator returned an invalid module action result.');
      return parsed.data;
    },
    async cancel(moduleId, requestId) {
      const backend = requireBackend();
      const tab = requireTab(moduleId);
      const request = validateRpcRequest({
        jsonrpc: '2.0', id: 1, method: 'automation/moduleActionCancel',
        params: { moduleId, requestId, hostWindowContext: { role: 'workspace', selectedTab: tab.slot } },
      });
      const result = await backend.request(request.method, request.params) as { canceled?: unknown } | null;
      return result?.canceled === true;
    },
  };
}
