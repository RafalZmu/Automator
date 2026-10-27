# Options

Options manages application-wide preferences such as the opener key, theme, startup behavior, and settings backup/restore. Its Variables section edits reusable typed JSON values and shows where references can be inserted. Interpreter defaults are associated with Script Runner; saved profiles continue to own their configured interpreter paths.

Before each settings replacement, Automator copies the previous `settings.json` to a `settings.json.bak-<timestamp>` file beside it and keeps only the newest 10 backups.

Variables use references such as {{variables.name}} in supported text fields and workflow JSON pointers in workflow mappings. Values may be JSON primitives, arrays, or objects. They are included in library backup/export; credentials are not.

## Code map

- ui/App.tsx renders Options and connects settings/variable state to the bridge.
- ui/variables/GlobalVariables.tsx and GlobalVariables.css implement the Variables editor and hints.
- contracts/variables.ts validates variable names and JSON values.
- src/Automator.Application/Automation/AutomationVariableService.cs persists/migrates variables and expands supported references.
- Application preferences are owned by the desktop/backend settings services; their import/export boundaries are described in the root README.
- Tests: tests/variables-contract.test.mjs, tests/Automator.Variables.Specs, and settings/backend integration tests.

Use named API secrets for credentials. Global variables are ordinary reusable data, not a secret vault.

## Agent access

Settings also hosts the **Agent access (MCP)** card (`ui/AgentAccessSettings.tsx`) for the built-in MCP server. It applies immediately and is independent of the Save button. See [Agent access](../agent-access/README.md).

## Notifications

The **Task finished popups** switch shows or hides the bottom-right popup that appears when a task ends. It applies immediately and is stored by the host, independent of the Save button. See [Activity and notifications](../activity-center/README.md).

The **Timer overlay** switch (on by default) shows the running Work Time timer as small, semi-transparent text with no background in the bottom-right corner of the work area of the display the mouse pointer is on (it follows the pointer between displays). The overlay is a click-through, non-focusable, always-on-top host window, so it never takes clicks or focus, and it is hidden while no timer is running or while the switch is off. The host refreshes the timer from the Work Time module every few seconds and ticks the clock each second. Its code is in `electron/screenOverlay.ts` and `electron/main.ts`; further indicators can be added as extra overlay lines. The switch is stored by the host with the other notification preferences, independent of the Save button.
