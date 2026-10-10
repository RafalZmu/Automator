# Website Launcher contributor guide

Read the root `AGENTS.md` first. Website Launcher stores named website sets in module settings and uses a module-scoped host capability to open them through the Windows default browser.

## Preserve the launch contract

- Keep saved row aliases letters-only and unique among Website Launcher rows; they may match an application alias. Tab search uses the row alias as entered. Global Action Search uses the alias plus `w`.
- Keep each row's groups and each group's websites ordered. Validate row, group, website IDs, names, counts, and absolute HTTP/HTTPS URLs in both the renderer contract and host module before launch.
- Route browser opening through `IAutomationWebsiteLauncher` and the `website.launch` capability. Do not shell-concatenate URLs or use renderer-side process/network APIs.
- For direct Chromium and Firefox launches, redirect and asynchronously drain/discard stdout and stderr through child exit. Do not wait for the browser process lifetime before returning the launch request. Keep the default URL-handler fallback on `UseShellExecute = true`.
- Opening groups through the Windows default URL handler is best-effort; do not promise the browser will create or reuse exact windows/tabs.
- Keep editing/saving in module settings and launch actions through the active module dispatcher. Global search routes to the module's live command.

## Code and checks

UI: `ui/modules/WebsiteLauncherView.tsx` and `WebsiteLauncherView.css`; contract: `ui/contracts/websiteLauncher.ts`.
Module: `src/Automator.Application/Automation/WebsiteLauncherModule.cs`; Windows adapter: `src/Automator.Windows/WindowsAutomationWebsiteLauncher.cs`.
Tests: `tests/website-launcher-contract.test.mjs`, Application specs, and Windows specs.

Update this guide and the sibling README when shortcut matching, grouping, settings, or browser launch behavior changes.
