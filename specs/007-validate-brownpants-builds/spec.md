# Feature Specification: Validate BrownPants on Windows, Linux, and macOS

**Feature Branch**: `007-validate-brownpants-builds`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User description: "Validate BrownPants builds, tests, adapters, and packed CLI on Windows Linux and macOS"

## User Scenarios & Testing

### User Story 1 - Trust BrownPants on the host platform (Priority: P1)

A developer can use BrownPants from a supported .NET 10 host on Windows, Linux, or macOS and have confidence that the CLI and its repository analysis work there. Windows remains covered for teams inspecting MFC and other Windows-specific repositories; the host platform does not restrict the repository content BrownPants can inspect.

**Why this priority**: Supporting repositories across ecosystems requires contributors and users on common development platforms to receive the same baseline quality, while Windows remains essential for the project's managed/native interop focus.

**Independent Test**: Run the repository validation workflow on each supported host and confirm restore, Release build, xUnit tests, adapter checks, tool packaging, and an installed-tool inspection succeed.

**Acceptance Scenarios**:

1. **Given** a supported Windows host, **When** the validation workflow runs, **Then** build, test, adapter, package, and installed CLI checks pass.
2. **Given** a supported Linux or macOS host, **When** the same validation workflow runs, **Then** build, test, adapter, package, and installed CLI checks pass using that platform's installed tool command.
3. **Given** a repository fixture containing Windows-specific project files, **When** it is inspected from any supported host, **Then** BrownPants reports those files without requiring the host to build them.

### User Story 2 - Diagnose host-specific failures (Priority: P2)

A contributor can identify which host failed and review its command output without losing successful results from other hosts.

**Why this priority**: Independent host results make portability regressions actionable and prevent one platform's success from masking another's failure.

**Independent Test**: Review workflow results and verify each host reports its own validation outcome and failing command.

**Acceptance Scenarios**:

1. **Given** a validation failure on one host, **When** the workflow completes, **Then** that host is visibly failed and outcomes for other hosts remain independently available.

### Edge Cases

- A platform uses a tool shim without a `.exe` extension.
- Adapter scripts run under PowerShell Core on non-Windows hosts.
- Windows-specific fixture paths and project metadata remain testable on non-Windows hosts without being evaluated or built.
- A packed tool installation succeeds but its command cannot be located or invoked.

## Requirements

### Functional Requirements

- **FR-001**: The project validation process MUST run on Windows, Linux, and macOS using the supported .NET 10 SDK.
- **FR-002**: Each host MUST independently restore, build, and run the complete xUnit suite.
- **FR-003**: Each host MUST run the Spec Kit and OpenSpec adapter checks.
- **FR-004**: Each host MUST pack and install the CLI tool, invoke its platform-appropriate command, and verify a JSON inspection returns a non-empty inventory with the supported report schema version.
- **FR-005**: Host-specific failures MUST be visible independently in the validation results.
- **FR-006**: Inspection of Windows-specific repository artifacts MUST remain possible on non-Windows hosts without requiring project evaluation or compilation.

## Success Criteria

### Measurable Outcomes

- **SC-001**: All validation stages pass on Windows, Linux, and macOS in one complete workflow run.
- **SC-002**: Each host's installed CLI returns valid JSON with `SchemaVersion` 1 and at least one file for the repository fixture.
- **SC-003**: The same Windows-specific project fixture produces inventory findings on all three hosts.
- **SC-004**: A failure on one host is reported without suppressing results from the other hosts.

## Assumptions

- The supported host set is Windows, Linux, and macOS with .NET 10.
- PowerShell Core is available to run existing adapter validation scripts on each host.
- Windows-specific project metadata is inspected as data; building MFC projects is outside this host portability requirement.
- The validation workflow may use host-specific tool executable names while preserving the same checks.
