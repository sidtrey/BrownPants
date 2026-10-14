# Tasks: Spec Kit Bug Assessment Evidence

**Input**: Design documents from `specs/018-speckit-bug-evidence/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, and `contracts/bug-evidence-hook.md`

**Tests**: Required by the specification and project constitution. Run the PowerShell adapter test against a temporary Spec Kit project.

## Phase 1: Setup

**Purpose**: Use the existing Spec Kit adapter and test harness; no new project setup is required.

## Phase 2: Foundational

**Purpose**: Extend existing adapter checks so the new optional event and command are required.

- [x] T001 Update static assertions in `tests/SpeckitExtensionTests.ps1` to require optional `before_bug_assess` registration and the declared `speckit.brownpants.bug-evidence` command.
- [x] T002 Update the live temporary-project test in `tests/SpeckitExtensionTests.ps1` to install BrownPants without the bug extension, then install the bundled `bug` extension and verify all eleven hook registrations, dispatch timing, and the installed bug-evidence skill.

**Checkpoint**: Static and live adapter assertions cover event registration, skill generation, and dispatch timing.

## Phase 3: User Story 1 - Find repository leads during bug assessment (Priority: P1)

**Goal**: A bug assessment can receive a bounded, clue-linked BrownPants report beside its official assessment.

**Independent Test**: A live temporary Spec Kit project with both extensions registers the event and the evidence command instructs the agent to scan pasted clues and write the report in the active bug directory.

### Tests for User Story 1

- [x] T003 [US1] Add test assertions for command safety, clue bounds, report path, skip behavior, and evidence limitations in `tests/SpeckitExtensionTests.ps1`.

### Implementation for User Story 1

- [x] T004 [US1] Add `speckit.brownpants.bug-evidence` to `integrations/speckit/extension.yml` and `integrations/speckit/extension.yml.template` with its command path.
- [x] T005 [US1] Register optional `before_bug_assess` in both extension manifests and add its optional project hook in `.specify/extensions.yml`.
- [x] T006 [US1] Create `integrations/speckit/commands/bug-evidence.md` with safe clue selection, CLI fallback, report writing, skip behavior, and no-causality guidance.
- [x] T007 [US1] Update installed adapter copies in `.specify/extensions/brownpants/extension.yml`, `.specify/extensions/brownpants/extension.yml.template`, and `.specify/extensions/brownpants/commands/bug-evidence.md`.

**Checkpoint**: The command and hook produce reviewable report instructions without replacing Spec Kit's assessment command.

## Phase 4: User Story 2 - Preserve the existing Spec Kit bug workflow (Priority: P1)

**Goal**: The optional integration stays fail-open and does not duplicate or disrupt official assess/fix/test commands.

**Independent Test**: Install BrownPants without the bug extension and verify existing lifecycle integration passes; with the bug extension, verify hook registration is optional and evidence command skip cases explicitly continue assessment.

### Tests for User Story 2

- [x] T008 [US2] Assert core hooks remain mandatory and unchanged, the new hook is optional, and no BrownPants assess/fix/test commands are introduced in `tests/SpeckitExtensionTests.ps1`.

### Implementation for User Story 2

- [x] T009 [US2] Document the optional bundled bug-extension prerequisite, installation, and evidence limits in `integrations/speckit/README.md` and `integrations/README.md`.

**Checkpoint**: Projects without the bundled bug extension continue to use all current BrownPants lifecycle hooks.

## Phase 5: Polish & Cross-Cutting Concerns

- [x] T010 Run the Spec Kit live adapter test, .NET Release build and xUnit suite, OpenSpec adapter test, package smoke check, and `git diff --check`; record results in `specs/018-speckit-bug-evidence/quickstart.md`.
- [x] T011 Review generated and installed manifests for parity and verify each requirement is covered by tests or a manual smoke scenario.

## Dependencies & Execution Order

- T001-T003 establish failing integration assertions before implementation.
- T004-T007 implement the optional hook and command; T005 depends on the command ID from T004.
- T008-T009 verify and explain fail-open behavior.
- T010-T011 complete validation and parity review.
- User stories are related through the shared adapter but can be validated independently: US1 tests successful evidence registration; US2 tests absence and skip behavior.

## Parallel Opportunities

T001 and T002 both touch the PowerShell test file and must be sequential. T004 and T006 can be drafted independently, but both should be applied sequentially to keep manifest and command contract synchronized. Documentation and the test finalization depend on the implemented command semantics.

## Implementation Strategy

Deliver the MVP by adding the optional assessment evidence hook and verifying it in the actual Spec Kit bug extension installation. Then verify fail-open behavior, update docs, and run all relevant quality gates.
