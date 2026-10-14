# Script Runner — slot 2

Script Runner saves reusable Python, Bash, and PowerShell scripts as profiles. A profile has an interpreter path, script path, optional fixed arguments, working directory, output mode, timeout, and an ordered list of typed inputs.

Inputs are declared in the profile editor (Inputs section) with a label, a type (Text, File, Folder, Yes or no, or Choice), an optional default, Required, an optional hint, and an optional example. The hint appears next to the label in the Run form; the example appears as faint placeholder text in the empty box and is never submitted. Bundled templates declare hints and examples for their inputs too. Each input becomes one argument, in list order, after the fixed arguments. Running a script with inputs opens a Run form that uses the same typed fields as the Library. Remember these values stores the non-sensitive values on the profile, so the next Run form and Workflow or Scheduler runs use them. Passwords are never remembered. Scripts without inputs run directly from their row. The interpreter path can be remembered as a default for new profiles; changing that default does not silently rewrite existing profile paths.

Each argument row is passed as one process argument, including values containing spaces. Text mode displays bounded stdout and stderr. JSON mode parses stdout as one JSON value and displays it as structured data. Scripts run as the signed-in Windows user and are not sandboxed.

The Codex task builder can save a successfully run Python task as a normal Script Runner profile. Codex approval metadata ties the saved task to the reviewed source and scope; changed task source or declared review details must be reviewed and reapproved before manual, Workflow, or Scheduler execution. Scope is consent context and does not limit the script's Windows-user permissions. See [Codex](../codex/README.md) for the authoring and review flow.

## Test case steps

See [TESTS.md](TESTS.md) for template, profile, path-field, and Electron integration cases with their steps and expected results.

## Script Library templates

The Script Runner Library provides registered bundled templates that can be installed as ordinary profiles. Each template card has a primary `Add to Script Runner` action beside `Details`; after installation, the card shows `Profile added`. Open `Details` on a template that is not installed to review its inputs and add it with the values you entered. For an installed template, `Details` opens the same Run form as its profile row. Both Firebird 3 templates keep their `Target database` path field visible on the Library card before Details or installation, so you can choose the database while adding the template. The field value stays transient for the template run and is not saved in the profile. Other template inputs appear in the Details form; both Firebird templates default to `SYSDBA` / `masterkey`, and each password is transient and is not saved with the profile. Template runs require the Script Runner form. Workflow or Scheduler attempts to run a template profile do not have transient credentials and fail with a message to run it interactively. Editing a library profile changes only its name and shows its inputs read-only. Make an editable copy turns it into your own script with the same inputs; templates with a password cannot be copied, because passwords are never saved.

File and directory fields accept a pasted or typed path, Browse, or one dropped filesystem item. Dropped items resolve to a local path; Automator does not read their contents. File and directory inputs require fully qualified paths; output destinations need not exist yet. The Firebird `gbak` executable and ZIP destinations are entered as paths in the template form. The password stays out of saved profiles, settings, and logs. During execution the local process command line may expose the credentials to other processes running as the same user; this visibility is accepted for the initial template.

Templates are bundled trusted code and run unsandboxed with the same Windows-user access as Automator. Review a template and trust its source before running it. The shared path field is currently reused in Script Runner only; other tabs can adopt it in a later focused change with their own authorization and docs.

The Library also includes **Archive stale .fdb databases**. Set a source folder and a number of days. Each `.fdb` under the folder (and its subfolders, unless Include subfolders is turned off) that has not been accessed for that many days is zipped beside it as `<name>.fdb.zip`, or as `<name>.fdb.<yyyyMMdd_HHmmss>.zip` when that name is already taken. The archive is checked against the original by SHA-256 before the original is deleted. Locked files are skipped and reported. **Preview only** is on by default and changes nothing; turn it off in the form to archive. Template profiles run only from the Script Runner form, so Scheduler and Workflows cannot run this template. For unattended runs use `scripts/Backup-StaleFdb.ps1` with Task Scheduler. Last access times are only meaningful when NTFS last access updates are enabled.

## Creating scripts that work well here

- Use .py with the configured Python executable, .sh with a configured Bash executable such as Git Bash, and .ps1 with PowerShell.
- Put every argument on its own line in the profile. Do not include shell quotes just to preserve spaces; Automator passes each line as one argument.
- Set the working directory explicitly if the script reads or writes relative paths.
- Keep stdout clean in JSON mode: emit one JSON document and send diagnostics to stderr. In text mode, stdout and stderr are shown separately.
- Validate arguments, avoid interactive prompts, return a meaningful nonzero exit code on failure, and keep expected runtime within the profile timeout.
- Treat scripts as trusted code with the same access as Automator. Never place credentials in source files, arguments, output, or logs.

## Running state and run history

A run belongs to the window, not to the Script Runner view. Start a script from the compact window's action search, close the window, and reopen it: the script keeps running, a **Running** banner with elapsed time and Cancel appears at the top of Script Runner, and a pill with the script name shows in the header on every screen (click it to jump to Script Runner). Only one script runs at a time.

Finished runs appear in **Run history** below the result: status (Succeeded, Failed, Timed out, Canceled, Error), start time, and duration, newest first, up to 30. Select an entry to read its output as a past result; **Clear history** removes finished entries. File Explorer action runs are listed too. History and output are memory-only in the window that ran the script and are cleared when Automator restarts; the Run activity list keeps permanent metadata without output. The compact launcher and the Workspace window each keep their own history.

## Removing profiles

The Remove button on a profile row uses a two-step inline confirmation: the first click shows Yes and Cancel, Escape or Cancel dismisses them, and they reset after six seconds. The command-palette delete keeps its own confirmation prompt.

## Code map

- `ui/modules/scriptRunStore.ts` owns running and finished runs for the window (memory only); `tests/script-run-store.test.mjs` covers it.
- `ui/modules/ScriptRunnerView.tsx` contains profile editing, interpreter defaults, Explorer registration handoff, run/cancel, and output presentation. `scriptTemplateViewModel.ts` creates validated PowerShell profile drafts from `.ps1` Explorer requests.
- `contracts/scriptRunner.ts` validates renderer-facing profile and result shapes.
- `src/Automator.Application/Automation/ScriptRunnerModule.cs` validates profiles, expands global variables in argument values, invokes the process capability, and interprets text/JSON output.
- `ScriptRunnerExecution.cs` provides the shared process path used by interactive profiles and Codex-generated Python drafts.
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

## Agent access

Saved scripts can be listed, inspected (typed inputs with hints and examples), run, polled and canceled by an MCP client through `list_scripts`, `get_script`, `run_script`, `get_run` and `cancel_run`. Runs use the same `runProfile` action and validation as this tab. See [Agent access](../../sections/agent-access/README.md).
