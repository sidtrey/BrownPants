# Tasks: Impact Graph Coverage

**Input**: Design documents in `specs/057-impact-graph-coverage/`

## Phase 1: Regression fixture (User Story 1)

**Independent test**: A changed method with a caller beyond the public semantic sample still reports that caller, while the public sample remains bounded.

- [x] T001 [US1] Add a red-first Roslyn fixture where unrelated resolved edges precede the changed method's caller; assert `--limit`-sized semantic output and complete caller/depth evidence in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`.

## Phase 2: Separate bounded impact graph (User Story 1)

- [x] T002 [US1] Retain a deterministic internal call graph with a 100,000-edge production ceiling independently of public samples, including an internal test overload and test-assembly access, in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`, `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`, and `src/BrownPants.Cli/BrownPants.Cli.csproj`.
- [x] T003 [US1] Make `CallImpactAnalyzer` traverse the internal graph when present, preserve the fallback for constructed semantic fixtures, and base edge-sampling completeness on graph retention rather than public display samples in `src/BrownPants.Cli/Analysis/CallImpactAnalyzer.cs` and `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs`.

## Phase 3: Explicit graph truncation (User Story 2)

**Independent test**: A test-injected low graph ceiling retains no more than its bound, reports the exact omitted count, and marks impact incomplete.

- [x] T004 [US2] Add bounded-graph truncation, constructor-edge, determinism, and unchanged public semantic JSON assertions in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` and `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T005 [US2] Add a packed CLI smoke for a relevant caller beyond a one-edge display sample and wire it into `.github/workflows/ci.yml` in `tests/PackedImpactGraphCoverageSmoke.ps1`.

## Phase 4: BrownPants self-validation and documentation

- [x] T006 [US1] Document separate display and impact graph bounds, completeness behavior, and remaining MSBuild limits in `README.md` and `specs/023-call-impact-report/contracts/impact-report.md`.
- [x] T007 [US1] Run the full build/test suite, packed install and smoke, Spec Kit/OpenSpec checks, acceptance hash/ledger checks, and BrownPants-on-BrownPants inventory/evidence/impact; record observed results and limitations in `specs/057-impact-graph-coverage/verification.md`.

## Dependencies & Execution Order

- T001 is the red acceptance fixture and precedes implementation.
- T002 provides bounded graph data; T003 consumes it for traversal and completeness.
- T004 proves the internal ceiling and output compatibility; T005 validates the installed CLI.
- T006 documents the behavior; T007 is the final evidence gate.
- User Story 1 is the MVP. User Story 2 adds safe, explicit behavior at the graph ceiling.
