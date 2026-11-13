# JavaScript and TypeScript Source Evidence Contract

`inspect` adds a `JavaScriptTypeScript` JSON object to the version 1 inventory and a matching Markdown section. Existing fields remain unchanged. The object contains `EvidenceLevel`, `ScannedFileCount`, `DeclarationCount`, `ModuleRelationshipCount`, `CallRelationshipCount`, sampled `Declarations`, `ModuleRelationships`, and `CallRelationships`, per-family omitted counts, `IssueCount`, sampled `Issues`, `OmittedIssueCount`, and evidence limits.

Each record carries repository-relative path and one-based source line/column. Module and call status values are `syntax-resolved`, `ambiguous`, or `unresolved`. An unresolved record carries a stable reason code. A resolved record carries one in-repository target. Ambiguous records carry bounded sorted candidate targets. Counts are exact within the scanned source set even when record arrays are sampled. `Issues` lists bounded source or configuration problems by path and stable code.

The report must state that parsing is bounded syntax analysis; it does not establish compiler/type-check success, conditional package export selection, overload binding, runtime dispatch, module execution, or actual package installation. Analysis adds no CLI flags, network behavior, dependency, or schema-version change.
