# Tasks: OpenSpec BrownPants Context Adapter

**Input**: Design documents in `specs/003-openspec-context-adapter/`
**Prerequisites**: Spec Kit `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/context-skill.md`, and `quickstart.md`
**Tests**: Explicitly required by success criteria. Use disposable fixtures and existing xUnit suite.

## Phase 1: Setup

- [X] T001 Create the OpenSpec adapter feature and context artifacts using Spec Kit scripts.

## Phase 2: User Story 1 - Ground OpenSpec work in evidence (Priority: P1)

**Independent Test**: Invoke the skill in a Codex/OpenSpec fixture and verify report output and change/source navigation guidance.

- [X] T002 [P] Create `integrations/openspec/skills/openspec-brownpants-context/SKILL.md` with BrownPants execution fallback, OpenSpec artifact navigation, and evidence limits.
- [X] T003 Create `integrations/openspec/Install.ps1` to install the skill under `.agents/skills/openspec-brownpants-context/` without modifying OpenSpec-managed files.
- [X] T004 [P] Write `integrations/openspec/README.md` with OpenSpec/Codex setup, manual invocation, and no-hook limitation.

## Phase 3: User Story 2 - Safe, repeatable installation (Priority: P2)

**Independent Test**: In a temporary OpenSpec project, install once, install again unchanged, then alter destination and confirm installer refuses to overwrite it.

- [X] T005 Add and run `tests/OpenSpecInstallerTests.ps1` to verify installation, idempotent reinstallation, conflict preservation, and unchanged OpenSpec files.
- [X] T006 Run `$openspec-brownpants-context` through Codex in a disposable OpenSpec project and verify the BrownPants report is used before workflow work.

## Phase 4: Polish

- [X] T007 Update root `README.md` and integration overview to list OpenSpec as a second SDD adapter.
- [X] T008 Run `dotnet test BrownPants.sln`, Release package creation, whitespace checks, and verify no OpenSpec-managed files changed in the fixture.

## Phase 5: Shared SDD workflow inventory

- [X] T009 Add a generic workflow inventory model and detect Spec Kit/OpenSpec artifacts, capability directories, and non-archived active changes.
- [X] T010 Render bounded artifact samples in Markdown and serialize workflow metadata to JSON.
- [X] T011 Add fixtures for OpenSpec-only and combined Spec Kit/OpenSpec repositories, including archived changes and sample-limit behavior.
- [X] T012 Run the full test suite and adapter checks; verify deterministic reports and update user documentation.

## Dependencies

T002 precedes T003 and T006. T003 precedes T005 and T006. T004 and T007 are independent documentation tasks. T008 follows implementation and integration validation.
