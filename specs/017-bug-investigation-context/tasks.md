# Tasks: Bug Investigation Context

**Input**: Design documents from `specs/017-bug-investigation-context/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/investigate-cli.md`

**Tests**: Fixture tests are required by the feature requirements and project constitution.

## Phase 1: Setup

**Purpose**: No project initialization is needed; the existing CLI and xUnit projects are used.

## Phase 2: Foundational

**Purpose**: Make repository discovery shareable without changing `inspect` behavior.

- [x] T001 Add a deterministic repository-relative file discovery method in `src/BrownPants.Cli/Analysis/RepositoryScanner.cs` and make `Scan` consume it while preserving ignored-directory, symlink, and ordering behavior.
- [x] T002 Add regression assertions for discovery ordering, ignored directories, and symlink behavior in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

**Checkpoint**: Existing inspect tests remain unchanged in output and behavior.

## Phase 3: User Story 1 - Turn a bug report into repository leads (Priority: P1)

**Goal**: A developer can search explicit clues and receive explainable source and likely-test locations.

**Independent Test**: Invoke the CLI against temporary repositories; verify path and line matches, test-candidate classification, unmatched clues, stable ordering, and bounded results.

### Tests for User Story 1

- [x] T003 [US1] Add CLI tests for repeated clues, case-insensitive literal content/path matches, unmatched clues, line numbers, test-candidate classification, read-only behavior, binary/oversized-file diagnostics, bounded inputs, and invalid or empty clues in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

### Implementation for User Story 1

- [x] T004 [US1] Add `InvestigationReport`, per-clue result, match, and read-issue records with independent schema version 1 in `src/BrownPants.Cli/Analysis/RepositoryInventory.cs`.
- [x] T005 [US1] Implement deterministic literal clue matching, path/content match kinds, test-path labeling, bounded samples, totals, and per-file read diagnostics in `src/BrownPants.Cli/Analysis/RepositoryInvestigator.cs`.
- [x] T006 [US1] Add `investigate [path] --clue <text>` parsing, repeatable clue validation, `--format`, `--limit`, and exit-code behavior to `src/BrownPants.Cli/Program.cs`.

**Checkpoint**: Markdown and JSON investigation results are generated for positive, negative, and bounded-match fixtures.

## Phase 4: User Story 2 - Use leads safely in an SDD workflow (Priority: P2)

**Goal**: The same clue evidence can be reviewed in Markdown or consumed as JSON without exposing source line contents.

**Independent Test**: Run identical clue inputs in both formats and compare ordered match records; repeat each invocation and compare output bytes.

### Tests for User Story 2

- [x] T007 [US2] Add Markdown/JSON parity, stable repeated-output, no-source-snippet, and explicit evidence-limit assertions in `tests/BrownPants.Tests/RepositoryScannerTests.cs`.

### Implementation for User Story 2

- [x] T008 [US2] Implement the Markdown investigation report with match counts, omitted counts, test-candidate labels, read issues, and evidence limitations in `src/BrownPants.Cli/Output/InvestigationReportRenderer.cs`.
- [x] T009 [US2] Document the `investigate` command, clue behavior, output examples, and limits in `README.md` and `specs/017-bug-investigation-context/quickstart.md`.

**Checkpoint**: Both output formats preserve clue provenance and avoid asserting root cause or test coverage.

## Phase 5: Polish & Cross-Cutting Concerns

- [x] T010 Review CLI help and error messages for the `investigate` command in `src/BrownPants.Cli/Program.cs` and `tests/BrownPants.Tests/RepositoryScannerTests.cs`.
- [x] T011 Run Release build, full xUnit suite, Spec Kit and OpenSpec adapter checks, packed-tool smoke check, and `git diff --check`; record results in `specs/017-bug-investigation-context/quickstart.md`.

## Dependencies & Execution Order

- T001 and T002 form the shared discovery foundation and must complete before T005.
- T003 precedes T004-T006; T004-T006 are sequential because command output depends on the report model and investigator.
- T007 follows T006; T008 follows T007; T009-T011 follow the report implementation.
- Story order: foundational tasks → US1 → US2 → polish. US2 depends on US1's report contract.

## Parallel Opportunities

The discovery regression test T002 can be drafted independently from the later CLI tests, but they share one test file and should be applied sequentially. Implementation tasks share `Program.cs` dependencies and are intentionally sequential. There are no safe multi-agent parallel tasks in this small feature.

## Implementation Strategy

Deliver the MVP by completing discovery reuse and US1, then validate clue matching independently. Complete US2 to verify stable, safe Markdown/JSON evidence, update user documentation, and run all repository quality gates.
