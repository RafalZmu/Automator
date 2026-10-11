# Source-mapped three-area impact graph

## Purpose

Make Automator's architecture navigable as one Obsidian graph that can answer both questions: “what is this code part of?” and “what may be affected if I change it?” The graph has three connected areas: application concepts, source files, and functions or methods.

## Approved structure

### Area 1: application concepts

Keep the current curated module map as the graph's architectural overview: application surfaces such as tabs, renderer and backend components, services, contracts, and persistence adapters. Add an explicit user/actor node connected to the surfaces the user interacts with, and add a SQLite database node connected to the library-store adapter. Preserve curated relationships and their source evidence. Area 1 remains human-reviewed because architectural ownership and interactions cannot be inferred reliably from syntax alone.

### Area 2: source files

Generate one Obsidian note for each tracked application source file with a `.cs`, `.ts`, or `.tsx` extension. The current tracked scope is 157 files (104 C#, 34 TypeScript, and 19 TSX), including tests. Do not index generated build output, dependencies, or binary files.

Each file note links to its owning Area 1 concept or concepts, lists its source path, and links to files it depends on. Ownership is determined by a reviewable mapping based on project and feature boundaries, with explicit overrides for shared files. Files that cannot be mapped receive an `unassigned` tag and a generator warning so they are not silently assigned to the wrong module. File relationships distinguish imports/project references from dependencies inferred through function usage.

### Area 3: functions and methods

Generate one note for each discovered source callable: C# methods, local functions, constructors, and lambdas; TypeScript/TSX function declarations, methods, function expressions, and arrow functions. Anonymous callables receive a readable label based on their file and source line. Each note links to its containing file and records a qualified name, signature where available, and source line. Calls are linked by symbol identity when the language tooling resolves them. A function note shows its callers as “used by” connections; its containing file and Area 1 tags remain visible for context.

Do not invent links for reflection, computed or dynamic invocation, event wiring, dependency injection, or callbacks that static analysis cannot resolve. Record unresolved call sites as limited-coverage diagnostics, and keep manually curated cross-boundary relationships with their evidence.

## Obsidian presentation

Keep the existing module graph notes and generator. Store generated file notes and function notes in their own subfolders under `docs/architecture/impact-graph/nodes/`, preserving the current module-note paths. Add frontmatter tags for graph layer (`impact-graph/module`, `impact-graph/file`, `impact-graph/function`), architectural area (`impact-graph/area/<id>`), language, and test sources. The guide will show Graph Search and Groups queries for each layer and area, plus how to use a function's backlinks to inspect its callers.

Generated notes are owned by the graph generator. Regeneration must be deterministic, validate references and ownership, and remove stale generated notes only when they carry the generator marker. It must refuse to overwrite unowned notes. Keep the curated Area 1 model separate from the generated source index so normal source changes do not rewrite reviewed architecture edges.

## Relationships and change impact

Keep curated module-to-module impact edges pointing from a changed dependency to a consumer. Add explicit module-to-file ownership and file-to-function declaration relationships. For source relationships, record the dependency direction needed for navigation and impact traversal: a changed file or function points toward its consumers/callers. Preserve source path and line evidence on generated edges.

Extend the impact command to accept module, file, or function nodes. A module query can expand through its owned files and declared functions before following dependency edges. A file or function query follows its recorded consumers/callers. Reports include direct and transitive nodes, the shortest recorded path, and source evidence. Labels and documentation must describe results as recorded possible impacts, not a proof of every runtime effect.

## Indexing approach

Use the repository's TypeScript compiler API for TypeScript and TSX syntax and symbol resolution. Use Roslyn for C# declarations and call sites, loading project boundaries and references where available so calls can be resolved to symbols. Do not create cross-file call links from same-name guesses when semantic resolution fails; report those call sites as unresolved. Keep the indexer independent of Automator runtime behavior; it reads source files and project metadata and writes the graph's generated index and notes.

Use stable IDs derived from normalized source paths and qualified symbol identities/signatures; anonymous callables also include their source span. Preserve the current manually curated Browser Automation execution trace by mapping its source anchors to generated function symbols, retaining its evidence and avoiding duplicate notes for those functions.

## Validation

The generator must check unique IDs, valid note links, existing source paths and line anchors, complete file ownership (or an explicit unassigned warning), and valid layer tags. Focused indexer checks will cover declarations, static calls, dependency direction, stable IDs, generated-note ownership, and stale-note cleanup. After implementation, run the repository-mandated `npm.cmd run verify` and report its exact result.

## Scope and limits

This change is limited to the architecture/impact graph, its source indexer, graph documentation, and focused graph-indexer validation. It does not alter Automator runtime behavior or add an in-app graph viewer. Obsidian's built-in graph remains the interactive surface. Static analysis cannot fully resolve reflection, runtime-selected implementations, event subscriptions, or every callback; the graph must surface these limits instead of presenting inferred links as complete.
