# Feature Specification: Copilot GUI Context Workflow

**Feature Branch**: `025-copilot-gui-context`

**Created**: 2026-10-14

**Status**: Won't do

**Input**: User description: "Make BrownPants practical for GUI-based AI users, including Visual Studio and VS Code users, and teams that cannot use AI-specific CLI or SDK clients."

## User Scenarios & Testing

### User Story 1 - Use BrownPants from Copilot chat (Priority: P1)

A developer using GitHub Copilot in Visual Studio or VS Code wants to request BrownPants repository evidence from a reusable chat action and continue working in the GUI they already use.

**Why this priority**: GUI access was explicitly identified as a user-experience gap, and Copilot is used in both Visual Studio and VS Code.

**Independent Test**: Install the shared workflow in a temporary repository, open it in either supported Copilot GUI, invoke the prompt, and verify that it guides the user through evidence collection and source validation.

**Acceptance Scenarios**:

1. **Given** a repository with the BrownPants command available, **When** a user invokes the BrownPants context prompt in supported Copilot chat, **Then** the prompt asks for a bounded Markdown inventory and directs the assistant to validate findings against source and project files before making claims.
2. **Given** a repository without the BrownPants command available, **When** the prompt is invoked, **Then** it explains how to obtain or provide the report and does not invent scanner findings.
3. **Given** a workspace without permission for agent terminal execution, **When** the prompt is invoked, **Then** it asks the user to run the command or provide an existing report and continues only from evidence available in chat.

### User Story 2 - Install or update the shared workflow safely (Priority: P1)

A maintainer wants to add the BrownPants Copilot workflow to a project without replacing existing prompts or requiring changes to application code.

**Why this priority**: A GUI workflow only helps teams if it is easy to adopt and safe to share in an existing repository.

**Independent Test**: Run installation against temporary repositories with an absent, identical, and conflicting destination prompt; verify create, no-op, and conflict behavior.

**Acceptance Scenarios**:

1. **Given** a target project with no BrownPants prompt, **When** the installer runs, **Then** it creates the prompt under the shared Copilot prompt directory and reports the destination.
2. **Given** an identical prompt already installed, **When** the installer runs again, **Then** it succeeds without changing the file.
3. **Given** a different file at the destination, **When** the installer runs, **Then** it preserves that file and reports a conflict with a manual review path.

## Edge Cases

- Target repository path does not exist or is a file.
- BrownPants is installed but fails to run; the prompt must request the actual diagnostic rather than infer an inventory.
- The host supports GUI chat but does not expose terminal execution or prompt-file discovery.
- Existing prompt destination uses different line endings or encoding.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST provide a manually invoked, repository-shareable BrownPants context workflow for GitHub Copilot chat in Visual Studio and VS Code.
- **FR-002**: The workflow MUST request bounded, deterministic BrownPants evidence and ask the assistant to validate relevant claims against cited source, project, and test files.
- **FR-003**: The workflow MUST distinguish observed findings, candidate signals, and evidence limits, and MUST NOT claim that inventory alone proves buildability, runtime behavior, or root cause.
- **FR-004**: The workflow MUST explain a user-driven fallback when the command cannot run or the host disallows terminal access, without fabricating BrownPants output.
- **FR-005**: The installer MUST create the workflow in a target repository, leave identical installations unchanged, and preserve/report conflicting user content.
- **FR-006**: Installation MUST be local-only and MUST NOT access the network, install AI clients or SDKs, or modify application source files.
- **FR-007**: Documentation MUST state supported hosts, invocation steps, prerequisites, and the limits of terminal/prompt support under organization policy.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A user can install the shared prompt in a clean temporary project with one documented command and receive an explicit destination report.
- **SC-002**: Clean, identical, and conflicting destination cases produce the specified create, no-change, and preserve outcomes in all installer checks.
- **SC-003**: Every prompt instruction for evidence retrieval, unavailable-tool fallback, source verification, and evidence limits is covered by automated integration checks.
- **SC-004**: The prompt and guide describe both Visual Studio and VS Code usage and make no claim that organization policy permits terminal execution.

## Assumptions

- GitHub Copilot chat is the initial GUI integration because both Visual Studio and VS Code support repository prompt files; other GUI hosts can receive separate adapters later.
- BrownPants' existing CLI remains the evidence engine; this feature does not add an AI SDK, service, or GUI application.
- Users or administrators control whether Copilot can execute terminal commands and whether repository prompt files are enabled.
- Users can manually run BrownPants and paste its output when agent terminal access is unavailable.
