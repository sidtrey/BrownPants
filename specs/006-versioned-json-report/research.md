# Research: Versioned JSON Report

## Decision: Use a root integer named SchemaVersion

**Decision**: Every successful JSON inventory includes `SchemaVersion` with integer value `1`.

**Rationale**: The root object is already serialized directly from RepositoryInventory using PascalCase properties. A root field is easy for consumers to inspect before reading nested fields and follows the existing naming convention. The report format version remains independent of NuGet and CLI versions.

**Alternatives considered**:

- Reuse the package version: package releases can change without changing the report contract, and a contract can evolve independently.
- Add a separate media type or wrapper object: this would restructure the current root JSON and break existing consumers.
- Put a version in Markdown: Markdown is human-facing context; schema detection belongs in the machine-readable format.

## Decision: Preserve version 1 for additive optional fields

**Decision**: Additive fields may remain in schema version 1. Removing or renaming existing fields, changing their types, or changing their documented meaning requires a schema version increment.

**Rationale**: Consumers can adopt new optional fields without rejecting otherwise compatible reports, while changes that affect existing readers remain explicit.

**Alternatives considered**: Incrementing on every package release would conflate product and report compatibility; leaving breaking changes unversioned would make consumer behavior unpredictable.

