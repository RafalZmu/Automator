# Script Runner — slot 2

Script Runner saves reusable Python, Bash, and PowerShell profiles. A profile has an interpreter path, script path, ordered argument values, working directory, output mode, and timeout. The interpreter path can be remembered as a default for new profiles; changing that default does not silently rewrite existing profile paths.

Each argument row is passed as one process argument, including values containing spaces. Text mode displays bounded stdout and stderr. JSON mode parses stdout as one JSON value and displays it as structured data. Scripts run as the signed-in Windows user and are not sandboxed.

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
