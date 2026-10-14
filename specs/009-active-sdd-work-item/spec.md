# Feature Specification: Report the Active SDD Work Item

**Feature Branch**: `009-active-sdd-work-item`

**Created**: 2026-10-14

**Status**: Draft

**Input**: User goal: improve BrownPants as a broad, Spec Kit compatible repository context tool.

## User Scenarios & Testing

### User Story 1 - Find the active work item (Priority: P1)

An engineer using an SDD workflow wants a repository report to identify the selected feature or change so they can begin work in the correct context without scanning every artifact directory.

**Why this priority**: BrownPants currently reports artifact samples and counts, but those samples are sorted and bounded. They do not identify which feature or change is active.

**Independent Test**: Scan a temporary repository with an active Spec Kit pointer or a single OpenSpec change and verify that the report identifies its repository relative directory in Markdown and JSON.

**Acceptance Scenarios**:

1. **Given** a valid Spec Kit feature pointer whose target contains scanned artifacts, **When** BrownPants inspects the repository, **Then** both report formats identify that feature directory as the active work item.
2. **Given** exactly one non-archived OpenSpec change directory, **When** BrownPants inspects the repository, **Then** both report formats identify that change directory as the active work item.
3. **Given** no selected feature or more than one active OpenSpec change, **When** BrownPants inspects the repository, **Then** it leaves the active work item unset and retains the existing counts and samples.

### User Story 2 - Trust selection evidence (Priority: P2)

An engineer wants malformed, unsafe, or stale selection metadata to be handled safely and visibly, so a report does not present an unverified path as active.

**Why this priority**: Incorrect active context can lead an agent to modify the wrong feature; selection must be bounded by evidence already inside the scanned repository.

**Independent Test**: Scan fixtures with malformed metadata, traversal paths, and targets without artifacts; verify no active path is reported and diagnostics are deterministic where applicable.

**Acceptance Scenarios**:

1. **Given** malformed or unsafe Spec Kit selection metadata, **When** BrownPants inspects the repository, **Then** no active path is reported and a stable configuration diagnostic explains why.
2. **Given** selection metadata naming a directory with no scanned artifacts, **When** BrownPants inspects the repository, **Then** it does not report that directory as active.

## Edge Cases

- A feature pointer uses Windows separators while inspection runs on another operating system.
- Feature metadata is unreadable, oversized, invalid UTF-8, malformed JSON, a reparse point, or has an unexpected value type.
- The selected directory is not represented by any scanned file.
- OpenSpec has zero, one, or multiple non-archived change directories, or only archived changes.
- Case variants exist for the feature metadata file.

## Requirements

### Functional Requirements

- **FR-001**: Reports MUST expose one optional repository relative active work item path per SDD workflow, without changing existing artifact counts or samples.
- **FR-002**: Spec Kit selection MUST use `.specify/feature.json` and accept its `feature_directory` only when it is a safe relative path represented by scanned files.
- **FR-003**: OpenSpec MUST select a change directory only when exactly one non-archived change directory is represented by scanned files.
- **FR-004**: Invalid, ambiguous, stale, or unsafe metadata MUST NOT produce an active work item path; invalid Spec Kit metadata MUST produce a deterministic diagnostic.
- **FR-005**: The report MUST identify the evidence type used to select an active work item, while leaving custom SDD workflow selections unset until an explicit safe selection contract exists.
- **FR-006**: Markdown and JSON reports MUST expose the same selection and diagnostics, and JSON schema version 1 MUST remain backward compatible through additive nullable fields.
- **FR-007**: Default inspection MUST remain deterministic and MUST NOT read outside the repository, evaluate projects, invoke external tools, or modify the inspected repository.

## Key Entities

- **Active work item**: A selected feature or change directory, represented by a repository relative path and a selection evidence type.
- **Selection diagnostic**: A stable code and message describing unusable Spec Kit selection metadata.

## Success Criteria

### Measurable Outcomes

- **SC-001**: For valid unambiguous Spec Kit and OpenSpec fixtures, Markdown and JSON identify the same active directory in every test run.
- **SC-002**: For invalid, ambiguous, or stale selection inputs, zero unsafe or unsupported paths are reported as active.
- **SC-003**: Existing SDD counts, samples, custom workflow behavior, and report ordering remain unchanged for repositories without selection evidence.
- **SC-004**: Repeated scans of identical fixtures produce byte-identical Markdown and JSON output.

## Assumptions

- Spec Kit's `feature_directory` is a local selection pointer and may use platform-specific directory separators.
- A unique non-archived OpenSpec change is enough evidence to expose it as the active change; multiple changes remain ambiguous.
- Custom SDD engines continue to be inventoried without selection until their declarative configuration supports a safe active-item field.
