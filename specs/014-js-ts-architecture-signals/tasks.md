# Tasks: Report JavaScript and TypeScript Factory and DI Signals

**Input**: Design artifacts in `specs/014-js-ts-architecture-signals/`

**Tests**: Required by the feature specification and project constitution.

## Phase 1: User Story 1 - Find JavaScript and TypeScript construction wiring (Priority: P1)

**Independent Test**: Temporary JS/TS modules produce source-located candidates; comments, strings, templates, and unrelated calls do not.

- [X] T001 [US1] Add positive/noise fixtures across JavaScript and TypeScript with Markdown/JSON parity and determinism assertions in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T002 [US1] Add comment/string/template filtering and factory/DI candidate matching in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T003 [US1] Report JavaScript and TypeScript lexical coverage in `src/BrownPants.Cli/Analysis/SourceLanguageCoverageAnalyzer.cs` and document candidate limits in `README.md`.
- [X] T004 [US1] Run the Release build/test suite and Spec Kit/OpenSpec adapter checks.

## Dependencies

- T001 establishes expected evidence and noise behavior; T002 implements it.
- T003 and T004 follow analyzer implementation.

## Implementation Strategy

Add representative fixtures first, implement lexical masking and bounded patterns, then validate JSON/Markdown parity and all existing language behavior.
