# Tasks: Filter Value-Returning Factory Candidates

**Input**: Design documents from `specs/022-filter-value-factory-candidates/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by AC-001 and AC-002; use xUnit temporary-repository fixtures.

**Organization**: Tasks are grouped by the sole independently testable user story.

## Phase 1: User Story 1 - Trust factory-method leads (Priority: P1)

**Independent Test**: Scan C# and VB.NET fixtures containing string path helpers and object-returning factory methods. String helpers are omitted; object methods remain source-located candidates.

- [X] T001 [US1] Add C# and VB.NET fixtures for nullable/string-returning `Create`, `Build`, and `Resolve` helpers and object-returning factory methods in `tests/BrownPants.Tests/RepositoryScannerTests.cs`; assert candidate parity in Markdown and JSON.
- [X] T002 [US1] Exclude string-returning methods from name-only factory-method candidate classification in both C# and VB.NET analyzers in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`, preserving existing object-returning candidates.
- [X] T003 [US1] Document the string-helper exclusion and candidate evidence limits in `README.md` and `specs/022-filter-value-factory-candidates/quickstart.md`.
- [X] T004 [US1] Run focused and full Release tests, verify the acceptance ledger, and run BrownPants JSON inspection against this repository; record actual results in `specs/022-filter-value-factory-candidates/verification.md`.

## Dependencies

- T001 precedes T002 so the regression is observed before the behavior changes.
- T002 precedes T003 and T004.

## Parallel Opportunities

- T003 documentation can proceed independently after the contract is agreed; source and test changes are sequenced because they share the analyzer behavior.

## Implementation Strategy

Complete the fixture first, then refine the Roslyn syntax rule. Verify the focused story and full suite, then use BrownPants on itself to confirm its architecture report no longer emits the observed false positive.