# Feature Specification: CLI Command Help

**Feature Branch**: `028-cli-command-help`

**Created**: 2026-10-14

**Status**: Draft

**Input**: BrownPants self-use exposed that `inspect --help` is rejected as an unknown option and top-level help lists syntax without explaining semantic analysis.

## User Scenarios & Testing

### User Story 1 - Learn commands and evidence options from the CLI (Priority: P1)

A developer or QA user needs to discover a command's purpose, options, and important evidence limits without searching README files or accidentally running analysis.

**Why this priority**: Discoverable help is part of a usable CLI. Users must understand that default invocation inventory is syntax-only and semantic resolution loads projects through MSBuild.

**Independent Test**: Invoke global, command-specific, and nested frame help through `Program.Run`; verify help text, exit codes, and that an invalid path is not inspected when help is requested.

**Acceptance Scenarios**:

1. **AC-001** — **Given** a user requests global help, **When** `--help`, `-h`, or `help` is invoked, **Then** the CLI lists supported commands and returns success.
2. **AC-002** — **Given** a user requests help for any command or frame operation, **When** `--help` or `-h` follows that command, **Then** the CLI prints command-specific usage and option descriptions without running the operation.
3. **AC-003** — **Given** inspect help is displayed, **When** the user reviews invocation analysis options, **Then** help distinguishes syntax-only inventory from `--semantic` target resolution and warns that MSBuild project evaluation may execute imported targets.
4. **AC-004** — **Given** help is requested with a nonexistent repository path, **When** the CLI handles the request, **Then** it succeeds without reading or modifying that path.
5. **AC-005** — **Given** an unknown help topic, **When** the CLI handles it, **Then** it returns a nonzero usage error that identifies the unknown topic and points to global help.

## Edge Cases

- Help appears after an invalid path or otherwise invalid option.
- `frame` has separate `create` and `validate` help.
- Existing no-argument behavior continues to print usage and return a usage error.
- An unknown command or option still returns a nonzero error.

## Requirements

### Functional Requirements

- **FR-001**: The CLI MUST provide global help for all supported commands and aliases.
- **FR-002**: Every top-level command and each frame operation MUST provide command-specific `--help` and `-h` output.
- **FR-003**: Help processing MUST occur before repository scanning, project evaluation, or file writes.
- **FR-004**: Inspect help MUST explain the evidence difference between default syntax inventory and opt-in semantic target resolution, including MSBuild side effects.
- **FR-005**: Unknown help topics MUST return an actionable usage error; normal validation failures MUST remain nonzero.

## Success Criteria

### Measurable Outcomes

- **SC-001**: Tests cover global help, every command help topic, nested frame help, and aliases with successful exit codes.
- **SC-002**: The invalid-path help test succeeds without attempting inspection.
- **SC-003**: Inspect help states syntax-only behavior, semantic target resolution, and possible MSBuild target evaluation.
- **SC-004**: Unknown help topics return nonzero and name the supported global help entry point.

## Assumptions

- Help remains local and deterministic; it does not inspect repository contents or depend on external documentation.
- Existing command behavior and defaults remain unchanged when help is not requested.
