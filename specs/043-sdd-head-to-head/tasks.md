# Tasks: Vanilla SDD and BrownPants Comparison

**Input**: Design documents in `/specs/043-sdd-head-to-head/`

**Prerequisites**: `spec.md`, `plan.md`, `checklists/requirements.md`.

## Phase 1: Freeze and reproduce the controlled defect

- [x] T001 [US1] Record the active feature acceptance hash and task, reproduce the false positive for `src/ContestController.cs`, and prepare detached worktrees from the same source commit with identical controlled inputs.
- [x] T002 [US1] Run the vanilla Codex CLI condition without BrownPants commands and capture its JSONL usage events, changes, validation results, command count, and completion outcome under temporary storage.
- [x] T003 [US1] Run the BrownPants-assisted Codex CLI condition with the same model/task/criteria and BrownPants context/evidence, capturing equivalent temporary outcomes and usage events.

## Phase 2: Compare, report, and release gate

- [x] T004 [US1, US2] Verify paired controls and provider-reported token usage, independently run the same focused/full tests, preserve unavailable usage for the interrupted attempt, and create the sanitized comparison report.
- [x] T005 [US2] Add the scenario, comparison link, acceptance-hash explanation, and plain-English project summary to `README.md`; add a tested parser for provider usage events and verify no raw traces or credentials are included.
- [x] T006 [US1, US2] Validate acceptance hash/evidence, run Release build/full tests, inventory the repository with BrownPants, and record exact results in `specs/043-sdd-head-to-head/verification.md`.

## Dependencies

- T001 precedes T002 and T003; both conditions start from the same source state.
- T004 follows both runs; T005 follows report finalization; T006 is the completion gate.
