# Tasks: Reproducible Brownfield SDD Comparison

**Input**: Design documents from `spec.md` and `plan.md`.

## Phase 1: Freeze the comparison task and evidence

- [X] T001 [P] [US1] Create the task-specific Spec Kit specification, plan, tasks, acceptance hash, and exact shared agent prompt under `specs/046-brownfield-sdd-comparison/experiment/`.
- [X] T002 [P] [US1] Build the identical temporary seed patch and failing regression check against `RepositoryScanner.IsTestPath`; record the source commit and patch hash.
- [X] T003 [US1] Create two detached worktrees from the same commit, apply the same seed, verify identical hashes, and observe the same pre-run failure in both.

## Phase 2: Run and audit both conditions

- [X] T004 [US1] Run the baseline using the shared Spec Kit artifacts without BrownPants commands; retain raw Codex JSONL only in temporary storage and record its worktree diff and output.
- [X] T005 [US1] Run the assisted condition using the same artifacts and prompt plus BrownPants inventory/source-impact evidence and acceptance evidence checks; record actual commands, raw usage, diff, and output.
- [X] T006 [US1] Independently run identical focused and full Release tests, inspect both diffs for correctness, and record review findings separately from automated results.

## Phase 3: Publish evidence-backed comparison

- [X] T007 [US2] Write a detailed, sanitized report with exact common inputs, treatment difference, seeded defect, commands, hashes, changed paths, review notes, test results, observed usage provenance, and limitations.
- [X] T008 [US2] Rewrite the README comparison as an accessible Spec Kit versus Spec Kit + BrownPants scenario based only on verified evidence; preserve the plain-English explanation of acceptance hashes.
- [X] T009 [US1, US2] Validate feature acceptance hash and task evidence ledger, run Release build/test and secret scan, and verify no raw traces or experimental worktrees are committed.

## Dependencies

- T001 and T002 precede T003; T003 precedes both agent runs.
- T004 and T005 use the same completed T003 fixture and run sequentially from independent worktrees.
- T006 follows both runs. T007 and T008 use audited T006 evidence. T009 is the publication gate.
