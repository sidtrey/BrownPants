# Tasks: Measurable Brownfield Capability Demo

## Phase 1 - Build the showcase fixture

- [x] T001 [P] [US1] Create the package-free managed order-processing fixture with generic overloads, a decoy call, factory/DI wiring, and checks under `examples/brownfield-bug-lab/managed/`.
- [x] T002 [P] [US1] Add analysis-only .NET Framework, MFC/interop, NHibernate mapping/config, and decoy artifacts under `examples/brownfield-bug-lab/legacy/`.
- [x] T003 [US1] Add the frozen bug report, task prompt, ground-truth answer key, and structured result schema under `examples/brownfield-bug-lab/evaluation/`.

## Phase 2 - Seed and measure the bug task

- [x] T004 [US1] Implement an isolated-copy seed/fix workflow that reproduces the same failing managed check without leaving the committed demo broken.
- [x] T005 [US1] Implement a deterministic evaluator that computes precision/recall for answer-key findings and checks exact generic/overload target edges against BrownPants JSON.
- [x] T006 [US1] Run the identical baseline and BrownPants-assisted task, independently validate both fixes, and record all results and deviations.

## Phase 3 - Publish the evidence

- [x] T007 [US1] Write the evidence-backed comparison and update the README to explain the measured developer and QA value, mixed-language limits, and results.
- [x] T008 [US1] Run managed demo checks, BrownPants inventory/impact checks, evaluator, acceptance hash/evidence validators, and `git diff --check`; record outcomes.

## Dependencies

T001-T003 establish the fixture and frozen task; T004-T005 depend on it; T006 depends on the passing evaluator and reproducible seed; T007 uses only audited T006 evidence; T008 is the publication gate.
