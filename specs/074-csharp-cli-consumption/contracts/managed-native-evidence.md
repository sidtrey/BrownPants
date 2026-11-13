# Managed-to-Native Candidate Evidence Contract

The inventory and evidence reports may expose a relation with `kind: "csharp-cppcli-call"` when source call syntax, project-reference metadata, and discovered C++/CLI declarations provide a candidate path.

Each relation preserves:

- a stable identifier and candidate status;
- repository-relative source and target paths and one-based lines when known;
- source and target symbols, candidate count, and candidate target locations;
- match reasons and explicit unresolved/ambiguous/unsupported state;
- completeness and omission information for bounded samples.

The relation does not prove that project conditions select the reference, the binary exists or was built from the discovered source, compiler overload resolution succeeds, a call executes, or the native operation has a runtime effect. Existing `cppcli-managed-call` keeps its current C++/CLI-to-managed direction and meaning.
