# Contract: Semantic Path Boundary

Semantic finding locations are scoped to the inspected repository:

- Declaration and invocation samples/counts are derived only from syntax trees with a normalized path below the repository root.
- A resolved method can remain in a call finding when its source declaration is outside the repository. In that case `TargetPath` and `TargetLine` are null.
- Non-null project and source location fields are repository-relative and use `/` separators. They contain no rooted or parent-traversal path.
- External syntax trees may still participate in Roslyn binding; they are not emitted as repository findings.

The JSON schema version and semantic record fields do not change.
