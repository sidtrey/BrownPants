# Tasks: CLI Command Help

**Input**: Design documents from `/specs/028-cli-command-help/`

**Prerequisites**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/cli-help.md`

**Tests**: Required by acceptance criteria and project constitution.

## Phase 1: User Story 1 - Learn commands and evidence options from the CLI (Priority: P1)

**Goal**: Users can discover command syntax and evidence limits without accidentally running an operation.

**Independent Test**: Exercise global and command-specific help through `Program.Run`, including an invalid path and all frame operations.

### Tests for User Story 1

- [x] T001 Add captured-output tests for global help, command help, aliases, no-scan behavior, and unknown topics in `tests/BrownPants.Tests/RepositoryScannerTests.cs`

### Implementation for User Story 1

- [x] T002 Add early help routing and static per-command help renderers in `src/BrownPants.Cli/Program.cs`
- [x] T003 Run Release build, full xUnit suite, acceptance validation, and local CLI help checks; record results in `specs/028-cli-command-help/verification.md`

## Dependencies & Execution Order

- T001 precedes T002. T003 is the final verification gate.

## Implementation Strategy

First capture expected help behavior through the current dispatcher, then add early deterministic routing without touching command execution paths. Verify help for every command and run the full project gates.
