# Tasks: Verify Spec Kit Setup with Codex

**Input**: Design artifacts in `specs/012-codex-speckit-smoke-test/`

**Tests**: Required by the specification and project constitution.

## Phase 1: User Story 1 - Verify Codex integration during CI (Priority: P1)

**Independent Test**: Run the live Spec Kit integration check and confirm Codex skills plus all ten BrownPants hooks in a temporary project.

- [x] T001 [US1] Initialize the live Spec Kit fixture with Codex skills and non-interactive flags in `tests/SpeckitExtensionTests.ps1`.
- [x] T002 [US1] Assert generated Codex skills before installing BrownPants in `tests/SpeckitExtensionTests.ps1`.
- [x] T003 [US1] Preserve extension installation, ten-hook assertions, and temporary fixture cleanup in `tests/SpeckitExtensionTests.ps1`.
- [x] T004 [US1] Run the required Spec Kit integration, build/test, and OpenSpec adapter checks.

## Dependencies

- T001 precedes T002 and T003; T004 verifies the completed scenario.

## Implementation Strategy

Exercise the documented Codex setup in the existing live adapter test, then verify the complete repository quality gates.
