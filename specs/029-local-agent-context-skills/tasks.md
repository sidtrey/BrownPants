# Tasks: Local Agent Context Skills

**Input**: Design documents from `/specs/029-local-agent-context-skills/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/installer.md`, `quickstart.md`

**Tests**: Required by the acceptance criteria. Add contract and installer checks before implementation.

## Phase 1: User Story 1 - Invoke context skills in local agent hosts (P1)

**Goal**: Codex and Claude Code users can invoke the same evidence-first workflow from their local host.

**Independent Test**: Verify shared skill content covers command invocation, evidence levels, direct source checks, and policy-respecting fallback.

### Tests

- [x] T001 [US1] Add skill-content assertions for evidence limits, language coverage, manual fallback, no network, and supported host invocation in `tests/AgentSkillIntegrationTests.ps1`

### Implementation

- [x] T002 [US1] Create the canonical evidence-first skill in `integrations/agent-skills/brownpants-context/SKILL.md`
- [x] T003 [P] [US1] Document Codex and Claude Code host surfaces, invocations, policy limits, and support boundaries in `integrations/agent-skills/README.md`

## Phase 2: User Story 2 - Install host skills safely (P1)

**Goal**: Maintainers can install either or both project-local skills while preserving existing files and project boundaries.

**Independent Test**: Exercise each host selection against temporary repositories, including both-host preflight and filesystem links.

### Tests

- [x] T004 [US2] Add install, idempotence, conflict, invalid-root, both-host preflight, and path-containment checks in `tests/AgentSkillIntegrationTests.ps1`

### Implementation

- [x] T005 [US2] Implement the explicit offline Codex/Claude/both installer with contained writes in `integrations/agent-skills/Install.ps1`
- [x] T006 [P] [US2] Link the adapter from `README.md` and `integrations/README.md`, and document install/use commands and host restrictions in `integrations/agent-skills/README.md`

## Phase 3: Polish and verification

- [x] T007 [P] Wire the integration test into `.github/workflows/ci.yml`
- [x] T008 Run PowerShell adapter checks, Release build, xUnit suite, acceptance hash/evidence checks, and cross-platform CI; record observed outcomes in `specs/029-local-agent-context-skills/verification.md`

## Dependencies

- T001 precedes T002. T004 precedes T005.
- T003 and T006 share the integration README and should be applied sequentially.
- T007 follows the test script. T008 is the final verification gate.

## Parallel Opportunities

- T001 and T003 use separate files.
- T004 and T006 use separate files until installer implementation begins.

## Implementation Strategy

Deliver and validate the shared skill first, then the safe installer. Keep installation independently testable for either host and for both-host selection. CI validates files and script behavior; desktop UI behavior remains documented from official host references.
