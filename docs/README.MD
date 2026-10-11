# Automator project and impact graph

This document explains what the project graph is intended to show and how the source-mapped Obsidian graph was built. It is designed to help answer a practical question: if a module, file, or function changes, what other parts of Automator may be affected?

## What the graph should show

The graph has three connected areas:

1. **Application and module area.** A curated, high-level map of the product: the application and user, desktop shell and renderer, tabs and shared surfaces, backend modules and services, persistence, and platform adapters. Edges describe the important interactions between those concepts.
2. **Source file area.** Each indexed source file is a node. It links back to the application or module area that owns it, and to files that depend on it through imports or type relationships. This keeps the code map grounded in the product structure.
3. **Function and method area.** Each indexed function or method is a node under its source file. Call relationships connect the function being depended on to its callers, so following edges outward shows code that may need attention when that function changes.

Ownership links connect the areas. Dependency and caller links point from a changed dependency toward its consumers. In Obsidian, enable arrows to see this direction. A graph path is evidence for a possible impact, not a guarantee that every behavior affected by a change has been found.

## How the graph was built

The implementation combines a manually curated application map with generated source relationships:

- The curated model describes application concepts, module boundaries, and reviewed relationships.
- The source index scans selected tracked application files. TypeScript and TSX relationships are extracted from TypeScript source information; C# relationships are extracted with Roslyn.
- Stable node IDs, source paths, and line anchors connect indexed symbols to the code they came from.
- A generator merges the curated model and source index, then writes Obsidian Markdown notes with links and tags. File notes point to their owning modules; function notes point to their files and callers.
- Tags identify the graph layer, application area, node kind, language, and test status. Obsidian's Graph View can filter or color nodes using these tags.

At the initial generated snapshot, the source index covered 157 selected application source files, 3,293 functions and methods, and 8,446 source-derived relationships. The merged graph contained 3,482 nodes and 12,040 edges. These counts change when the source index or curated ownership model changes.

Generated graph data lives under `architecture/impact-graph/`: `model.json` is the curated model, `source-index.json` is the source scan, `nodes/` contains Obsidian notes, and `reports/` contains saved impact reports.

## Browse it in Obsidian

Open the repository's `docs` folder as the Obsidian vault. Use Graph View filters and groups with the tags `#impact-graph/module`, `#impact-graph/file`, and `#impact-graph/function` to show each area. Area and language tags can narrow the view further. The graph-specific README has ready-to-use filters, a Browser Automation example, and notes about arrows and local graphs: [Automator impact graph](architecture/impact-graph/README.md).

The curated architecture overview also includes a product-level diagram and a representative Browser Automation execution trace: [Automator multi-layer graph](architecture/automator-multilayer-graph.md).

## Generate and query impact

Run these commands from the repository root:

    node scripts/impact-graph.mjs generate
    node scripts/impact-graph.mjs validate
    node scripts/impact-graph.mjs list module
    node scripts/impact-graph.mjs list file
    node scripts/impact-graph.mjs list function
    node scripts/impact-graph.mjs impact browser
    node scripts/impact-graph.mjs impact ui/modules/ApiView.tsx
    node scripts/impact-graph.mjs impact Qualified.Type.method

The impact command accepts a stable node ID, an exact source file path, or a unique qualified function name. It reports direct and transitive dependents with relationship labels and source evidence. Add `--note` to write the result as a tagged Obsidian report. The validator checks graph references, source anchors, and generated-note consistency without rewriting notes.

## Coverage and limits

Static source analysis cannot reliably capture every runtime relationship. Reflection, event subscriptions, dynamic dispatch, generated code, runtime configuration, and behavior without a resolvable source edge may be absent. Treat the graph and impact report as a navigable map of recorded dependencies; review the source and tests before deciding the full scope of a change.

When code ownership or dependency rules change, update the curated model or ownership rules, regenerate the graph, and run the validator. See the [impact graph README](architecture/impact-graph/README.md) for more details.
