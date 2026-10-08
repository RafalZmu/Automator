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
