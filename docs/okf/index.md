---
okf_version: "0.2"
---

# Automator contributor knowledge

Open Knowledge Format bundle for agents and people working on Automator. Read the project rules first, then the guide for the surface you are changing. Generated from `AGENTS.md` and `docs/` by `scripts/build-okf.mjs`.

# Project

* [Automator workspace instructions](/project/workspace-rules.md) - Repository-wide rules for restarting, verifying, packaging and documenting Automator changes.

# Tabs

* [API — slot 3](/tabs/api.md) - API stores reusable HTTP request profiles.
* [Browser Automation — slot 4](/tabs/browser-automation.md) - Playwright Test Explorer discovers tests in the managed project or a selected existing Playwright project.
* [Codex — slot 9](/tabs/codex.md) - The Assistant selector chooses the local CLI that drafts the task: Codex (default) or Claude Code.
* [Launcher — slot 1](/tabs/launcher.md) - The Launcher binds short letter aliases to installed apps, EXEs, and shortcuts.
* [Scheduler — slot 6](/tabs/scheduler.md) - Scheduler creates interval, daily, and weekly schedules in local time.
* [Script Runner — slot 2](/tabs/script-runner.md) - Script Runner saves reusable Python, Bash, and PowerShell scripts as profiles.
* [Website Launcher — slot 8](/tabs/website-launcher.md) - Website Launcher saves named shortcuts with a letters-only alias.
* [Work Time — slot 7](/tabs/work-time.md) - Work Time logs elapsed work intervals rather than running a Pomodoro cycle.
* [Workflows — slot 5](/tabs/workflows.md) - Workflows chain saved Script Runner, API, Browser Automation, and Codex-generated Playwright task profiles.

# Shared sections

* [Activity and Notifications](/sections/activity-center.md) - Run Activity lists recent cross-module runs with the module, action/profile, status, origin, time, and duration.
* [Agent access (MCP)](/sections/agent-access.md) - Automator runs a Model Context Protocol server inside the Electron host so Claude Code (or any MCP client) can use scripts, browser automations and Work Time without anyone starting a separate service.
* [Global Action Search](/sections/global-action-search.md) - Global Action Search appears as the default home surface when the compact launcher opens.
* [Options](/sections/options.md) - Options manages application-wide preferences such as the opener key, theme, startup behavior, and settings backup/restore.
* [Workspace window](/sections/workspace.md) - Workspace is the larger window for editing complex profiles and reviewing module data.

# Architecture

* [Bundled automation tabs](/architecture/bundled-tabs.md) - Automator tabs are compiled into the .NET backend.
* [Automator multi-layer graph](/architecture/multilayer-graph.md) - This document links a semantic view of the application to a concrete method-level execution trace.

# Directories

* [Project](project/) - Repository-wide rules every contributor and agent must follow.
* [Tabs](tabs/) - One guide per numbered workspace tab: behavior, code map, contributor rules, test cases.
* [Sections](sections/) - Guides for shared surfaces such as Options, the Workspace window and Agent access (MCP).
* [Architecture](architecture/) - How tabs register and how a request travels through renderer, Electron host and .NET backend.
