# Research: Pluggable SDD Discovery

## Decision: Read one repository-local JSON declaration

**Decision**: Use `.brownpants/sdd-workflows.json` with a numeric schema version and a list of workflow definitions.

**Rationale**: JSON is already used by the CLI and can be parsed with the framework library. A single stable location is discoverable and easy for other engines or tools to emit. One schema avoids probing multiple candidate files or guessing engine layouts.

**Alternatives considered**: Hard-code more detector classes (requires a BrownPants release for every engine); infer arbitrary directories such as `docs/specs` (false positives); load engine plugins (code execution and dependency risks); add CLI flags (configuration would not travel with the repository).

## Decision: Derive results from the scanner inventory

**Decision**: Validate root strings and match them against repository-relative file paths that RepositoryScanner already discovered. Do not enumerate or open configured artifact roots.

**Rationale**: This preserves ignored-directory and symlink behavior, makes counts reproducible, and prevents configuration from causing external filesystem reads. Reject absolute paths, parent segments, wildcard syntax, and empty roots before matching. Because custom roots are never opened or enumerated, and are matched only against the existing in-root file inventory, no external path resolution is needed.

**Alternatives considered**: Enumerate each configured directory separately (duplicate paths and behavior drift); resolve and read every supplied path (larger traversal surface and inconsistent ignore rules).

## Decision: Make malformed configuration visible without suppressing built-ins

**Decision**: Add structured diagnostics with repository-relative source path, stable code, and actionable message. Catch invalid JSON and definition validation failures at the configuration boundary, then continue built-in workflow detection.

**Rationale**: A bad extension declaration should not make the primary inventory disappear. Structured diagnostics are useful to both terminal readers and automation.

**Alternatives considered**: Fail the entire inspect command (unnecessarily blocks unrelated evidence); silently skip invalid definitions (misleading report); use plain strings only (less useful for automation).

## Decision: Keep the schema strict and versioned

**Decision**: Require schema version 1, reject unknown properties, and bound the declaration file to 1 MiB. Use System.Text.Json, whose deserialization reports invalid JSON or incompatible content with JsonException ([Microsoft System.Text.Json documentation](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserialize)).

**Rationale**: The version enables intentional evolution, strict fields catch misspellings, and file-size and 100-workflow bounds keep parsing and output work controlled. No runtime package or engine-specific executable configuration is needed.

**Alternatives considered**: Flexible unversioned JSON (unclear migration); permissive unknown properties (typos go unnoticed); YAML parser dependency (additional package for one small file).
