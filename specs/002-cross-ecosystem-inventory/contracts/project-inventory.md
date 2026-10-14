# Repository Inventory Report Contract

## Added JSON fields

- `ProjectSystems`: array of `{ Name, ManifestPaths }` records. Names are stable human-readable ecosystem/build-system labels; each path is repository-relative. Rows are sorted by name, and paths are distinct and sorted.
- `LanguageCoverage`: array of `{ Language, SourceFileCount, AnalysisLevel }` records for recognized source extensions. `AnalysisLevel` distinguishes Roslyn syntax/candidate analysis, lexical candidate patterns, and inventory-only evidence.
- `Extensions` includes `[no extension]` for files without a suffix. Other suffixes remain lowercase and are retained even when unknown.

## Markdown requirements

Markdown includes a Project ecosystems section and a Language coverage section derived from the same records as JSON. It labels inventory-only languages clearly and renders `[no extension]` counts. Existing `ProjectMarkers`, C#/VB analysis, native project data, and architecture findings remain available.

## Limits

Ecosystem detection uses conventional filenames and paths. It does not inspect dependency contents, resolve package graphs, run build tools, or prove that a manifest is active. Language coverage identifies the analysis actually provided; it does not claim unsupported source parsing.
