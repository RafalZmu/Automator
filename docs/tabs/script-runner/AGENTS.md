# Script Runner contributor guide

Read the root AGENTS.md first. Script Runner executes saved, trusted user scripts as the current Windows user. It is not a sandbox.

## Adding a profile or script capability

- Keep profile validation and execution in `src/Automator.Application/Automation/ScriptRunnerModule.cs` and `ScriptRunnerSavedProfileHandler.cs`; renderer code must call the module through AutomationServices.
- Preserve structured process arguments. The UI stores one argument per line, and each line is one argument value. Never build a shell command by concatenating profile fields.
- The accepted interpreter/script pairs are Python with .py, Bash with .sh, and PowerShell with .ps1. PowerShell is invoked non-interactively with -NoProfile; Bash should use the configured executable, such as Git Bash.
- Interpreter defaults apply when creating or changing a profile. A saved profile retains its own interpreter path. All paths are absolute; working directory, per-argument limits, and the 1–3600 second timeout are validated.
- Script processes must not require interactive prompts. Exit code zero means success; nonzero exits and timeouts must remain visible as failures/warnings. Keep stdout/stderr bounded.
- For JSON output mode, stdout must contain exactly one valid JSON document. Send progress and diagnostics to stderr so they do not corrupt JSON parsing.
- Global variable references in argument values use the form {{variables.name}}. Do not interpolate arbitrary environment variables or commands.
- Do not put secrets, script output, or response data in application logs. Do not automatically execute follow-up actions from script-provided data; actions must be declared and validated by the Automator module and require a user click.
- Bundled template origins are valid only for the exact registered version, installed script path, and embedded asset bytes. Do not preserve template origin when converting a library profile into an ordinary edited script.
- Firebird template credentials are transient form values and must never be saved to profile arguments. Template profiles require an interactive Script Runner run; Workflow/Scheduler saved-profile calls have no transient inputs and must fail clearly without starting the script.
- Keep the Firebird `Target database` path field visible on its Library card before Details or installation; carry its value as a transient template input and never add it to saved profile arguments.
- Keep a distinct primary `Add to Script Runner` action directly on every Library template card beside Details. Show `Profile added` for an installed template; Details must still open its interactive form for that saved profile.
- File and directory template inputs must be fully qualified paths, but may name output paths that do not exist yet.
- Use the shared `ui/components/PathField.tsx` for Script Runner script paths, working directories, and template file/directory inputs. It supports typed/pasted paths, an app-owned Browse callback, and one dropped filesystem item resolved in preload through Electron `webUtils.getPathForFile`; it must never read dropped file contents.
- Keep path picker IPC limited to the active Script Runner tab and the allowlisted `file`/`directory` picker kinds. Native dialog options remain host controlled. Browser preview has no local filesystem and should show a recoverable path-resolution message for dropped items.
- The path field is a reusable UI component only. Do not migrate unrelated tabs or add a generic cross-tab file capability without updating their own contributor guides and authorization boundaries.

## Script authoring guidance

Write scripts to accept explicit arguments, use the configured working directory rather than assuming Automator's current directory, validate inputs, and exit promptly. Python scripts should write machine-readable JSON with the standard JSON library; PowerShell scripts should emit one JSON value in JSON mode; Bash scripts should use the configured Bash-compatible syntax and avoid interactive shell state. Test scripts should use isolated temporary files and clean them up.

## Code and checks

UI: `ui/modules/ScriptRunnerView.tsx`; DTOs: `contracts/scriptRunner.ts`; module and saved-profile runner: `src/Automator.Application/Automation/ScriptRunnerModule.cs` and `ScriptRunnerSavedProfileHandler.cs`; process adapter: `src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs`.
Tests: `tests/electron/script-runner.test.cjs` and the Application/Infrastructure specification projects.

Update the sibling README when supported profile fields or script output behavior changes.

## File Explorer actions

- Explorer mappings live in the Script Runner Library collection `explorer-actions`, separate from profiles and settings. Use the version-1 `listExplorerActions`, `saveExplorerAction`, `deleteExplorerAction`, and `runExplorerAction` module actions; renderer code must never write the Windows registry.
- Regular mapped profiles must contain the literal `{{file.path}}` token in saved arguments. Display a resolved copy for one run; never save argument edits made in the Explorer form. Template mappings choose one declared file/directory input, and all input values stay transient.
- Subscribe to `onFileExplorerLaunch` at App level before reporting renderer readiness. Keep queued requests while the view mounts or another Explorer form is open; Cancel performs no run, and only explicit Run dispatches the constrained run action.
- Re-read mappings/profiles/catalog when opening a launch form. The backend revalidates mapping/profile/template origin, absolute existing file, extension, and inputs immediately before execution. Display stale-definition and invalid-path errors.
- Menu registration is a narrow `IAutomationFileExplorerMenu` host capability. Only the stable packaged Windows host supplies an executable path. Development, portable, and test-mode runs must leave the real per-user registry untouched. Reconcile only owned menu keys and preserve file associations and unrelated keys.
- Mapping changes remain saved when registration returns `error` or `disabled`; display the outcome and retry reconciliation when reopening the section. Remove all mappings through the declared delete action to remove the owned submenu entries as well. Deleting a saved profile removes its mappings.
- V1 supplies one selected file. Windows 11 can show the classic Automator submenu under Show more options. Both Firebird Backup and Backup and ZIP are Library opt-ins and must not be installed or mapped automatically.
- Stable packaged Windows startup also registers **Automator → Register in Automator** for `.ps1` files, even when no run actions are mapped. The fixed `register-powershell-script` launch ID is reserved; selecting it opens an editable PowerShell profile draft with the filename and containing folder filled in. Saving is explicit, and registration must never execute the script.
- Keep PowerShell registration handoff constrained to absolute `.ps1` paths in the Electron parser and typed RPC schema. Draft naming and unique profile IDs belong in `scriptTemplateViewModel.ts`; do not persist a profile until the user submits **Save profile**.

Explorer UI: `ui/modules/ExplorerActionsPanel.tsx` and `explorerActionViewModel.ts`; launch queue/context: `ui/App.tsx` and `ui/FileExplorerLaunchContext.ts`; registry adapter: `src/Automator.Windows/WindowsAutomationFileExplorerMenu.cs`. Update this guide, README, and TESTS together when these boundaries change.

PathField uses a React per-instance ID so multiple Library cards and transient forms can reuse the same label without associating it with another input. Preserve that label/control association when changing shared fields.
