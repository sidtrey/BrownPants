# Tasks: Later-Phase Spec Kit Context Refresh

**Input**: Design documents in `specs/004-speckit-lifecycle-context/`
**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, and `quickstart.md`
**Tests**: Required by success criteria; validate both official hook registration and Codex skill execution.

## Phase 1: Setup

- [X] T001 Create the feature and design artifacts with Spec Kit scripts, including before_specify and before_plan BrownPants context runs.

## Phase 2: User Story 1 - Refresh evidence before tasks and implementation (Priority: P1)

**Independent Test**: Official Spec Kit resolves nine pre-hooks and one post-hook to mandatory BrownPants commands; Codex executes hooks at the correct workflow points.

- [X] T002 Add mandatory `before_tasks` and `before_implement` hooks to `integrations/speckit/extension.yml` and `extension.yml.template`.
- [X] T003 [P] Add the matching registrations to `.specify/extensions/brownpants/extension.yml`, `.specify/extensions/brownpants/extension.yml.template`, and `.specify/extensions.yml`.
- [X] T004 Update `integrations/speckit/commands/context.md`, `.specify/extensions/brownpants/commands/context.md`, and `.agents/skills/speckit-brownpants-context/SKILL.md` with task and implementation evidence guidance.

## Phase 3: User Story 2 - Keep lifecycle instructions discoverable (Priority: P2)

**Independent Test**: Adapter and root documentation name all nine phases and explain the local tool fallback and evidence limits.

- [X] T005 [P] Update `integrations/speckit/README.md`, `.specify/extensions/brownpants/README.md`, and root `README.md` to describe all mandatory hooks.

## Phase 4: Verification

- [X] T006 Install the extension with the official Spec Kit CLI in a disposable project and verify mandatory hook registrations.
- [X] T007 Invoke representative `$speckit-constitution`, `$speckit-tasks`, and `$speckit-implement` Codex skills in disposable projects; verify the context command completes before each workflow.
- [X] T008 Add `tests/SpeckitExtensionTests.ps1` to keep mandatory hook registrations and phase-aware guidance synchronized.
- [X] T009 Run `dotnet test BrownPants.sln`, both PowerShell integration checks, create the Release tool package, and run `git diff --check`.

## Phase 5: Full lifecycle coverage

- [X] T010 Register mandatory context hooks before clarify, checklist, analyze, constitution, and task-to-issues in all extension and project manifests; require Spec Kit 1.0 or later.
- [X] T011 Extend shared context guidance, adapter documentation, and tests to cover all nine supported lifecycle phases.
- [X] T012 Verify all nine pre-hooks with the official Spec Kit CLI and rerun the adapter test scripts.
- [X] T013 Register a mandatory `after_implement` hook and add post-implementation guidance that compares tasks, worktree changes, and actual test/build results.
- [X] T014 Verify all ten hooks with the official Spec Kit CLI and run a Codex implementation smoke test proving the post-hook follows implementation but precedes the completion report.

## Dependencies

T002 and T003 precede T006. T004 precedes T007. T005 and T008 are independent. T006, T007, and T008 precede T009.
