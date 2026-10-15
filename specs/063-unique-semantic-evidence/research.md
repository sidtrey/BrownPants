# Research: Unique Semantic Evidence

- BrownPants-on-BrownPants inspection of the Bug Lab found 38 resolved semantic relationships, but `evidence --semantic` emitted 36 managed-call findings.
- The missing rows were `new Order(3, false)` at `managed/tests/Program.cs:15` and `new ImportedRow(5, false)` at `managed/tests/Program.cs:22`. Each shares its line, caller, and target with a separate constructor call.
- `EvidenceReport.cs` hashes findings without expression or column and removes duplicate IDs using `DistinctBy`. The completeness count uses semantic totals, so the report marked itself truncated even though the caller requested a limit of 1000.
- `EvidenceFocusAnalyzer.Select` also requires unique IDs because it builds a dictionary keyed by ID.
- `CSharpSemanticAnalyzer` traverses syntax nodes in source order and already retains the source node, so a one-based column can be extracted without another file read.

## Decision

Add source columns to resolved semantic relationships and JSON findings, then deterministically disambiguate ID collisions without dropping rows. Preserve every previously unique ID and keep the schema version and existing candidate grouping contract.
