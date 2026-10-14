# Tasks: Acceptance Criteria Integrity

**Input**: Design documents in `specs/021-acceptance-criteria-integrity/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, and contracts

**Acceptance criteria integrity**: The scenarios and success criteria are frozen in [acceptance-criteria.sha256](acceptance-criteria.sha256). Verify before and after implementation with `pwsh -NoProfile -File scripts/acceptance-criteria-hash.ps1 -SpecPath specs/021-acceptance-criteria-integrity/spec.md`. A mismatch requires explicit user review before refresh.

## Phase 1: User Story 1 - Trace Tasks to Acceptance Evidence (P1)

**Independent test**: Valid evidence ledgers pass; missing criteria, invalid task IDs, task-state mismatches, and missing evidence files fail with actionable messages.

- [X] T001 [P] [US1] Add fixture tests for complete, missing, duplicate, unknown, unsafe-path, and task-state cases in `tests/AcceptanceEvidenceTests.ps1`.
- [X] T002 [US1] Implement read-only criteria/task/evidence validation in `scripts/check-acceptance-evidence.ps1` against `specs/021-acceptance-criteria-integrity/contracts/acceptance-evidence.schema.json`.

## Phase 2: User Story 2 - Review Intentional Baseline Changes (P1)

**Independent test**: Temporary Git histories prove initial baselines pass, existing sidecar edits require a reviewer label and matching changed record, and baseline deletion fails.

- [X] T003 [P] [US2] Add temporary-Git-repository tests for new, changed, deleted, and renamed baselines plus valid/invalid review records in `tests/AcceptanceCriteriaReviewTests.ps1`.
- [X] T004 [US2] Implement base/head comparison, review-label enforcement, and digest/reason/criterion validation in `scripts/check-acceptance-criteria-review.ps1`.
- [X] T005 [US2] Integrate PR base/head and label-triggered checks, full Git history, and changed-task ledger validation in `.github/workflows/ci.yml`; add workflow assertions in `tests/AcceptanceCriteriaIntegrityWorkflowTests.ps1`.

## Phase 3: User Story 3 - Apply Integrity in Spec Kit (P2)

**Independent test**: Synchronized Spec Kit guidance explains freeze, task mapping, observed evidence, completion verification, and maintainer-reviewed amendments.

- [X] T006 [US3] Add and validate a complete `acceptance-evidence.json` for this feature in `specs/021-acceptance-criteria-integrity/acceptance-evidence.json`.
- [X] T007 [US3] Update the Spec Kit context adapter and installed skill with the integrity workflow in `integrations/speckit/commands/context.md`, `.specify/extensions/brownpants/commands/context.md`, and `.agents/skills/speckit-brownpants-context/SKILL.md`; assert parity in `tests/SpeckitExtensionTests.ps1`.
- [X] T008 [US3] Document ledger validation, reviewed rebaseline procedure, label setup, and limitations in `README.md` and `specs/021-acceptance-criteria-integrity/quickstart.md`.

## Phase 4: Verification

- [X] T009 Run the evidence and review checker tests, workflow and Spec Kit integration checks, acceptance hash tests, full Release build/test suite, and `git diff --check`; map every acceptance criterion to observed evidence.

## Dependencies

- T001 precedes T002. T003 precedes T004.
- T005 depends on T002 and T004; T006 depends on the final task list and T002.
- T007 and T008 can follow the verified workflow contracts. T009 runs after all implementation and documentation tasks.
