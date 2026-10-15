# Tasks: Copilot GUI Context Workflow

**Input**: Design documents from `/specs/026-copilot-gui-context/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the feature specification. Write prompt and installer checks before implementation.

## Phase 1: User Story 1 - Use BrownPants from Copilot chat (Priority: P1)

**Goal**: A GUI user can invoke a reusable BrownPants evidence workflow and receive evidence-limited guidance.

**Independent Test**: Run prompt-content assertions for bounded inventory, evidence citations, caveats, and user-driven fallback.

### Tests for User Story 1

- [x] T001 [P] [US1] Add prompt contract assertions for bounded inventory, fallback, evidence limits, and supported-host caveats in `tests/CopilotPromptIntegrationTests.ps1`

### Implementation for User Story 1

- [x] T002 [US1] Create Copilot prompt metadata and evidence-first workflow in `integrations/copilot/prompts/brownpants-context.prompt.md`

## Phase 2: User Story 2 - Install the shared workflow safely (Priority: P1)

**Goal**: A maintainer can install the prompt without overwriting team-owned content or writing outside the selected project.

**Independent Test**: Run installer checks against temporary roots and verify installation, idempotency, conflict preservation, invalid-root handling, and destination containment.

### Tests for User Story 2

- [x] T003 [P] [US2] Add absent, identical, conflict, invalid-root, and path-escape installer cases in `tests/CopilotPromptIntegrationTests.ps1`

### Implementation for User Story 2

- [x] T004 [US2] Implement local prompt installation with root containment checks and conflict preservation in `integrations/copilot/Install.ps1`

## Phase 3: Polish & Cross-Cutting Concerns

- [x] T005 [P] Document install/use steps, prerequisites, supported Copilot hosts, harness limits, and manual fallback in `integrations/copilot/README.md`
- [x] T006 [P] Link the adapter from `integrations/README.md` and `README.md`
- [x] T007 Add the installer/prompt integration check to `.github/workflows/ci.yml`
- [x] T008 Run the integration check on Windows, Linux, and macOS, full build and xUnit suite, acceptance digest/evidence checks, and existing adapter checks; record observed results in `specs/026-copilot-gui-context/verification.md`

## Dependencies & Execution Order

- T001 precedes T002. T003 precedes T004. T005 and T006 use separate documentation files and can run independently after the prompt contract is stable.
- T007 follows creation of the integration test script. T008 is the final verification gate.
- User Story 2 installs the prompt delivered by User Story 1.

## Parallel Opportunities

- T001 and T006 touch separate files and can be authored independently.
- T005 and T006 touch separate documentation files.
- Test tasks T001 and T003 share a script and must be applied sequentially.

## Implementation Strategy

Deliver and verify the chat prompt, then add the safe installer and its failure cases, then document and wire CI. Host UI behavior is documented from official support; repository CI validates artifacts and installer behavior, not GUI launches.
