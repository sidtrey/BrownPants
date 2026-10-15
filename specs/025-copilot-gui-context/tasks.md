# Tasks: Copilot GUI Context Workflow

**Input**: Design documents from `/specs/025-copilot-gui-context/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the feature specification. Write installer and prompt checks before implementation.

## Phase 1: User Story 1 - Use BrownPants from Copilot chat (Priority: P1)

**Goal**: A GUI user can invoke a reusable BrownPants evidence workflow and receive evidence-limited guidance.

**Independent Test**: Run prompt-content assertions and verify all required collection, fallback, and source-validation instructions.

### Tests for User Story 1

- [ ] T001 [P] [US1] Add prompt contract assertions for bounded inventory, manual fallback, safe default mode, and evidence limits in `tests/CopilotPromptIntegrationTests.ps1`

### Implementation for User Story 1

- [ ] T002 [US1] Create Copilot prompt frontmatter and evidence-first context workflow in `integrations/copilot/prompts/brownpants-context.prompt.md`
- [ ] T003 [P] [US1] Document host support, prompt invocation, and policy limits in `integrations/copilot/README.md`

## Phase 2: User Story 2 - Install or update the shared workflow safely (Priority: P1)

**Goal**: A maintainer can install the prompt without overwriting team-owned content.

**Independent Test**: Run the PowerShell installer checks against temporary target roots and verify create, identical no-op, conflict preservation, and invalid-root failure.

### Tests for User Story 2

- [ ] T004 [P] [US2] Add absent, identical, conflicting, and invalid-root installation checks in `tests/CopilotPromptIntegrationTests.ps1`

### Implementation for User Story 2

- [ ] T005 [US2] Add a local-only, path-contained, conflict-preserving installer in `integrations/copilot/Install.ps1`
- [ ] T006 [P] [US2] Document installation, host prerequisites, and manual-report fallback in `integrations/copilot/README.md`

## Phase 3: Polish & Cross-Cutting Concerns

- [ ] T007 [P] Add the Copilot integration check to `.github/workflows/ci.yml` and list the adapter in `integrations/README.md`
- [ ] T008 Update the root `README.md` with a Copilot GUI integration link and a concise supported-host limitation
- [ ] T009 Run `pwsh -NoProfile -File tests/CopilotPromptIntegrationTests.ps1`, the full build and xUnit suite, acceptance hash/evidence checks, and adapter tests; record results in `specs/025-copilot-gui-context/verification.md`

## Dependencies & Execution Order

- T001 precedes T002; T004 precedes T005. T003 and T006 share one README and therefore run sequentially.
- T007 and T008 follow the integration artifacts. T009 is the final verification gate.
- US1 and US2 are distinct deliverables, but the installer copies the prompt created by US1.

## Parallel Opportunities

- T001 can be written independently of T003.
- T007 and T008 touch separate documentation/workflow files after the installer contract is stable.
- The prompt test and installer test tasks both touch one test script and should be applied sequentially.

## Implementation Strategy

Deliver the manually invoked prompt first, then the safe installer, then documentation and CI. Verify prompt behavior through assertions and test installation against temporary repositories. Host GUI launch tests are out of scope; documentation must identify that limit.
