# Feature Specification: Verify Spec Kit Setup with Codex

**Feature Branch**: `012-codex-speckit-smoke-test`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User goal: prove BrownPants works with Spec Kit in Codex CLI projects.

## User Scenarios & Testing

### User Story 1 - Verify Codex integration during CI (Priority: P1)

A maintainer wants CI to exercise the documented Spec Kit setup path so changes to either Spec Kit or BrownPants do not silently break Codex skill installation or BrownPants lifecycle hooks.

**Independent Test**: With the supported Spec Kit CLI installed, initialize a temporary Codex project, install the local BrownPants extension, and verify its skills and hooks.

**Acceptance Scenarios**:

1. **Given** an empty temporary project, **When** Spec Kit initializes with Codex skills, **Then** the standard specify, plan, and implement skills exist under `.agents/skills`.
2. **Given** the initialized project, **When** BrownPants is installed as a local extension, **Then** Spec Kit lists BrownPants as enabled with all ten hooks.

## Edge Cases

- Spec Kit is absent locally: the live check may skip in developer runs, but required CI must fail if it is absent.
- Initialization must be non-interactive and must not require Codex itself to be installed.
- The temporary project must be removed after success or failure.

## Requirements

### Functional Requirements

- **FR-001**: The live installer check MUST initialize a temporary project using the documented Codex skills integration.
- **FR-002**: The check MUST verify representative Spec Kit skills are generated in `.agents/skills`.
- **FR-003**: The check MUST install the repository's local BrownPants extension and verify all ten lifecycle hooks are enabled.
- **FR-004**: The required CI mode MUST fail when the Spec Kit CLI is unavailable; optional local runs MAY skip the live check.
- **FR-005**: Temporary files MUST be isolated and cleaned up after the check.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The live integration test passes on Windows, macOS, and Ubuntu CI.
- **SC-002**: The test detects missing Codex skill files or BrownPants lifecycle registrations.
- **SC-003**: The test completes without interactive input or a Codex CLI installation.

## Assumptions

- CI pins a supported Spec Kit CLI version and installs it before running the adapter test.
- Spec Kit's Codex integration writes agent skills below `.agents/skills`.
