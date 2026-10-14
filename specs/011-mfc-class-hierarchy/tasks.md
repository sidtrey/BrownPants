# Tasks: Report MFC Class Inheritance Signals

**Input**: Design artifacts in `specs/011-mfc-class-hierarchy/`

**Tests**: Required by the feature specification and project constitution.

## Phase 1: User Story 1 - Find MFC application and UI class hierarchies (Priority: P1)

**Independent Test**: Temporary C++ headers with same-line and multiline MFC base declarations produce `MFC class inheritance` signals with the right path and declaration line; comments, literals, and unrelated base types do not.

- [x] T001 [US1] Add native MFC inheritance fixtures and Markdown/JSON source-location assertions in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T002 [US1] Add bounded multiline class declaration tracking and common MFC base matching in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [x] T003 [US1] Document the MFC inheritance candidate category and evidence limit in `README.md`.
- [x] T004 [US1] Verify the feature with `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, Spec Kit/OpenSpec adapter checks, and report output from `specs/011-mfc-class-hierarchy/quickstart.md`.

## Dependencies

- T001 establishes expected detection and noise behavior; T002 implements it.
- T003 and T004 follow the analyzer implementation.

## Parallel Opportunities

No tasks are marked parallel because the native fixture and analyzer changes share the same evidence contract.

## Implementation Strategy

Add fixture coverage first. Detect only known MFC base types within bounded sanitized declarations, retain candidate labeling and deterministic sampling, then update documentation and run the complete build, test, and adapter gates.
