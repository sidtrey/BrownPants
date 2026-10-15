# Tasks: SDD Evidence Handoff

## Phase 1 - Evidence contract and command

- [x] T001 [US1] Add versioned evidence report/finding models and deterministic projection from `RepositoryInventory` in `src/BrownPants.Cli/Analysis/`.
- [x] T002 [US1] Add `evidence` CLI dispatch, options, help, JSON output, safe static default, and explicit semantic opt-in in `src/BrownPants.Cli/Program.cs`.
- [x] T003 [P] [US1] Add command contract tests for candidate/resolved labeling, path/line/signature preservation, stable ordering and IDs, deduplication, and test-path findings in `tests/BrownPants.Tests/`.
- [x] T004 [P] [US1] Add CLI tests for defaults, semantic opt-in/limits, invalid options, and incomplete semantic evidence in `tests/BrownPants.Tests/`.

## Phase 2 - Integrate and measure

- [x] T005 [US1] Update bug-lab instructions to consume the structured handoff while preserving the frozen prompt rubric and candidate/resolved semantics.
- [x] T006 [US1] Run the deterministic export against the fixed fixture and audit all findings against the answer key and decoys without using the key in the command.
- [x] T007 [US1] Repeat the paired baseline/assisted comparison under the existing protocol, publish counts/tokens/errors, and claim advantage only if the frozen threshold passes.
- [x] T008 [US1] Run build, tests, packaging smoke where command surface requires it, acceptance hash/evidence checks, and diff review; update verification evidence.

## Dependencies

T001 precedes T002-T004. T001-T004 precede T005-T006. T007 depends on the reviewed export and unchanged frozen protocol. T008 closes all implementation tasks and records observed evidence.
