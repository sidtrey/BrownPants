# Tasks: Filter Test Methods from Factory Signals

**Input**: [spec.md](spec.md), [plan.md](plan.md), research and contract artifacts in this directory.
**Story**: US1 is the single P1 story. Tests are required by the specification and constitution.

## Phase 1: User Story 1 — Trust .NET factory-method findings (P1)

**Goal**: Omit recognized attributed test methods from factory-method candidates and make production method evidence point to its declaration.

**Independent test**: Temporary C# and VB fixture scans prove recognized test markers are omitted, actual factories remain, and line/evidence identify the signature. Self-inspection omits the two named submission tests.

- [X] T001 [US1] Add C# and VB repository-scanner fixtures for xUnit, NUnit, and MSTest attributes, qualification/suffix/arguments, and attributed production factories in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T002 [US1] Implement method-level test-attribute recognition and declaration-anchored factory evidence in `src/BrownPants.Cli/Analysis/ArchitecturePatternAnalyzer.cs`; keep other signal categories and existing return/name rules unchanged.
- [X] T003 [US1] Run the focused and complete Release test suites and verify JSON/Markdown regressions through `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [X] T004 [US1] Verify the acceptance hash, validate the task evidence ledger, inspect JSON/Markdown self-reports for the named test methods, and record observed outcomes in `specs/031-filter-test-factory-signals/verification.md`.

## Dependencies & Execution Order

- T001 precedes T002 (regression-first); T002 precedes T003 and T004.
- No independent user stories or parallel source edits are needed; all code and fixture changes share the analyzer/test workflow.

## Implementation Strategy

Complete US1 as one increment. Run focused tests after the regression fixtures and implementation, then run the full Release suite and report/hash/ledger checks. The story is complete only when every acceptance scenario has observed evidence.
