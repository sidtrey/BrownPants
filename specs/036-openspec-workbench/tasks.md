# Tasks: OpenSpec Review Workbench

**Input**: Design documents from `/specs/036-openspec-workbench/`

## Phase 1: Selection and parsing

**Independent Test**: Create temporary repositories with one or multiple OpenSpec changes and verify default/explicit selection, parsed artifacts, safe output, and unchanged inputs.

- [x] T001 [US1] Add fixture tests for Spec Kit regression, valid/invalid engine selection, unique and explicit OpenSpec changes, ambiguous/archive-only/missing/outside changes, required proposal, multiple delta specs with repeated requirement titles, nested task states, hostile Markdown, note references, offline output, and unchanged inputs in `tests/BrownPants.Tests/SddWorkbenchTests.cs`.
- [x] T002 [US1] Add engine-aware active/explicit selection and bounded OpenSpec artifact discovery/read safety in `src/BrownPants.Cli/Analysis/SddWorkbenchAnalyzer.cs`.
- [x] T003 [US1] Parse OpenSpec proposal titles, requirement/scenario headings, tasks, source paths, and stable feedback IDs while retaining existing Spec Kit models in `src/BrownPants.Cli/Analysis/SddWorkbenchAnalyzer.cs`.

## Phase 2: Rendering and command

- [x] T004 [US1] Add engine-aware title, summary labels, requirements/scenarios, and artifact rendering in `src/BrownPants.Cli/Output/SddWorkbenchHtmlReport.cs`.
- [x] T005 [US1] Add `--engine speckit|openspec`, engine-specific selection errors, help, and protected input-path checks in `src/BrownPants.Cli/Program.cs`.

## Phase 3: Documentation and verification

- [x] T006 [US2] Document the OpenSpec command, selection rules, note references, and scope limits in `README.md`, `specs/036-openspec-workbench/quickstart.md`, and `specs/036-openspec-workbench/contracts/workbench-openspec.md`.
- [x] T007 Verify AC-001–AC-007 and SC-001–SC-005 using focused/full Release tests, acceptance hash/evidence checks, and a BrownPants-on-BrownPants smoke run; record actual output in `specs/036-openspec-workbench/verification.md`.

## Dependencies

T001 precedes T002–T005. T002 and T003 precede T004. T005 depends on T002–T004. T006 follows stable behavior. T007 is the completion gate.
