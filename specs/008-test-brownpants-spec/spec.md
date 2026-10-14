# Feature Specification: Test BrownPants with the Spec Kit CLI

**Feature Branch**: `008-test-brownpants-spec`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Test BrownPants Spec Kit extension installation with the real Specify CLI across supported hosts"

## User Scenarios & Testing

### User Story 1 - Trust the Spec Kit extension installation (Priority: P1)

A BrownPants maintainer verifies that the extension can be installed by Spec Kit into a clean project and that its command and lifecycle hooks are registered in the installed project. This validation runs on Windows, Linux, and macOS so path and filesystem differences do not silently break setup.

**Why this priority**: Static checks can confirm that manifest text looks right but cannot prove that Spec Kit accepts and installs the extension.

**Independent Test**: With a supported Spec Kit CLI installed, install the local extension into an isolated temporary project and confirm the installed command, all declared hooks, and project registration are present.

**Acceptance Scenarios**:

1. **Given** an empty project with a `.specify` directory, **When** the local BrownPants extension is installed by Spec Kit, **Then** installation succeeds and the extension appears as enabled.
2. **Given** a successful installation, **When** the installed project registration is inspected, **Then** each declared lifecycle hook targets the BrownPants context command and remains mandatory.
3. **Given** the installed extension, **When** its files are inspected, **Then** the command named in the manifest exists at its declared path.
4. **Given** any supported host OS, **When** the integration test runs, **Then** it uses an isolated temporary project and leaves the repository's own Spec Kit registration unchanged.

### Edge Cases

- Spec Kit CLI is unavailable locally; the integration test reports a clear skip, while CI explicitly installs the supported CLI before running it.
- Installation fails or returns a nonzero exit status.
- Temporary paths contain spaces or host-specific separators.
- The installer registers fewer hooks than the manifest declares.

## Requirements

### Functional Requirements

- **FR-001**: The adapter validation MUST install the local extension through the Spec Kit CLI into an isolated project.
- **FR-002**: The validation MUST confirm the installed extension is enabled and its declared context command exists.
- **FR-003**: The validation MUST confirm all ten current lifecycle hooks are present, mandatory, and target the declared command.
- **FR-004**: The integration check MUST clean up only its own temporary project and MUST NOT modify the repository's active `.specify` files.
- **FR-005**: CI MUST execute the integration check on Windows, Linux, and macOS using a supported Spec Kit CLI release.
- **FR-006**: Local adapter checks MUST report a clear skip when the Spec Kit CLI is not installed.

## Success Criteria

### Measurable Outcomes

- **SC-001**: The real Spec Kit installer succeeds on all three CI host operating systems.
- **SC-002**: All ten manifest hooks are present as mandatory registrations and reference `speckit.brownpants.context`.
- **SC-003**: The installed command file matches the manifest path and is present in the isolated project.
- **SC-004**: After the check, the repository's tracked Spec Kit registration has no modifications from the integration test.

## Assumptions

- The supported Spec Kit CLI release is pinned in CI for repeatable integration behavior.
- CI may install the CLI from its official Python package distribution.
- Installation validation does not execute agent-generated command instructions; it validates Spec Kit's installation and registration artifacts.
