# Feature Specification: Full Spec Kit Lifecycle Context Refresh

**Feature Branch**: `004-speckit-lifecycle-context`
**Created**: 2026-10-14
**Status**: Draft
**Input**: Refresh BrownPants evidence across the supported Spec Kit lifecycle, with phase-specific guidance and verified Codex hook execution.

## User Scenarios & Testing

### User Story 1 - Refresh evidence before implementation work (Priority: P1)

As a developer using Spec Kit on a brownfield repository, I want BrownPants context refreshed before each lifecycle step so clarification, planning, task generation, implementation, analysis, and follow-on actions account for current repository evidence.

**Why this priority**: Spec Kit revisits requirements and artifacts over multiple phases; repository state can change between any two phases.

**Independent Test**: Install the extension in a disposable Spec Kit/Codex project, resolve nine pre-hooks and one post-hook through the official CLI, then invoke representative Codex skills and verify the BrownPants command runs at the right point.

**Acceptance Scenarios**:

1. **Given** a Spec Kit project with the BrownPants extension installed, **When** any supported lifecycle command runs, **Then** its mandatory pre-hook refreshes repository evidence.
2. **Given** an active Spec Kit artifact and current repository source, **When** clarify, checklist, analyze, or task-to-issues runs, **Then** context guidance identifies which artifacts and evidence to compare.
3. **Given** implementation tasks are marked complete, **When** `$speckit-implement` finishes, **Then** the mandatory post-hook refreshes repository evidence before the completion report.
4. **Given** C# or VB.NET and native findings, **When** context is applied to any phase, **Then** candidate evidence and analysis limits remain explicit and source files are verified directly.

### User Story 2 - Keep the shared context instructions phase-aware (Priority: P2)

As a contributor, I want one context command to explain how evidence informs every supported Spec Kit phase, so hooks do not need divergent copies of analyzer guidance.

**Independent Test**: Review the extension command and Codex skill for explicit guidance for all nine hooked phases and matching evidence limits.

## Edge Cases

- If BrownPants is unavailable, a mandatory hook reports the missing prerequisite and the agent must not invent scan evidence.
- If the scan reports changed or missing files, the agent checks the actual worktree and active feature artifacts before updating tasks.
- `--semantic` remains opt-in and is used only when the project is trusted.

## Requirements

### Functional Requirements

- **FR-001**: The Spec Kit extension MUST register mandatory hooks before `constitution`, `specify`, `clarify`, `plan`, `checklist`, `tasks`, `analyze`, `implement`, and `taskstoissues`, plus after `implement`, to the same BrownPants context command.
- **FR-002**: The shared context instructions MUST explain how to use repository evidence during all nine pre-hooked phases and the post-implementation review.
- **FR-003**: Before task generation, the agent MUST inspect the active spec and plan against current repository manifests, source paths, tests, and analysis coverage.
- **FR-004**: Before implementation, the agent MUST inspect active plan/tasks and relevant current source and project files, and treat the BrownPants report as a navigation map.
- **FR-005**: All phases MUST preserve evidence limits: inventory-only languages are not parsed, lexical/syntax matches are candidates, and parser or project-load errors do not prove build failures.
- **FR-006**: README and integration documentation MUST describe all registered hooks and the local BrownPants tool fallback.
- **FR-007**: The extension MUST declare the minimum Spec Kit version required to support all registered lifecycle hooks.
- **FR-008**: The post-implementation guidance MUST require review of the task list, worktree changes, and actual build/test results; a refreshed inventory alone MUST NOT be presented as validation.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The official Spec Kit CLI installs the extension and reports exactly ten active mandatory hooks: nine pre-hooks and one post-implementation hook.
- **SC-002**: Codex runs the BrownPants context command before both `$speckit-tasks` and `$speckit-implement` in a disposable project.
- **SC-003**: The context command and generated Codex skill provide consistent phase-aware guidance for all ten hook registrations.
- **SC-004**: All existing BrownPants tests pass and the Release tool package builds.

## Assumptions

- Spec Kit 1.0.0 and later support all nine registered lifecycle events and Codex workflow skills honor mandatory hooks.
- Repository inventory should refresh at phases that translate plans into executable tasks and source edits.
- One shared context command is sufficient; phase-specific instructions can be selected from the active command context.
