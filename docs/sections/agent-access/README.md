# Agent access (MCP)

Automator runs a Model Context Protocol server inside the Electron host so Claude Code (or any MCP client) can use scripts, browser automations and Work Time without anyone starting a separate service. The server starts with the app, stops when the app quits, and listens on `127.0.0.1` only.

## Connecting Claude

Open **Settings → Agent access (MCP)**, check that the status reads *Listening*, press **Copy Claude command** and run it in a terminal:

```
claude mcp add --transport http automator http://127.0.0.1:47821/mcp --header "Authorization: Bearer <token>"
```

The command is built and copied by the host. The token is never shown in or sent to the renderer.

## Settings

- **Let Claude use Automator** turns the server on or off. It is on by default and the choice persists across launches.
- **Port** defaults to 47821. If the port is busy or reserved by Windows the status says so and the server stays off until another port is chosen. Automator never picks a different port silently, because Claude's saved URL would break.
- **Regenerate token** (two-step confirmation) invalidates the old token immediately. Agents must be registered again with a newly copied command.
- Configuration lives in `mcp.json` beside the host's local data (`%LOCALAPPDATA%\Automator`, or the isolated test directory). The token is encrypted with Electron `safeStorage` (Windows DPAPI). It is not part of the settings backup.

## Tools

| Tool | Purpose |
|---|---|
| `list_scripts`, `get_script` | Saved Script Runner profiles and their typed inputs (type, required, hint, example, options, defaults). |
| `run_script` | Runs a script with typed `values`. Returns the result inline, or a `runId` if it takes longer than `waitSeconds` (default 20, max 60). |
| `get_run`, `list_runs`, `cancel_run` | Poll, list and cancel runs started through MCP. |
| `list_browser_automations` | Browser profiles (start URL, allowed hosts) and, with `includeTests`, discoverable Playwright tests. |
| `run_browser_automation` | One action in a browser profile (`target: profile`) or Playwright tests (`target: tests`, kind `test`, `file` or `tag`). Async like `run_script`. |
| `get_running_timers` | The running Work Time timer (at most one), paused timers and the count of unsaved drafts. |
| `register_time` | Adds a finished entry for `hours`/`minutes` ending now (or at `endedAt`), with description and tags. |

Every saved script and browser profile is runnable; there is no per-item flag. The agent's own per-tool permission prompt is the confirmation step, so keep `run_script` and `run_browser_automation` on "ask" in Claude Code.

## How it works

`electron/mcp/server.ts` is a small stateless Streamable-HTTP MCP implementation (`initialize`, `ping`, `tools/list`, `tools/call`; responses are plain JSON). Each request is checked in order: `Host` must be `127.0.0.1:<port>` or `localhost:<port>` (DNS-rebinding guard), a present `Origin` must be loopback, and `Authorization: Bearer` must match the token (constant-time compare). Bodies are capped at 256 KiB and tool results at 200,000 characters.

Tools call the backend through `electron/mcp/backendBridge.ts`, which sends the normal `automation/moduleAction` RPC with a host-built `hostWindowContext` of `{role: 'workspace', selectedTab: <slot>}`. The slot, contract version and action versions come from the live tab registry. Because the host builds the context, an MCP client can reach a module even when its tab is not selected, and can never supply its own context.

The backend blocks for the length of a run, so `electron/mcp/runRegistry.ts` keeps the call open and records the outcome in memory: the newest 50 runs, at most 4 running at once, request ids prefixed `mcp-`. Cancelling uses `automation/moduleActionCancel`. Output is never persisted or logged. Run Activity still records metadata for each run through the backend. Quitting the app cancels running runs before the backend stops.

## Code map

- `electron/mcp/controller.ts` — lifetime, Settings actions, status and the copy command.
- `electron/mcp/server.ts` — HTTP listener, access checks and MCP message handling.
- `electron/mcp/tools.ts` — tool definitions (zod inputs exported as JSON Schema) and result shaping.
- `electron/mcp/runRegistry.ts` — async run tracking and cancellation.
- `electron/mcp/backendBridge.ts` — module action and cancel calls with the trusted window context.
- `electron/mcp/config.ts` — `mcp.json` load/save and token protection.
- `contracts/mcp.ts` — status and setting schemas shared with the renderer.
- `electron/main.ts` — `startMcp`, `stopMcp` and the `automator:mcp-*` IPC handlers; `electron/preload.ts` exposes them.
- `ui/AgentAccessSettings.tsx` — the Settings card.
- Tests: `tests/mcp-server.test.mjs` (protocol, access control, tools, run registry, config and controller with a fake backend) and `tests/electron/mcp-host.test.cjs` (real host and backend over HTTP).
