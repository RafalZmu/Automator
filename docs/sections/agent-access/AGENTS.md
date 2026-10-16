# Agent access contributor guide

Read the root AGENTS.md first. This surface lets an external agent run real actions on the user's computer, so treat every change as a security change.

- The server must stay bound to `127.0.0.1`. Do not add `0.0.0.0`, an IPv6 wildcard or LAN binding, and do not weaken the Host, Origin or bearer-token checks in `checkRequestAccess`. Keep the token comparison constant-time.
- The bearer token must never reach the renderer, logs, run output, backups or error text. The host builds and copies the Claude command itself. Do not add a renderer API that returns the token.
- `hostWindowContext` is always built by `backendBridge.ts`. Never accept a context, slot or module id from the MCP client beyond the fixed tool-to-module mapping, and never add a tool that forwards an arbitrary module action.
- Do not add tools for arbitrary shell, arbitrary JavaScript, or editing/deleting scripts, profiles or credentials without an explicit product decision. Browser actions must remain bounded by each profile's allowed hosts.
- Log only tool names and status (`Mcp.ToolCall`). Never log arguments, script values, stdout/stderr or page text. Unexpected errors return a generic message; only `McpToolError` and backend RPC messages are shown to the agent.
- Script tools must go through `aiScripts()` so only profiles with `aiAccess === true` are listed, described or run. Browser profile tools do the same with `aiBrowserProfiles()` / the `aiAccess` filter. Never add a script or profile tool that reads profiles without that filter; missing and unshared scripts must be indistinguishable.
- Script output and web text returned to the agent are untrusted. Keep the warning in the tool descriptions.
- Keep run output in memory only (`RunRegistry`). Preserve the 50-run and 4-concurrent limits and cancel running runs on quit and when the server is switched off.
- Keep a stable default port (`MCP_DEFAULT_PORT`); never fall back to another port silently. Test mode uses an ephemeral port and the isolated data directory, so tests must not use fixed ports or touch `%LOCALAPPDATA%\Automator`.
- New tools need a zod input schema, correct `readOnlyHint`/`destructiveHint` annotations, a case in `tests/mcp-server.test.mjs`, and a row in the README table. Tool names are snake_case and part of a public contract.
- Work Time tools reuse the `focus-sessions` actions and their validation. If the Work Time contract changes, update `register_time` and `get_running_timers` and their tests in the same change.
- Update this README, the Settings text in `ui/AgentAccessSettings.tsx`, and the script-runner, browser-automation and work-time docs when tool behavior changes.

Code: electron/mcp, contracts/mcp.ts, ui/AgentAccessSettings.tsx.
