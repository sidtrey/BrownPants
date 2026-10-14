# Tasks: JVM Factory and Dependency-Injection Signals

**Input**: Design documents from `specs/015-jvm-architecture-signals/`

**Tests**: Required by the feature specification and project constitution.

## Phase 1: User Story 1 - Find JVM construction and wiring conventions (Priority: P1)

**Independent Test**: Temporary Java, Kotlin, and Kotlin script files yield source-located candidates while comments and literals do not.

- [X] T001 [US1] Add Java, Kotlin, and Kotlin script positive/noise fixtures, LF/CRLF checks, and report parity assertions in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T002 [US1] Implement language-aware comment/literal masking and factory/DI candidates in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`.
- [X] T003 [US1] Mark Java, Kotlin, and Kotlin script lexical coverage in `src/BrownPants.Cli/Analysis/SourceLanguageCoverageAnalyzer.cs` and document evidence limits in `README.md`.
- [X] T004 [US1] Run the Release build/test suite and Spec Kit/OpenSpec adapter checks; review the refreshed repository report.

## Dependencies

- T001 establishes evidence and lexical filtering behavior before T002.
- T003 follows analyzer behavior; T004 follows all implementation tasks.

## Implementation Strategy

Add fixtures first, implement JVM lexical masking and bounded patterns, then verify the full report contract and existing analyzer behavior.
