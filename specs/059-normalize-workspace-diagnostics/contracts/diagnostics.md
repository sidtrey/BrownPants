# Contract: Semantic Workspace Diagnostics

- Semantic workspace messages remain strings in the existing `Issues` JSON field.
- In-repository absolute path references become repository-relative paths with `/` separators.
- Absolute path references outside the scan root become `[external-path]`.
- Diagnostic reason text, deterministic ordering, count fields, sample limits, and completeness decisions are preserved.
- No new JSON property or schema version is introduced.
- Path normalization applies consistently to inspect, evidence, and other consumers of `CSharpSemanticAnalysis.Issues`.
