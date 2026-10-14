import { Bot, Copy } from 'lucide-react';
import { useEffect, useState } from 'react';
import type { McpStatus } from '../contracts/mcp';
import { InlineConfirmButton } from './components/InlineConfirmButton';
import type { AutomatorBridge } from './types';

const stateLabel: Record<McpStatus['state'], string> = {
  off: 'Off',
  listening: 'Listening',
  portInUse: 'Port in use',
  error: 'Could not start',
};

const message = (error: unknown) => (error instanceof Error ? error.message : String(error));

/** Settings card for the built-in MCP server. The bearer token never reaches the renderer; the host copies the command. */
export function AgentAccessSettings({ bridge, reportError }: { bridge: AutomatorBridge; reportError: (message: string) => void }) {
  const [status, setStatus] = useState<McpStatus | null>(null);
  const [port, setPort] = useState('');
  const [notice, setNotice] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let current = true;
    bridge.getMcpStatus().then((next) => {
      if (!current) return;
      setStatus(next);
      setPort(String(next.port));
    }).catch((error) => { if (current) reportError(message(error)); });
    return () => { current = false; };
  }, [bridge, reportError]);

  const apply = async (action: () => Promise<McpStatus>, done?: string) => {
    setBusy(true);
    try {
      const next = await action();
      setStatus(next);
      setPort(String(next.port));
      setNotice(done ?? '');
    } catch (error) { reportError(message(error)); }
    finally { setBusy(false); }
  };

  const portNumber = Number(port);
  const portValid = Number.isInteger(portNumber) && portNumber >= 1024 && portNumber <= 65535;

  return (
    <section className="settings-section" aria-label="Agent access">
      <div className="section-heading"><Bot size={15} /><h2>Agent access (MCP)</h2></div>
      <label className="setting-row switch-row" htmlFor="agent-access-switch">
        <span className="setting-copy">
          <strong>Let Claude use Automator</strong>
          <span>Runs on this PC only, starts with Automator, and needs the access token to connect.</span>
        </span>
        <input id="agent-access-switch" type="checkbox" checked={status?.enabled ?? false} disabled={!status || busy}
          onChange={(event) => void apply(() => bridge.setMcpEnabled(event.target.checked))} />
      </label>
      {status && (
        <>
          <div className="setting-row">
            <div className="setting-copy">
              <strong>{stateLabel[status.state]}</strong>
              <span>{status.url ?? status.error ?? 'Turn on access to start the server.'}</span>
            </div>
            <span className="agent-port">
              <label htmlFor="agent-port-input">Port</label>
              <input id="agent-port-input" inputMode="numeric" value={port} disabled={busy} aria-invalid={!portValid}
                onChange={(event) => setPort(event.target.value)} />
              <button type="button" disabled={busy || !portValid || portNumber === status.port}
                onClick={() => void apply(() => bridge.setMcpPort(portNumber), 'Port changed. Register Claude again with the new command.')}>Apply</button>
            </span>
          </div>
          {status.state === 'portInUse' && status.error && <p className="settings-notice" role="alert">{status.error}</p>}
          <div className="settings-tools">
            <button type="button" disabled={busy || status.state !== 'listening'}
              onClick={() => void bridge.copyMcpCommand().then(() => setNotice('Command copied. Paste it in a terminal to register Automator with Claude Code.')).catch((error) => reportError(message(error)))}>
              <Copy size={15} /> Copy Claude command
            </button>
            <InlineConfirmButton label="Regenerate token" confirmLabel="Yes, regenerate token" disabled={busy}
              warning="Connected agents lose access until you register them again."
              onConfirm={() => apply(() => Promise.resolve(bridge.rotateMcpToken()), 'Token regenerated. Copy the new command.')}>
              Regenerate token
            </InlineConfirmButton>
          </div>
        </>
      )}
      {notice && <p className="settings-notice" aria-live="polite">{notice}</p>}
    </section>
  );
}
