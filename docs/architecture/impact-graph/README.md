# Automator impact graph

This Obsidian graph combines the curated application map with generated source files and functions. It answers: **if this module, file, or function changes, which recorded dependents may be affected?** The graph complements the [Automator multi-layer graph](../automator-multilayer-graph.md).

## Browse the graph in Obsidian

The `docs` folder is the vault root. Open **Graph view → Filters → Search files** and use these searches; add the same searches under **Groups** to color each layer or concern:

| View | Search files / Groups filter |
| --- | --- |
| Curated modules | `path:architecture/impact-graph/nodes tag:#impact-graph/module` |
| Source files | `path:architecture/impact-graph/nodes tag:#impact-graph/file` |
| Functions | `path:architecture/impact-graph/nodes tag:#impact-graph/function` |
| TypeScript files/functions | `path:architecture/impact-graph/nodes tag:#impact-graph/language/typescript` |
| TSX files/functions | `path:architecture/impact-graph/nodes tag:#impact-graph/language/tsx` |
| C# files/functions | `path:architecture/impact-graph/nodes tag:#impact-graph/language/csharp` |
| Application tests | `path:architecture/impact-graph/nodes tag:#impact-graph/test` |
| Browser area | `path:architecture/impact-graph/nodes tag:#impact-graph/area/browser` |
| Workflows area | `path:architecture/impact-graph/nodes tag:#impact-graph/area/workflows` |
| Runtime area | `path:architecture/impact-graph/nodes tag:#impact-graph/area/runtime` |

Every node also has `impact-graph/kind/<kind>` and area tags based on reviewed module ownership. Enable **Display → Arrows** to see edge direction. Clear filters before opening **Local Graph** from a module to follow its owned files and functions. Use the **Backlinks** panel for incoming links: these are consumers/callers pointing toward a dependency. For a curated trace, start at the [Browser Automation module](nodes/browser.md) and follow its linked source nodes.

## Query impact

Run commands from the repository root:

```powershell
node .\scripts\impact-graph.mjs list
node .\scripts\impact-graph.mjs list file
node .\scripts\impact-graph.mjs impact browser
node .\scripts\impact-graph.mjs impact ui/modules/ApiView.tsx
node .\scripts\impact-graph.mjs impact Qualified.Type.method
node .\scripts\impact-graph.mjs impact browser --note
node .\scripts\impact-graph.mjs generate
node .\scripts\impact-graph.mjs validate
```

`impact` accepts a node ID, exact source file path, or unique qualified function name. Exact file paths take precedence over titles. Use a function's stable ID when a qualified name is ambiguous; the command reports matching IDs. `list` accepts `module`, `file`, or `function` to filter layers.

Reports group direct and transitive dependents and show the shortest recorded edge path, relationship labels, and source evidence. A module query expands its owned files and functions before following impact edges. A file query follows import/type-consumer relationships and callers of functions declared in that file. A function query follows caller edges. `contains` edges expand module/file ownership but are never reported as impact dependencies. `--note` writes an Obsidian report tagged with its source layer, source kind, and application area; an existing unowned report is preserved and causes an error.

## Coverage and limits

The graph combines manually curated module dependencies with compiler/Roslyn source relationships. Static analysis can miss reflection, event subscriptions, dynamic dispatch, callbacks, generated code, runtime configuration, and behavioral effects with no resolvable source edge. Treat the report as evidence about recorded dependencies, not a guarantee that every affected behavior is listed. Scanner diagnostics describe source-index coverage and are not themselves graph validation errors.

When ownership or dependencies change, update the curated model or ownership rules and regenerate:

```powershell
node .\scripts\impact-graph.mjs generate
node .\scripts\impact-graph.mjs validate
```

The validator is read-only. It checks merged edge endpoints, source anchors, required generated node notes, generator ownership, and stale owned notes/reports without rewriting or deleting them. See Obsidian's [Graph view guide](https://obsidian.md/help/plugins/graph) for filters, groups, arrows, and local graph controls.
