# E2E test catalog contributor guide

Read the root AGENTS.md first. The catalog is one HTML file with its data inline; keep it that way so it opens from disk and stays shareable as a single file.

- Run `node scripts/e2e-catalog.mjs` after adding, renaming or removing any test, and commit the updated `docs/e2e/index.html`. A renamed test gets a new id and loses hand-written steps unless its `docs/tabs/<tab>/TESTS.md` entry has them.
- Write steps and an expected result for every new test, either in the tab's `TESTS.md` (preferred) or by adding them to the test's entry in the page's data block.
- Never let the generator touch `ideas`, and never reformat the data block by hand: one record per line keeps merges clean. The browser and the generator share the same serialization.
- When an idea is implemented, remove it from the page in the same change (or leave it with status accepted until the test merges) and make sure the new test appears in the list.
- Keep the scope to end-to-end and API tests; change `layerOf` and `e2eLayers` in the generator (and the page's layer list) if that definition changes.
- Categories are listed in both `scripts/e2e-catalog.mjs` and the page; add a new tab or section to both, plus a path rule.
- The page must work with no network and no build step: no external scripts, fonts or styles.

Code: `docs/e2e/index.html`, `scripts/e2e-catalog.mjs`, `tests/e2e-catalog.test.mjs`.
