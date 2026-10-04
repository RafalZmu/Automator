import { EventEmitter } from 'node:events';
import { spawn, type ChildProcessWithoutNullStreams } from 'node:child_process';
import {
  backendInitializeResultSchema,
  backendNotificationSchema,
  MAX_RPC_REQUEST_LINE_BYTES,
  MAX_RPC_RESPONSE_LINE_BYTES,
  PROTOCOL_VERSION,
  validateRpcRequest,
  type RpcMethod,
} from '../contracts/rpc';

type JsonObject = Record<string, unknown>;
type BackendError = { code: number; message: string; data?: unknown };

const DEFAULT_REQUEST_TIMEOUT_MS = 15_000;
const LONG_RUNNING_REQUEST_TIMEOUT_MS = 65 * 60 * 1000;
const LONG_RUNNING_METHODS = new Set<RpcMethod>(['automation/httpRequest', 'automation/moduleAction']);

export class BackendProcess extends EventEmitter {
  private child?: ChildProcessWithoutNullStreams;
  private pending = new Map<string, { resolve: (value: unknown) => void; reject: (error: Error) => void; timer: NodeJS.Timeout }>();
  private nextId = 1;
  private stdoutBuffer = '';
  private stopped = false;

  get childProcessId(): number | null { return this.child?.pid ?? null; }

  constructor(
    private readonly executable: string,
    private readonly args: string[],
    private readonly cwd: string,
    private readonly env: NodeJS.ProcessEnv,
    private readonly log: (event: string, message: string, properties?: JsonObject) => void,
  ) { super(); }

  async start(initialization: JsonObject): Promise<unknown> {
    if (this.child) throw new Error('The backend process is already running.');
    this.stopped = false;
    const child = spawn(this.executable, this.args, {
      cwd: this.cwd,
      env: this.env,
      windowsHide: true,
      stdio: ['pipe', 'pipe', 'pipe'],
    });
    this.child = child;
    child.stdout.setEncoding('utf8');
    child.stderr.setEncoding('utf8');
    child.stdout.on('data', (chunk: string) => this.consumeStdout(chunk));
    child.stderr.on('data', (chunk: string) => this.log('Backend.Stderr', chunk.trimEnd()));
    child.on('error', (error) => {
      this.log('Backend.ProcessError', 'The backend process could not be started or monitored.', { error: error.message });
      this.rejectPending(error);
      this.emit('failure', error);
    });
    child.on('exit', (code, signal) => {
      const error = new Error(`Backend exited (code=${code ?? 'null'}, signal=${signal ?? 'none'}).`);
      this.log('Backend.Exited', 'The backend process exited.', { code, signal, expected: this.stopped });
      this.rejectPending(error);
      this.emit('exit', { code, signal, expected: this.stopped });
      this.child = undefined;
    });
    const result = await this.request('initialize', { protocolVersion: PROTOCOL_VERSION, ...initialization });
    const parsed = backendInitializeResultSchema.safeParse(result);
    if (!parsed.success) throw new Error(`Backend initialize response failed contract validation: ${parsed.error.message}`);
    this.emit('initialized', parsed.data);
    return parsed.data;
  }

  async request(method: RpcMethod, params: JsonObject): Promise<unknown> {
    const child = this.child;
    if (!child || child.killed || child.exitCode !== null) throw new Error('The backend is not running.');
    const id = this.nextId++;
    const request = validateRpcRequest({ jsonrpc: '2.0', id, method, params });
    const line = `${JSON.stringify(request)}\n`;
    if (Buffer.byteLength(line, 'utf8') - 1 > MAX_RPC_REQUEST_LINE_BYTES)
      throw new Error('Backend request exceeds the maximum JSON-RPC message size.');
    return new Promise((resolve, reject) => {
      const timeoutMs = LONG_RUNNING_METHODS.has(method) ? LONG_RUNNING_REQUEST_TIMEOUT_MS : DEFAULT_REQUEST_TIMEOUT_MS;
      const timer = setTimeout(() => {
        this.pending.delete(String(id));
        reject(new Error(`Backend request ${method} timed out.`));
      }, timeoutMs);
      this.pending.set(String(id), { resolve, reject, timer });
      child.stdin.write(line, 'utf8', (error) => {
        if (!error) return;
        const pending = this.pending.get(String(id));
        if (!pending) return;
        clearTimeout(pending.timer);
        this.pending.delete(String(id));
        pending.reject(error);
      });
    });
  }

  async stop(): Promise<void> {
    const child = this.child;
    if (!child) return;
    this.stopped = true;
    const exited = new Promise<void>((resolve) => child.once('exit', () => resolve()));
    child.stdin.end();
    await Promise.race([
      exited,
      new Promise<void>((resolve) => setTimeout(resolve, 2500)),
    ]);
    if (child.exitCode === null && child.signalCode === null) {
      child.kill();
      await Promise.race([exited, new Promise<void>((resolve) => setTimeout(resolve, 5000))]);
    }
    if (child.exitCode === null && child.signalCode === null) {
      throw new Error('The backend did not exit after stdin EOF and termination; refusing to start another hook process.');
    }
  }

  private consumeStdout(chunk: string): void {
    this.stdoutBuffer += chunk;
    if (this.stdoutBuffer.length > 32_000_000 && !this.stdoutBuffer.includes('\n')) {
      this.failProtocol('Backend emitted an oversized line without a terminator.');
      return;
    }
    while (true) {
      const newline = this.stdoutBuffer.indexOf('\n');
      if (newline < 0) return;
      const line = this.stdoutBuffer.slice(0, newline).trimEnd();
      this.stdoutBuffer = this.stdoutBuffer.slice(newline + 1);
      if (!line) continue;
      if (Buffer.byteLength(line, 'utf8') > MAX_RPC_RESPONSE_LINE_BYTES) {
        this.failProtocol('Backend emitted a line larger than the 28 MB protocol limit.');
        return;
      }
      let message: any;
      try { message = JSON.parse(line); }
      catch (error) {
        this.failProtocol(`Backend stdout contained non-JSON data: ${error instanceof Error ? error.message : String(error)}`);
        return;
      }
      if (message === null || typeof message !== 'object' || Array.isArray(message) || message.jsonrpc !== '2.0') {
        this.failProtocol('Backend emitted a response with an invalid JSON-RPC envelope.');
        return;
      }
      if (Object.hasOwn(message, 'method')) {
        const notification = backendNotificationSchema.safeParse(message);
        if (!notification.success) {
          this.failProtocol(`Backend notification failed contract validation: ${notification.error.message}`);
          return;
        }
        this.emit('notification', notification.data);
        continue;
      }
      const validId = typeof message.id === 'string' || (typeof message.id === 'number' && Number.isFinite(message.id));
      const hasResult = Object.hasOwn(message, 'result');
      const hasError = Object.hasOwn(message, 'error');
      const validError = message.error !== null && typeof message.error === 'object'
        && Number.isInteger(message.error.code) && typeof message.error.message === 'string';
      if (!validId || hasResult === hasError || (hasError && !validError)) {
        this.failProtocol('Backend emitted a response with an invalid id, result, or error object.');
        return;
      }
      const pending = this.pending.get(String(message.id));
      if (!pending) {
        this.log('Backend.UnmatchedResponse', 'A backend response did not match a pending request.', { id: message.id });
        continue;
      }
      clearTimeout(pending.timer);
      this.pending.delete(String(message.id));
      if (message.error && typeof message.error.message === 'string') {
        const detail = message.error as BackendError;
        pending.reject(Object.assign(new Error(detail.message), { code: detail.code, data: detail.data }));
      } else {
        pending.resolve(message.result);
      }
    }
  }

  private failProtocol(message: string): void {
    this.log('Backend.ProtocolError', message);
    const error = new Error(message);
    this.rejectPending(error);
    this.emit('failure', error);
    this.child?.kill();
  }

  private rejectPending(error: Error): void {
    for (const pending of this.pending.values()) {
      clearTimeout(pending.timer);
      pending.reject(error);
    }
    this.pending.clear();
  }
}
