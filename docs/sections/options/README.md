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

The **Timer overlay** switch (on by default) shows the running Work Time timer as large, slightly transparent, click-through text with a soft halo and no background. The overlay is a transparent, non-focusable, always-on-top host window sized to its measured text and anchored by its bottom-right corner. By default that corner is the bottom-right of the work area of the primary display; it does not follow the mouse between displays. It is hidden while no timer is running or while the switch is off. The host refreshes the timer from the Work Time module every few seconds and ticks the clock each second. Its code is in `electron/screenOverlay.ts` and `electron/main.ts`; further indicators can be added as extra overlay lines. The **Timer overlay colour** picker sets the text colour only (default white; the halo flips between dark and light with the colour's brightness). **Timer overlay position** has **Move** and **Reset**: Move makes the overlay draggable (dashed outline; it shows a sample line when no timer runs) until **Done** is pressed, Settings is closed, the overlay switch is turned off, or 60 seconds pass without a drag (the host tells the open Settings so the button returns to Move), then it is click-through again; **Reset** moves the overlay to the default corner immediately, even in move mode; each drag saves the bottom-right corner in screen coordinates, reused while it is still on a visible display (it may sit over the taskbar; otherwise the default applies). Reset returns to the default. The switch, colour and position are stored by the host with the other notification preferences in `notifications.json`, apply immediately, and are independent of the Save button.
