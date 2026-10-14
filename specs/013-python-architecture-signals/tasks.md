# Tasks: Report Python Factory and DI Signals

**Input**: Design artifacts in `specs/013-python-architecture-signals/`

**Tests**: Required by the feature specification and project constitution.

## Phase 1: User Story 1 - Find Python construction and dependency wiring (Priority: P1)

**Independent Test**: Temporary Python modules containing supported patterns produce source-located candidates; comments, strings, and ordinary calls do not.

- [x] T001 [US1] Add positive/noise and explicit CRLF fixtures plus Markdown/JSON determinism assertions in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T002 [US1] Add Python comment/string filtering, CRLF-aware escaped continuation handling, and factory/DI candidate matching in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [x] T003 [US1] Report Python lexical candidate coverage in `src/BrownPants.Cli/Analysis/SourceLanguageCoverageAnalyzer.cs`, clarify generic candidate wording in `src/BrownPants.Cli/Output/MarkdownReport.cs`, and document pattern limits in `README.md`.
- [x] T004 [US1] Run the Release build/test suite and Spec Kit/OpenSpec adapter checks.

## Dependencies

- T001 defines expected evidence and filtering behavior; T002 implements it.
- T003 and T004 follow analyzer implementation.

## Implementation Strategy

Add fixture expectations first, implement one-pass lexical masking and bounded convention matches, then validate report contracts and all existing language behavior.
