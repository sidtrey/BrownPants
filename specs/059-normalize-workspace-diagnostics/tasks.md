# Tasks: Normalize Workspace Diagnostics

**Input**: Design documents from `specs/059-normalize-workspace-diagnostics/`

## Phase 1: Normalize semantic workspace evidence (User Story 1)

**Independent test**: The same workspace diagnostic is useful and identical across checkout roots, contains relative in-repo paths, redacts external paths, and leaves completeness counts unchanged.

- [x] T001 [US1] Implement an internal path-aware workspace diagnostic normalizer that rewrites in-repository absolute paths, redacts external paths, preserves explanations, and handles root-prefix collisions in `src/BrownPants.Cli/Analysis/WorkspaceDiagnosticNormalizer.cs`.
- [x] T002 [US1] Normalize every issue before storing it in `CSharpSemanticAnalysis.Issues` in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`.
- [x] T003 [US1] Add fixture coverage for in-repo paths, outside paths, prefix collisions, alternate checkout roots, stable separators, URLs, and unchanged semantic JSON fields/counts in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T004 [US1] Update diagnostic documentation, run focused and full verification, rerun BrownPants-on-BrownPants semantic inspect, and record results in `specs/059-normalize-workspace-diagnostics/verification.md`.

## Dependencies & Execution Order

- T001 provides the normalization behavior used by T002.
- T003 verifies T001 and T002; T004 runs after focused and full checks pass.

## Implementation Strategy

Add the pure normalization helper first and test it with temporary roots. Apply it at the semantic issue capture boundary, then verify public semantic serialization and the actual BrownPants workspace failure. Preserve the existing JSON fields and completeness decisions.
