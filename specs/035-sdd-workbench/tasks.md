# Tasks: Local SDD Review Workbench

**Input**: Design documents from `/specs/035-sdd-workbench/`

## Phase 1: User Story 1 - Review an active feature visually (P1)

**Independent Test**: Generate a workbench from temporary Spec Kit feature fixtures and verify selected artifacts, story/criteria summaries, task/evidence status, selection errors, output safety, and safe HTML encoding.

### Tests

- [x] T001 [US1] Add temporary-repository coverage for active and explicit selection, missing/ambiguous/outside features, task/evidence summaries, hostile Markdown, output overwrite protection, and standalone HTML in `tests/BrownPants.Tests/SddWorkbenchTests.cs`.

### Implementation

- [x] T002 [US1] Implement bounded, repository-contained Spec Kit artifact reading and parsing for stories, AC/SC items, tasks, and acceptance evidence in `src/BrownPants.Cli/Analysis/SddWorkbenchAnalyzer.cs`.
- [x] T003 [US1] Implement accessible static HTML rendering with encoded source content and no external resources in `src/BrownPants.Cli/Output/SddWorkbenchHtmlReport.cs`.
- [x] T004 [US1] Add `workbench` CLI parsing, active feature resolution, default output, overwrite protection, and repository-relative path checks in `src/BrownPants.Cli/Program.cs`.

## Phase 2: User Story 2 - Return feedback without editing source artifacts (P1)

### Tests and implementation

- [x] T005 [US2] Add note reference controls and a local JSON download in `src/BrownPants.Cli/Output/SddWorkbenchHtmlReport.cs`, then verify exported note shape and unchanged inputs in `tests/BrownPants.Tests/SddWorkbenchTests.cs`.

## Phase 3: User Story 3 - Use the workbench in restricted environments (P1)

### Documentation and verification

- [x] T006 [US3] Document the command, browser workflow, note handoff, and offline/security limits in `specs/035-sdd-workbench/quickstart.md`, `README.md`, and the feature contract.
- [x] T007 Verify AC-001–AC-007 and SC-001–SC-005 with focused tests, full Release build/tests, acceptance hash/evidence checks, and a self-inspection smoke run; record evidence in `specs/035-sdd-workbench/verification.md`.

## Dependencies & Execution Order

- T001 precedes T002–T005. Keep tests red until the analyzer, renderer, and CLI are implemented.
- T002 precedes T003 and T004. T003 and T004 both precede T005.
- T006 follows stable command behavior; T007 is the completion gate.

## Implementation Strategy

Build the report from one active or explicitly selected Spec Kit feature. Use bounded source reads and stable parsed summaries, retain expandable original text for completeness, render static accessible HTML, and add client-side review-note export. Verify no writes occur outside the generated HTML and downloaded handoff.
