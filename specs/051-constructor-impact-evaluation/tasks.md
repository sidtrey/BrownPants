# Tasks: Constructor Impact Bug Benchmark

## Phase 1 - Freeze the independent scenario

- [x] T001 [US1] Add a separate constructor-injection task specification and pin its acceptance hash in `examples/brownfield-bug-lab/evaluation/constructor-impact/task-spec.md` and `acceptance-criteria.sha256`.
- [x] T002 [US1] Add the bug report, JSON result schema, and agent prompt in `examples/brownfield-bug-lab/evaluation/constructor-impact/` without changing the existing batch-bug task inputs.
- [x] T003 [US1] Add a safe exact-text seed script for the wrong-repository constructor defect in `examples/brownfield-bug-lab/evaluation/constructor-impact/Seed-ConstructorBug.ps1`.

## Phase 2 - Prepare and run matched Codex copies

- [x] T004 [US1] Create matched baseline/assisted worktrees, verify clean and seeded prechecks, hash equal source copies, and provide only assisted `impact.md` in `examples/brownfield-bug-lab/evaluation/constructor-impact/Prepare-ConstructorComparison.ps1`.
- [x] T005 [US1] Add the Codex CLI runner with the same explicit model, task, and `workspace-write` sandbox for both conditions; preserve JSONL events and final responses outside agent roots in `examples/brownfield-bug-lab/evaluation/constructor-impact/Run-ConstructorComparison.ps1`.

## Phase 3 - Score and publish the observed outcome

- [x] T006 [US1] Add the hidden expected-finding key and scorer for source repair, exact caller paths/signatures, duplicate/false-positive handling, precision, recall, and observed token usage in `examples/brownfield-bug-lab/evaluation/constructor-impact/`.
- [x] T007 [US1] Document the reproducible preparation, Codex execution, scoring commands, fairness controls, and evidence limits in `examples/brownfield-bug-lab/evaluation/constructor-impact/protocol.md` and `examples/brownfield-bug-lab/README.md`.
- [x] T008 [US1] Run the hashed task verification, preparation, one matched Codex pair, scoring, both post-fix checks, source/condition separation checks, and document the actual result in `docs/comparisons/constructor-impact-benchmark-2026-10.md` and `.json`.

## Dependencies

T002-T003 depend on the frozen task artifact in T001. T004 requires T001-T003. T005 requires prepared copies from T004. T006 scores only completed outputs from T005; T007 records the exact protocol and measured result from T006. T008 is the final evidence gate.

## Independent test

The scenario is viable without a model: the clean check must pass, and both seeded checks must fail with `Single-item overload did not save exactly one order.` Only after that gate passes may Codex runs begin.
