# E2E test catalog

`docs/e2e/index.html` is a single self-contained page that lists every automated test in the repository by category, and holds the team's ideas for tests that do not exist yet. Open it directly in a browser (double-click the file); it needs no server and no network.

## What it shows

- **Implemented tests** (generated): title, category, layer (Electron UI, Contract / model, .NET spec, PowerShell spec, Packaged app), source file and line, steps and expected result.
- **Ideas** (hand-added, tagged `idea`): title, category, layer, priority, steps, expected result, author, date and a status (new, accepted, rejected).
- Search across all text, filter by category, layer and kind (all, implemented, ideas), and **Copy ideas as Markdown** for handing ideas to someone who will implement them.

## How ideas are stored

Ideas live inside the page's `e2e-data` JSON block, one record per line, so they are shared by committing the file. In the browser, **Add test idea** keeps the idea as an unsaved draft in that browser (local storage) and shows a banner. **Save to file** (Chrome or Edge) writes the page back in place when you pick `docs/e2e/index.html`; other browsers download a copy to move over the original. Commit the changed file to share the ideas. Two people adding ideas on separate branches merge cleanly unless they edit the same idea.

## How the test list is generated

`node scripts/e2e-catalog.mjs` (`npm.cmd run e2e:catalog`) scans `tests/` and rewrites only the `tests` part of the data block:

- JavaScript tests: `test(` and `it(` titles in `tests/**/*.{cjs,mjs}`.
- .NET specs: the `("title", Method)` entries in each `tests/Automator.*.Specs/Program.cs`.
- PowerShell specs: one entry per `tests/*.spec.ps1`.
- Steps and expected results: taken from `docs/tabs/<tab>/TESTS.md` when a `###` heading matches the test title; otherwise kept from the previous version of the page, matched by test id (a hash of file and title). `node scripts/e2e-catalog.mjs --missing out.json` lists tests that still lack either.
- Category: the tab whose `TESTS.md` documents the test (tests documented in several tabs go to Platform and shell), otherwise the path and title rules at the top of `scripts/e2e-catalog.mjs`.

`npm.cmd run e2e:catalog:check` fails when the page differs from the source tests. It is not part of `verify`; run the generator after adding or renaming tests.

Code: `docs/e2e/index.html`, `scripts/e2e-catalog.mjs`, `tests/e2e-catalog.test.mjs`.
