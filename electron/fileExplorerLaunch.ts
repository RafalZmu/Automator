import path from 'node:path';
import type { FileExplorerLaunchRequest } from '../contracts/rpc.ts';
import { FILE_EXPLORER_REGISTER_SCRIPT_ACTION_ID } from '../contracts/fileExplorer.ts';

export function parseFileExplorerLaunch(args: string[]): FileExplorerLaunchRequest | null {
  const index = args.indexOf('--automator-file-action');
  if (index < 0 || args.length !== index + 4 || args[index + 2] !== '--') return null;
  const actionId = args[index + 1];
  const filePath = args[index + 3];
  if (!/^[a-z0-9][a-z0-9._-]{0,63}$/.test(actionId) || !path.win32.isAbsolute(filePath) || !/^(?:[A-Za-z]:[\\/]|\\\\)/.test(filePath)
      || /[\x00-\x1f]/.test(filePath) || filePath.length > 4096) return null;
  if (actionId === FILE_EXPLORER_REGISTER_SCRIPT_ACTION_ID && path.win32.extname(filePath).toLowerCase() !== '.ps1') return null;
  return { actionId, filePath };
}

export function stableExplorerExecutable(packaged: boolean, testMode: boolean, portable: boolean,
  executable: string, temporary: boolean): string | null {
  return packaged && !testMode && !portable && !temporary ? executable : null;
}

export class FileExplorerLaunchQueue {
  private requests: FileExplorerLaunchRequest[] = [];
  private draining = false;
  enqueue(request: FileExplorerLaunchRequest): void { this.requests.push(request); }
  async drain(ready: boolean, deliver: (request: FileExplorerLaunchRequest) => Promise<void>): Promise<void> {
    if (!ready || this.draining) return;
    this.draining = true;
    try {
      while (this.requests.length) {
        await deliver(this.requests[0]);
        this.requests.shift();
      }
    } finally { this.draining = false; }
  }
}
