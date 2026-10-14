# Tasks: Go Factory and Dependency-Injection Signals

**Input**: Design documents from `specs/016-go-architecture-signals/`

**Tests**: Required by the feature specification and project constitution.

## Phase 1: User Story 1 - Find Go construction and wiring conventions (Priority: P1)

**Independent Test**: Temporary Go modules produce source-located constructor and DI candidates; comments and literals do not.

- [X] T001 [US1] Add Go constructor/provider positive and noise fixtures, LF/CRLF checks, and Markdown/JSON parity assertions in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T002 [US1] Implement Go comment/literal masking and factory/Fx/Dig candidate matching in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T003 [US1] Mark Go lexical coverage in `src/BrownPants.Cli/Analysis/SourceLanguageCoverageAnalyzer.cs` and document evidence limits in `README.md`.
- [X] T004 [US1] Run the Release build/test suite and Spec Kit/OpenSpec adapter checks; review the refreshed repository report.

## Dependencies

- T001 defines positive and negative evidence before T002.
- T003 follows analyzer implementation; T004 follows all feature changes.

## Implementation Strategy

Add fixtures first, implement bounded Go lexical masking and patterns, then verify report parity, determinism, and existing analyzers.
