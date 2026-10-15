# Tasks: Nested Callable Identities

**Input**: Design documents in `specs/065-nested-callable-identities/`

## Phase 1: User Story 1 - Identify calls in nested C# callables (P1)

**Independent test**: Compiler fixtures produce non-empty, parent-qualified and distinct caller identities for lambdas, anonymous methods, and local functions; calls in one lambda retain one graph identity.

- [X] T001 [US1] Add failing C# semantic regression fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs` for two lambdas, repeated calls in one lambda, an anonymous method, and a local function; assert relative source anchors and matched local-function targets.
- [X] T002 [US1] Implement recursive callable identity formatting for named methods, anonymous functions, and local functions in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs`; use the same formatter for declarations, call targets, and caller contexts, redacting external paths.
- [X] T003 [US1] Add impact graph regression coverage in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` showing that lambda call edges remain distinct, stable, and traceable after semantic analysis.

## Phase 2: User Story 2 - Preserve identities across managed languages (P2)

**Independent test**: Visual Basic semantic fixture identifies a lambda caller; Bug Lab semantic impact displays the DI callback context without altering named method/constructor results.

- [X] T004 [US2] Add Visual Basic lambda caller identity and repeated-output assertions in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T005 [US2] Update `README.md` semantic call-report guidance and record observed Bug Lab call/evidence output and limits in `specs/065-nested-callable-identities/verification.md`.
- [X] T006 [US2] Run focused tests, Release build/full suite, Bug Lab impact/evidence, acceptance hash and evidence-ledger checks; record actual outcomes in `specs/065-nested-callable-identities/verification.md`.

## Dependencies and Order

- T001 proves the empty/colliding caller defect before implementation.
- T002 depends on T001 and must keep declaration/target identity formatting consistent.
- T003 follows T002 and verifies impact graph integration.
- T004 tests the shared formatter in Visual Basic.
- T005 and T006 follow behavior verification and documentation.
