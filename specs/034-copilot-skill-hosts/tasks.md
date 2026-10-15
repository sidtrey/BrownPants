# Tasks: BrownPants GUI Skill for GitHub Copilot

**Input**: Design documents from `/specs/034-copilot-skill-hosts/`

## Phase 1: User Story 1 - Use BrownPants from Copilot agent experiences (P1)

**Independent Test**: Run `pwsh -NoProfile -File tests/AgentSkillIntegrationTests.ps1` and confirm Copilot and all-host install behavior, docs, and unchanged Codex/Claude behavior.

### Tests

- [X] T001 [US1] Add AC-001–AC-003 and SC-001–SC-002 installer assertions for Copilot-only, all-host, rerun, conflict, and all-target preflight in `tests/AgentSkillIntegrationTests.ps1`.

### Implementation

- [X] T002 [US1] Implement AC-001–AC-003 and SC-001–SC-002 with `copilot` and `all` installer selections mapped to `.github/skills/brownpants-context` in `integrations/agent-skills/Install.ps1`, preserving existing selection semantics.
- [X] T003 [US1] Document AC-004–AC-005 and SC-003 host support, fallback, discovery, and prompt-vs-skill limits in `integrations/agent-skills/README.md`, `integrations/copilot/README.md`, `integrations/README.md`, and `README.md`.

## Phase 2: Verification

- [X] T004 Verify AC-001–AC-005 and SC-001–SC-004 by running the PowerShell integration check, full Release build/test suite, acceptance hash/evidence validation, Spec Kit and OpenSpec adapter checks; record observed results in `specs/034-copilot-skill-hosts/verification.md`.

## Dependencies & Execution Order

- T001 precedes T002; the test must fail against the current installer before implementation.
- T003 may be completed after the destination contract is stable.
- T004 is the completion gate.

## Implementation Strategy

Prove safe install and preflight behavior first, implement the new targets through the existing target-definition mechanism, then document supported host paths and run offline integration checks.
