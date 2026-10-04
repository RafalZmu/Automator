# Options

Options manages application-wide preferences such as the opener key, theme, startup behavior, and settings backup/restore. Its Variables section edits reusable typed JSON values and shows where references can be inserted. Interpreter defaults are associated with Script Runner; saved profiles continue to own their configured interpreter paths.

Variables use references such as {{variables.name}} in supported text fields and workflow JSON pointers in workflow mappings. Values may be JSON primitives, arrays, or objects. They are included in library backup/export; credentials are not.

## Code map

- ui/App.tsx renders Options and connects settings/variable state to the bridge.
- ui/variables/GlobalVariables.tsx and GlobalVariables.css implement the Variables editor and hints.
- contracts/variables.ts validates variable names and JSON values.
- src/Automator.Application/Automation/AutomationVariableService.cs persists/migrates variables and expands supported references.
- Application preferences are owned by the desktop/backend settings services; their import/export boundaries are described in the root README.
- Tests: tests/variables-contract.test.mjs, tests/Automator.Variables.Specs, and settings/backend integration tests.

Use named API secrets for credentials. Global variables are ordinary reusable data, not a secret vault.
