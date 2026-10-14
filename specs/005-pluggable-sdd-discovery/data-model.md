# Data Model: Pluggable SDD Discovery

## Custom workflow definition

One object in the repository's versioned declaration:

- **Name**: required non-empty display name; unique case-insensitively across built-in and custom workflows.
- **ArtifactRoots**: required non-empty list of normalized repository-relative directory roots.
- **SpecificationRoots**: optional directory roots whose immediate child directories with files count as specification directories.
- **ChangeRoots**: optional directory roots whose immediate child directories with files count as changes.
- **ArchivedChangeDirectoryNames**: optional single directory names excluded from the active change count, compared case-insensitively.

Root matching is against the scanner's existing repository-relative file list. Paths outside the scanned root are never enumerated or opened.

## SDD workflow inventory

Existing row fields remain: **Name**, **IsInitialized**, **SpecificationDirectoryCount**, nullable **ActiveChangeDirectoryCount**, **ArtifactFileCount**, and bounded **ArtifactPaths**.

For custom definitions:

- A workflow is initialized when one or more unique files match its artifact roots.
- Specification directories count immediate child names beneath all configured specification roots that contain at least one discovered file.
- Active changes count immediate child names beneath all configured change roots that contain at least one discovered file, less configured archive directory names.
- If no change roots are configured, active change count is null.
- Overlapping roots are deduplicated by repository-relative file path.

## Configuration diagnostic

- **Path**: stable repository-relative configuration path.
- **Code**: stable identifier for parser, schema, duplicate-name, or invalid-field errors.
- **Message**: actionable deterministic explanation without host-specific absolute paths.

Diagnostics are sorted by path, code, then message. Invalid custom entries are omitted while valid entries and built-in detection continue.
