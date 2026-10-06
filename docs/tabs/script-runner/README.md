# Script Runner — slot 2

Script Runner saves reusable Python, Bash, and PowerShell profiles. A profile has an interpreter path, script path, ordered argument values, working directory, output mode, and timeout. The interpreter path can be remembered as a default for new profiles; changing that default does not silently rewrite existing profile paths.

Each argument row is passed as one process argument, including values containing spaces. Text mode displays bounded stdout and stderr. JSON mode parses stdout as one JSON value and displays it as structured data. Scripts run as the signed-in Windows user and are not sandboxed.

## Script Library templates

The Script Runner Library provides registered bundled templates that can be installed as ordinary profiles. Template forms collect typed inputs for an interactive run; the Firebird 3 backup template defaults to `SYSDBA` / `masterkey`, and its password is transient and is not saved with the profile. Template runs require the Script Runner form. Workflow or Scheduler attempts to run a template profile do not have transient credentials and fail with a message to run it interactively. Editing a template profile converts it to a regular script profile.

File and directory fields accept a pasted or typed path, Browse, or one dropped filesystem item. Dropped items resolve to a local path; Automator does not read their contents. File and directory inputs require fully qualified paths; output destinations need not exist yet. The Firebird `gbak` executable and ZIP destinations are entered as paths in the template form. The password stays out of saved profiles, settings, and logs. During execution the local process command line may expose the credentials to other processes running as the same user; this visibility is accepted for the initial template.

Templates are bundled trusted code and run unsandboxed with the same Windows-user access as Automator. Review a template and trust its source before running it. The shared path field is currently reused in Script Runner only; other tabs can adopt it in a later focused change with their own authorization and docs.

## Creating scripts that work well here

- Use .py with the configured Python executable, .sh with a configured Bash executable such as Git Bash, and .ps1 with PowerShell.
- Put every argument on its own line in the profile. Do not include shell quotes just to preserve spaces; Automator passes each line as one argument.
- Set the working directory explicitly if the script reads or writes relative paths.
- Keep stdout clean in JSON mode: emit one JSON document and send diagnostics to stderr. In text mode, stdout and stderr are shown separately.
- Validate arguments, avoid interactive prompts, return a meaningful nonzero exit code on failure, and keep expected runtime within the profile timeout.
- Treat scripts as trusted code with the same access as Automator. Never place credentials in source files, arguments, output, or logs.

## Code map

- `ui/modules/ScriptRunnerView.tsx` contains profile editing, interpreter defaults, run/cancel, and output presentation.
- `contracts/scriptRunner.ts` validates renderer-facing profile and result shapes.
- `src/Automator.Application/Automation/ScriptRunnerModule.cs` validates profiles, expands global variables in argument values, invokes the process capability, and interprets text/JSON output.
- `ScriptRunnerSavedProfileHandler.cs` lets Workflows and Scheduler run a saved script profile.
- `src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs` owns process execution and bounded capture.
- Tests include `tests/electron/script-runner.test.cjs` and .NET Application/Infrastructure specs.
