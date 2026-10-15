# Tasks: Source-Backed Constructor Declarations

**Input**: Design documents in `specs/066-source-backed-constructors/`

## Phase 1: User Story 1 - Trace construction into implicit constructors (P1)

**Independent test**: Roslyn binds construction of a class with no explicit constructor, the target maps to its source type declaration, and impact from that type file returns the construction caller.

- [X] T001 [US1] Add a failing C# semantic regression in `tests/BrownPants.Tests/RepositoryScannerTests.cs` for an implicit parameterless constructor, asserting target/declaration identity and the repository-relative type source location.
- [X] T002 [US1] Add failing constructor impact coverage in `tests/BrownPants.Tests/CallImpactAnalyzerTests.cs` for implicit and primary constructors; assert source-file seeds, caller locations, depth, and `constructor` relationship kind.

## Phase 2: User Story 2 - Trace primary constructors (P2)

**Independent test**: A C# primary constructor is declared once at its type source location, and explicit overloads plus source-backed VB implicit constructors retain matching constructor targets.

- [X] T003 [US2] Add failing C# primary-constructor and VB implicit-constructor semantic cases in `tests/BrownPants.Tests/RepositoryScannerTests.cs`, including compatibility assertions for explicit overloads and deterministic declaration output.
- [X] T004 [US1] Extend declaration collection in `src/BrownPants.Cli/Analysis/CSharpSemanticAnalyzer.cs` to map source-backed implicit and primary constructor symbols to their type declarations, preserving canonical identities and deduplicating existing explicit declarations.

## Phase 3: Integration and verification

- [X] T005 [US1] Update `README.md` semantic analysis guidance and record constructor identity/impact output, release results, and evidence limits in `specs/066-source-backed-constructors/verification.md`.
- [X] T006 Run focused tests, Release build/full suite, Bug Lab impact and semantic self-inspection, acceptance hash, evidence ledger validation, and diff checks; record observed results in `specs/066-source-backed-constructors/verification.md`.

## Dependencies and Order

- T001-T003 establish failing semantic and impact behavior before implementation.
- T004 depends on those regressions; T005 and T006 follow implementation.
- T001 and T002 touch separate test files and can be prepared independently. T003 touches the same semantic test file as T001, so keep it sequential.
