# Feature Specification: Local Agent Context Skills

**Feature Branch**: `029-local-agent-context-skills`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Continue the GUI and IDE access work: make BrownPants usable from Codex and Claude Code local GUI workflows without requiring their AI-specific CLI or SDK clients.

## User Scenarios & Testing

### User Story 1 - Invoke BrownPants context from local agent hosts (Priority: P1)

A developer wants to ask Codex or Claude Code for BrownPants-backed repository context from the host they already use, including their desktop or IDE experience.

**Why this priority**: GUI access can make evidence-based SDD available to teams that do not allow separate AI CLI or SDK clients.

**Independent Test**: Install the project-local skill for each host and review its invocation guidance, evidence requirements, and terminal-unavailable fallback through automated contract checks.

**Acceptance Scenarios**:

1. **AC-001** Given a project with the Codex skill installed, when a user invokes the BrownPants context workflow in a supported Codex local host, then the skill requests a bounded inventory and directs the agent to verify relevant claims against source, project, test, and SDD files.
2. **AC-002** Given a project with the Claude Code skill installed, when a user invokes the workflow in a local Claude Code session, then it provides the same evidence requirements and accurately explains the host-specific invocation.
3. **AC-003** Given BrownPants or agent terminal access is unavailable or disallowed, when the workflow is invoked, then it asks the user to run BrownPants in an approved environment or provide an existing report; it does not bypass policy or invent findings.

### User Story 2 - Install host skills safely (Priority: P1)

A maintainer wants to install one or both host workflows into a project without overwriting team-authored skills or writing outside the selected project.

**Independent Test**: Run the installer against temporary project roots for each host and both hosts; verify creation, identical no-op, conflict preservation, invalid-root rejection, and path containment.

**Acceptance Scenarios**:

1. **AC-004** Given a valid project and a selected host, when installation runs, then the shared skill is placed in that host's documented project skill location and the installer reports the relative destination.
2. **AC-005** Given the same skill is already installed, when installation runs again, then it succeeds without rewriting the file.
3. **AC-006** Given different content already exists at a destination, when installation runs, then it fails with an actionable conflict and preserves that content.
4. **AC-007** Given an invalid project root or a destination link that escapes it, when installation runs, then it fails without writing outside the selected project.

## Edge Cases

- One host destination is identical while the other conflicts during a both-host install.
- The host refreshes skills only after session restart or reload.
- Organization policy disables project skills, terminal tools, or repository access.
- BrownPants is not installed, the command fails, or no report is provided.
- The project already has a similarly named or user-edited skill.

## Requirements

### Functional Requirements

- **FR-001**: The feature MUST provide a project-local, user-invoked BrownPants context skill for Codex and Claude Code local hosts.
- **FR-002**: Both host workflows MUST share the same evidence-first behavior: bounded read-only inventory, source verification, and explicit evidence limits.
- **FR-003**: The workflow MUST explain that default C#/VB invocation evidence is syntax-based, C/C++ pattern matches are lexical candidates, and inventory-only languages require direct source verification.
- **FR-004**: The workflow MUST request user-provided evidence when the tool or terminal is unavailable and MUST NOT bypass host or organization policy.
- **FR-005**: A local installer MUST support selecting Codex, Claude Code, or both; it MUST be idempotent, preserve conflicting content, and constrain writes to the selected project root.
- **FR-006**: Documentation MUST identify the supported local host surfaces, invocation syntax, skill locations, prerequisites, and policy limitations; it MUST distinguish local Claude Code sessions from Claude Cowork/cloud sessions.
- **FR-007**: The integration MUST use BrownPants' local CLI and MUST NOT require an AI-specific CLI, SDK, network access, or hosted service.

## Success Criteria

### Measurable Outcomes

- **SC-001**: One documented command can install the skill for Codex, Claude Code, or both in a temporary project.
- **SC-002**: Automated checks cover host destinations, creation, idempotence, conflict preservation, invalid roots, and path escape refusal.
- **SC-003**: Automated contract checks confirm both host workflows share evidence limits, policy-respecting fallback, and the stated language-analysis boundaries.
- **SC-004**: The guide provides an invocation example for each host and identifies host surfaces that are not supported by the project-local skill.
- **SC-005**: Installer and contract checks pass on Windows, Linux, and macOS without network access or project evaluation.

## Assumptions

- Codex project skills use `.agents/skills/`; Claude Code project skills use `.claude/skills/` and are available in local sessions when project customization is enabled.
- Both adapters are manually invoked; no lifecycle hooks or AI-client API calls are introduced.
- Automated checks validate files and installer behavior, not live desktop GUI behavior. Host-specific support is documented from official vendor sources.
- Users can run BrownPants from an approved local shell and paste its report when host terminal access is unavailable.
