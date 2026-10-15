# Feature Specification: BrownPants GUI Skill for GitHub Copilot

**Feature Branch**: `034-copilot-skill-hosts`

**Created**: 2026-10-14

**Status**: Draft

**Input**: Extend BrownPants' GUI workflow to current GitHub Copilot agent hosts while preserving existing Visual Studio and VS Code prompt support.

## User Scenarios & Testing

### User Story 1 - Use BrownPants from Copilot agent experiences (Priority: P1)

A developer using GitHub Copilot agent mode in Visual Studio Code or a supported Visual Studio release wants the same BrownPants evidence workflow available from the repository, without adding an AI-specific CLI or service.

**Why this priority**: BrownPants already offers a Copilot prompt for Visual Studio 2022 and VS Code's Local harness, but newer Copilot agent experiences use repository skills instead of prompt files. Supporting the shared skill format makes the existing workflow reachable in those GUI hosts.

**Independent Test**: Install the shared skill into a temporary repository's Copilot skill location; verify its contents, safe rerun/conflict behavior, host documentation, and guidance for restricted terminal access.

**Acceptance Scenarios**:

1. **AC-001** Given an existing repository, when the installer is run for Copilot, then it installs the canonical BrownPants skill under `.github/skills/brownpants-context/SKILL.md`, which is discoverable in supported Copilot agent hosts.
2. **AC-002** Given the installer is run for `all`, when all destinations are available, then Codex, Claude Code, and Copilot copies are installed; existing `codex`, `claude`, and `both` selections retain their current destinations and behavior.
3. **AC-003** Given any selected destination conflicts, when a multi-host installation is requested, then the installer reports the conflict before writing to any selected destination and preserves existing files.
4. **AC-004** Given a Copilot host has no terminal capability or cannot access BrownPants, when the skill is used, then it asks the user to run BrownPants in an approved environment or provide a report, and does not fabricate findings or bypass policy.
5. **AC-005** Given host-specific documentation, when users choose a workflow, then it distinguishes `.prompt.md` support in Visual Studio 2022 and VS Code Local harness from `SKILL.md` support in Copilot agent hosts, states that skill discovery does not guarantee activation, and gives version qualifications for Visual Studio.

## Edge Cases

- The repository uses a conflicting `.github/skills/brownpants-context/SKILL.md`.
- A requested multi-host installation has one identical destination and one conflicting destination.
- The project root is missing or resolves through a link outside the selected directory.
- A Copilot host discovers the skill but does not select it for a request.
- A host policy blocks terminal access or the `brownpants` command is unavailable.

## Requirements

### Functional Requirements

- **FR-001**: The installer MUST support a Copilot destination at `.github/skills/brownpants-context/SKILL.md` and a combined `all` selection while preserving existing selections.
- **FR-002**: The installer MUST apply existing containment, idempotency, and conflict-preservation guarantees to the Copilot destination.
- **FR-003**: Multi-host installation MUST preflight every destination before writing any selected skill.
- **FR-004**: The shared skill MUST remain a read-only, evidence-first BrownPants context workflow and MUST NOT invoke an AI-host CLI, install software, or bypass host or organization policy.
- **FR-005**: Documentation MUST describe supported host families and the distinction between prompt files and agent skills, including their invocation/activation limits and terminal fallback.
- **FR-006**: The integration MUST remain local-only and add no package, network dependency, or change to BrownPants scanner behavior.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Copilot-only and all-host installation place byte-identical skill files in every selected destination.
- **SC-002**: Automated checks verify installation, idempotency, conflict preservation, and all-destination preflight for Copilot and all-host selections.
- **SC-003**: The integration guide accurately names supported prompt and skill locations, host qualifications, and policy-respecting fallback behavior.
- **SC-004**: Existing Codex and Claude integration checks continue to pass without network access.

## Assumptions

- Current VS Code Copilot Agent mode and GitHub Copilot agent experiences consume workspace `SKILL.md` files; supported workspace paths and host behavior are documented by their vendors and may evolve.
- Visual Studio 2022 continues to use repository prompt files; current Visual Studio 2026 skill support is version-qualified in documentation.
- Skill discovery exposes a workflow to a host but does not guarantee that the model selects it for every relevant request.
- Users or administrators control terminal availability; BrownPants does not work around restrictions.
