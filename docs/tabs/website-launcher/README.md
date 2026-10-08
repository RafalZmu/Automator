# Website Launcher — slot 8

Website Launcher saves named shortcuts with a letters-only alias. Each shortcut contains ordered browser groups; each group contains ordered websites. From this tab, users type the alias into its action search. In Global Action Search they append `w` to disambiguate the website shortcut from an application with the same alias, for example `docsw`.

Launch requests go through the active `website-launcher` module action and the host-owned `website.launch` capability. The Windows adapter opens HTTP(S) URLs through the system default browser in group and website order. The browser controls whether URLs reuse an existing window or open new tabs, so exact window grouping is best-effort.

## Code map

- `ui/modules/WebsiteLauncherView.tsx` edits shortcuts, browser groups, and website tabs; `WebsiteLauncherView.css` styles the editor.
- `ui/contracts/websiteLauncher.ts` validates persisted website shortcut data and aliases.
- `src/Automator.Application/Automation/WebsiteLauncherModule.cs` validates module settings and dispatches launch requests.
- `src/Automator.Windows/WindowsAutomationWebsiteLauncher.cs` delegates URL opening to the Windows default handler.
- `ui/App.tsx` makes saved aliases available to Global Action Search.
- Tests: `tests/website-launcher-contract.test.mjs`, `tests/Automator.Application.Specs`, and `tests/Automator.Windows.Specs`.

Website URLs are restricted to absolute HTTP and HTTPS addresses and are not fetched by Automator.

## Test case steps

See [TESTS.md](TESTS.md) for website settings, alias, command-matching, and launch-order test steps.
