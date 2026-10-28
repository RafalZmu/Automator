# Launcher — slot 1

The Launcher binds short letter aliases to installed apps, EXEs, and shortcuts. It can minimize the foreground app, restore/maximize an app already running, or launch and maximize a new app. The app catalog is opened with /; aliases are edited on app rows.

When the compact panel opens, its home surface is Global Action Search. That surface searches actions from all tabs and does not consume a numbered slot. Typing a unique command runs it; selecting a tab-specific result routes to that tab. The normal app-alias flow remains available on slot 1.

While a text-like field (text input, search, number, textarea, select, or contenteditable) has focus in the open launcher, digits 1–9 and `/` type into that field instead of selecting a tab or opening the catalog. Escape still closes the panel. Up and Down (row switching, only when there is more than one row of tabs) also yield to text fields, except an empty launcher, Quick Actions or tab search box. The renderer reports text-entry focus to the backend through `launcher/setWindowContext` (`textEntryFocused`), and the native keyboard hook uses it; buttons and checkboxes do not change the behavior.

The compact launcher uses a fixed 760×800 preferred window size and clamps to the selected display's work area when needed. Each time the panel opens, it is placed centered horizontally under the pointer on the display the pointer is on. Moving the pointer while the panel is open does not move it. The active tab view scrolls vertically; the topbar, tab selector, command bar, and footer stay in place.

## Test case steps

See [TESTS.md](TESTS.md) for launcher, host, platform, and end-to-end cases with their steps and expected results.

## Code map


The Quick Actions home also lists the open tasks from Quick notes (every unticked markdown checkbox, oldest note first) in the lower half of the panel, grouped under the title of their note; the list is hidden when there are none, and selecting a task opens the Quick notes tab. The backend publishes the tasks as the `openTasks` value of the `notes` module state (`NotesModule.CreateOpenTaskStateValue`), refreshed at start and after every note save or delete, and cut to fit one state value with the full count kept.

- `ui/App.tsx` composes launcher state, tabs, alias entry, and the Quick Actions home.
- `ui/commands/QuickActionsHome.tsx` renders the global action-search home; shared command matching and registration live under `ui/commands`.
- `ui/viewRegistry.tsx` connects tab IDs to renderer views.
- `electron/main.ts` owns the compact window, tray, opener, and foreground handoff.
- `src/Automator.Application/Launcher` contains launcher state and app-binding rules; `src/Automator.Windows` provides Windows discovery and activation.
- The key integration tests are under `tests/electron`.

See `docs/sections/global-action-search/README.md` for command registration details.
