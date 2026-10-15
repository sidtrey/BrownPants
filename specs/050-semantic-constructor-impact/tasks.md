# Tasks: Semantic Constructor Impact

## Phase 1 - Semantic constructor edges

- [x] T001 [US1] Extend semantic models with relationship kind and separate constructor syntax/resolved/unresolved counts while retaining existing invocation counters in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.
- [x] T002 [US1] Resolve C# object creation/target-typed creation/constructor initializers and VB object creation/constructor initializers; include explicit constructor declarations in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`.
- [x] T003 [P] [US1] Add exact overload, target location, unresolved reason, count reconciliation, and deterministic sample tests for C# constructor forms in `tests/BrownPants.Tests/`.
- [x] T004 [P] [US1] Add Visual Basic constructor/object-creation binding coverage in `tests/BrownPants.Tests/`.

## Phase 2 - Impact and report integration

- [x] T005 [US1] Extend impact traversal and models to trace constructor targets and preserve `relationshipKind` in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs`.
- [x] T006 [US1] Update Markdown/JSON semantic and impact output plus the evidence handoff's constructor labels and completeness counts in `src/BrownPants.Cli/Output/` and `src/BrownPants.Cli/Analysis/EvidenceReport.cs`.
- [x] T007 [US1] Add direct/transitive constructor impact, method compatibility, and test-path coverage in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` and `RepositoryScannerTests.cs`.
- [x] T008 [US1] Run full build/test, packed CLI smoke, BrownPants self-inspection, acceptance hash/evidence validation, and diff review; record actual output in `verification.md`.

## Dependencies

T001-T004 establish constructor edges; T005-T007 depend on them; T008 validates the integrated behavior and records all evidence.
