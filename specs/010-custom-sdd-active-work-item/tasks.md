# Tasks: Configure Active Work Items for Custom SDD Engines

**Input**: Design artifacts in `specs/010-custom-sdd-active-work-item/`

**Tests**: Required by the feature specification and project constitution.

## Phase 1: User Story 1 - Identify active work in a custom SDD engine (Priority: P1)

**Independent Test**: A temporary repository with a valid custom selection shows the same normalized path and evidence in Markdown and JSON; an older declaration without the field retains prior inventory and null selection fields.

- [x] T001 [US1] Add valid and omitted custom selection fixture assertions for Markdown and JSON in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T002 [US1] Add optional `activeWorkItemPath` parsing to custom workflow definitions in `src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs`.
- [x] T003 [US1] Resolve valid custom selections with `CustomWorkflowDeclaration` evidence in `src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs`.

## Phase 2: User Story 2 - Reject unsupported custom selections safely (Priority: P2)

**Independent Test**: Unsafe, outside-root, and stale custom paths yield deterministic issues, null active fields, and preserved workflow counts.

- [x] T004 [US2] Add invalid type, unsafe path, outside-root, and stale custom selection fixtures in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T005 [US2] Validate custom selection paths against artifact roots and scanned files with stable diagnostics in `src/BrownPants.Cli/Analysis/SddWorkflowAnalyzer.cs`.
- [x] T006 [US2] Document the custom active selection property and update the workflow configuration contract in `README.md` and `specs/005-pluggable-sdd-discovery/contracts/custom-workflow-config.md`.
- [x] T007 [US2] Verify the feature with `dotnet build BrownPants.sln`, `dotnet test BrownPants.sln`, adapter checks, and Markdown/JSON reports from `specs/010-custom-sdd-active-work-item/quickstart.md`.

## Dependencies

- T001 establishes successful behavior; T002-T003 implement it.
- T004 establishes safety behavior; T005 implements it.
- T006-T007 follow both stories.

## Parallel Opportunities

No tasks are marked parallel because all fixture cases share one test file and parser changes are sequential.

## Implementation Strategy

Deliver explicit custom selection first while preserving existing declarations, then validate invalid, ambiguous, and stale input behavior. Keep diagnostics nonfatal to otherwise valid workflow inventory. Complete the full .NET and adapter checks before marking the feature ready.
