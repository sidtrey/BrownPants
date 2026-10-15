# Feature Specification: Cursor BrownPants Context Command

**Feature Branch**: `037-cursor-context-command`

**Created**: 2026-10-15

**Status**: Implemented

**Input**: FIFO item 6: make BrownPants easier to invoke from GUI-first AI coding hosts. Cursor exposes project-local slash commands as Markdown files; add a safe adapter and document the terminal-restricted fallback.

## User Scenarios & Testing

### User Story 1 - Invoke BrownPants from Cursor Chat (Priority: P1)

A developer using Cursor wants to invoke BrownPants context from the chat UI without switching to an AI-specific command-line client.

**Independent Test**: Install the repository command into a temporary project, inspect its text and path, and verify the documented slash-command name.

**Acceptance Scenarios**:

1. **AC-001** Given a project root, when the Cursor adapter is installed, then it copies a repository-local Markdown command to `.cursor/commands/brownpants-context.md` that users can invoke as `/brownpants-context`.
2. **AC-002** Given the destination already contains identical command text, when installation is repeated, then it reports the command is current and does not rewrite the file.
3. **AC-003** Given a conflicting destination, an invalid project root, or a destination link that resolves outside the project, when installation runs, then it preserves the existing content and refuses unsafe writes with an actionable error.

### User Story 2 - Respect organization terminal restrictions (Priority: P1)

A developer wants the Cursor command to request local BrownPants evidence when host policy allows it and remain useful when Cursor cannot run terminal commands.

**Independent Test**: Inspect the bundled command instructions and verify the approved-shell/report fallback without tool installation or network access.

**Acceptance Scenarios**:

1. **AC-004** Given terminal execution is allowed, when `/brownpants-context` is invoked, then it requests a bounded read-only BrownPants report and asks the agent to verify relevant claims against repository files.
2. **AC-005** Given terminal execution is denied or BrownPants is unavailable, when the command is invoked, then it asks for a report generated in an approved environment or supplied by the user and does not bypass policy or invent findings.
3. **AC-006** Given the adapter is installed, when its workflow is used, then installation and instructions require no AI-specific CLI/SDK, network service, or new package and do not claim that Cursor automatically activates or executes the command.

## Edge Cases

- Destination parent directories do not exist.
- The `.cursor` or `commands` destination is a symlink/junction outside the project.
- A pre-existing command is different or is a directory.
- The project root is missing or is a file.
- Cursor's agent mode is unavailable or terminal access is disabled by organization policy.

## Requirements

### Functional Requirements

- **FR-001**: The adapter MUST install one bundled Markdown command at `.cursor/commands/brownpants-context.md` under an existing selected project root.
- **FR-002**: Installation MUST be idempotent for identical files and MUST preserve conflicting files with an actionable error.
- **FR-003**: Installation MUST contain writes within the selected project and reject symlink/junction/reparse-point traversal.
- **FR-004**: The command MUST request only deterministic read-only BrownPants context and direct users to verify evidence in repository source.
- **FR-005**: The command MUST provide an approved-shell or user-supplied-report fallback when terminal access is unavailable, without claiming to bypass host policy.
- **FR-006**: Documentation MUST state that Cursor custom commands are a host feature, may change, and require local terminal permission for automatic BrownPants execution.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A contributor can install the Cursor command with one documented PowerShell command and invoke it by its documented `/brownpants-context` name.
- **SC-002**: Integration tests prove installation, repeat-install behavior, conflict preservation, invalid-root rejection, and destination path containment.
- **SC-003**: Prompt-contract tests verify the read-only report command, evidence verification guidance, and terminal-denied fallback without install/network instructions.
- **SC-004**: The repository Release build and complete xUnit suite remain green; no runtime dependency is added.

## Assumptions

- Cursor's project command convention is `.cursor/commands/<name>.md`, and a filename `brownpants-context.md` provides `/brownpants-context`.
- Cursor agent/terminal settings and organization policy remain authoritative. The adapter cannot guarantee that the host discovers or executes the command.
- BrownPants itself is a local repository utility, not an AI-specific CLI. Organizations may still require its execution to happen in an approved shell.
