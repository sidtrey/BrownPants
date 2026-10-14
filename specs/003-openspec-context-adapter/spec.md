# Feature Specification: OpenSpec BrownPants Context Adapter

**Feature Branch**: `003-openspec-context-adapter`
**Created**: 2026-10-14
**Status**: Draft
**Input**: Provide OpenSpec users a Codex skill that applies BrownPants repository evidence before proposal or implementation work.

## User Scenarios & Testing

### User Story 1 - Ground OpenSpec work in repository evidence (Priority: P1)

As a developer using OpenSpec with Codex, I can invoke a BrownPants context skill before proposing or applying a change, so the workflow begins from the actual repository structure and source evidence.

**Why this priority**: Brownfield context is BrownPants' primary value and the necessary first integration with another SDD engine.

**Independent Test**: In an OpenSpec/Codex fixture, invoke the skill and verify it runs `brownpants inspect`, summarizes report limits, and directs the agent to relevant change and source files.

**Acceptance Scenarios**:

1. **Given** an OpenSpec project and an installed BrownPants tool, **When** the context skill is invoked before `$openspec-propose`, **Then** it reports detected ecosystems, language coverage, and relevant `openspec/specs/` and `openspec/changes/` paths.
2. **Given** an active OpenSpec change, **When** the skill is invoked before `$openspec-apply-change`, **Then** it directs the agent to review that change's `proposal.md`, `design.md`, `tasks.md`, delta specs, and relevant source files.
3. **Given** unsupported source languages, **When** the report is summarized, **Then** they are described only at their stated inventory coverage level.

### User Story 2 - Install the adapter without replacing OpenSpec files (Priority: P2)

As an OpenSpec/Codex user, I can install the BrownPants skill into the shared Codex skills directory without modifying OpenSpec-managed skills.

**Independent Test**: Install into a disposable OpenSpec project and verify the skill is discoverable alongside OpenSpec skills and the installation is safe to repeat.

**Acceptance Scenarios**:

1. **Given** OpenSpec skills in `.agents/skills/`, **When** the BrownPants adapter is installed, **Then** a separate `openspec-brownpants-context` skill is added without changing existing OpenSpec skills.
2. **Given** the skill is already installed, **When** installation is repeated, **Then** it reports the existing destination and does not overwrite a user-edited file.

### User Story 3 - Include OpenSpec state in the shared evidence report (Priority: P1)

As a developer using any supported SDD workflow, I want the BrownPants report to identify OpenSpec specifications and active changes alongside Spec Kit state, so the shared inventory remains useful across engines.

**Independent Test**: Scan fixtures containing OpenSpec only and both OpenSpec and Spec Kit; verify workflow detection, active versus archived change counts, bounded artifact paths, and deterministic JSON/Markdown output.

### Edge Cases

- If BrownPants is unavailable globally and no repository-local tool shim exists, the skill reports the prerequisite and makes no architectural claims.
- If multiple OpenSpec changes are active, the skill asks which change is in scope instead of choosing arbitrarily.
- If OpenSpec artifacts use a customized schema, the skill uses the actual files present and does not assume every standard artifact exists.

## Requirements

### Functional Requirements

- **FR-001**: The adapter MUST provide a Codex skill named `openspec-brownpants-context`.
- **FR-002**: The skill MUST run `brownpants inspect . --format markdown`, with the documented `.tools` fallback for Windows and Unix-like systems.
- **FR-003**: The skill MUST direct the agent to inspect OpenSpec source-of-truth specs under `openspec/specs/` and change artifacts under `openspec/changes/` when present.
- **FR-004**: The skill MUST distinguish observed repository evidence, OpenSpec requirements, and proposed design decisions.
- **FR-005**: The installer MUST add the skill without overwriting existing OpenSpec-managed or user-edited skills.
- **FR-006**: Documentation MUST state that this adapter has no Spec Kit-style lifecycle hooks and invocation is explicit.
- **FR-007**: The adapter MUST preserve BrownPants' analysis limits and never imply inventory-only languages are parsed.
- **FR-008**: The core report MUST identify supported SDD workflows independently and include OpenSpec current-spec and active-change counts when `openspec/` artifacts are present.
- **FR-009**: OpenSpec artifact paths in the report MUST respect the sample limit, report omitted counts, and exclude archived changes from the active-change count.

## Success Criteria

### Measurable Outcomes

- **SC-001**: On a disposable OpenSpec/Codex project, the skill is discoverable and successfully runs the BrownPants inventory command.
- **SC-002**: A smoke invocation reports paths for the active OpenSpec change and relevant source files without modifying application files.
- **SC-003**: Reinstalling the adapter preserves a pre-existing user-edited destination file.
- **SC-004**: All existing BrownPants tests pass and the Release tool package builds.
- **SC-005**: Markdown and JSON reports identify an OpenSpec-only repository and a repository containing both OpenSpec and Spec Kit, with active and archived changes distinguished.

## Assumptions

- OpenSpec's Codex integration uses `.agents/skills/openspec-*` skills and its standard artifacts use `openspec/specs/` and `openspec/changes/<change>/`.
- The adapter adds an explicit Codex skill; it does not patch generated OpenSpec skills or claim automatic lifecycle execution.
- The installed BrownPants CLI is available through PATH or `.tools`.
