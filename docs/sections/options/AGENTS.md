# Options contributor guide

Read the root AGENTS.md first. Options owns user preferences and reusable global values; module profiles and secrets remain in their respective stores.

- Keep UI sections and tab-independent settings in the Options surface in ui/App.tsx; shared variable editing belongs in ui/variables/GlobalVariables.tsx.
- Preserve migration and validation when changing the settings or global-variable schema. Update import/export handling and migration tests in the same change.
- Store ordinary typed reusable values in the global variable service. Never move credentials into global variables; API secrets use Windows Credential Manager.
- Keep missing-path detection and relink behavior for imported app/profile data.
- Update the root README and this section's README when user-visible options or storage ownership changes.

Code: ui/App.tsx, ui/variables, contracts/variables.ts, electron/main.ts, and src/Automator.Application/Automation/AutomationVariableService.cs.
