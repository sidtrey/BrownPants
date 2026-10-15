# Feature Specification: Copilot GUI Context Workflow

**Feature Branch**: `026-copilot-gui-context`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Make BrownPants practical for GUI-based AI users, including Visual Studio and VS Code users, and teams that cannot use AI-specific CLI or SDK clients."

## User Scenarios & Testing

### User Story 1 - Use BrownPants from Copilot chat (Priority: P1)

A developer using GitHub Copilot in Visual Studio or VS Code wants a reusable, user-invoked chat workflow that provides BrownPants evidence within the GUI.

**Why this priority**: GUI access is an identified user-experience gap. Teams need BrownPants to fit the assistant experience they already use.

**Independent Test**: Install the workflow in a temporary repository, invoke it in a supported Copilot GUI, and verify its instructions for bounded evidence, source validation, and unavailable-tool fallback.

**Acceptance Scenarios**:

1. **AC-001** Given a supported Copilot chat with the repository prompt installed, when a user invokes the BrownPants context workflow, then the prompt requests a bounded Markdown repository inventory and instructs the assistant to validate relevant claims against source, project, and test files.
2. **AC-002** Given BrownPants inventory output, when the assistant summarizes it, then it distinguishes observed facts from candidate signals and states evidence limits without claiming runtime behavior, buildability, or root cause from inventory alone.
3. **AC-003** Given the host cannot run terminal commands or BrownPants is unavailable, when the workflow is invoked, then it asks the user to run BrownPants or provide an existing report and does not invent findings or install tools.

### User Story 2 - Install the shared workflow safely (Priority: P1)

A maintainer wants to add the Copilot workflow to an existing repository without changing application files or overwriting team-owned prompt content.

**Why this priority**: The GUI workflow must be safe and straightforward to adopt in real repositories.

**Independent Test**: Run the installer against temporary repository roots with an absent, identical, conflicting, and invalid destination, then verify the requested outcomes and file preservation.

**Acceptance Scenarios**:

1. **AC-004** Given an existing project without the BrownPants prompt, when the installer runs, then it creates the prompt under `.github/prompts/` and reports the installed path.
2. **AC-005** Given the identical prompt is already installed, when the installer runs again, then it succeeds without rewriting that file.
3. **AC-006** Given a different file already occupies the prompt destination, when the installer runs, then it fails with an actionable conflict message and preserves the file contents.
4. **AC-007** Given a missing, invalid, or path-escaping project root, when the installer runs, then it fails without writing outside the selected project.

## Edge Cases

- Project root is missing, is a file, or resolves through a link outside the selected directory.
- Prompt destination is a directory or is a user-edited file.
- BrownPants returns an error or no report is available.
- Copilot GUI is available but repository prompts or terminal tools are disabled by host configuration or organization policy.
- Installer is rerun when the installed prompt is already identical.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST provide a manually invoked, repository-shareable BrownPants context workflow for GitHub Copilot chat in Visual Studio and VS Code.
- **FR-002**: The workflow MUST request bounded BrownPants evidence and require verification against cited source, project, and test files before source-level claims.
- **FR-003**: The workflow MUST distinguish observed facts, candidate signals, and evidence limits, and MUST NOT claim inventory alone proves buildability, runtime behavior, or root cause.
- **FR-004**: The workflow MUST explain a user-driven fallback when BrownPants or host terminal access is unavailable, without fabricating output or installing software.
- **FR-005**: The installer MUST create the prompt in a selected project, leave identical installations unchanged, and preserve/report conflicting content.
- **FR-006**: The installer MUST reject invalid roots and destinations resolving outside the selected project; installation MUST remain local-only and MUST NOT modify application source files.
- **FR-007**: Documentation MUST state supported hosts and invocation, prerequisites, and limitations when prompt discovery or terminal access is restricted.
- **FR-008**: The workflow MUST rely on BrownPants' existing local CLI and MUST NOT require an AI-specific CLI, SDK, service, or network access.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A user can install the shared prompt in a clean temporary project with one documented command and receives an explicit destination report.
- **SC-002**: Absent, identical, conflicting, invalid-root, and path-containment cases produce the specified installer outcomes in automated checks.
- **SC-003**: Automated checks cover bounded evidence instructions, source validation, evidence limits, and the no-terminal fallback.
- **SC-004**: The guide describes both Visual Studio and VS Code invocation and makes no claim that organization policy permits terminal execution.
- **SC-005**: The installer tests pass on Windows, Linux, and macOS without network access or project evaluation.

## Assumptions

- GitHub Copilot is the first GUI integration because both Visual Studio and VS Code document repository prompt files; supported VS Code harnesses are stated explicitly.
- BrownPants' current CLI remains the evidence engine; this feature adds no AI client, SDK, hosted service, or GUI application.
- Users and administrators control whether Copilot can access repository prompt files and terminal tools.
- Users can manually run BrownPants and paste its output when agent terminal access is unavailable.
