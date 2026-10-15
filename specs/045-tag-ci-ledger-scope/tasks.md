# Tasks: Release Tag Acceptance Ledger Scope

**Input**: [spec.md](spec.md), [plan.md](plan.md)

## Phase 1: Event Range Selection (US1)

- [x] T001 [US1] Extract event-aware changed-artifact selection into `scripts/get-changed-spec-artifacts.ps1`, preserving pull request, regular push, new branch, tag parent, and root-commit cases.
- [x] T002 [US1] Replace inline Git range selection in `.github/workflows/ci.yml` with the tested helper and pass `github.ref_type` to it.
- [x] T003 [US1] Extend `tests/AcceptanceCriteriaIntegrityWorkflowTests.ps1` with temporary Git-history scenarios for AC-001 through AC-004, and invoke this contract test in CI.

## Phase 2: Patch Release Verification (US1)

- [x] T004 [US1] Record workflow behavior and CI regression evidence in `specs/045-tag-ci-ledger-scope/verification.md`; map all tasks to frozen criteria in the acceptance ledger.
- [x] T005 [US1] Bump the CLI and Spec Kit extension to 0.2.1, update install/CI smoke versions, and add `docs/releases/0.2.1.md` describing the corrective release.
- [x] T006 [US1] Run workflow contract tests, full Release build/tests, package audit, packed-tool smoke, acceptance hash/evidence validation, cross-platform branch CI, then push the v0.2.1 tag and verify its CI and package digest.
