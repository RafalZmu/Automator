# Script Runner — slot 2

Script Runner saves reusable Python, Bash, and PowerShell profiles. A profile has an interpreter path, script path, ordered argument values, working directory, output mode, and timeout. The interpreter path can be remembered as a default for new profiles; changing that default does not silently rewrite existing profile paths.

Each argument row is passed as one process argument, including values containing spaces. Text mode displays bounded stdout and stderr. JSON mode parses stdout as one JSON value and displays it as structured data. Scripts run as the signed-in Windows user and are not sandboxed.

## Test case steps

See [TESTS.md](TESTS.md) for template, profile, path-field, and Electron integration cases with their steps and expected results.

## Script Library templates

The Script Runner Library provides registered bundled templates that can be installed as ordinary profiles. Each template card has a primary `Add to Script Runner` action beside `Details`; after installation, the card shows `Profile added`. Open `Details` to review a template and access its interactive run form. Both Firebird 3 templates keep their `Target database` path field visible on the Library card before Details or installation, so you can choose the database while adding the template. The field value stays transient for the template run and is not saved in the profile. Other template inputs appear in the Details form; both Firebird templates default to `SYSDBA` / `masterkey`, and each password is transient and is not saved with the profile. Template runs require the Script Runner form. Workflow or Scheduler attempts to run a template profile do not have transient credentials and fail with a message to run it interactively. Editing a template profile converts it to a regular script profile.

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

- `ui/modules/ScriptRunnerView.tsx` contains profile editing, interpreter defaults, Explorer registration handoff, run/cancel, and output presentation. `scriptTemplateViewModel.ts` creates validated PowerShell profile drafts from `.ps1` Explorer requests.
- `contracts/scriptRunner.ts` validates renderer-facing profile and result shapes.
- `src/Automator.Application/Automation/ScriptRunnerModule.cs` validates profiles, expands global variables in argument values, invokes the process capability, and interprets text/JSON output.
- `ScriptRunnerSavedProfileHandler.cs` lets Workflows and Scheduler run a saved script profile.
- `src/Automator.Infrastructure/Automation/LocalProcessExecutionService.cs` owns process execution and bounded capture.
- Tests include `tests/electron/script-runner.test.cjs` and .NET Application/Infrastructure specs.

## File Explorer actions

Open **Explorer actions** in Script Runner and press **Add action**. Choose a saved profile, enter the menu label and extensions (for example `.fdb, .txt`), then save. Extensions are normalized and duplicate entries are removed. For a regular script, put `{{file.path}}` in its saved arguments first. For an installed Library template, choose which file or directory parameter receives the selection.

In the stable Windows package, right-click a matching file in File Explorer and choose **Automator → your action**. Windows 11 may place the submenu under **Show more options**. The app opens Script Runner with a review form and the selected file prefilled, including paths with spaces and Unicode. Press **Run** to execute or **Cancel** to dismiss it. Regular arguments can be edited for that one run; template fields use typed inputs and transient credentials. Neither is saved to the profile. The backend checks the selected existing absolute file and extension again when Run is pressed.

Mappings can be edited or removed individually. **Remove all Explorer actions** clears the mappings and owned menu entries; deleting a saved profile removes its mappings. A registration error leaves the saved mappings visible; reopen the section to retry. Registration is disabled in development, portable, and test-mode hosts. Automator only writes its owned per-user menu keys and never changes the default file association.

In the stable Windows package, the `.ps1` **Automator** submenu also contains **Register in Automator**, even when no run actions are mapped. It opens a new PowerShell profile draft with the script path, filename-based name, and containing folder filled in. Review the interpreter and other fields, then choose **Save profile** to add it to Script Runner; the script is not run by registration or saving. Cancel leaves the Library unchanged.

The Library includes **Firebird 3 database backup** and **Firebird 3 database backup and ZIP**. Install either template and map it yourself; neither action is enabled automatically. Both offer the target database on the Library card, with backup destinations, `gbak`, username, and password in their typed run forms. V1 handles one selected file at a time.

Explorer action UI lives in `ui/modules/ExplorerActionsPanel.tsx`; `explorerActionViewModel.ts` prepares transient forms. `ui/App.tsx` retains typed preload launch requests before the Script Runner view mounts. `ScriptRunnerExplorerActions.cs` stores and validates mappings/runs, while the Windows capability reconciles the classic cascading menu.
