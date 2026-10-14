# Tasks: Report the Active SDD Work Item

**Input**: Design artifacts in `specs/009-active-sdd-work-item/`

**Tests**: Required by the feature specification and project constitution.

## Phase 1: User Story 1 - Find the active work item (Priority: P1)

**Independent Test**: Temporary repositories report a valid Spec Kit pointer and one unique OpenSpec change consistently in Markdown and JSON; ambiguous OpenSpec changes remain unset.

- [x] T001 [US1] Add Spec Kit active pointer and Markdown/JSON fixture coverage in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T002 [US1] Add unique and ambiguous OpenSpec selection plus custom workflow null-selection coverage in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T003 [US1] Add nullable active work item path and evidence fields to `SddWorkflowInventory` in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.
- [x] T004 [US1] Select validated Spec Kit and unique OpenSpec work item paths in `src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs`.
- [x] T005 [US1] Render active work item path and evidence in `src/BrownPants.Cli/Output/MarkdownReport.cs`.

## Phase 2: User Story 2 - Trust selection evidence (Priority: P2)

**Independent Test**: Invalid, unsafe, duplicate, oversized, or stale Spec Kit pointer fixtures produce stable diagnostics and never produce an active path.

- [x] T006 [US2] Add malformed, traversal, stale, duplicate, and oversized pointer fixture tests in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T007 [US2] Bound and validate Spec Kit metadata reads and emit deterministic diagnostics in `src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs`.
- [x] T008 [US2] Document active selection evidence and ambiguity behavior in `README.md`.
- [x] T009 [US2] Verify the change with `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, and Markdown/JSON CLI reports from `specs/009-active-sdd-work-item/quickstart.md`.

## Dependencies

- T001-T002 establish successful selection behavior; T003-T005 implement it.
- T006 establishes invalid-input behavior; T007 implements it.
- T008-T009 follow both user story implementations.

## Parallel Opportunities

No tasks are marked parallel because the scanner tests share one fixture file and the implementation tasks depend on the same inventory contract.

## Implementation Strategy

Deliver the valid, unambiguous selection path first, then enforce invalid-input safety and diagnostics. Preserve counts and sample behavior throughout. Complete the build, fixture suite, and both report formats before marking the feature ready.
