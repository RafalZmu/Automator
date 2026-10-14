import { MCP_DEFAULT_PORT, mcpPortSchema, type McpStatus } from '../../contracts/mcp.ts';
import type { McpBackend } from './backendBridge.ts';
import { claudeAddCommand, generateMcpToken, loadMcpConfig, saveMcpConfig, type McpConfig, type TokenProtector } from './config.ts';
import { RunRegistry } from './runRegistry.ts';
import { McpHttpServer, type McpLog } from './server.ts';

export type McpControllerOptions = {
  configPath: string;
  protector: TokenProtector;
  backend: McpBackend;
  version: string;
  log: McpLog;
  /** Test runs use an ephemeral port so they never collide with a running Automator. */
  ephemeralPort: boolean;
};

/** Owns the MCP server's lifetime: starts with the app, follows the Settings switch, stops on quit. */
export class McpController {
  readonly runs: RunRegistry;
  private config: McpConfig | undefined;
  private server: McpHttpServer | undefined;
  private state: McpStatus['state'] = 'off';
  private error: string | null = null;
  private readonly options: McpControllerOptions;

  constructor(options: McpControllerOptions) {
    this.options = options;
    this.runs = new RunRegistry((moduleId, requestId) => options.backend.cancel(moduleId, requestId));
  }

  async start(): Promise<void> {
    this.config = loadMcpConfig(this.options.configPath, this.options.protector,
      { port: this.options.ephemeralPort ? 0 : MCP_DEFAULT_PORT });
    if (this.config.enabled) await this.listen();
  }

  async stop(): Promise<void> {
    await this.runs.cancelAll();
    await this.close();
  }

  status(): McpStatus {
    const config = this.requireConfig();
    const port = this.server?.port ?? config.port;
    return {
      enabled: config.enabled, state: this.state, port,
      url: this.state === 'listening' ? `http://127.0.0.1:${port}/mcp` : null,
      error: this.error,
    };
  }

  async setEnabled(enabled: boolean): Promise<McpStatus> {
    const config = this.requireConfig();
    config.enabled = enabled;
    this.persist();
    if (enabled) await this.listen(); else { await this.runs.cancelAll(); await this.close(); }
    return this.status();
  }

  async setPort(port: number): Promise<McpStatus> {
    const config = this.requireConfig();
    config.port = mcpPortSchema.parse(port);
    this.persist();
    if (config.enabled) { await this.close(); await this.listen(); }
    return this.status();
  }

  /** Invalidates the old token immediately; connected agents must be re-registered with the new command. */
  rotateToken(): McpStatus {
    this.requireConfig().token = generateMcpToken();
    this.persist();
    this.options.log('Mcp.TokenRotated', 'The MCP bearer token was regenerated.');
    return this.status();
  }

  /** The command contains the token, so only the host ever builds it; the renderer just asks for a copy. */
  claudeCommand(): string {
    const config = this.requireConfig();
    return claudeAddCommand(this.server?.port ?? config.port, config.token);
  }

  private requireConfig(): McpConfig {
    if (!this.config) throw new Error('The MCP server has not been initialized.');
    return this.config;
  }

  private persist(): void { saveMcpConfig(this.options.configPath, this.requireConfig(), this.options.protector); }

  private async close(): Promise<void> {
    const server = this.server;
    this.server = undefined;
    this.state = 'off';
    this.error = null;
    if (server) {
      await server.stop();
      this.options.log('Mcp.Stopped', 'The MCP server stopped.');
    }
  }

  private async listen(): Promise<void> {
    if (this.server) return;
    const config = this.requireConfig();
    const server = new McpHttpServer({
      getToken: () => this.requireConfig().token, backend: this.options.backend, runs: this.runs,
      version: this.options.version, log: this.options.log,
    });
    try {
      const port = await server.start(config.port);
      this.server = server;
      this.state = 'listening';
      this.error = null;
      this.options.log('Mcp.Listening', 'The MCP server is listening on loopback.', { port });
    } catch (error) {
      const code = (error as NodeJS.ErrnoException).code;
      this.state = code === 'EADDRINUSE' ? 'portInUse' : 'error';
      if (code === 'EADDRINUSE') this.error = `Port ${config.port} is already in use. Choose another port.`;
      else if (code === 'EACCES') this.error = `Windows does not allow port ${config.port} (it is reserved). Choose another port.`;
      else this.error = 'The MCP server could not start. See the Automator log.';
      this.options.log(code === 'EADDRINUSE' ? 'Mcp.PortInUse' : 'Mcp.StartFailed', 'The MCP server could not start.',
        { port: config.port, error: String(error) });
    }
  }
}
