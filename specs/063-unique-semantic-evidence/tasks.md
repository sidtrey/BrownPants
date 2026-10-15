# Tasks: Unique Semantic Evidence

**Input**: Design documents in `specs/063-unique-semantic-evidence/`

## Phase 1: User Story 1 - Receive every resolved source relationship (P1)

**Independent test**: Build a semantic fixture with two constructor calls of the same target on a single line and verify both full and focused output, unique IDs, distinct columns, and accurate completeness.

- [X] T001 [US1] Add a failing same-line repeated-constructor regression in `tests/BrownPants.Tests/EvidenceCommandTests.cs` that checks both full and `--changed` semantic evidence, unique IDs, distinct columns, and unchanged IDs for a separate single call.
- [X] T002 [US1] Capture one-based expression columns on method invocations and constructor expressions in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs` and `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.
- [X] T003 [US1] Replace lossy `DistinctBy(Id)` with deterministic collision disambiguation in `src/BrownPants.Cli/Analysis/EvidenceReport.cs`; expose optional semantic columns and finalize before focus selection/grouping.
- [X] T004 [US1] Verify Bug Lab full and focused semantic evidence preserves all 38 resolved relationships, accurate completeness, and exact columns; document the additive source-column field in `README.md` and record output in `specs/063-unique-semantic-evidence/verification.md`.
- [X] T005 [US1] Run Release build/full tests, criteria hash and evidence-ledger checks, then record results in `specs/063-unique-semantic-evidence/verification.md`.

## Dependencies and Order

- T001 establishes the regression before implementation.
- T002 provides source location identity required by T003.
- T003 depends on T001/T002 and fixes both full and focused evidence paths.
- T004/T005 follow the implementation and capture final evidence.
