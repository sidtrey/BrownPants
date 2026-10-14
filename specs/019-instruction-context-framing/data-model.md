# Data Model: Instruction Context Frames

## Instruction Source

- `Path`: normalized repository-relative path using `/`.
- `Recognition`: convention identifier or `Explicit`.
- `Scope`: global, directory/path-scoped, or unknown, with evidence and optional pattern.
- `Sections`: ordered Markdown headings and stable anchors; absent for non-Markdown text.
- `ByteCount`: exact UTF-8 byte count when readable.
- `ApproximateTokenCount`: heuristic estimate, nullable, explicitly approximate.
- `ContentDigest`: SHA-256 of the inspected bytes for stale checks.
- `Status`: readable, unsupported, too large, malformed metadata, or read error.

## Response Frame

- `Version`: frame-format version.
- `Created`: omitted for deterministic output unless explicitly supplied externally.
- `Budget`: requested word/token budget and unit.
- `Points`: ordered concise priorities.
- `Sources`: referenced source paths, section anchors, and captured digests.
- `ReviewItems`: unresolved conflicts, uncited claims, and budget shortfall.
- `HostCaveat`: statement that effective loading and token savings depend on host configuration.

## Draft Input

Frame creation accepts a JSON draft with `budget`, ordered `points` (`text` plus one or more `sources` containing `path` and optional `anchor`), and `reviewItems`. BrownPants resolves each path and anchor against the current inventory, attaches current digests and scope to the output source map, and reports missing references or budget overflow. This lets an SDD engine draft wording without making the core model-dependent.

## Validation Rules

Paths must resolve to discovered files within the repository. Each point must cite one or more known source sections or a file-level reference when headings are unavailable. Duplicate paths are normalized. Unsupported sources remain visible but cannot back semantic claims. An existing output is preserved unless overwrite is explicitly requested. Digest mismatch marks a source stale.
