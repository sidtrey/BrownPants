# Tasks: Cursor BrownPants Context Command

**Input**: Design documents from `/specs/037-cursor-context-command/`

## Phase 1: Command and safe installer

**Independent Test**: Install the command in a temporary repository and verify the slash-command contract, idempotency, conflict preservation, invalid roots, and link containment.

- [x] T001 [US1] Add temporary-project tests for command content, destination, idempotency, conflicts, invalid roots, and reparse-point escape handling in `tests/CursorCommandIntegrationTests.ps1`.
- [x] T002 [US1] Add the evidence-first Cursor command with terminal-denied fallback in `integrations/cursor/commands/brownpants-context.md`.
- [x] T003 [US1] Add a contained, idempotent installer for `.cursor/commands/brownpants-context.md` in `integrations/cursor/Install.ps1`.

## Phase 2: Integration and docs

- [x] T004 [US1] Add the Cursor integration test to `.github/workflows/ci.yml`.
- [x] T005 [US2] Document installation, `/brownpants-context`, policy fallback, Cursor beta status, and live-GUI evidence limits in `integrations/cursor/README.md` and the root `README.md`.

## Phase 3: Verification

- [x] T006 Verify AC-001–AC-006 and SC-001–SC-004 with the Cursor fixture test, adapter checks, Release build/tests, acceptance hash/evidence checks, and BrownPants-on-BrownPants context; record actual outputs in `specs/037-cursor-context-command/verification.md`.

## Dependencies

T001 precedes T002/T003. T004/T005 follow the command and installer. T006 is the completion gate.
